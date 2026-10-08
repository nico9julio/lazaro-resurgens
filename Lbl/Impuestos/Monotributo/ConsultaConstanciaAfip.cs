using System;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using Afip.Ws.Autenticacion;

namespace Lbl.Impuestos.Monotributo
{
	/// <summary>
	/// Resultado de la consulta de constancia de inscripción ante ARCA / AFIP.
	/// </summary>
	[Serializable]
	public class ResultadoConsultaConstancia
	{
		public bool Exito { get; set; }
		public string Categoria { get; set; }
		public string CuitConsultado { get; set; }
		public string RazonSocial { get; set; }
		public string Apellido { get; set; }
		public string Nombre { get; set; }
		public string TipoPersona { get; set; } // "FISICA", "JURIDICA"
		public string NumeroDocumento { get; set; } // DNI
		public string Domicilio { get; set; }
		public string Localidad { get; set; }
		public string CodigoPostal { get; set; }
		public string Provincia { get; set; }
		public int IdProvincia { get; set; }
		public string EstadoClave { get; set; }
		public int IdSituacionTributaria { get; set; } // 2: RI, 4: Monotributo, 5: Exento
		public string DescripcionSituacion { get; set; }
		public string Mensaje { get; set; }
		public bool RequierePermisoWebservice { get; set; }
		public string ErrorTecnico { get; set; }

		/// <summary>
		/// Devuelve el instructivo paso a paso para delegar la relación del servicio en ARCA / AFIP.
		/// </summary>
		public static string ObtenerGuiaConfiguracionPermiso()
		{
			return "Para habilitar la consulta automática en ARCA / AFIP con el mismo certificado de facturación ya configurado:\r\n\r\n" +
			       "1. Ingresa a la web de ARCA / AFIP (afip.gob.ar) con Clave Fiscal.\r\n" +
			       "2. Entra al servicio «Administrador de Relaciones de Clave Fiscal».\r\n" +
			       "3. Haz clic en «Nueva Relación» y luego en «Buscar».\r\n" +
			       "4. Despliega el menú «ARCA» -> «WebServices».\r\n" +
			       "5. Busca y selecciona «Consulta de constancia de inscripción» -> «Buscar».\r\n" +
			       "6. En el campo 'Representante', selecciona el certificado de facturación existente (computador fiscal/alias).\r\n" +
			       "7. Haz clic en «Confirmar».\r\n\r\n" +
			       "(El permiso suele activarse de inmediato o en pocos minutos. Luego podrás realizar la consulta sin errores).";
		}
	}

	/// <summary>
	/// Representa los datos completos de un contribuyente obtenidos de ARCA / AFIP.
	/// </summary>
	[Serializable]
	public class DatosContribuyenteAfip : ResultadoConsultaConstancia
	{
	}

	/// <summary>
	/// Servicio para consultar la categoría de Monotributo actual de la empresa
	/// a través del Web Service oficial de AFIP / ARCA (ws_sr_constancia_inscripcion - personaServiceA5).
	/// </summary>
	public static class ConsultaConstanciaAfip
	{
		public const string WsConstanciaProduccion = "https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA5";
		public const string WsConstanciaHomologacion = "https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA5";
		public const string ServicioWsaa = "ws_sr_constancia_inscripcion";

		/// <summary>
		/// Obtiene el CUIT numérico de la empresa activa.
		/// </summary>
		public static string ObtenerCuitEmpresa()
		{
			string cuit = "";
			try
			{
				if (Lbl.Sys.Config.Empresa.ClaveTributaria != null)
				{
					cuit = Lbl.Sys.Config.Empresa.ClaveTributaria.ToString();
				}
			}
			catch { }
			if (string.IsNullOrWhiteSpace(cuit) && Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null)
			{
				cuit = Lfx.Workspace.Master.CurrentConfig.ReadGlobalSetting<string>("Sistema.Empresa.CUIT", "");
			}
			if (!string.IsNullOrWhiteSpace(cuit))
			{
				cuit = cuit.Replace("-", "").Replace(".", "").Replace(" ", "").Trim();
			}
			return cuit;
		}

