using System;
using NUnit.Framework;
using Assert = NUnit.Framework.Legacy.ClassicAssert;
using Lbl.Impuestos.Monotributo;

namespace Lbl.Test.Entity
{
        [TestFixture]
        public class MonotributoTest
        {
                [Test]
                public void PeriodoRecategorizacion_PrimerSemestre_ApuntaAJulio()
                {
                        DateTime fecha = new DateTime(2026, 3, 15);
                        PeriodoRecategorizacion periodo = PeriodoRecategorizacion.ObtenerParaFecha(fecha);

                        Assert.AreEqual(7, periodo.MesRecategorizacion);
                        Assert.AreEqual(2026, periodo.AnioRecategorizacion);
                        Assert.AreEqual(new DateTime(2025, 7, 1), periodo.FechaInicioPeriodo);
                        Assert.AreEqual(new DateTime(2026, 6, 30), periodo.FechaFinPeriodo);
                        Assert.AreEqual(new DateTime(2026, 7, 20), periodo.FechaVencimientoTramite);
                }

                [Test]
                public void PeriodoRecategorizacion_SegundoSemestre_ApuntaAEnero()
                {
                        DateTime fecha = new DateTime(2026, 10, 7);
                        PeriodoRecategorizacion periodo = PeriodoRecategorizacion.ObtenerParaFecha(fecha);

                        Assert.AreEqual(1, periodo.MesRecategorizacion);
                        Assert.AreEqual(2027, periodo.AnioRecategorizacion);
                        Assert.AreEqual(new DateTime(2026, 1, 1), periodo.FechaInicioPeriodo);
                        Assert.AreEqual(new DateTime(2026, 12, 31), periodo.FechaFinPeriodo);
                        Assert.AreEqual(new DateTime(2027, 1, 20), periodo.FechaVencimientoTramite);
                }

                [Test]
                public void PeriodoRecategorizacion_DiasCalculadosCorrectamente()
                {
                        DateTime fecha = new DateTime(2026, 1, 10);
                        // Del 1 al 20 de enero rige el trámite para el período cerrado el 31 de dic de 2025
                        PeriodoRecategorizacion periodo = PeriodoRecategorizacion.ObtenerParaFecha(fecha);
                        Assert.AreEqual(new DateTime(2025, 1, 1), periodo.FechaInicioPeriodo);
                        Assert.AreEqual(new DateTime(2025, 12, 31), periodo.FechaFinPeriodo);
                        Assert.AreEqual(365, periodo.DiasTotales);
                        Assert.AreEqual(365, periodo.DiasTranscurridos(fecha));
                        Assert.AreEqual(0, periodo.DiasRestantes(fecha));
                }

                [Test]
                public void EscalasMonotributo_DeterminaCategoriasCorrectamente()
                {
                        EscalasMonotributo escalas = new EscalasMonotributo();
                        escalas.RestablecerPredeterminados();

                        Assert.AreEqual(11, escalas.Categorias.Count);

                        // Monto bajo entra en Cat A
                        var catA = escalas.ObtenerCategoriaParaMonto(5000000m);
                        Assert.IsNotNull(catA);
                        Assert.AreEqual("A", catA.Letra);

                        // Monto intermedio entra en Cat B
                        var catB = escalas.ObtenerCategoriaParaMonto(10000000m);
                        Assert.IsNotNull(catB);
                        Assert.AreEqual("B", catB.Letra);

                        // Monto que entra en Cat C
                        var catC = escalas.ObtenerCategoriaParaMonto(15000000m);
                        Assert.IsNotNull(catC);
                        Assert.AreEqual("C", catC.Letra);

                        // Monto que supera el tope máximo de K (queda excluido)
                        var catExcluida = escalas.ObtenerCategoriaParaMonto(150000000m);
                        Assert.IsNull(catExcluida);
                        Assert.IsTrue(escalas.EstaExcluido(150000000m));
                }

