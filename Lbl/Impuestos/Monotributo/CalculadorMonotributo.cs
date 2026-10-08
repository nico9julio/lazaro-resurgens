using System;
using System.Collections.Generic;

namespace Lbl.Impuestos.Monotributo
{
        /// <summary>
        /// Realiza las consultas y cálculos fiscales para determinar la facturación devengada,
        /// la facturación proyectada y el encuadre de categorías de Monotributo para ARCA / AFIP.
        /// </summary>
        public static class CalculadorMonotributo
        {
                /// <summary>
                /// Tipos de comprobantes de venta que suman a la facturación fiscal.
                /// Facturas A, B, C, E, M y Notas de Débito A, B, C, E, M.
                /// </summary>
                public const string SqlTiposFacturaVenta = "'FA', 'FB', 'FC', 'FE', 'FM', 'NDA', 'NDB', 'NDC', 'NDE', 'NDM'";

                /// <summary>
                /// Tipos de notas de crédito que restan de la facturación fiscal.
                /// Notas de Crédito A, B, C, E, M.
                /// </summary>
                public const string SqlTiposNotaCreditoVenta = "'NCA', 'NCB', 'NCC', 'NCE', 'NCM'";

                /// <summary>
                /// Ejecuta el cálculo integral de facturación de Monotributo.
                /// </summary>
                public static ResumenMonotributo Calcular(Lfx.Data.IConnection conn, DateTime? fechaReferencia = null)
                {
                        DateTime hoy = fechaReferencia ?? DateTime.Now;
                        ResumenMonotributo resumen = new ResumenMonotributo();
                        resumen.FechaCalculo = hoy;

                        if (conn == null)
                                return resumen;

                        try
                        {
                                // 1. Mes actual hasta el momento (MTD)
                                resumen.InicioMesActual = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0);
                                resumen.FinMesActual = hoy;
                                resumen.FacturadoMesActual = ObtenerFacturacionNeta(conn, resumen.InicioMesActual, resumen.FinMesActual);

                                // 2. Mes anterior completo
                                DateTime mesAnt = hoy.AddMonths(-1);
                                resumen.InicioMesAnterior = new DateTime(mesAnt.Year, mesAnt.Month, 1, 0, 0, 0);
                                int ultDiaMesAnt = DateTime.DaysInMonth(mesAnt.Year, mesAnt.Month);
                                resumen.FinMesAnterior = new DateTime(mesAnt.Year, mesAnt.Month, ultDiaMesAnt, 23, 59, 59);
                                resumen.FacturadoMesAnterior = ObtenerFacturacionNeta(conn, resumen.InicioMesAnterior, resumen.FinMesAnterior);

                                // 3. Período de recategorización semestral
                                PeriodoRecategorizacion periodo = PeriodoRecategorizacion.ObtenerParaFecha(hoy);
                                resumen.PeriodoRecat = periodo;

                                DateTime fechaCorteRecat = (hoy < periodo.FechaFinPeriodo) ? hoy : periodo.FechaFinPeriodo;
                                resumen.FacturadoPeriodoRecat = ObtenerFacturacionNeta(conn, periodo.FechaInicioPeriodo, fechaCorteRecat);

                                // 4. Últimos 12 meses móviles (rolling 12 months)
                                // Desde el primer día de hace 11 meses hasta hoy (abarca 12 meses calendarios)
                                DateTime hace11Meses = hoy.AddMonths(-11);
                                resumen.InicioUltimos12Meses = new DateTime(hace11Meses.Year, hace11Meses.Month, 1, 0, 0, 0);
                                resumen.FacturadoUltimos12Meses = ObtenerFacturacionNeta(conn, resumen.InicioUltimos12Meses, hoy);

                                // 5. Proyección para la próxima recategorización
                                int diasTrans = periodo.DiasTranscurridos(hoy);
                                int diasTot = periodo.DiasTotales;
                                int diasRest = periodo.DiasRestantes(hoy);

                                resumen.DiasTranscurridos = diasTrans;
                                resumen.DiasTotales = diasTot;
                                resumen.DiasRestantes = diasRest;

                                if (diasTrans >= diasTot)
                                {
                                        // Período ya finalizado (por ejemplo durante la ventana de recategorización de julio o enero)
                                        resumen.FacturadoProyectado = resumen.FacturadoPeriodoRecat;
                                        resumen.PromedioDiario = (diasTot > 0) ? (resumen.FacturadoPeriodoRecat / diasTot) : 0m;
                                        resumen.PromedioMensual = resumen.PromedioDiario * 30.416m;
                                }
                                else if (diasTrans > 0)
                                {
                                        resumen.PromedioDiario = resumen.FacturadoPeriodoRecat / (decimal)diasTrans;
                                        resumen.PromedioMensual = resumen.PromedioDiario * 30.416m;
                                        // Proyección = Facturado acumulado + (promedio diario * días restantes del período de 12 meses)
                                        resumen.FacturadoProyectado = resumen.FacturadoPeriodoRecat + (resumen.PromedioDiario * (decimal)diasRest);
                                }
                                else
                                {
                                        resumen.FacturadoProyectado = 0m;
                                        resumen.PromedioDiario = 0m;
                                        resumen.PromedioMensual = 0m;
                                }

                                // 6. Categorías, encuadre y progreso porcentual
                                EscalasMonotributo escalas = EscalasMonotributo.Instancia;
                                escalas.Cargar();
                                resumen.CategoriaActual = escalas.ObtenerCategoriaParaMonto(resumen.FacturadoPeriodoRecat);
                                resumen.ExcluidoActual = escalas.EstaExcluido(resumen.FacturadoPeriodoRecat);

                                CategoriaMonotributo catProy;
                                decimal topeAnt, topeAct, pctTramo;
                                bool esUlt;
                                string sigLetra;

                                escalas.CalcularProgresoCategoria(resumen.FacturadoProyectado, out catProy, out topeAnt, out topeAct, out pctTramo, out esUlt, out sigLetra);

                                resumen.CategoriaProyectada = catProy;
                                resumen.TopeAnteriorCategoria = topeAnt;
                                resumen.PorcentajeTramoActual = pctTramo;
                                resumen.EsUltimaCategoria = esUlt;
                                resumen.SiguienteCategoriaLetra = sigLetra;
                                resumen.ExcluidoProyectado = escalas.EstaExcluido(resumen.FacturadoProyectado);

                                if (resumen.CategoriaProyectada != null)
                                {
                                        resumen.MargenProyectado = Math.Max(0m, resumen.CategoriaProyectada.IngresosBrutosMaximos - resumen.FacturadoProyectado);
                                        if (resumen.CategoriaProyectada.IngresosBrutosMaximos > 0m)
                                        {
                                                resumen.PorcentajeTopeProyectado = Math.Min(100m, (resumen.FacturadoProyectado / resumen.CategoriaProyectada.IngresosBrutosMaximos) * 100m);
                                        }
                                }
                                else
                                {
                                        resumen.MargenProyectado = 0m;
                                        resumen.PorcentajeTopeProyectado = 100m;
                                }

                                // 6b. Categoría inscripta registrada en AFIP (si está configurada)
                                string catLetraInscripta = "";
                                if (Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null)
                                {
                                        catLetraInscripta = Lfx.Workspace.Master.CurrentConfig.ReadGlobalSetting<string>("Sistema.Monotributo.CategoriaInscripta", "");
                                }

                                if (!string.IsNullOrEmpty(catLetraInscripta) && catLetraInscripta != "*" && !catLetraInscripta.Equals("auto", StringComparison.OrdinalIgnoreCase))
                                {
                                        string catLimpia = escalas.NormalizarLetra(catLetraInscripta);
                                        if (!string.IsNullOrEmpty(catLimpia))
                                        {
                                                if (catLetraInscripta != catLimpia && Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null)
                                                {
                                                        Lfx.Workspace.Master.CurrentConfig.WriteGlobalSetting("Sistema.Monotributo.CategoriaInscripta", catLimpia);
                                                }
                                                catLetraInscripta = catLimpia;
                                        }

                                        resumen.CategoriaInscripta = escalas.ObtenerCategoria(catLetraInscripta);
                                        if (resumen.CategoriaInscripta != null)
                                        {
                                                resumen.MargenCategoriaInscripta = resumen.CategoriaInscripta.IngresosBrutosMaximos - resumen.FacturadoPeriodoRecat;
                                                if (resumen.CategoriaInscripta.IngresosBrutosMaximos > 0m)
                                                {
                                                        resumen.PorcentajeCategoriaInscripta = (resumen.FacturadoPeriodoRecat / resumen.CategoriaInscripta.IngresosBrutosMaximos) * 100m;
                                                }
                                                resumen.SuperoCategoriaInscripta = resumen.FacturadoPeriodoRecat > resumen.CategoriaInscripta.IngresosBrutosMaximos;
                                                resumen.MargenProyeccionCategoriaInscripta = resumen.CategoriaInscripta.IngresosBrutosMaximos - resumen.FacturadoProyectado;

                                                // 6c. Análisis de baja de categoría u oportunidad de ahorro
                                                if (resumen.CategoriaProyectada != null)
                                                {
                                                        int comp = string.Compare(resumen.CategoriaProyectada.Letra, resumen.CategoriaInscripta.Letra, StringComparison.OrdinalIgnoreCase);
                                                        if (comp < 0)
                                                        {
                                                                resumen.HayOportunidadBaja = true;
                                                                resumen.CategoriaBaja = resumen.CategoriaProyectada;
                                                                resumen.MargenParaPasarseDeBajaProyectado = Math.Max(0m, resumen.CategoriaBaja.IngresosBrutosMaximos - resumen.FacturadoProyectado);
                                                                resumen.MargenParaPasarseDeBajaPeriodo = Math.Max(0m, resumen.CategoriaBaja.IngresosBrutosMaximos - resumen.FacturadoPeriodoRecat);
                                                                if (resumen.CategoriaBaja.IngresosBrutosMaximos > 0m)
                                                                {
                                                                        resumen.PorcentajeConsumoBajaProyectado = Math.Min(100m, (resumen.FacturadoProyectado / resumen.CategoriaBaja.IngresosBrutosMaximos) * 100m);
                                                                        resumen.PorcentajeConsumoBajaPeriodo = Math.Min(100m, (resumen.FacturadoPeriodoRecat / resumen.CategoriaBaja.IngresosBrutosMaximos) * 100m);
                                                                }
                                                                resumen.AhorroEstimadoMensualBaja = Math.Max(0m, resumen.CategoriaInscripta.CuotaServicios - resumen.CategoriaBaja.CuotaServicios);
                                                        }
                                                        else
                                                        {
                                                                var catInf = escalas.ObtenerCategoriaAnterior(resumen.CategoriaInscripta.Letra);
                                                                if (catInf != null)
                                                                {
                                                                        resumen.CategoriaBaja = catInf;
                                                                        resumen.MargenParaPasarseDeBajaProyectado = catInf.IngresosBrutosMaximos - resumen.FacturadoProyectado;
                                                                        resumen.MargenParaPasarseDeBajaPeriodo = catInf.IngresosBrutosMaximos - resumen.FacturadoPeriodoRecat;
                                                                        if (catInf.IngresosBrutosMaximos > 0m)
                                                                        {
                                                                                resumen.PorcentajeConsumoBajaProyectado = (resumen.FacturadoProyectado / catInf.IngresosBrutosMaximos) * 100m;
                                                                                resumen.PorcentajeConsumoBajaPeriodo = (resumen.FacturadoPeriodoRecat / catInf.IngresosBrutosMaximos) * 100m;
                                                                        }
                                                                }
                                                        }
                                                }
                                        }
                                }

                                // 7. Desglose mensual para los últimos 12 meses
                                for (int i = 11; i >= 0; i--)
                                {
                                        DateTime m = hoy.AddMonths(-i);
                                        DateTime mInicio = new DateTime(m.Year, m.Month, 1, 0, 0, 0);
                                        int mUltDia = DateTime.DaysInMonth(m.Year, m.Month);
                                        DateTime mFin = (i == 0) ? hoy : new DateTime(m.Year, m.Month, mUltDia, 23, 59, 59);

                                        decimal fFac = ObtenerTotalFacturas(conn, mInicio, mFin);
                                        decimal fNc = ObtenerTotalNotasCredito(conn, mInicio, mFin);

                                        resumen.DetalleMensual.Add(new MesFacturacion(m.Year, m.Month, fFac, fNc));
                                }
                        }
                        catch (Exception ex)
                        {
                                System.Diagnostics.Trace.WriteLine("Error al calcular situación de Monotributo: " + ex.Message);
                        }