		/// <summary>
		/// Localiza el certificado digital (.p12) de la empresa.
		/// </summary>
		public static string ResolverRutaCertificado(bool esHomo)
		{
			try
			{
				string carpetaAfip = Path.Combine(Lbl.Sys.Config.CarpetaEmpresa, "AFIP");
				if (esHomo)
				{
					string certHomo = Path.Combine(carpetaAfip, "Certificado_homo.p12");
					if (File.Exists(certHomo)) return certHomo;
				}
				else
				{
					string certProd = Path.Combine(carpetaAfip, "Certificado_prod.p12");
					if (File.Exists(certProd)) return certProd;
				}

				string certGen = Path.Combine(carpetaAfip, "Certificado.p12");
				if (File.Exists(certGen)) return certGen;

				// Buscar en la carpeta raíz de la empresa
				string certEmpresa = Path.Combine(Lbl.Sys.Config.CarpetaEmpresa, "Certificado.p12");
				if (File.Exists(certEmpresa)) return certEmpresa;

				// Buscar en Documents/Lázaro
				string userFolder = Lfx.Environment.Folders.UserFolder;
				if (Directory.Exists(userFolder))
				{
					var archivos = Directory.GetFiles(userFolder, "*.p12", SearchOption.AllDirectories);
					if (archivos != null && archivos.Length > 0)
					{
						if (esHomo)
						{
							foreach (var a in archivos)
							{
								if (a.IndexOf("homo", StringComparison.OrdinalIgnoreCase) >= 0) return a;
							}
						}
						else
						{
							foreach (var a in archivos)
							{
								if (a.IndexOf("prod", StringComparison.OrdinalIgnoreCase) >= 0) return a;
							}
						}
						return archivos[0];
					}
				}
			}
			catch { }

			return null;
		}

		/// <summary>
		/// Obtiene o solicita un Ticket de Acceso (Token y Sign) al WSAA para el servicio ws_sr_constancia_inscripcion.
		/// </summary>
		public static TicketAcceso ObtenerTicketAcceso(bool esHomo, string cuit, string rutaCertificado)
		{
			string claveConfig = esHomo ? "AFIP.TicketAcceso.Constancia.Homo" : "AFIP.TicketAcceso.Constancia.Prod";
			string ticketStr = null;
			if (Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null)
			{
				ticketStr = Lfx.Workspace.Master.CurrentConfig.ReadGlobalSetting<string>(claveConfig, null);
			}

			if (!string.IsNullOrWhiteSpace(ticketStr))
			{
				var partes = ticketStr.Split(new string[] { "|lazaro_separador|" }, StringSplitOptions.None);
				if (partes.Length >= 4)
				{
					DateTime exp;
					if (DateTime.TryParse(partes[3], out exp) && exp > DateTime.Now.AddMinutes(5))
					{
						if (partes.Length >= 5 && !string.IsNullOrEmpty(cuit))
						{
							if (!string.Equals(partes[4].Trim(), cuit.Trim(), StringComparison.OrdinalIgnoreCase))
							{
								ticketStr = null;
							}
						}
						if (ticketStr != null)
						{
							DateTime gen = DateTime.Now;
							DateTime.TryParse(partes[2], out gen);
							return new TicketAcceso
							{
								Token = partes[0],
								Sign = partes[1],
								GenerationTime = gen,
								ExpirationTime = exp
							};
						}
					}
				}
			}

			// Solicitar nuevo Ticket de Acceso al WSAA
			var auth = new ServicioAutenticacion();
			auth.Homologacion = esHomo;
			auth.Servicio = ServicioWsaa;
			auth.RutaCertificado = rutaCertificado;

			var ta = auth.Autenticar();
			if (ta != null && ta.EsValido() && Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null)
			{
				string nuevoStr = ta.Token + "|lazaro_separador|" + ta.Sign + "|lazaro_separador|" +
				                  Lfx.Types.Formatting.FormatDateTimeSql(ta.GenerationTime) + "|lazaro_separador|" +
				                  Lfx.Types.Formatting.FormatDateTimeSql(ta.ExpirationTime) + "|lazaro_separador|" +
				                  (cuit ?? "");
				try
				{
					Lfx.Workspace.Master.CurrentConfig.WriteGlobalSetting(claveConfig, nuevoStr);
				}
				catch { }
			}

			return ta;
		}

