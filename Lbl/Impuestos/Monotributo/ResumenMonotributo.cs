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
                public decimal PorcentajeTopeProyectado { get; set; }
                public decimal TopeAnteriorCategoria { get; set; }
                public bool EsUltimaCategoria { get; set; }
                public string SiguienteCategoriaLetra { get; set; }
                public bool ExcluidoActual { get; set; }
                public bool ExcluidoProyectado { get; set; }

                // Categoría inscripta en ARCA / AFIP (opcional)
                public CategoriaMonotributo CategoriaInscripta { get; set; }
                public decimal MargenCategoriaInscripta { get; set; }
                public decimal PorcentajeCategoriaInscripta { get; set; }
                public bool SuperoCategoriaInscripta { get; set; }
                public decimal MargenProyeccionCategoriaInscripta { get; set; }

                // Métricas de Oportunidad de Baja de Categoría
                public bool HayOportunidadBaja { get; set; }
                public CategoriaMonotributo CategoriaBaja { get; set; }
                public decimal MargenParaPasarseDeBajaProyectado { get; set; }
                public decimal MargenParaPasarseDeBajaPeriodo { get; set; }
                public decimal PorcentajeConsumoBajaProyectado { get; set; }
                public decimal PorcentajeConsumoBajaPeriodo { get; set; }
                public decimal AhorroEstimadoMensualBaja { get; set; }

                // Métricas de Planificación Operativa (Meses restantes, límites de facturación segura)
                public int MesesRestantes
                {
                        get
                        {
                                if (DiasRestantes <= 0) return 0;
                                return (int)Math.Max(1, Math.Ceiling(DiasRestantes / 30.0));
                        }
                }

                public decimal LimiteMensualCategoriaInscripta
                {
                        get
                        {
                                if (DiasRestantes <= 0 || MargenCategoriaInscripta <= 0m) return 0m;
                                decimal meses = (decimal)DiasRestantes / 30.0m;
                                return meses > 0m ? (MargenCategoriaInscripta / meses) : 0m;
                        }
                }

                public decimal LimiteDiarioCategoriaInscripta
                {
                        get
                        {
                                if (DiasRestantes <= 0 || MargenCategoriaInscripta <= 0m) return 0m;
                                return MargenCategoriaInscripta / (decimal)DiasRestantes;
                        }
                }

                public decimal LimiteMensualBaja
                {
                        get
                        {
                                if (DiasRestantes <= 0 || MargenParaPasarseDeBajaPeriodo <= 0m) return 0m;
                                decimal meses = (decimal)DiasRestantes / 30.0m;
                                return meses > 0m ? (MargenParaPasarseDeBajaPeriodo / meses) : 0m;
                        }
                }

                public decimal LimiteDiarioBaja
                {
                        get
                        {
                                if (DiasRestantes <= 0 || MargenParaPasarseDeBajaPeriodo <= 0m) return 0m;
                                return MargenParaPasarseDeBajaPeriodo / (decimal)DiasRestantes;
                        }
                }

                // 7. Desglose mensual
                public List<MesFacturacion> DetalleMensual { get; set; }

                public ResumenMonotributo()
                {
                        FechaCalculo = DateTime.Now;
                        DetalleMensual = new List<MesFacturacion>();
                        SiguienteCategoriaLetra = "";
                        HayOportunidadBaja = false;
                        MargenParaPasarseDeBajaProyectado = 0m;
                        MargenParaPasarseDeBajaPeriodo = 0m;
                        PorcentajeConsumoBajaProyectado = 0m;
                        PorcentajeConsumoBajaPeriodo = 0m;
                        AhorroEstimadoMensualBaja = 0m;
                }

                /// <summary>
                /// Genera un resumen textual completo ideal para ToolTips o reportes rápidos.
                /// </summary>
                public string GenerarTextoInformativo()
                {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("==================================================");
                        sb.AppendLine("      SITUACIÓN MONOTRIBUTO (ARCA / AFIP)");
                        sb.AppendLine("==================================================");

                        sb.AppendLine("1. SITUACIÓN ACTUAL REGISTRADA EN AFIP (Última recategorización):");
                        if (CategoriaInscripta != null)
                        {
                                sb.AppendLine(string.Format("   • Categoría oficial registrada: Categoría {0}", CategoriaInscripta.Letra));
                                sb.AppendLine(string.Format("   • Tope máximo anual de Cat. {0}: {1}", CategoriaInscripta.Letra, FormatoMoneda.Formatear(CategoriaInscripta.IngresosBrutosMaximos)));
                                if (SuperoCategoriaInscripta)
                                {
                                        sb.AppendLine(string.Format("   • Consumido de Cat. {0}: {1:N1}% (¡SUPERADO por {2}!)", CategoriaInscripta.Letra, PorcentajeCategoriaInscripta, FormatoMoneda.Formatear(Math.Abs(FacturadoPeriodoRecat - CategoriaInscripta.IngresosBrutosMaximos))));
                                }
                                else
                                {
                                        sb.AppendLine(string.Format("   • Consumido de Cat. {0}: {1:N1}% ({2} restante antes de superar)", CategoriaInscripta.Letra, PorcentajeCategoriaInscripta, FormatoMoneda.Formatear(MargenCategoriaInscripta)));
                                }
                        }
                        else
                        {
                                sb.AppendLine("   • Categoría oficial en AFIP: No verificada (Haga clic en 'Consultar en ARCA / AFIP' para verificarla).");
                        }

                        sb.AppendLine();
                        sb.AppendLine("2. FACTURACIÓN DEL PERÍODO:");
                        if (PeriodoRecat != null)
                        {
                                sb.AppendLine(string.Format("   • Próxima recategorización: {0}", PeriodoRecat.Nombre));
                                sb.AppendLine(string.Format("   • Período evaluado: {0:dd/MM/yyyy} al {1:dd/MM/yyyy}", PeriodoRecat.FechaInicioPeriodo, PeriodoRecat.FechaFinPeriodo));
                                sb.AppendLine(string.Format("   • Días transcurridos: {0} de {1} ({2:N0}% del período)", DiasTranscurridos, DiasTotales, PeriodoRecat.PorcentajeTranscurrido(FechaCalculo)));
                        }
                        sb.AppendLine(string.Format("   • Mes actual hasta hoy: {0}", FormatoMoneda.Formatear(FacturadoMesActual)));
                        sb.AppendLine(string.Format("   • Mes anterior completo: {0}", FormatoMoneda.Formatear(FacturadoMesAnterior)));
                        string etiquetaAcum = (PeriodoRecat != null && PeriodoRecat.FechaInicioPeriodo.Month == 1) ?
                                string.Format("   • Facturado acumulado {0} (AFIP): {1}", PeriodoRecat.FechaInicioPeriodo.Year, FormatoMoneda.Formatear(FacturadoPeriodoRecat)) :
                                string.Format("   • Facturado acumulado período (AFIP): {0}", FormatoMoneda.Formatear(FacturadoPeriodoRecat));
                        sb.AppendLine(etiquetaAcum);
                        sb.AppendLine(string.Format("   • Últimos 12 meses móviles: {0}", FormatoMoneda.Formatear(FacturadoUltimos12Meses)));

                        sb.AppendLine();
                        sb.AppendLine("3. PROYECCIÓN Y ENCUADRE (Próxima recategorización):");
                        sb.AppendLine(string.Format("   • Facturación anual PROYECTADA: {0}", FormatoMoneda.Formatear(FacturadoProyectado)));

                        if (ExcluidoProyectado)
                        {
                                sb.AppendLine("   [ATENCIÓN]: La proyección supera el tope máximo de la Categoría K.");
                                sb.AppendLine("     Riesgo de EXCLUSIÓN hacia Régimen General (Responsable Inscripto).");
                        }
                        else if (CategoriaProyectada != null)
                        {
                                if (EsUltimaCategoria)
                                {
                                        sb.AppendLine(string.Format("   • Encuadre proyectado: Categoría {0} (¡ÚLTIMA CATEGORÍA DEL RÉGIMEN!)", CategoriaProyectada.Letra));
                                        sb.AppendLine(string.Format("   • Tope anual Cat. {0}: {1}", CategoriaProyectada.Letra, FormatoMoneda.Formatear(CategoriaProyectada.IngresosBrutosMaximos)));
                                        sb.AppendLine(string.Format("   • Margen restante antes de EXCLUSIÓN: {0}", FormatoMoneda.Formatear(MargenProyectado)));
                                        sb.AppendLine("     [IMPORTANTE]: Al superar esta categoría se produce la EXCLUSIÓN del Monotributo.");
                                }
                                else
                                {
                                        sb.AppendLine(string.Format("   • Encuadre proyectado: Categoría {0} (Tope: {1})", CategoriaProyectada.Letra, FormatoMoneda.Formatear(CategoriaProyectada.IngresosBrutosMaximos)));
                                        sb.AppendLine(string.Format("   • Margen restante para pasar a Cat. {0}: {1}", SiguienteCategoriaLetra, FormatoMoneda.Formatear(MargenProyectado)));
                                }
                        }

                        // Diagnóstico de situación según categoría inscripta vs facturación/proyección
                        if (CategoriaInscripta != null)
                        {
                                sb.AppendLine();
                                sb.AppendLine("4. DIAGNÓSTICO Y PROYECCIÓN INFORMATIVA:");

                                if (SuperoCategoriaInscripta)
                                {
                                        sb.AppendLine(string.Format("   [!] TOPE SUPERADO EN PERÍODO: Has facturado {0}, superando el tope de Cat. {1} ({2}) por {3}.",
                                                FormatoMoneda.Formatear(FacturadoPeriodoRecat),
                                                CategoriaInscripta.Letra,
                                                FormatoMoneda.Formatear(CategoriaInscripta.IngresosBrutosMaximos),
                                                FormatoMoneda.Formatear(FacturadoPeriodoRecat - CategoriaInscripta.IngresosBrutosMaximos)));
                                        if (CategoriaActual != null && CategoriaActual.Letra != CategoriaInscripta.Letra)
                                        {
                                                sb.AppendLine(string.Format("      En la próxima recategorización deberás ascender obligatoriamente al menos a Categoría {0}.", CategoriaActual.Letra));
                                        }
                                        else
                                        {
                                                sb.AppendLine("      En la próxima recategorización deberás ascender de categoría para regularizar tu situación.");
                                        }
                                }
                                else if (ExcluidoProyectado)
                                {
                                        sb.AppendLine("   [!] RIESGO DE EXCLUSIÓN TOTAL: Tu facturación proyectada supera el tope de la Categoría K.");
                                        sb.AppendLine("      Riesgo de traspaso obligatorio a Régimen General (Responsable Inscripto: IVA y Ganancias).");
                                }
                                else if (CategoriaProyectada != null)
                                {
                                        int comp = string.Compare(CategoriaProyectada.Letra, CategoriaInscripta.Letra, StringComparison.OrdinalIgnoreCase);
                                        if (comp > 0)
                                        {
                                                sb.AppendLine(string.Format("   [ALERTA DE ASCENSO]: Proyectas Categoría {0} (superior a tu Cat. {1} inscripta).", CategoriaProyectada.Letra, CategoriaInscripta.Letra));
                                                sb.AppendLine(string.Format("      Tu ritmo de facturación requerirá subir a Categoría {0} en la próxima recategorización para evitar recategorización de oficio de ARCA.", CategoriaProyectada.Letra));
                                        }
                                        else if (comp < 0)
                                        {
                                                sb.AppendLine(string.Format("   [OPORTUNIDAD DE BAJA]: Tu facturación proyectada te ubica en Categoría {0} (inferior a tu Cat. {1} inscripta).", CategoriaProyectada.Letra, CategoriaInscripta.Letra));
                                                sb.AppendLine(string.Format("      • Tope máximo anual de Cat. {0} (límite para no superarla): {1}", CategoriaProyectada.Letra, FormatoMoneda.Formatear(CategoriaProyectada.IngresosBrutosMaximos)));
                                                sb.AppendLine(string.Format("      • Facturación proyectada: {0} ({1:N1}% consumido de Cat. {2})", FormatoMoneda.Formatear(FacturadoProyectado), PorcentajeConsumoBajaProyectado, CategoriaProyectada.Letra));
                                                sb.AppendLine(string.Format("      • Margen disponible para conservar la baja: Resta hasta {0} de facturación anual antes de superar el tope de Cat. {1}.", FormatoMoneda.Formatear(MargenParaPasarseDeBajaProyectado), CategoriaProyectada.Letra));
                                                sb.AppendLine(string.Format("        (Si facturas más de {0}, perderás la posibilidad de bajar a Cat. {1} y quedarás en Cat. {2} o superior).", FormatoMoneda.Formatear(MargenParaPasarseDeBajaProyectado), CategoriaProyectada.Letra, CategoriaInscripta.Letra));
                                                sb.AppendLine(string.Format("      • Acumulado real del período: {0} ({1:N1}% de Cat. {2}) • Restan {3} antes del tope de Cat. {2}.", FormatoMoneda.Formatear(FacturadoPeriodoRecat), PorcentajeConsumoBajaPeriodo, CategoriaProyectada.Letra, FormatoMoneda.Formatear(MargenParaPasarseDeBajaPeriodo)));
                                                if (AhorroEstimadoMensualBaja > 0)
                                                {
                                                        sb.AppendLine(string.Format("      • Ahorro estimado: Aproximadamente {0}/mes menos de cuota mensual al recategorizarte a Cat. {1}.", FormatoMoneda.Formatear(AhorroEstimadoMensualBaja), CategoriaProyectada.Letra));
                                                }
                                        }
                                        else
                                        {
                                                sb.AppendLine(string.Format("   [OK] CATEGORÍA ADECUADA: Tu facturación proyectada ({0}) se ajusta a tu Categoría {1} inscripta.", FormatoMoneda.Formatear(FacturadoProyectado), CategoriaInscripta.Letra));
                                                sb.AppendLine(string.Format("      • Margen antes de superar Cat. {0}: Resta facturar {1}.", CategoriaInscripta.Letra, FormatoMoneda.Formatear(MargenCategoriaInscripta)));
                                                if (CategoriaBaja != null)
                                                {
                                                        if (MargenParaPasarseDeBajaProyectado > 0)
                                                        {
                                                                sb.AppendLine(string.Format("      • Oportunidad de baja a Cat. {0} (tope {1}): Tu proyección está a {2} de entrar en Cat. {0}.",
                                                                        CategoriaBaja.Letra, FormatoMoneda.Formatear(CategoriaBaja.IngresosBrutosMaximos), FormatoMoneda.Formatear(MargenParaPasarseDeBajaProyectado)));
                                                        }
                                                        else
                                                        {
                                                                sb.AppendLine(string.Format("      • Categoría inferior Cat. {0} (tope {1}): Tu proyección la supera por {2}. Para poder bajar a Cat. {0}, tu facturación anual no debería superar {1}.",
                                                                        CategoriaBaja.Letra, FormatoMoneda.Formatear(CategoriaBaja.IngresosBrutosMaximos), FormatoMoneda.Formatear(Math.Abs(MargenParaPasarseDeBajaProyectado)), FormatoMoneda.Formatear(CategoriaBaja.IngresosBrutosMaximos)));
                                                        }
                                                }
                                        }
                                }
                        }

                        if (PeriodoRecat != null)
                        {
                                sb.AppendLine();
                                sb.AppendLine(string.Format("Vencimiento del trámite de recategorización: aprox. {0:dd/MM/yyyy}", PeriodoRecat.FechaVencimientoTramite));
                        }

                        sb.AppendLine();
                        sb.AppendLine("--------------------------------------------------");
                        sb.AppendLine("AVISO INFORMATIVO:");
                        sb.AppendLine("Esta información es una estimación orientativa interna del sistema");
                        sb.AppendLine("basada en los comprobantes registrados. No constituye asesoramiento profesional,");
                        sb.AppendLine("tributario ni contable. Consulte siempre a su profesional contable matriculado.");
                        sb.AppendLine("--------------------------------------------------");

                        return sb.ToString();
                }
        }
}
