using System;

namespace Lbl.Impuestos.Monotributo
{
        [Serializable]
        public class MesFacturacion
        {
                public int Anio { get; set; }
                public int Mes { get; set; }
                public decimal Facturas { get; set; }
                public decimal NotasCredito { get; set; }
                public decimal TotalNeto
                {
                        get
                        {
                                return Facturas - NotasCredito;
                        }
                }

                public string NombreMes
                {
                        get
                        {
                                try
                                {
                                        DateTime dt = new DateTime(Anio, Mes, 1);
                                        return dt.ToString("MMMM yyyy");
                                }
                                catch
                                {
                                        return string.Format("{0:00}/{1}", Mes, Anio);
                                }
                        }
                }

                public MesFacturacion()
                {
                }

                public MesFacturacion(int anio, int mes, decimal facturas, decimal notasCredito)
                {
                        this.Anio = anio;
                        this.Mes = mes;
                        this.Facturas = facturas;
                        this.NotasCredito = notasCredito;
                }
        }
}