                        return resumen;
                }

                /// <summary>
                /// Obtiene la facturación neta (Facturas - Notas de Crédito) emitida entre dos fechas dadas.
                /// </summary>
                public static decimal ObtenerFacturacionNeta(Lfx.Data.IConnection conn, DateTime desde, DateTime hasta)
                {
                        decimal facturas = ObtenerTotalFacturas(conn, desde, hasta);
                        decimal notasCredito = ObtenerTotalNotasCredito(conn, desde, hasta);
                        return facturas - notasCredito;
                }

                public static decimal ObtenerTotalFacturas(Lfx.Data.IConnection conn, DateTime desde, DateTime hasta)
                {
                        if (conn == null) return 0m;
                        string f1 = desde.ToString("yyyy-MM-dd") + " 00:00:00";
                        string f2 = hasta.ToString("yyyy-MM-dd") + " 23:59:59";

                        string sql = string.Format(
                                "SELECT SUM(total) FROM comprob WHERE tipo_fac IN ({0}) AND compra=0 AND impresa>0 AND anulada=0 AND fecha BETWEEN '{1}' AND '{2}'",
                                SqlTiposFacturaVenta, f1, f2);

                        try
                        {
                                return conn.FieldDecimal(sql);
                        }
                        catch
                        {
                                return 0m;
                        }
                }

                public static decimal ObtenerTotalNotasCredito(Lfx.Data.IConnection conn, DateTime desde, DateTime hasta)
                {
                        if (conn == null) return 0m;
                        string f1 = desde.ToString("yyyy-MM-dd") + " 00:00:00";
                        string f2 = hasta.ToString("yyyy-MM-dd") + " 23:59:59";

                        string sql = string.Format(
                                "SELECT SUM(total) FROM comprob WHERE tipo_fac IN ({0}) AND compra=0 AND impresa>0 AND anulada=0 AND fecha BETWEEN '{1}' AND '{2}'",
                                SqlTiposNotaCreditoVenta, f1, f2);

                        try
                        {
                                return conn.FieldDecimal(sql);
                        }
                        catch
                        {
                                return 0m;
                        }
                }
        }
}