		/// <summary>
		/// Consulta la categoría actual del Monotributo para la empresa configurada en ARCA / AFIP.
		/// </summary>
		public static ResultadoConsultaConstancia Consultar()
		{
			string cuit = ObtenerCuitEmpresa();
			if (string.IsNullOrWhiteSpace(cuit))
			{
				return new ResultadoConsultaConstancia
				{
					Exito = false,
					Mensaje = "No se encontró el CUIT de la empresa configurado en el sistema."
				};
			}

			var res = ConsultarContribuyente(cuit);
			if (res.Exito && string.IsNullOrWhiteSpace(res.Categoria))
			{
				res.Exito = false;
				res.Mensaje = "No se encontró registro de Monotributo activo para el CUIT " + cuit + " en la respuesta de ARCA / AFIP.";
			}
			return res;
		}

		/// <summary>
		/// Consulta los datos completos de inscripción y padrón de un CUIT ante ARCA / AFIP (ws_sr_constancia_inscripcion - personaServiceA5).
		/// </summary>
		public static ResultadoConsultaConstancia ConsultarContribuyente(string cuitAConsultar)
		{
			if (string.IsNullOrWhiteSpace(cuitAConsultar))
			{
				return new ResultadoConsultaConstancia
				{
					Exito = false,
					Mensaje = "Debe ingresar un número de CUIT a consultar."
				};
			}

			cuitAConsultar = cuitAConsultar.Replace("-", "").Replace(".", "").Replace(" ", "").Replace("/", "").Trim();
			if (cuitAConsultar.Length != 11)
			{
				return new ResultadoConsultaConstancia
				{
					Exito = false,
					CuitConsultado = cuitAConsultar,
					Mensaje = "El número de CUIT debe tener 11 dígitos numéricos."
				};
			}

			bool esHomo = Lbl.Sys.Config.AfipHomologacion;
			string cuitEmpresa = ObtenerCuitEmpresa();
			if (string.IsNullOrWhiteSpace(cuitEmpresa))
			{
				return new ResultadoConsultaConstancia
				{
					Exito = false,
					CuitConsultado = cuitAConsultar,
					Mensaje = "No se encontró el CUIT de la empresa configurado en el sistema."
				};
			}

			string rutaCert = ResolverRutaCertificado(esHomo);
			if (string.IsNullOrEmpty(rutaCert) || !File.Exists(rutaCert))
			{
				return new ResultadoConsultaConstancia
				{
					Exito = false,
					CuitConsultado = cuitAConsultar,
					Mensaje = "No se encontró el certificado digital (.p12) de AFIP en la carpeta de la empresa. Asegúrate de tener configurado el certificado de facturación electrónica."
				};
			}

			TicketAcceso ta = null;
			try
			{
				ta = ObtenerTicketAcceso(esHomo, cuitEmpresa, rutaCert);
			}
			catch (Exception ex)
			{
				string msg = ex.Message;
				bool faltaPermiso = msg.IndexOf("permiso", StringComparison.OrdinalIgnoreCase) >= 0 ||
				                    msg.IndexOf("autoriz", StringComparison.OrdinalIgnoreCase) >= 0 ||
				                    msg.IndexOf("relacion", StringComparison.OrdinalIgnoreCase) >= 0 ||
				                    msg.IndexOf("ws_sr_constancia_inscripcion", StringComparison.OrdinalIgnoreCase) >= 0 ||
				                    msg.IndexOf("service", StringComparison.OrdinalIgnoreCase) >= 0 ||
				                    msg.IndexOf("CMS", StringComparison.OrdinalIgnoreCase) >= 0;

				return new ResultadoConsultaConstancia
				{
					Exito = false,
					CuitConsultado = cuitAConsultar,
					RequierePermisoWebservice = faltaPermiso,
					ErrorTecnico = msg,
					Mensaje = faltaPermiso ?
						"El certificado digital configurado aún no tiene autorizada la relación para 'Consulta de constancia de inscripción' en ARCA / AFIP." :
						"Error al autenticar ante AFIP (WSAA): " + msg
				};
			}

			if (ta == null || !ta.EsValido())
			{
				return new ResultadoConsultaConstancia
				{
					Exito = false,
					CuitConsultado = cuitAConsultar,
					Mensaje = "No se pudo obtener un Ticket de Acceso válido para el servicio de constancia."
				};
			}

			return InvocarWebServicePersonaA5(esHomo, cuitEmpresa, cuitAConsultar, ta);
		}

