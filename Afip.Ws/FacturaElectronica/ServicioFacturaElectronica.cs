using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Runtime.Serialization;
using Afip.Ws.AfipAutenticacion;
using Afip.Ws.AfipFe;

namespace Afip.Ws.FacturaElectronica
{
        /// <summary>
        /// Cliente del servicio web de facturación electrónica WSFEv1.
        /// </summary>
        public class ServicioFacturaElectronica : ServicioAfip
        {
                /// <summary>
                /// El CUIT del cliente del servicio web.
                /// </summary>
                public string Cuit { get; set; }

                /// <summary>
                /// El ticket de acceso al servicio web.
                /// </summary>
                public Autenticacion.TicketAcceso TicketAcceso { get; set; }

                /// <summary>
                /// Flag global para activar el modo debug / logging de payloads XML en todas las instancias.
                /// </summary>
                public static bool ModoDebug { get; set; }

                /// <summary>
                /// Flag por instancia para activar el modo debug / logging de payloads XML.
                /// </summary>
                public bool Debug { get; set; }

                /// <summary>
                /// URL oficial de WSFEv1 en producción.
                /// </summary>
                public const string UrlWsfeProduccion = "https://servicios1.afip.gov.ar/wsfev1/service.asmx";

                /// <summary>
                /// URL oficial de WSFEv1 en homologación (testing).
                /// </summary>
                public const string UrlWsfeHomologacion = "https://wswhomo.afip.gov.ar/wsfev1/service.asmx";

                /// <summary>
                /// Flag global para activar el modo homologación en todas las instancias.
                /// </summary>
                public static bool ModoHomologacion { get; set; }

                /// <summary>
                /// Flag por instancia para activar el modo homologación.
                /// </summary>
                public bool Homologacion { get; set; }

                /// <summary>
                /// URL personalizada del servicio WSFEv1. Si no se especifica, se determina automáticamente.
                /// </summary>
                public string Url { get; set; }

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
                /// Crea una instancia del cliente WCF ServiceSoapClient configurada según el entorno (Producción u Homologación).
                /// </summary>
                public ServiceSoapClient CrearClienteSoap()
                {
                        string url = this.Url;
                        if (string.IsNullOrWhiteSpace(url)) {
                                if (this.EsHomologacionActivo()) {
                                        url = UrlWsfeHomologacion;
                                }
                        }

                        if (string.IsNullOrWhiteSpace(url)) {
                                return new ServiceSoapClient();
                        } else {
                                return new ServiceSoapClient("ServiceSoap", url);
                        }
                }

                /// <summary>
                /// Determina si el modo debug está activo para esta instancia (vía propiedad o variable de entorno LAZARO_AFIP_DEBUG / AFIP_DEBUG).
                /// </summary>
                public bool EsModoDebugActivo()
                {
                        return this.Debug 
                                || ModoDebug 
                                || Environment.GetEnvironmentVariable("LAZARO_AFIP_DEBUG") == "1"
                                || Environment.GetEnvironmentVariable("AFIP_DEBUG") == "1";
                }

                public ServicioFacturaElectronica()
                { }

                public ServicioFacturaElectronica(string cuit, Autenticacion.TicketAcceso ta)
                        : this()
                {
                        this.Cuit = cuit;
                        this.TicketAcceso = ta;
                }

                /// <summary>
                /// Consulta el estado de los servicios web de AFIP.
                /// </summary>
                /// <param name="homologacion">True para consultar el servidor de homologación</param>
                /// <returns>True si los servicios están funcionando</returns>
                public static bool ProbarEstadoServicios(bool homologacion = false)
                {
                        string url = (homologacion || ModoHomologacion) ? UrlWsfeHomologacion : null;
                        using (var Clie = string.IsNullOrWhiteSpace(url) ? new ServiceSoapClient() : new ServiceSoapClient("ServiceSoap", url)) {
                                var Estado = Clie.FEDummy();

                                return Estado.AppServer == "OK" && Estado.AuthServer == "OK"; // && Estado.DbServer == "OK";
                        }
                }

                /// <summary>
                /// Devuelve True si tiene un ticket de acceso vigente.
                /// </summary>
                public bool TieneTicketDeAccesoValido()
                {
                        return this.TicketAcceso != null && this.TicketAcceso.EsValido();
                }

