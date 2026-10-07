using System;
using System.Collections.Generic;
using System.Text;

namespace Lbl.Impuestos.Monotributo
{
        /// <summary>
        /// Almacena el resultado consolidado del análisis de facturación y proyección de Monotributo.
        /// </summary>
        [Serializable]
        public class ResumenMonotributo
        {
                public DateTime FechaCalculo { get; set; }

                // 1. Facturado en el mes hasta el momento (MTD)
                public decimal FacturadoMesActual { get; set; }
                public DateTime InicioMesActual { get; set; }
                public DateTime FinMesActual { get; set; }

                // 2. Facturado el mes anterior completo
                public decimal FacturadoMesAnterior { get; set; }
                public DateTime InicioMesAnterior { get; set; }
                public DateTime FinMesAnterior { get; set; }

                // 3. Período de recategorización semestral y acumulado
                public PeriodoRecategorizacion PeriodoRecat { get; set; }
                public decimal FacturadoPeriodoRecat { get; set; }

                // 4. Últimos 12 meses móviles (rolling 12 months)
                public decimal FacturadoUltimos12Meses { get; set; }
                public DateTime InicioUltimos12Meses { get; set; }

                // 5. Facturación proyectada para la próxima recategorización
                public decimal FacturadoProyectado { get; set; }
                public int DiasTranscurridos { get; set; }
                public int DiasTotales { get; set; }
                public int DiasRestantes { get; set; }
                public decimal PromedioDiario { get; set; }
                public decimal PromedioMensual { get; set; }

                // 6. Categorías y encuadre
                public CategoriaMonotributo CategoriaActual { get; set; }
                public CategoriaMonotributo CategoriaProyectada { get; set; }
                public decimal MargenProyectado { get; set; }
                public decimal PorcentajeTramoActual { get; set; }
                public decimal TopeAnteriorCategoria { get; set; }
                public bool EsUltimaCategoria { get; set; }
                public string SiguienteCategoriaLetra { get; set; }
                public bool ExcluidoActual { get; set; }
                public bool ExcluidoProyectado { get; set; }

                // 7. Desglose mensual
                public List<MesFacturacion> DetalleMensual { get; set; }

                public ResumenMonotributo()
                {
                        FechaCalculo = DateTime.Now;
                        DetalleMensual = new List<MesFacturacion>();
                        SiguienteCategoriaLetra = "";
                }

                /// <summary>
                /// Genera un resumen textual completo ideal para ToolTips o reportes rápidos.
                /// </summary>
                public string GenerarTextoInformativo()
                {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("=== SITUACIÓN MONOTRIBUTO (ARCA / AFIP) ===");
                        if (PeriodoRecat != null)
                        {
                                sb.AppendLine(string.Format("Próxima: {0}", PeriodoRecat.Nombre));
                                sb.AppendLine(string.Format("Período evaluado: {0:dd/MM/yyyy} al {1:dd/MM/yyyy}", PeriodoRecat.FechaInicioPeriodo, PeriodoRecat.FechaFinPeriodo));
                                sb.AppendLine(string.Format("Días transcurridos: {0} de {1} ({2:N0}% del período)", DiasTranscurridos, DiasTotales, PeriodoRecat.PorcentajeTranscurrido(FechaCalculo)));
                        }
                        sb.AppendLine("--------------------------------------------------");
                        sb.AppendLine(string.Format("• Mes actual hasta hoy: {0}", FormatoMoneda.Formatear(FacturadoMesActual)));
                        sb.AppendLine(string.Format("• Mes anterior completo: {0}", FormatoMoneda.Formatear(FacturadoMesAnterior)));
                        sb.AppendLine(string.Format("• Acumulado período recategorización: {0}", FormatoMoneda.Formatear(FacturadoPeriodoRecat)));
                        sb.AppendLine(string.Format("• Últimos 12 meses móviles: {0}", FormatoMoneda.Formatear(FacturadoUltimos12Meses)));
                        sb.AppendLine("--------------------------------------------------");
                        sb.AppendLine(string.Format("• Total PROYECTADO recategorización: {0}", FormatoMoneda.Formatear(FacturadoProyectado)));

                        if (ExcluidoProyectado)
                        {
                                sb.AppendLine("⚠ ATENCIÓN: La proyección supera el tope máximo de la Categoría K.");
                                sb.AppendLine("  Riesgo de EXCLUSIÓN hacia Régimen General (Responsable Inscripto).");
                        }
                        else if (CategoriaProyectada != null)
                        {
                                if (EsUltimaCategoria)
                                {
                                        sb.AppendLine(string.Format("• Categoría proyectada: Categoría {0} (¡ÚLTIMA CATEGORÍA DEL RÉGIMEN!)", CategoriaProyectada.Letra));
                                        sb.AppendLine(string.Format("• Progreso en tramo Cat. {0}: {1:N0}% completado ({2:N0}% restante)", CategoriaProyectada.Letra, PorcentajeTramoActual, Math.Max(0m, 100m - PorcentajeTramoActual)));
                                        sb.AppendLine(string.Format("• Margen restante antes de EXCLUSIÓN: {0}", FormatoMoneda.Formatear(MargenProyectado)));
                                        sb.AppendLine("  ⚠ IMPORTANTE: Al superar esta categoría se produce la EXCLUSIÓN del Monotributo.");
                                }
                                else
                                {
                                        sb.AppendLine(string.Format("• Categoría proyectada: Categoría {0} (Tope: {1})", CategoriaProyectada.Letra, FormatoMoneda.Formatear(CategoriaProyectada.IngresosBrutosMaximos)));
                                        sb.AppendLine(string.Format("• Progreso en tramo Cat. {0}: {1:N0}% completado", CategoriaProyectada.Letra, PorcentajeTramoActual));
                                        sb.AppendLine(string.Format("• Resta para pasar a Cat. {0}: {1} ({2:N0}% restante)", SiguienteCategoriaLetra, FormatoMoneda.Formatear(MargenProyectado), Math.Max(0m, 100m - PorcentajeTramoActual)));
                                }
                        }

                        if (PeriodoRecat != null)
                        {
                                sb.AppendLine(string.Format("Vencimiento del trámite: aprox. {0:dd/MM/yyyy}", PeriodoRecat.FechaVencimientoTramite));
                        }

                        return sb.ToString();
                }
        }
}
