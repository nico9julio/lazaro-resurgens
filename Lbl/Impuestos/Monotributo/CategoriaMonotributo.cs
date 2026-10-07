using System;

namespace Lbl.Impuestos.Monotributo
{
        [Serializable]
        public class CategoriaMonotributo
        {
                public string Letra { get; set; }
                public decimal IngresosBrutosMaximos { get; set; }
                public decimal CuotaServicios { get; set; }
                public decimal CuotaBienes { get; set; }

                public CategoriaMonotributo()
                {
                }

                public CategoriaMonotributo(string letra, decimal ingresosBrutosMaximos, decimal cuotaServicios = 0, decimal cuotaBienes = 0)
                {
                        this.Letra = (letra ?? "").Trim().ToUpperInvariant();
                        this.IngresosBrutosMaximos = ingresosBrutosMaximos;
                        this.CuotaServicios = cuotaServicios;
                        this.CuotaBienes = cuotaBienes;
                }

                public override string ToString()
                {
                        return string.Format("Categoría {0} (Tope: {1:C2})", Letra, IngresosBrutosMaximos);
                }
        }
}