                /// <summary>
                /// Obtiene una lista de los tipos de comprobante admitidos por el servicio web.
                /// </summary>
                public CbteTipo[] ObtenerTiposDeComprobante()
                {
                        using (var Clie = this.CrearClienteSoap()) {
                                var Res = Clie.FEParamGetTiposCbte(this.CrearFEAuthRequest());

                                return Res.ResultGet;
                        }
                }

                /// <summary>
                /// Obtiene una lista de los conceptos admitidos por el servicio web.
                /// </summary>
                public ConceptoTipo[] ObtenerConceptos()
                {
                        using (var Clie = this.CrearClienteSoap()) {
                                var Res = Clie.FEParamGetTiposConcepto(this.CrearFEAuthRequest());

                                return Res.ResultGet;
                        }
                }

                /// <summary>
                /// Obtiene los datos del último comprobante autorizado para un PV.
                /// </summary>
                /// <param name="pv">El punto de venta a consultar.</param>
                /// <param name="tipoComprob">El tipo de comprobante a consultar.</param>
                public FERecuperaLastCbteResponse UltimoComprobante(int pv, Tablas.ComprobantesTipos tipoComprob)
                {
                        using (var Clie = this.CrearClienteSoap()) {
                                var Res = Clie.FECompUltimoAutorizado(this.CrearFEAuthRequest(), pv, (int)tipoComprob);

                                return Res;
                        }
                }