		private static ResultadoConsultaConstancia InvocarWebServicePersonaA5(bool esHomo, string cuitRepresentada, string cuitAConsultar, TicketAcceso ta)
		{
			string url = esHomo ? WsConstanciaHomologacion : WsConstanciaProduccion;

			string soapEnvelope = string.Format(
				"<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
				"<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:a5=\"http://a5.soap.ws.server.puc.sr/\">" +
				"  <soapenv:Header/>" +
				"  <soapenv:Body>" +
				"    <a5:getPersona_v2>" +
				"      <token>{0}</token>" +
				"      <sign>{1}</sign>" +
				"      <cuitRepresentada>{2}</cuitRepresentada>" +
				"      <idPersona>{3}</idPersona>" +
				"    </a5:getPersona_v2>" +
				"  </soapenv:Body>" +
				"</soapenv:Envelope>",
				ta.Token,
				ta.Sign,
				cuitRepresentada,
				cuitAConsultar
			);

			try
			{
				ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
				request.Method = "POST";
				request.ContentType = "text/xml; charset=utf-8";
				request.Headers.Add("SOAPAction", "\"\"");
				request.Timeout = 15000;

				byte[] bytes = Encoding.UTF8.GetBytes(soapEnvelope);
				request.ContentLength = bytes.Length;
				using (Stream stream = request.GetRequestStream())
				{
					stream.Write(bytes, 0, bytes.Length);
				}

				string respuestaXml = null;
				try
				{
					using (WebResponse response = request.GetResponse())
					using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
					{
						respuestaXml = reader.ReadToEnd();
					}
				}
				catch (WebException webEx)
				{
					if (webEx.Response != null)
					{
						using (StreamReader reader = new StreamReader(webEx.Response.GetResponseStream(), Encoding.UTF8))
						{
							respuestaXml = reader.ReadToEnd();
						}
					}
					else
					{
						return new ResultadoConsultaConstancia
						{
							Exito = false,
							CuitConsultado = cuitAConsultar,
							ErrorTecnico = webEx.Message,
							Mensaje = "Error de conexión al Web Service de ARCA / AFIP: " + webEx.Message
						};
					}
				}

				if (string.IsNullOrWhiteSpace(respuestaXml))
				{
					return new ResultadoConsultaConstancia
					{
						Exito = false,
						CuitConsultado = cuitAConsultar,
						Mensaje = "No se recibió respuesta del servicio de padrón de AFIP."
					};
				}

				return ParsearRespuestaPersonaA5(respuestaXml, cuitAConsultar);
			}
			catch (Exception ex)
			{
				return new ResultadoConsultaConstancia
				{
					Exito = false,
					CuitConsultado = cuitAConsultar,
					ErrorTecnico = ex.Message,
					Mensaje = "Error invocando Web Service de AFIP: " + ex.Message
				};
			}
		}