                [Test]
                public void ProyeccionFacturacion_CalculoProrrateado()
                {
                        // Simulamos cálculo de proyección para un período de 365 días donde transcurrieron 100 días y se facturaron $2.000.000
                        decimal facturadoAcum = 2000000m;
                        int diasTrans = 100;
                        int diasTot = 365;
                        int diasRest = diasTot - diasTrans;

                        decimal promDiario = facturadoAcum / diasTrans; // 20.000/día
                        decimal proyectado = facturadoAcum + (promDiario * diasRest); // 2.000.000 + 5.300.000 = 7.300.000

                        Assert.AreEqual(7300000m, proyectado);

                        EscalasMonotributo escalas = new EscalasMonotributo();
                        escalas.RestablecerPredeterminados();
                        var catProy = escalas.ObtenerCategoriaParaMonto(proyectado);

                        Assert.IsNotNull(catProy);
                        Assert.AreEqual("A", catProy.Letra); // Tope de A es ~8.992.597,87
                        decimal margen = catProy.IngresosBrutosMaximos - proyectado;
                        Assert.IsTrue(margen > 0);
                }

                [Test]
                public void FormatoMoneda_FormateaConPuntosParaMilesYSinCentavos()
                {
                        decimal monto = 1234567.89m;
                        string fmt = FormatoMoneda.Formatear(monto);
                        Assert.AreEqual("$ 1.234.568", fmt);

                        string num = FormatoMoneda.FormatearNumero(monto);
                        Assert.AreEqual("1.234.568", num);

                        Assert.AreEqual("$ 0", FormatoMoneda.Formatear(0m));
                }

                [Test]
                public void EscalasMonotributo_CalculoProgresoTramoYUltimaCategoria()
                {
                        EscalasMonotributo escalas = new EscalasMonotributo();
                        escalas.RestablecerPredeterminados();

                        // Categoría A: de 0 a ~8.992.597,87
                        decimal topeA = escalas.ObtenerCategoria("A").IngresosBrutosMaximos;
                        CategoriaMonotributo cat;
                        decimal topeAnt, topeAct, pct;
                        bool esUlt;
                        string sig;

                        // Mitad de categoría A
                        escalas.CalcularProgresoCategoria(topeA / 2m, out cat, out topeAnt, out topeAct, out pct, out esUlt, out sig);
                        Assert.AreEqual("A", cat.Letra);
                        Assert.AreEqual(0m, topeAnt);
                        Assert.AreEqual(topeA, topeAct);
                        Assert.AreEqual(50m, Math.Round(pct, 0));
                        Assert.IsFalse(esUlt);
                        Assert.AreEqual("B", sig);

                        // Categoría K (última):
                        decimal topeK = escalas.ObtenerCategoria("K").IngresosBrutosMaximos;
                        decimal topeJ = escalas.ObtenerCategoria("J").IngresosBrutosMaximos;
                        decimal mitadK = topeJ + ((topeK - topeJ) / 2m);

                        escalas.CalcularProgresoCategoria(mitadK, out cat, out topeAnt, out topeAct, out pct, out esUlt, out sig);
                        Assert.AreEqual("K", cat.Letra);
                        Assert.AreEqual(topeJ, topeAnt);
                        Assert.AreEqual(topeK, topeAct);
                        Assert.AreEqual(50m, Math.Round(pct, 0));
                        Assert.IsTrue(esUlt);
                        Assert.AreEqual("EXCLUSIÓN", sig);

                        // Excluido:
                        escalas.CalcularProgresoCategoria(topeK + 1000m, out cat, out topeAnt, out topeAct, out pct, out esUlt, out sig);
                        Assert.IsNull(cat);
                        Assert.AreEqual(100m, pct);
                        Assert.IsTrue(esUlt);
                        Assert.AreEqual("EXCLUSIÓN", sig);
                }
        }
}
