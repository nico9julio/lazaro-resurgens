using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Lbl.Impuestos.Monotributo
{
        /// <summary>
        /// Administra las escalas de categorías de Monotributo vigentes en Argentina.
        /// Permite guardar valores configurados por el usuario, restablecer valores predeterminados
        /// y opcionalmente consultar/sincronizar escalas desde una URL o API externa en formato JSON (como Servidos API).
        /// </summary>
        public class EscalasMonotributo
        {
                private static EscalasMonotributo m_Instancia = null;
                private List<CategoriaMonotributo> m_Categorias = new List<CategoriaMonotributo>();

                public static string UltimaFuenteMetadata { get; set; }
                public static string UltimaVigenciaMetadata { get; set; }

                public static EscalasMonotributo Instancia
                {
                        get
                        {
                                if (m_Instancia == null)
                                {
                                        m_Instancia = new EscalasMonotributo();
                                        m_Instancia.Cargar();
                                }
                                return m_Instancia;
                        }
                }

                public List<CategoriaMonotributo> Categorias
                {
                        get
                        {
                                return m_Categorias;
                        }
                }

                public EscalasMonotributo()
                {
                }

                /// <summary>
                /// Devuelve las escalas predeterminadas oficiales de ARCA (ex AFIP) vigentes.
                /// </summary>
                public static List<CategoriaMonotributo> ObtenerEscalasPredeterminadas()
                {
                        return new List<CategoriaMonotributo>()
                        {
                                new CategoriaMonotributo("A", 8992597.87m, 49527.18m, 49527.18m),
                                new CategoriaMonotributo("B", 13175201.52m, 56379.08m, 56379.08m),
                                new CategoriaMonotributo("C", 18473166.15m, 66020.12m, 64530.58m),
                                new CategoriaMonotributo("D", 22934610.05m, 84612.93m, 82564.81m),
                                new CategoriaMonotributo("E", 26977793.60m, 119811.45m, 108267.51m),
                                new CategoriaMonotributo("F", 33809379.57m, 150784.21m, 129930.65m),
                                new CategoriaMonotributo("G", 40431835.35m, 230312.94m, 158815.05m),
                                new CategoriaMonotributo("H", 61344853.64m, 522706.68m, 317895.01m),
                                new CategoriaMonotributo("I", 68664410.05m, 963747.86m, 0m),
                                new CategoriaMonotributo("J", 78632948.76m, 1167299.76m, 0m),
                                new CategoriaMonotributo("K", 94805682.90m, 1614446.04m, 702103.24m)
                        };
                }

                public void Cargar()
                {
                        string strConfig = null;
                        try
                        {
                                if (Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null)
                                {
                                        strConfig = Lfx.Workspace.Master.CurrentConfig.ReadGlobalSetting<string>("Sistema.Monotributo.Escalas", null);
                                        UltimaVigenciaMetadata = Lfx.Workspace.Master.CurrentConfig.ReadGlobalSetting<string>("Sistema.Monotributo.UltimaMetadata", "");
                                }
                        }
                        catch
                        {
                                strConfig = null;
                        }

                        if (!string.IsNullOrEmpty(strConfig))
                        {
                                var cargadas = DeserializarEscalas(strConfig);
                                if (cargadas != null && cargadas.Count > 0)
                                {
                                        m_Categorias = cargadas;
                                        return;
                                }
                        }

                        m_Categorias = ObtenerEscalasPredeterminadas();
                }

                public void Guardar(List<CategoriaMonotributo> nuevasCategorias)
                {
                        if (nuevasCategorias != null && nuevasCategorias.Count > 0)
                        {
                                m_Categorias = nuevasCategorias;
                                string str = SerializarEscalas(nuevasCategorias);
                                if (Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null)
                                {
                                        Lfx.Workspace.Master.CurrentConfig.WriteGlobalSetting("Sistema.Monotributo.Escalas", str);
                                }
                        }
                }

                public void RestablecerPredeterminados()
                {
                        m_Categorias = ObtenerEscalasPredeterminadas();
                        UltimaVigenciaMetadata = "";
                        if (Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null)
                        {
                                Lfx.Workspace.Master.CurrentConfig.DeleteGlobalSetting("Sistema.Monotributo.Escalas", 0);
                                Lfx.Workspace.Master.CurrentConfig.DeleteGlobalSetting("Sistema.Monotributo.UltimaMetadata", 0);
                        }
                }

                /// <summary>
                /// Determina en qué categoría encuadra un importe de ingresos brutos anuales acumulados o proyectados.
                /// Devuelve null si supera el tope máximo de la categoría K (queda excluido del Monotributo).
                /// </summary>
                public CategoriaMonotributo ObtenerCategoriaParaMonto(decimal montoAnual)
                {
                        if (m_Categorias == null || m_Categorias.Count == 0)
                                Cargar();

                        // Ordenar por tope ascendente
                        List<CategoriaMonotributo> ordenadas = new List<CategoriaMonotributo>(m_Categorias);
                        ordenadas.Sort((a, b) => a.IngresosBrutosMaximos.CompareTo(b.IngresosBrutosMaximos));

                        foreach (var cat in ordenadas)
                        {
                                if (montoAnual <= cat.IngresosBrutosMaximos)
                                        return cat;
                        }

                        return null; // Excluido (supera la categoría máxima K)
                }

                public bool EstaExcluido(decimal montoAnual)
                {
                        if (m_Categorias == null || m_Categorias.Count == 0)
                                Cargar();

                        decimal topeMaximo = 0;
                        foreach (var cat in m_Categorias)
                        {
                                if (cat.IngresosBrutosMaximos > topeMaximo)
                                        topeMaximo = cat.IngresosBrutosMaximos;
                        }

                        return montoAnual > topeMaximo;
                }

                public CategoriaMonotributo ObtenerCategoria(string letra)
                {
                        if (string.IsNullOrEmpty(letra))
                                return null;

                        string l = letra.Trim().ToUpperInvariant();
                        foreach (var cat in m_Categorias)
                        {
                                if (cat.Letra == l)
                                        return cat;
                        }
                        return null;
                }

                public decimal ObtenerTopeMaximo()
                {
                        decimal max = 0;
                        foreach (var cat in m_Categorias)
                        {
                                if (cat.IngresosBrutosMaximos > max)
                                        max = cat.IngresosBrutosMaximos;
                        }
                        return max;
                }

                /// <summary>
                /// Devuelve las categorías ordenadas por ingresos brutos máximos de forma ascendente.
                /// </summary>
                public List<CategoriaMonotributo> ObtenerCategoriasOrdenadas()
                {
                        if (m_Categorias == null || m_Categorias.Count == 0)
                                Cargar();

                        List<CategoriaMonotributo> ordenadas = new List<CategoriaMonotributo>(m_Categorias);
                        ordenadas.Sort((a, b) => a.IngresosBrutosMaximos.CompareTo(b.IngresosBrutosMaximos));
                        return ordenadas;
                }

                /// <summary>
                /// Devuelve el tope de ingresos brutos de la categoría inmediatamente anterior.
                /// Para la categoría A devuelve 0.
                /// </summary>
                public decimal ObtenerTopeAnterior(string letra)
                {
                        if (string.IsNullOrEmpty(letra))
                                return 0m;

                        var ordenadas = ObtenerCategoriasOrdenadas();
                        string l = letra.Trim().ToUpperInvariant();

                        decimal topeAnt = 0m;
                        foreach (var cat in ordenadas)
                        {
                                if (cat.Letra == l)
                                        return topeAnt;
                                topeAnt = cat.IngresosBrutosMaximos;
                        }
                        return 0m;
                }

                /// <summary>
                /// Indica si la letra corresponde a la última categoría del Monotributo (habitualmente la K).
                /// </summary>
                public bool EsUltimaCategoria(string letra)
                {
                        if (string.IsNullOrEmpty(letra))
                                return false;

                        var ordenadas = ObtenerCategoriasOrdenadas();
                        if (ordenadas.Count == 0)
                                return false;

                        return ordenadas[ordenadas.Count - 1].Letra == letra.Trim().ToUpperInvariant();
                }

                /// <summary>
                /// Devuelve la siguiente categoría a la actual, o null si es la última (K).
                /// </summary>
                public CategoriaMonotributo ObtenerSiguienteCategoria(string letra)
                {
                        if (string.IsNullOrEmpty(letra))
                                return null;

                        var ordenadas = ObtenerCategoriasOrdenadas();
                        string l = letra.Trim().ToUpperInvariant();

                        for (int i = 0; i < ordenadas.Count; i++)
                        {
                                if (ordenadas[i].Letra == l)
                                {
                                        if (i + 1 < ordenadas.Count)
                                                return ordenadas[i + 1];
                                        else
                                                return null; // Última categoría
                                }
                        }
                        return null;
                }

                /// <summary>
                /// Calcula el progreso porcentual dentro del tramo de la categoría correspondiente a un monto de facturación.
                /// Para cada categoría, el progreso arranca en 0% (al superar el tope anterior) y llega a 100% (al tocar el tope actual).
                /// Para la última categoría (K), al llegar al 100% no se pasa a otra categoría sino que se produce la exclusión.
                /// </summary>
                public void CalcularProgresoCategoria(decimal montoAnual, out CategoriaMonotributo categoriaActual,
                        out decimal topeAnterior, out decimal topeActual, out decimal porcentajeTramo, out bool esUltima, out string siguienteLetra)
                {
                        var ordenadas = ObtenerCategoriasOrdenadas();
                        categoriaActual = ObtenerCategoriaParaMonto(montoAnual);
                        topeAnterior = 0m;
                        topeActual = 0m;
                        porcentajeTramo = 0m;
                        esUltima = false;
                        siguienteLetra = "";

                        if (ordenadas.Count == 0)
                                return;

                        if (categoriaActual == null)
                        {
                                // Supera la categoría máxima: Excluido
                                var ult = ordenadas[ordenadas.Count - 1];
                                topeActual = ult.IngresosBrutosMaximos;
                                topeAnterior = ordenadas.Count > 1 ? ordenadas[ordenadas.Count - 2].IngresosBrutosMaximos : 0m;
                                porcentajeTramo = 100m;
                                esUltima = true;
                                siguienteLetra = "EXCLUSIÓN";
                                return;
                        }

                        topeActual = categoriaActual.IngresosBrutosMaximos;
                        topeAnterior = ObtenerTopeAnterior(categoriaActual.Letra);
                        esUltima = EsUltimaCategoria(categoriaActual.Letra);

                        var sig = ObtenerSiguienteCategoria(categoriaActual.Letra);
                        siguienteLetra = sig != null ? sig.Letra : "EXCLUSIÓN";

                        decimal tamanoTramo = topeActual - topeAnterior;
                        if (tamanoTramo > 0m)
                        {
                                decimal enTramo = Math.Max(0m, montoAnual - topeAnterior);
                                porcentajeTramo = Math.Min(100m, Math.Round((enTramo / tamanoTramo) * 100m, 2, MidpointRounding.AwayFromZero));
                        }
                        else
                        {
                                porcentajeTramo = 0m;
                        }
                }

                private string SerializarEscalas(List<CategoriaMonotributo> categorias)
                {
                        StringBuilder sb = new StringBuilder();
                        foreach (var cat in categorias)
                        {
                                if (sb.Length > 0)
                                        sb.Append(";");
                                sb.AppendFormat(CultureInfo.InvariantCulture, "{0}:{1}:{2}:{3}",
                                        cat.Letra, cat.IngresosBrutosMaximos, cat.CuotaServicios, cat.CuotaBienes);
                        }
                        return sb.ToString();
                }

                private List<CategoriaMonotributo> DeserializarEscalas(string texto)
                {
                        List<CategoriaMonotributo> res = new List<CategoriaMonotributo>();
                        string[] partes = texto.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string parte in partes)
                        {
                                string[] campos = parte.Split(':');
                                if (campos.Length >= 2)
                                {
                                        string letra = campos[0].Trim().ToUpperInvariant();
                                        decimal tope = 0, serv = 0, bien = 0;
                                        decimal.TryParse(campos[1], NumberStyles.Any, CultureInfo.InvariantCulture, out tope);
                                        if (campos.Length >= 3)
                                                decimal.TryParse(campos[2], NumberStyles.Any, CultureInfo.InvariantCulture, out serv);
                                        if (campos.Length >= 4)
                                                decimal.TryParse(campos[3], NumberStyles.Any, CultureInfo.InvariantCulture, out bien);

                                        if (!string.IsNullOrEmpty(letra) && tope > 0)
                                        {
                                                res.Add(new CategoriaMonotributo(letra, tope, serv, bien));
                                        }
                                }
                        }
                        return res;
                }

                /// <summary>
                /// Permite consultar y descargar las escalas actualizadas desde un endpoint HTTP/API JSON externo.
                /// </summary>
                public bool ActualizarDesdeUrl(string url)
                {
                        if (string.IsNullOrEmpty(url))
                                return false;

                        try
                        {
                                using (WebClient client = new WebClient())
                                {
                                        client.Encoding = Encoding.UTF8;
                                        string json = client.DownloadString(url);
                                        return ActualizarDesdeJson(json);
                                }
                        }
                        catch
                        {
                                return false;
                        }
                }

                /// <summary>
                /// Parsea un contenido JSON simple que contenga las categorías y topes.
                /// Soporta objetos tipo: [ {"letra":"A", "tope":8992597.87}, ... ]
                /// </summary>
                public bool ActualizarDesdeJson(string json)
                {
                        if (string.IsNullOrEmpty(json))
                                return false;

                        try
                        {
                                List<CategoriaMonotributo> lista = new List<CategoriaMonotributo>();
                                // Parseo ligero sin dependencias pesadas
                                string[] items = json.Split(new char[] { '{', '}' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (string item in items)
                                {
                                        if (!item.Contains(":") || !item.Contains(","))
                                                continue;

                                        string letra = null;
                                        decimal tope = 0;
                                        decimal serv = 0;
                                        decimal bien = 0;

                                        string[] props = item.Split(',');
                                        foreach (string prop in props)
                                        {
                                                string[] kv = prop.Split(':');
                                                if (kv.Length >= 2)
                                                {
                                                        string k = kv[0].Replace("\"", "").Trim().ToLowerInvariant();
                                                        string v = kv[1].Replace("\"", "").Trim();
                                                        if (k == "letra" || k == "categoria")
                                                                letra = v.ToUpperInvariant();
                                                        else if (k == "tope" || k == "ingresosbrutosmaximos" || k == "maximo")
                                                                decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out tope);
                                                        else if (k == "cuotaservicios" || k == "servicios")
                                                                decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out serv);
                                                        else if (k == "cuotabienes" || k == "bienes")
                                                                decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out bien);
                                                }
                                        }

                                        if (!string.IsNullOrEmpty(letra) && tope > 0)
                                        {
                                                lista.Add(new CategoriaMonotributo(letra, tope, serv, bien));
                                        }
                                }

                                if (lista.Count >= 5)
                                {
                                        Guardar(lista);
                                        return true;
                                }
                        }
                        catch
                        {
                        }

                        return false;
                }

                /// <summary>
                /// Sincroniza las escalas de categorías consultando y parseando directamente la web oficial de ARCA/AFIP (https://www.afip.gob.ar/monotributo/categorias.asp).
                /// No requiere API Key ni registro en plataformas externas.
                /// Esta operación es exclusivamente manual a petición del usuario.
                /// </summary>
                public bool SincronizarDesdeWebAfip(string activityType, out string infoMetadata, out string mensajeError)
                {
                        infoMetadata = "";
                        mensajeError = "";

                        List<CategoriaMonotributo> nuevas;
                        bool ok = ParserWebAfip.ObtenerEscalasDesdeWeb(activityType, out nuevas, out infoMetadata, out mensajeError);
                        if (ok && nuevas != null && nuevas.Count >= 5)
                        {
                                Guardar(nuevas);
                                UltimaFuenteMetadata = "ARCA Oficial (Web)";
                                UltimaVigenciaMetadata = infoMetadata;

                                if (Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null && !string.IsNullOrEmpty(infoMetadata))
                                {
                                        Lfx.Workspace.Master.CurrentConfig.WriteGlobalSetting("Sistema.Monotributo.UltimaMetadata", infoMetadata);
                                }
                                return true;
                        }

                        if (string.IsNullOrEmpty(mensajeError))
                                mensajeError = "No se pudieron obtener suficientes categorías de la web oficial de AFIP.";

                        return false;
                }

                /// <summary>
                /// Sincroniza las escalas vigentes consultando la Tax API de Servidos (servidos.ar/developers).
                /// Utiliza el endpoint GET https://api.servidos.ar/api/v1/tax/monotributo/categories?activity_type={activityType}
                /// </summary>
                public bool SincronizarServidosApi(string apiKey, string activityType, out string infoMetadata, out string mensajeError)
                {
                        infoMetadata = "";
                        mensajeError = "";

                        if (string.IsNullOrEmpty(apiKey))
                        {
                                mensajeError = "Debe proporcionar una API Key válida de Servidos.";
                                return false;
                        }

                        string actType = (activityType ?? "servicios").Trim().ToLowerInvariant();
                        if (actType != "comercio" && actType != "servicios")
                                actType = "servicios";

                        string url = "https://api.servidos.ar/api/v1/tax/monotributo/categories?activity_type=" + actType;

                        try
                        {
                                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                                request.Method = "GET";
                                request.Headers.Add("x-api-key", apiKey.Trim());
                                request.Accept = "application/json";
                                request.Timeout = 15000;

                                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                                using (Stream stream = response.GetResponseStream())
                                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                                {
                                        string json = reader.ReadToEnd();
                                        return ActualizarDesdeServidosJson(json, actType, out infoMetadata, out mensajeError);
                                }
                        }
                        catch (WebException wex)
                        {
                                string detalle = "";
                                try
                                {
                                        if (wex.Response != null)
                                        {
                                                using (Stream stream = wex.Response.GetResponseStream())
                                                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                                                {
                                                        detalle = reader.ReadToEnd();
                                                }
                                        }
                                }
                                catch { }

                                if (wex.Status == WebExceptionStatus.ProtocolError && wex.Response is HttpWebResponse httpResp)
                                {
                                        int code = (int)httpResp.StatusCode;
                                        if (code == 401 || code == 403)
                                                mensajeError = "API Key no autorizada o inválida (código HTTP " + code + "). Verifique su clave de servidos.ar.";
                                        else if (code == 429)
                                                mensajeError = "Se ha superado el límite de peticiones de su API Key (500/mes en el nivel gratuito).";
                                        else
                                                mensajeError = "Error HTTP " + code + " al consultar la API de Servidos: " + wex.Message;
                                }
                                else
                                {
                                        mensajeError = "Error de red al consultar api.servidos.ar: " + wex.Message;
                                }

                                if (!string.IsNullOrEmpty(detalle) && detalle.Length < 300)
                                        mensajeError += "\r\nDetalle del servidor: " + detalle;

                                return false;
                        }
                        catch (Exception ex)
                        {
                                mensajeError = "Error al procesar la sincronización: " + ex.Message;
                                return false;
                        }
                }

                /// <summary>
                /// Parsea la respuesta JSON oficial devuelta por la API de Servidos.
                /// </summary>
                public bool ActualizarDesdeServidosJson(string json, string tipoActividad, out string infoMetadata, out string mensajeError)
                {
                        infoMetadata = "";
                        mensajeError = "";

                        if (string.IsNullOrEmpty(json))
                        {
                                mensajeError = "La respuesta de la API está vacía.";
                                return false;
                        }

                        try
                        {
                                using (JsonDocument doc = JsonDocument.Parse(json))
                                {
                                        JsonElement root = doc.RootElement;
                                        JsonElement dataElem;

                                        if (root.TryGetProperty("data", out dataElem))
                                        {
                                                if (dataElem.TryGetProperty("metadata", out JsonElement metaElem))
                                                {
                                                        string fuente = metaElem.TryGetProperty("source", out var s) ? s.GetString() : "ARCA";
                                                        string validFrom = metaElem.TryGetProperty("validFrom", out var vf) ? vf.GetString() : "";
                                                        string validUntil = metaElem.TryGetProperty("validUntil", out var vu) ? vu.GetString() : "";

                                                        infoMetadata = string.Format("Fuente: {0} | Vigencia: {1} a {2}", fuente, validFrom, validUntil);
                                                        UltimaFuenteMetadata = fuente;
                                                        UltimaVigenciaMetadata = infoMetadata;
                                                }

                                                if (dataElem.TryGetProperty("categories", out JsonElement catArray))
                                                {
                                                        List<CategoriaMonotributo> nuevas = new List<CategoriaMonotributo>();
                                                        foreach (JsonElement catElem in catArray.EnumerateArray())
                                                        {
                                                                string letra = catElem.TryGetProperty("category", out var c) ? c.GetString() : "";
                                                                decimal maxAnnual = catElem.TryGetProperty("maxAnnual", out var m) ? m.GetDecimal() : 0m;
                                                                decimal monthlyPayment = catElem.TryGetProperty("monthlyPayment", out var p) ? p.GetDecimal() : 0m;

                                                                decimal cuotaServ = (tipoActividad == "comercio") ? 0m : monthlyPayment;
                                                                decimal cuotaBien = (tipoActividad == "comercio") ? monthlyPayment : 0m;

                                                                if (!string.IsNullOrEmpty(letra) && maxAnnual > 0)
                                                                {
                                                                        nuevas.Add(new CategoriaMonotributo(letra, maxAnnual, cuotaServ, cuotaBien));
                                                                }
                                                        }

                                                        if (nuevas.Count >= 5)
                                                        {
                                                                Guardar(nuevas);
                                                                if (Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null && !string.IsNullOrEmpty(infoMetadata))
                                                                {
                                                                        Lfx.Workspace.Master.CurrentConfig.WriteGlobalSetting("Sistema.Monotributo.UltimaMetadata", infoMetadata);
                                                                }
                                                                return true;
                                                        }
                                                }
                                        }
                                }
                        }
                        catch (Exception ex)
                        {
                                mensajeError = "No se pudo interpretar el formato JSON de Servidos: " + ex.Message;
                                return false;
                        }

                        // Fallback a parser genérico si el formato no coincidió
                        if (ActualizarDesdeJson(json))
                                return true;

                        mensajeError = "No se encontraron categorías válidas en la respuesta de la API.";
                        return false;
                }
        }
}
