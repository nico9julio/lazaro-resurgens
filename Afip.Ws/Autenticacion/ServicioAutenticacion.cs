using System;
using System.Collections.Generic;
using System.Xml;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Afip.Ws.Autenticacion
{
        /// <summary>
        /// Cliente del servicio web de Autenticación y Autorización (WSAA).
        /// </summary>
        public class ServicioAutenticacion : ServicioAfip
        {
                // Entero de 32 bits sin signo que identifica el requerimiento
                // No se usa UInt32 porque no es compatible con CLS
                public static long UniqueId = 1;

                /// <summary>
                /// URLs oficiales de AFIP para WSAA (Servicio de Autenticación y Autorización).
                /// </summary>
                public const string UrlWsaaProduccion = "https://wsaa.afip.gov.ar/ws/services/LoginCms";
                public const string UrlWsaaHomologacion = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";

                /// <summary>
                /// Flag global para activar el entorno de homologación en WSAA.
                /// </summary>
                public static bool ModoHomologacion = false;

                /// <summary>
                /// Flag a nivel de instancia para alternar entre Producción y Homologación en WSAA.
                /// </summary>
                public bool Homologacion { get; set; }

                // Identificacion del WSN para el cual se solicita el TA
                public string Servicio = "wsfe";

                // Identificacion del WSN para el cual se solicita el TA (si es null se resuelve automáticamente)
                public string UrlWsaa = null;

                // Ruta del certificado X509 (con clave privada) usado para firmar, en formato PKCS 12 (.p12)
                public string RutaCertificado = @"Certificado.p12";

                /// <summary>
                /// Determina si esta instancia debe operar contra el entorno de homologación.
                /// </summary>
                public bool EsHomologacionActivo()
                {
                        return this.Homologacion 
                                || ModoHomologacion 
                                || Environment.GetEnvironmentVariable("LAZARO_AFIP_HOMOLOGACION") == "1"
                                || Environment.GetEnvironmentVariable("AFIP_HOMO") == "1";
                }

                /// <summary>
                /// Resuelve la URL del WSAA según la configuración y el entorno.
                /// </summary>
                public string ObtenerUrlWsaa()
                {
                        if (!string.IsNullOrWhiteSpace(this.UrlWsaa)) {
                                return this.UrlWsaa;
                        }
                        return this.EsHomologacionActivo() ? UrlWsaaHomologacion : UrlWsaaProduccion;
                }

                /// <summary>
                /// Autenticar
                /// </summary>
                /// <returns></returns>
                public TicketAcceso Autenticar()
                {
                        // Paso 1: Generar el Login Ticket Request
                        var Tra = this.ObtenerTra();

                        // Paso 2: Firmar el Login Ticket Request
                        var CertificadoFirmante = new X509Certificate2(this.RutaCertificado);

                        // Crear un CMS firmado
                        var CmsFirmado = Criptografia.CrearCmsFirmado(Tra.OuterXml, CertificadoFirmante);

                        // Lo convierto a base 64
                        var CmsFirmadoBase64 = Convert.ToBase64String(CmsFirmado.Encode());

                        // Paso 3: Invocar al WSAA para obtener el Login Ticket Response
                        string urlWsaa = this.ObtenerUrlWsaa();
                        using (var ServicioWsaa = string.IsNullOrWhiteSpace(urlWsaa) ? new AfipAutenticacion.LoginCMSClient() : new AfipAutenticacion.LoginCMSClient("LoginCms", urlWsaa)) {

                                var LoginTicketResponse = ServicioWsaa.loginCms(CmsFirmadoBase64);

                                // Paso 4: Analizar el Login Ticket Response recibido del WSAA
                                var XmlLoginTicketResponse = new XmlDocument();
                                XmlLoginTicketResponse.LoadXml(LoginTicketResponse);

                                UniqueId = long.Parse(XmlLoginTicketResponse.SelectSingleNode("//uniqueId").InnerText);

                                var Res = new TicketAcceso
                                {
                                        GenerationTime = DateTime.Parse(XmlLoginTicketResponse.SelectSingleNode("//generationTime").InnerText),
                                        ExpirationTime = DateTime.Parse(XmlLoginTicketResponse.SelectSingleNode("//expirationTime").InnerText),
                                        Sign = XmlLoginTicketResponse.SelectSingleNode("//sign").InnerText,
                                        Token = XmlLoginTicketResponse.SelectSingleNode("//token").InnerText
                                };

                                return Res;
                        }
                }

                /// <summary>
                /// Genera un TRA (Ticket de Requerimiento de Acceso).
                /// </summary>
                /// <returns>Un documento XML conteniendo el TRA.</returns>
                protected XmlDocument ObtenerTra()
                {
                        const string XmlStrLoginTicketRequestTemplate = "<loginTicketRequest><header><uniqueId></uniqueId><generationTime></generationTime><expirationTime></expirationTime></header><service></service></loginTicketRequest>";

                        XmlNode XmlNodoUniqueId;
                        XmlNode XmlNodoGenerationTime;
                        XmlNode XmlNodoExpirationTime;
                        XmlNode XmlNodoService;

                        var XmlLoginTicketRequest = new XmlDocument();
                        XmlLoginTicketRequest.LoadXml(XmlStrLoginTicketRequestTemplate);

                        XmlNodoUniqueId = XmlLoginTicketRequest.SelectSingleNode("//uniqueId");
                        XmlNodoGenerationTime = XmlLoginTicketRequest.SelectSingleNode("//generationTime");
                        XmlNodoExpirationTime = XmlLoginTicketRequest.SelectSingleNode("//expirationTime");
                        XmlNodoService = XmlLoginTicketRequest.SelectSingleNode("//service");

                        XmlNodoGenerationTime.InnerText = DateTime.Now.AddMinutes(-10).ToString("s");
                        XmlNodoExpirationTime.InnerText = DateTime.Now.AddMinutes(+10).ToString("s");
                        XmlNodoUniqueId.InnerText = Convert.ToString(UniqueId++);
                        XmlNodoService.InnerText = this.Servicio;

                        return XmlLoginTicketRequest;
                }
        }
}