		public static ResultadoConsultaConstancia ParsearRespuestaPersonaA5(string xml, string cuit)
		{
			var res = new ResultadoConsultaConstancia
			{
				CuitConsultado = cuit
			};

			try
			{
				XmlDocument doc = new XmlDocument();
				doc.LoadXml(xml);

				// 1. Verificar si hay Fault de SOAP
				XmlNode faultNode = doc.SelectSingleNode("//faultstring") ?? doc.SelectSingleNode("//*[local-name()='faultstring']");
				if (faultNode != null)
				{
					string faultMsg = faultNode.InnerText;
					res.Exito = false;
					res.ErrorTecnico = faultMsg;
					if (faultMsg.IndexOf("autoriz", StringComparison.OrdinalIgnoreCase) >= 0 ||
					    faultMsg.IndexOf("permiso", StringComparison.OrdinalIgnoreCase) >= 0 ||
					    faultMsg.IndexOf("relacion", StringComparison.OrdinalIgnoreCase) >= 0 ||
					    faultMsg.IndexOf("cuitRepresentada", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						res.RequierePermisoWebservice = true;
						res.Mensaje = "El certificado no tiene permisos para 'Consulta de constancia de inscripción' en ARCA / AFIP.";
					}
					else
					{
						res.Mensaje = "Respuesta de AFIP (Fault): " + faultMsg;
					}
					return res;
				}

				// 2. Verificar errores específicos en la respuesta
				XmlNode errConst = doc.SelectSingleNode("//*[local-name()='errorConstancia']");
				XmlNode errMono = doc.SelectSingleNode("//*[local-name()='errorMonotributo']");
				XmlNode errReg = doc.SelectSingleNode("//*[local-name()='errorRegimenGeneral']");
				string errConstTxt = errConst != null ? errConst.InnerText.Trim() : null;
				if (!string.IsNullOrEmpty(errConstTxt) && (errConstTxt.IndexOf("no existe", StringComparison.OrdinalIgnoreCase) >= 0 ||
				                                          errConstTxt.IndexOf("inválid", StringComparison.OrdinalIgnoreCase) >= 0 ||
				                                          errConstTxt.IndexOf("invalido", StringComparison.OrdinalIgnoreCase) >= 0))
				{
					res.Exito = false;
					res.Mensaje = "ARCA / AFIP informa: " + errConstTxt;
					return res;
				}

				// 3. Determinar tipo de persona (FISICA o JURIDICA)
				string tipoPersonaRaw = ObtenerTextoNodo(doc, "tipoPersona") ?? "";
				if (tipoPersonaRaw.IndexOf("JURID", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					res.TipoPersona = "JURIDICA";
				}
				else if (tipoPersonaRaw.IndexOf("FISIC", StringComparison.OrdinalIgnoreCase) >= 0 ||
				         tipoPersonaRaw.IndexOf("HUMAN", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					res.TipoPersona = "FISICA";
				}
				else
				{
					if (cuit.StartsWith("30") || cuit.StartsWith("33") || cuit.StartsWith("34"))
						res.TipoPersona = "JURIDICA";
					else
						res.TipoPersona = "FISICA";
				}

				// 4. Extraer Nombres / Razón Social
				string apellido = ObtenerTextoNodo(doc, "apellido");
				string nombre = ObtenerTextoNodo(doc, "nombre");
				string razonSocial = ObtenerTextoNodo(doc, "razonSocial");
				string denominacion = ObtenerTextoNodo(doc, "denominacion");

				if (res.TipoPersona == "JURIDICA")
				{
					res.RazonSocial = !string.IsNullOrEmpty(razonSocial) ? razonSocial : denominacion;
				}
				else
				{
					res.Apellido = apellido;
					res.Nombre = nombre;
					if (string.IsNullOrEmpty(res.Apellido) && string.IsNullOrEmpty(res.Nombre) && !string.IsNullOrEmpty(denominacion))
					{
						if (denominacion.Contains(","))
						{
							var partes = denominacion.Split(new char[] { ',' }, 2);
							res.Apellido = partes[0].Trim();
							res.Nombre = partes[1].Trim();
						}
						else
						{
							res.Apellido = denominacion.Trim();
						}
					}

					// Extraer DNI de los 8 dígitos centrales del CUIT
					if (cuit.Length == 11)
					{
						res.NumeroDocumento = cuit.Substring(2, 8).TrimStart('0');
					}
				}

				// 5. Domicilio fiscal
				XmlNode domNode = doc.SelectSingleNode("//*[local-name()='domicilioFiscal']") ??
				                  doc.SelectSingleNode("//*[local-name()='domicilio']");
				if (domNode != null)
				{
					XmlNode dirNode = domNode.SelectSingleNode("*[local-name()='direccion']");
					XmlNode locNode = domNode.SelectSingleNode("*[local-name()='localidad']");
					XmlNode cpNode = domNode.SelectSingleNode("*[local-name()='codPostal']");
					XmlNode provNode = domNode.SelectSingleNode("*[local-name()='descripcionProvincia']");
					XmlNode idProvNode = domNode.SelectSingleNode("*[local-name()='idProvincia']");

					if (dirNode != null) res.Domicilio = dirNode.InnerText.Trim();
					if (locNode != null) res.Localidad = locNode.InnerText.Trim();
					if (cpNode != null) res.CodigoPostal = cpNode.InnerText.Trim();
					if (provNode != null) res.Provincia = provNode.InnerText.Trim();
					if (idProvNode != null)
					{
						int idProv;
						if (int.TryParse(idProvNode.InnerText.Trim(), out idProv))
							res.IdProvincia = idProv;
					}
				}

				// 6. Estado de clave
				res.EstadoClave = ObtenerTextoNodo(doc, "estadoClave");

				// 7. Situación tributaria e impuestos
				// A) Monotributo
				XmlNode catNode = doc.SelectSingleNode("//*[local-name()='categoriaMonotributo']");
				if (catNode != null && !string.IsNullOrWhiteSpace(catNode.InnerText))
				{
					string catRaw = catNode.InnerText.Trim();
					string catLetra = EscalasMonotributo.Instancia.NormalizarLetra(catRaw);

					// Si no se extrajo directamente del InnerText, revisar nodos hijos específicos
					if (string.IsNullOrEmpty(catLetra))
					{
						XmlNode nodoId = catNode.SelectSingleNode("*[local-name()='idCategoria' or local-name()='categoria' or local-name()='letra' or local-name()='codigoCategoria']");
						if (nodoId != null && !string.IsNullOrWhiteSpace(nodoId.InnerText))
						{
							catLetra = EscalasMonotributo.Instancia.NormalizarLetra(nodoId.InnerText.Trim());
						}
					}

					// Buscar descripción si existe como nodo separado
					XmlNode descCatNode = catNode.SelectSingleNode("*[local-name()='descripcionCategoria']");
					string descCat = descCatNode != null ? descCatNode.InnerText.Trim() : "";

					res.Categoria = !string.IsNullOrEmpty(catLetra) ? catLetra : catRaw;
					res.IdSituacionTributaria = 4; // Responsable Monotributista
					if (!string.IsNullOrEmpty(catLetra) && !string.IsNullOrEmpty(descCat))
					{
						res.DescripcionSituacion = string.Format("Responsable Monotributista (Cat. {0} - {1})", catLetra, descCat);
					}
					else if (!string.IsNullOrEmpty(catLetra))
					{
						res.DescripcionSituacion = string.Format("Responsable Monotributista (Cat. {0})", catLetra);
					}
					else
					{
						res.DescripcionSituacion = "Responsable Monotributista";
					}
				}

				if (string.IsNullOrEmpty(res.Categoria))
				{
					XmlNode catAltNode = doc.SelectSingleNode("//*[local-name()='datosMonotributo']//*[local-name()='categoria' or local-name()='letra']");
					if (catAltNode != null && !string.IsNullOrWhiteSpace(catAltNode.InnerText))
					{
						string catAlt = EscalasMonotributo.Instancia.NormalizarLetra(catAltNode.InnerText.Trim());
						if (!string.IsNullOrEmpty(catAlt))
						{
							res.Categoria = catAlt;
							res.IdSituacionTributaria = 4;
							res.DescripcionSituacion = string.Format("Responsable Monotributista (Cat. {0})", catAlt);
						}
					}
				}

				// B) Revisar impuestos registrados en datosRegimenGeneral u otros nodos <impuesto>
				var impuestosNodes = doc.SelectNodes("//*[local-name()='impuesto']");
				if (impuestosNodes != null)
				{
					foreach (XmlNode imp in impuestosNodes)
					{
						XmlNode idImpNode = imp.SelectSingleNode("*[local-name()='idImpuesto']");
						if (idImpNode != null)
						{
							int idImp;
							if (int.TryParse(idImpNode.InnerText.Trim(), out idImp))
							{
								if (idImp == 20 || idImp == 21) // Monotributo
								{
									if (res.IdSituacionTributaria == 0)
									{
										res.IdSituacionTributaria = 4;
										res.DescripcionSituacion = "Responsable Monotributista";
									}
								}
								else if (idImp == 30) // IVA (Responsable Inscripto)
								{
									if (res.IdSituacionTributaria != 4)
									{
										res.IdSituacionTributaria = 2; // Responsable Inscripto
										res.DescripcionSituacion = "Responsable Inscripto";
									}
								}
								else if (idImp == 32) // IVA Exento
								{
									if (res.IdSituacionTributaria != 4 && res.IdSituacionTributaria != 2)
									{
										res.IdSituacionTributaria = 5; // Exento
										res.DescripcionSituacion = "Exento";
									}
								}
							}
						}
					}
				}

				// Si tiene nodo <datosMonotributo> activo
				if (res.IdSituacionTributaria == 0 && doc.SelectSingleNode("//*[local-name()='datosMonotributo']") != null)
				{
					res.IdSituacionTributaria = 4;
					res.DescripcionSituacion = "Responsable Monotributista";
				}

				// 8. Determinar si se obtuvieron datos válidos
				if (!string.IsNullOrEmpty(res.RazonSocial) || !string.IsNullOrEmpty(res.Apellido) ||
				    !string.IsNullOrEmpty(res.Nombre) || !string.IsNullOrEmpty(res.Domicilio) ||
				    res.IdSituacionTributaria > 0)
				{
					res.Exito = true;
					string nombreMostrar = res.TipoPersona == "JURIDICA" ?
						res.RazonSocial :
						((res.Apellido ?? "") + (string.IsNullOrEmpty(res.Nombre) ? "" : ", " + res.Nombre)).Trim();
					if (string.IsNullOrEmpty(nombreMostrar))
						nombreMostrar = res.CuitConsultado;

					res.Mensaje = string.Format("Datos obtenidos de ARCA / AFIP para {0}.", nombreMostrar);
					return res;
				}

				// Si hubo errores informados por AFIP
				string errOtro = (errMono != null ? errMono.InnerText : null) ??
				                 (errReg != null ? errReg.InnerText : null) ??
				                 (errConst != null ? errConst.InnerText : null);
				if (!string.IsNullOrWhiteSpace(errOtro))
				{
					res.Exito = false;
					res.Mensaje = "ARCA / AFIP informa: " + errOtro.Trim();
					return res;
				}

				res.Exito = false;
				res.Mensaje = "No se encontraron datos registrados para el CUIT " + cuit + " en ARCA / AFIP.";
				return res;
			}
			catch (Exception ex)
			{
				res.Exito = false;
				res.ErrorTecnico = ex.Message;
				res.Mensaje = "Error procesando respuesta XML de AFIP: " + ex.Message;
				return res;
			}
		}

		private static string ObtenerTextoNodo(XmlDocument doc, string localName)
		{
			XmlNode node = doc.SelectSingleNode("//*[local-name()='" + localName + "']");
			if (node != null && !string.IsNullOrWhiteSpace(node.InnerText))
			{
				return node.InnerText.Trim();
			}
			return null;
		}
	}
}