                /// <summary>
                /// Solicitar un CAE para uno o más comprobantes.
                /// </summary>
                /// <param name="solCae">Los datos para la solicitud del CAE.</param>
                /// <returns>La cantidad de comprobantes aprobados, o 0 si todos fueron rechazados.</returns>
                public int SolictarCae(SolicitudCae solCae)
                {
                        using (var Clie = this.CrearClienteSoap()) {

                                var DetallesComprobantes = new FECAEDetRequest[solCae.Comprobantes.Count];

                                var i = 0;
                                foreach (ComprobanteSolicitud Comprob in solCae.Comprobantes) {
                                        var DetalleComprobante = new FECAEDetRequest
                                        {
                                                Concepto = (int)Comprob.Conceptos,
                                                DocTipo = (int)Comprob.Cliente.DocumentoTipo,
                                                DocNro = Comprob.Cliente.DocumentoNumero,
                                                CbteDesde = Comprob.Numero,
                                                CbteHasta = Comprob.Numero,
                                                CbteFch = DateTime.Now.ToString("yyyyMMdd"),
                                                ImpTotal = Math.Round(decimal.ToDouble(Comprob.ImporteTotal()), 2),
                                                ImpTotConc = Math.Round(decimal.ToDouble(Comprob.ImporteNetoNoGravado), 2),
                                                ImpNeto = Math.Round(decimal.ToDouble(Comprob.ImporteNetoGravado), 2),
                                                ImpOpEx = Math.Round(decimal.ToDouble(Comprob.ImporteExento), 2),
                                                ImpIVA = Math.Round(decimal.ToDouble(Comprob.ImporteIva()), 2),
                                                //ImpTrib = Comprob.TotalTributos(),
                                                MonId = "PES",
                                                MonCotiz = 1,
                                                CondicionIVAReceptorId = Comprob.Cliente != null && Comprob.Cliente.CondicionIvaReceptorId > 0
                                                        ? Comprob.Cliente.CondicionIvaReceptorId
                                                        : 5,
                                        };

                                        // Agregar comprobantes asociados
                                        if(Comprob.ComprobantesAsociados != null && Comprob.ComprobantesAsociados.Count > 0)
                                        {
                                                var CbtesAsocList = new List<CbteAsoc>();
                                                foreach (var ComprobAsoc in Comprob.ComprobantesAsociados) 
                                                {
                                                        CbtesAsocList.Add(new CbteAsoc()
                                                        {
                                                                Tipo = (int)ComprobAsoc.Tipo,
                                                                PtoVta = ComprobAsoc.PuntoDeVenta,
                                                                Nro = ComprobAsoc.Numero,
                                                                Cuit = ComprobAsoc.Cuit,
                                                                CbteFch = ComprobAsoc.Fecha.ToString("yyyyMMdd"),
                                                        });
                                                }

                                                DetalleComprobante.CbtesAsoc = CbtesAsocList.ToArray();
                                        }

                                        // Si es un comprobante con servicios, agregar los campos obligatorios
                                        if ((Comprob.Conceptos | Tablas.Conceptos.Servicios) == Tablas.Conceptos.Servicios) {
                                                DetalleComprobante.FchServDesde = Comprob.ServicioFechaDesde.ToString("yyyyMMdd");
                                                DetalleComprobante.FchServHasta = Comprob.ServicioFechaHasta.ToString("yyyyMMdd");
                                                DetalleComprobante.FchVtoPago = Comprob.FechaVencimientoPago.ToString("yyyyMMdd");
                                        }

                                        // Agregar la tabla de alícuotas
                                        if (Comprob.ImportesAlicuotas != null && Comprob.ImportesAlicuotas.Count > 0) {
                                                DetalleComprobante.Iva = new AlicIva[Comprob.ImportesAlicuotas.Count];
                                                var j = 0;
                                                foreach (ImporteAlicuota Alic in Comprob.ImportesAlicuotas) {
                                                        DetalleComprobante.Iva[j++] = new AlicIva
                                                        {
                                                                Id = (int)Alic.Alicuota,
                                                                BaseImp = Math.Round(decimal.ToDouble(Alic.BaseImponible), 2),
                                                                Importe = Math.Round(decimal.ToDouble(Alic.Importe), 2)
                                                        };
                                                }
                                        }

                                        DetallesComprobantes[i++] = DetalleComprobante;
                                }


                                // Crear la solicitud
                                var CaeReq = new FECAERequest
                                {
                                        FeCabReq = new FECAECabRequest
                                        {
                                                CantReg = solCae.Comprobantes.Count,
                                                PtoVta = solCae.PuntoDeVenta,
                                                CbteTipo = (int)solCae.TipoComprobante,
                                        },
                                        FeDetReq = DetallesComprobantes
                                };

                                // Llamar al WS para hacer la solicitud
                                var AuthReq = this.CrearFEAuthRequest();

                                // Si el modo debug está activo, volcar payload saliente
                                if (this.EsModoDebugActivo()) {
                                        VolcarXmlDebug("FECAESolicitar_Request", AuthReq, CaeReq);
                                }

                                var Res = Clie.FECAESolicitar(AuthReq, CaeReq);

                                // Si el modo debug está activo, volcar respuesta entrante
                                if (this.EsModoDebugActivo()) {
                                        VolcarXmlDebug("FECAESolicitar_Response", Res);
                                }

                                var Aprobados = 0;

                                solCae.Observaciones = new List<Observacion>();
                                /* if (Res.FeCabResp.Resultado == "R") {
                                        // Todo el lote rechazado
                                        foreach (var Er in Res.Errors) {
                                                solCae.Observaciones.Add(new Observacion(Er.Code, Er.Msg));
                                        }
                                } else { */
                                // Aprobado total o parcial
                                var ci = 0;
                                foreach (var De in Res.FeDetResp) {
                                        var Comprob = solCae.Comprobantes[ci++];
                                        Comprob.Cae = new Cae();
                                        if (De.Resultado == "A") {
                                                // Aprobado
                                                Comprob.Cae.CodigoCae = De.CAE;
                                                Comprob.Cae.Vencimiento = DateTime.ParseExact(De.CAEFchVto, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
                                                Comprob.Numero = System.Convert.ToInt32(De.CbteDesde);
                                                Aprobados++;
                                        }

                                        if (De.Observaciones != null && De.Observaciones.Count<Obs>() > 0) {
                                                Comprob.Obs = new Observacion(De.Observaciones[0].Code, De.Observaciones[0].Msg, Comprob);
                                                foreach (var Er in De.Observaciones) {
                                                        solCae.Observaciones.Add(new Observacion(Er.Code, Er.Msg, Comprob));
                                                }
                                        }
                                }
                                /* } */

                                return Aprobados;
                        }
                }


                private FEAuthRequest CrearFEAuthRequest()
                {
                        var Res = new FEAuthRequest
                        {
                                Cuit = long.Parse(this.Cuit.Replace("-", "").Replace(" ", "").Replace(".", "")),
                                Sign = this.TicketAcceso.Sign,
                                Token = this.TicketAcceso.Token
                        };

                        return Res;
                }

                /// <summary>
                /// Calcula el dígito verificador del código de identificación que se imprime en código
                /// de barras en los comprobantes de AFIP.
                /// http://www.afip.gov.ar/afip/resol170204.html
                /// </summary>
                /// <param name="codigo">El código de identificación, sin el digito verificador.</param>
                /// <returns>El digito verificador.</returns>
                public static int DigitoVerificadorIdentificacionComprobante(string codigo)
                {
                        var Sumatoria = 0;
                        // Sumar todos los dígitos en posiciones impares
                        for (int i = 0; i <= codigo.Length - 1; i += 2)
                        {
                                Sumatoria += int.Parse(codigo.Substring(i, 1));
                        }

                        // Multiplicar por 3
                        Sumatoria *= 3;

                        // Sumar todos los dígitos en posiciones pares
                        for (int i = 1; i <= codigo.Length - 1; i += 2) {
                                Sumatoria += int.Parse(codigo.Substring(i, 1));
                        }

                        // Buscar la diferencia con el próximo múltiplo de 10
                        // Por ejemplo, si acá tengo un 88, el resultado es 2, si tengo 54 el resultado es 6, etc.
                        var ProximoMultiploDe10 = decimal.ToInt32(Math.Ceiling(Sumatoria / 10m) * 10);
                        var Resultado = ProximoMultiploDe10 - Sumatoria;
                        // También podría ser 10 - (N módulo de 10) si N > 0

                        return Resultado;
                }

                /// <summary>
                /// Vuelca objetos serializados a XML en consola y en un archivo de texto en la carpeta temporal (%TEMP%),
                /// permitiendo inspeccionar los payloads XML de envío y respuesta de AFIP cuando el modo debug está activo.
                /// </summary>
                /// <param name="etiqueta">Identificador para el volcado (ej: FECAESolicitar_Request).</param>
                /// <param name="objetos">Los objetos DataContract a serializar en el volcado.</param>
                public static void VolcarXmlDebug(string etiqueta, params object[] objetos)
                {
                        try {
                                var sb = new StringBuilder();
                                sb.AppendLine("<!-- ===================================================================== -->");
                                sb.AppendLine(string.Format("<!-- AFIP DEBUG DUMP: {0} - {1:yyyy-MM-dd HH:mm:ss} -->", etiqueta, DateTime.Now));
                                sb.AppendLine("<!-- ===================================================================== -->");

                                foreach (var obj in objetos) {
                                        if (obj == null) continue;
                                        var serializer = new DataContractSerializer(obj.GetType());
                                        var settings = new XmlWriterSettings
                                        {
                                                Indent = true,
                                                Encoding = Encoding.UTF8,
                                                OmitXmlDeclaration = true
                                        };
                                        using (var sw = new System.IO.StringWriter()) {
                                                using (var writer = XmlWriter.Create(sw, settings)) {
                                                        serializer.WriteObject(writer, obj);
                                                }
                                                sb.AppendLine(sw.ToString());
                                        }
                                }

                                string contenido = sb.ToString();

                                // Emitir por consola estándar y Debug
                                Console.WriteLine(contenido);
                                System.Diagnostics.Debug.WriteLine(contenido);

                                // Guardar en archivo de texto en directorio temporal (%TEMP%)
                                string nombreArchivo = string.Format("afip_{0}.xml", etiqueta.ToLowerInvariant());
                                string rutaArchivo = System.IO.Path.Combine(System.IO.Path.GetTempPath(), nombreArchivo);
                                System.IO.File.WriteAllText(rutaArchivo, contenido, Encoding.UTF8);
                                Console.WriteLine(string.Format("[AFIP DEBUG] Payload guardado en: {0}", rutaArchivo));
                        } catch (Exception ex) {
                                Console.WriteLine(string.Format("[AFIP DEBUG ERROR] Error al volcar XML: {0}", ex.Message));
                        }
                }
        }
}
