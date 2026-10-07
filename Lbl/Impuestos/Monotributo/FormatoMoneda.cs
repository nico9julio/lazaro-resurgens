using System;
using System.Globalization;

namespace Lbl.Impuestos.Monotributo
{
        /// <summary>
        /// Provee formateo de montos monetarios en moneda argentina sin centavos
        /// y con separador de miles con puntos (ej: $ 1.234.568).
        /// </summary>
        public static class FormatoMoneda
        {
                private static readonly NumberFormatInfo s_FormatoPuntos;

                static FormatoMoneda()
                {
                        s_FormatoPuntos = new NumberFormatInfo
                        {
                                NumberGroupSeparator = ".",
                                NumberDecimalSeparator = ",",
                                NumberGroupSizes = new int[] { 3 },
                                NumberDecimalDigits = 0,
                                CurrencyGroupSeparator = ".",
                                CurrencyDecimalSeparator = ",",
                                CurrencyGroupSizes = new int[] { 3 },
                                CurrencyDecimalDigits = 0,
                                CurrencySymbol = "$"
                        };
                }

                /// <summary>
                /// Formatea un monto con símbolo de moneda, separador de miles con punto y sin centavos.
                /// Ejemplo: 1234567.89 -> "$ 1.234.568"
                /// </summary>
                public static string Formatear(decimal monto)
                {
                        decimal redondeado = Math.Round(monto, MidpointRounding.AwayFromZero);
                        if (redondeado < 0)
                                return "-$ " + Math.Abs(redondeado).ToString("#,##0", s_FormatoPuntos);
                        return "$ " + redondeado.ToString("#,##0", s_FormatoPuntos);
                }

                /// <summary>
                /// Formatea un monto numérico con separador de miles con punto y sin centavos (sin signo $).
                /// Ejemplo: 1234567.89 -> "1.234.568"
                /// </summary>
                public static string FormatearNumero(decimal monto)
                {
                        decimal redondeado = Math.Round(monto, MidpointRounding.AwayFromZero);
                        if (redondeado < 0)
                                return "-" + Math.Abs(redondeado).ToString("#,##0", s_FormatoPuntos);
                        return redondeado.ToString("#,##0", s_FormatoPuntos);
                }

                public static NumberFormatInfo FormatoInfo
                {
                        get { return s_FormatoPuntos; }
                }
        }
}
