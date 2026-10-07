using System;

namespace Lbl.Impuestos.Monotributo
{
        /// <summary>
        /// Representa un período de recategorización semestral para monotributistas en Argentina (ARCA / AFIP).
        /// En Argentina existen dos recategorizaciones al año:
        /// 1. Recategorización de Julio: evalúa los 12 meses transcurridos entre el 1 de julio del año anterior y el 30 de junio del año en curso. Rige desde el 1 de agosto.
        /// 2. Recategorización de Enero: evalúa los 12 meses transcurridos entre el 1 de enero y el 31 de diciembre del año en curso. Rige desde el 1 de febrero.
        /// </summary>
        [Serializable]
        public class PeriodoRecategorizacion
        {
                public string Nombre { get; set; }
                public int MesRecategorizacion { get; set; } // 1 (Enero) o 7 (Julio)
                public int AnioRecategorizacion { get; set; }
                public DateTime FechaInicioPeriodo { get; set; }
                public DateTime FechaFinPeriodo { get; set; }
                public DateTime FechaVencimientoTramite { get; set; }
                public DateTime FechaInicioVigencia { get; set; }

                public int DiasTotales
                {
                        get
                        {
                                return Math.Max(1, (int)(FechaFinPeriodo.Date - FechaInicioPeriodo.Date).TotalDays + 1);
                        }
                }

                public int DiasTranscurridos(DateTime fechaActual)
                {
                        if (fechaActual < FechaInicioPeriodo)
                                return 0;
                        if (fechaActual >= FechaFinPeriodo)
                                return DiasTotales;
                        return Math.Max(1, (int)(fechaActual.Date - FechaInicioPeriodo.Date).TotalDays + 1);
                }

                public int DiasRestantes(DateTime fechaActual)
                {
                        if (fechaActual >= FechaFinPeriodo)
                                return 0;
                        return Math.Max(0, DiasTotales - DiasTranscurridos(fechaActual));
                }

                public decimal PorcentajeTranscurrido(DateTime fechaActual)
                {
                        int trans = DiasTranscurridos(fechaActual);
                        int total = DiasTotales;
                        if (total <= 0) return 100m;
                        decimal pct = (decimal)trans / (decimal)total * 100m;
                        return Math.Min(100m, Math.Max(0m, pct));
                }

                /// <summary>
                /// Determina el período de recategorización correspondiente para una fecha dada en Argentina.
                /// </summary>
                public static PeriodoRecategorizacion ObtenerParaFecha(DateTime fecha)
                {
                        PeriodoRecategorizacion periodo = new PeriodoRecategorizacion();

                        // En enero hasta el día 20 rige el trámite de recategorización del año calendario anterior
                        if (fecha.Month == 1 && fecha.Day <= 20)
                        {
                                periodo.Nombre = "Recategorización Enero " + fecha.Year.ToString();
                                periodo.MesRecategorizacion = 1;
                                periodo.AnioRecategorizacion = fecha.Year;
                                periodo.FechaInicioPeriodo = new DateTime(fecha.Year - 1, 1, 1);
                                periodo.FechaFinPeriodo = new DateTime(fecha.Year - 1, 12, 31);
                                periodo.FechaVencimientoTramite = new DateTime(fecha.Year, 1, 20);
                                periodo.FechaInicioVigencia = new DateTime(fecha.Year, 2, 1);
                        }
                        // Desde el 21 de enero hasta el 20 de julio (aproximadamente fin del trámite de julio),
                        // la recategorización objetivo es la de Julio del año en curso.
                        else if ((fecha.Month == 1 && fecha.Day > 20) || (fecha.Month >= 2 && fecha.Month <= 6) || (fecha.Month == 7 && fecha.Day <= 20))
                        {
                                periodo.Nombre = "Recategorización Julio " + fecha.Year.ToString();
                                periodo.MesRecategorizacion = 7;
                                periodo.AnioRecategorizacion = fecha.Year;
                                periodo.FechaInicioPeriodo = new DateTime(fecha.Year - 1, 7, 1);
                                periodo.FechaFinPeriodo = new DateTime(fecha.Year, 6, 30);
                                periodo.FechaVencimientoTramite = new DateTime(fecha.Year, 7, 20);
                                periodo.FechaInicioVigencia = new DateTime(fecha.Year, 8, 1);
                        }
                        // Desde el 21 de julio hasta el 31 de diciembre, la próxima recategorización es la de Enero del año siguiente.
                        else
                        {
                                int anioSiguiente = fecha.Year + 1;
                                periodo.Nombre = "Recategorización Enero " + anioSiguiente.ToString();
                                periodo.MesRecategorizacion = 1;
                                periodo.AnioRecategorizacion = anioSiguiente;
                                periodo.FechaInicioPeriodo = new DateTime(fecha.Year, 1, 1);
                                periodo.FechaFinPeriodo = new DateTime(fecha.Year, 12, 31);
                                periodo.FechaVencimientoTramite = new DateTime(anioSiguiente, 1, 20);
                                periodo.FechaInicioVigencia = new DateTime(anioSiguiente, 2, 1);
                        }

                        return periodo;
                }

                public override string ToString()
                {
                        return string.Format("{0} ({1:dd/MM/yyyy} a {2:dd/MM/yyyy})", Nombre, FechaInicioPeriodo, FechaFinPeriodo);
                }
        }
}
