using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Lbl.Impuestos.Monotributo
{
	/// <summary>
	/// Parser nativo para extraer las escalas y cuotas de categorías de Monotributo
	/// directamente desde la tabla publicada en el sitio oficial de ARCA / AFIP.
	/// URL oficial: https://www.afip.gob.ar/monotributo/categorias.asp
	/// </summary>
	public static class ParserWebAfip
	{
		public const string UrlAfip = "https://www.afip.gob.ar/monotributo/categorias.asp";
		public const string UrlArcaFallback = "https://www.arca.gob.ar/monotributo/categorias.asp";

		/// <summary>
		/// Descarga el HTML de la web oficial de AFIP/ARCA y parsea la tabla de categorías vigente.
		/// Esta operación es exclusivamente manual y bajo demanda del usuario.
		/// </summary>
		public static bool ObtenerEscalasDesdeWeb(string tipoActividad, out List<CategoriaMonotributo> categorias, out string infoMetadata, out string mensajeError)
		{
			categorias = new List<CategoriaMonotributo>();
			infoMetadata = "";
			mensajeError = "";

			string html = null;

			// Intentar primero con la URL de AFIP
			try
			{
				html = DescargarHtml(UrlAfip);
			}
			catch (Exception ex1)
			{
				// Fallback a dominio ARCA si AFIP redirige o falla la resolución
				try
				{
					html = DescargarHtml(UrlArcaFallback);
				}
				catch (Exception ex2)
				{
					mensajeError = "No fue posible conectar con el servidor de AFIP / ARCA:\r\n" + ex1.Message + "\r\n(Fallback ARCA: " + ex2.Message + ")";
					return false;
				}
			}

			if (string.IsNullOrEmpty(html))
			{
				mensajeError = "La respuesta del portal web de AFIP está vacía.";
				return false;
			}

			return ParsearHtml(html, tipoActividad, out categorias, out infoMetadata, out mensajeError);
		}

		/// <summary>
		/// Realiza la petición HTTP GET al portal oficial configurando TLS 1.2+, User-Agent y descompresión.
		/// </summary>
		private static string DescargarHtml(string url)
		{
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.Method = "GET";
			request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36";
			request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
			request.Headers.Add("Accept-Language", "es-AR,es;q=0.9,en;q=0.8");
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			request.Timeout = 15000;

			using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
			using (Stream stream = response.GetResponseStream())
			using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
			{
				return reader.ReadToEnd();
			}
		}

		/// <summary>
		/// Parsea el contenido HTML localizando la tabla de categorías de Monotributo.
		/// </summary>
		public static bool ParsearHtml(string html, string tipoActividad, out List<CategoriaMonotributo> categorias, out string infoMetadata, out string mensajeError)
		{
			categorias = new List<CategoriaMonotributo>();
			infoMetadata = "";
			mensajeError = "";

			if (string.IsNullOrEmpty(html))
			{
				mensajeError = "El contenido HTML a analizar está vacío.";
				return false;
			}

			try
			{
				// Extraer información de vigencia desde el atributo summary o caption si está disponible
				string vigencia = "";
				Match matchSummary = Regex.Match(html, @"summary=""Tabla de categorías de monotributo\s+(vigente[^""]*)""", RegexOptions.IgnoreCase);
				if (matchSummary.Success)
				{
					vigencia = matchSummary.Groups[1].Value.Trim();
				}
				else
				{
					Match matchVig = Regex.Match(html, @"(vigente\s+desde\s+\d{1,2}/\d{1,2}/\d{4})", RegexOptions.IgnoreCase);
					if (matchVig.Success)
						vigencia = matchVig.Groups[1].Value.Trim();
				}

				if (!string.IsNullOrEmpty(vigencia))
				{
					infoMetadata = string.Format("Fuente: ARCA / AFIP oficial (Web) | {0} | Sincronizado: {1}", vigencia, DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
				}
				else
				{
					infoMetadata = string.Format("Fuente: ARCA / AFIP oficial (Web) | Sincronizado: {0}", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
				}

				// Expresión regular para cada fila <tr> que contenga un <th> con la letra de la categoría (A-K)
				Regex rowRegex = new Regex(@"(?s)<tr\b[^>]*>\s*<th\b([^>]*)>\s*([A-K])\s*</th>(.*?)</tr>", RegexOptions.IgnoreCase);
				MatchCollection rows = rowRegex.Matches(html);

				if (rows.Count == 0)
				{
					// Intento alternativo más flexible: buscar <tr> y luego verificar <th> interno
					Regex altRowRegex = new Regex(@"(?s)<tr\b[^>]*>(.*?)</tr>", RegexOptions.IgnoreCase);
					MatchCollection allRows = altRowRegex.Matches(html);
					foreach (Match r in allRows)
					{
						string body = r.Groups[1].Value;
						Match thM = Regex.Match(body, @"<th\b[^>]*>\s*([A-K])\s*</th>", RegexOptions.IgnoreCase);
						if (thM.Success)
						{
							ProcesarFila(thM.Groups[1].Value, body, categorias);
						}
					}
				}
				else
				{
					foreach (Match rowMatch in rows)
					{
						string letra = rowMatch.Groups[2].Value;
						string body = rowMatch.Groups[3].Value;
						ProcesarFila(letra, body, categorias);
					}
				}

				if (categorias.Count >= 8)
				{
					// Ordenar por tope ascendente
					categorias.Sort((a, b) => a.IngresosBrutosMaximos.CompareTo(b.IngresosBrutosMaximos));
					return true;
				}

				mensajeError = string.Format("Se detectaron {0} categorías en la página de AFIP, pero se esperaban al menos 8 (A a H/K).", categorias.Count);
				return false;
			}
			catch (Exception ex)
			{
				mensajeError = "Error al parsear la tabla de Monotributo de AFIP: " + ex.Message;
				return false;
			}
		}

		private static void ProcesarFila(string letra, string body, List<CategoriaMonotributo> lista)
		{
			string l = (letra ?? "").Trim().ToUpperInvariant();
			if (string.IsNullOrEmpty(l) || l.Length != 1 || l[0] < 'A' || l[0] > 'K')
				return;

			// Extraer todos los <td> de la fila
			Regex tdRegex = new Regex(@"(?s)<td\b([^>]*)>(.*?)</td>", RegexOptions.IgnoreCase);
			MatchCollection tds = tdRegex.Matches(body);

			decimal ingresosBrutos = 0m;
			decimal cuotaServicios = 0m;
			decimal cuotaBienes = 0m;

			List<string> valoresOrdenados = new List<string>();

			foreach (Match td in tds)
			{
				string attrs = td.Groups[1].Value.ToLowerInvariant();
				string rawContent = td.Groups[2].Value;
				string limpio = LimpiarTextoHtml(rawContent);
				valoresOrdenados.Add(limpio);

				// 1. Identificación por atributos semánticos (data-title o headers)
				if (attrs.Contains("ing_br") || attrs.Contains("ingresos brutos"))
				{
					ingresosBrutos = ParsearMonto(limpio);
				}
				else if (attrs.Contains("th_total_loc") || attrs.Contains("total: locaciones") || (attrs.Contains("total") && attrs.Contains("servicios")))
				{
					cuotaServicios = ParsearMonto(limpio);
				}
				else if (attrs.Contains("th_total_ven") || attrs.Contains("total: venta") || (attrs.Contains("total") && attrs.Contains("muebles")))
				{
					cuotaBienes = ParsearMonto(limpio);
				}
			}

			// 2. Fallback posicional si los atributos no coincidieron (estructura canónica de la tabla de AFIP):
			// Celda 0: Ingresos brutos
			// Celda 9: Total Locaciones de Servicios
			// Celda 10: Total Venta de Cosas Muebles
			if (ingresosBrutos <= 0 && valoresOrdenados.Count > 0)
			{
				ingresosBrutos = ParsearMonto(valoresOrdenados[0]);
			}
			if (cuotaServicios <= 0 && valoresOrdenados.Count >= 10)
			{
				cuotaServicios = ParsearMonto(valoresOrdenados[9]);
			}
			if (cuotaBienes <= 0 && valoresOrdenados.Count >= 11)
			{
				cuotaBienes = ParsearMonto(valoresOrdenados[10]);
			}

			if (ingresosBrutos > 0)
			{
				// Si no se pudo obtener alguna cuota, asumir la otra como aproximación
				if (cuotaServicios <= 0 && cuotaBienes > 0)
					cuotaServicios = cuotaBienes;
				if (cuotaBienes <= 0 && cuotaServicios > 0)
					cuotaBienes = cuotaServicios;

				lista.Add(new CategoriaMonotributo(l, ingresosBrutos, cuotaServicios, cuotaBienes));
			}
		}

		private static string LimpiarTextoHtml(string html)
		{
			if (string.IsNullOrEmpty(html)) return "";
			// Eliminar tags HTML
			string sinTags = Regex.Replace(html, @"<[^>]+>", " ");
			// Decodificar entidades (&nbsp;, etc.)
			string decodificado = WebUtility.HtmlDecode(sinTags);
			// Normalizar espacios
			return Regex.Replace(decodificado, @"\s+", " ").Trim();
		}

		public static decimal ParsearMonto(string texto)
		{
			if (string.IsNullOrEmpty(texto)) return 0m;

			string limpio = texto.Replace("$", "").Replace("\u00A0", "").Trim();
			if (string.IsNullOrEmpty(limpio)) return 0m;

			// Intentar formato regional es-AR (puntos para miles, comas para decimales)
			if (decimal.TryParse(limpio, NumberStyles.Currency | NumberStyles.Number, new CultureInfo("es-AR"), out decimal res))
				return res;

			// Intentar formato estándar o alternativo
			string inv = limpio.Replace(".", "").Replace(",", ".");
			if (decimal.TryParse(inv, NumberStyles.Any, CultureInfo.InvariantCulture, out res))
				return res;

			return 0m;
		}
	}
}
