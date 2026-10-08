using System;
using System.Collections.Generic;
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

                [Test]
                public void ParserWebAfip_ParsearMonto_ManejaFormatosMonetarios()
                {
                        Assert.AreEqual(12009410.45m, ParserWebAfip.ParsearMonto("$12.009.410,45"));
                        Assert.AreEqual(49527.18m, ParserWebAfip.ParsearMonto("$ 49.527,18 "));
                        Assert.AreEqual(126610838.75m, ParserWebAfip.ParsearMonto("$126.610.838,75"));
                        Assert.AreEqual(0m, ParserWebAfip.ParsearMonto(""));
                        Assert.AreEqual(0m, ParserWebAfip.ParsearMonto("-"));
                }

                [Test]
                public void ParserWebAfip_ParsearHtml_ExtraeCategoriasYCuotasCorrectamente()
                {
                        string htmlEjemplo = @"
                        <table class=""table table-bordered table-striped"" summary=""Tabla de categorías de monotributo vigente desde 01/08/2024"">
                            <tbody>
                                <tr>
                                    <th id=""th_A_t15"" scope=""row"" data-title=""Categoría"">A</th>
                                    <td data-title=""Ingresos brutos (****)"">$12.009.410,45 </td>
                                    <td data-title=""Superficie Afectada (*)"">Hasta 30 m2</td>
                                    <td data-title=""Energía Eléctrica Consumida Anualmente"">Hasta 3330 Kw</td>
                                    <td data-title=""Alquileres Devengados Anualmente"">$2.792.886,15 </td>
                                    <td data-title=""Precio unitario máximo para venta de Cosas Muebles"">$716.840,77 </td>
                                    <td data-title=""Impuesto Integrado: Locaciones y/o Prestaciones de Servicios"">$5.585,77 </td>
                                    <td data-title=""Impuesto Integrado: Venta de Cosas Muebles"">$5.585,77 </td>
                                    <td data-title=""Aportes al SIPA (**)"">$18.246,86 </td>
                                    <td data-title=""Aportes Obra Social (***)"">$25.694,55 </td>
                                    <td data-title=""Total: Locaciones y/o prestaciones de servicios"">$49.527,18 </td>
                                    <td data-title=""Total: Venta de Cosas Muebles"">$49.527,18 </td>
                                </tr>
                                <tr>
                                    <th id=""th_B_t15"" scope=""row"" data-title=""Categoría"">B</th>
                                    <td data-title=""Ingresos brutos (****)"">$17.595.182,74 </td>
                                    <td data-title=""Superficie Afectada (*)"">Hasta 45 m2</td>
                                    <td data-title=""Energía Eléctrica Consumida Anualmente"">Hasta 5000 Kw</td>
                                    <td data-title=""Alquileres Devengados Anualmente"">$2.792.886,15 </td>
                                    <td data-title=""Precio unitario máximo para venta de Cosas Muebles"">$716.840,77 </td>
                                    <td data-title=""Impuesto Integrado: Locaciones y/o Prestaciones de Servicios"">$10.612,98 </td>
                                    <td data-title=""Impuesto Integrado: Venta de Cosas Muebles"">$10.612,98 </td>
                                    <td data-title=""Aportes al SIPA (**)"">$20.071,55 </td>
                                    <td data-title=""Aportes Obra Social (***)"">$25.694,55 </td>
                                    <td data-title=""Total: Locaciones y/o prestaciones de servicios"">$56.379,08 </td>
                                    <td data-title=""Total: Venta de Cosas Muebles"">$56.379,08 </td>
                                </tr>
                                <tr><th scope=""row"">C</th><td data-title=""Ingresos brutos"">$24.670.494,31</td><td/><td/><td/><td/><td/><td/><td/><td/><td data-title=""Total: Locaciones"">$66.020,12</td><td data-title=""Total: Venta"">$64.530,58</td></tr>
                                <tr><th scope=""row"">D</th><td data-title=""Ingresos brutos"">$30.628.651,43</td><td/><td/><td/><td/><td/><td/><td/><td/><td data-title=""Total: Locaciones"">$84.612,93</td><td data-title=""Total: Venta"">$82.564,81</td></tr>
                                <tr><th scope=""row"">E</th><td data-title=""Ingresos brutos"">$36.028.231,33</td><td/><td/><td/><td/><td/><td/><td/><td/><td data-title=""Total: Locaciones"">$119.811,45</td><td data-title=""Total: Venta"">$108.267,51</td></tr>
                                <tr><th scope=""row"">F</th><td data-title=""Ingresos brutos"">$45.151.659,41</td><td/><td/><td/><td/><td/><td/><td/><td/><td data-title=""Total: Locaciones"">$150.784,21</td><td data-title=""Total: Venta"">$129.930,65</td></tr>
                                <tr><th scope=""row"">G</th><td data-title=""Ingresos brutos"">$53.995.798,87</td><td/><td/><td/><td/><td/><td/><td/><td/><td data-title=""Total: Locaciones"">$230.312,94</td><td data-title=""Total: Venta"">$158.815,05</td></tr>
                                <tr><th scope=""row"">H</th><td data-title=""Ingresos brutos"">$81.924.660,37</td><td/><td/><td/><td/><td/><td/><td/><td/><td data-title=""Total: Locaciones"">$522.706,68</td><td data-title=""Total: Venta"">$317.895,01</td></tr>
                                <tr><th scope=""row"">I</th><td data-title=""Ingresos brutos"">$91.699.761,90</td><td/><td/><td/><td/><td/><td/><td/><td/><td data-title=""Total: Locaciones"">$963.747,86</td><td data-title=""Total: Venta"">$474.992,78</td></tr>
                                <tr><th scope=""row"">J</th><td data-title=""Ingresos brutos"">$105.012.519,20</td><td/><td/><td/><td/><td/><td/><td/><td/><td data-title=""Total: Locaciones"">$1.167.299,76</td><td data-title=""Total: Venta"">$580.793,69</td></tr>
                                <tr><th scope=""row"">K</th><td data-title=""Ingresos brutos"">$126.610.838,75</td><td/><td/><td/><td/><td/><td/><td/><td/><td data-title=""Total: Locaciones"">$1.614.446,04</td><td data-title=""Total: Venta"">$702.103,24</td></tr>
                            </tbody>
                        </table>";

                        List<CategoriaMonotributo> cats;
                        string meta, error;
                        bool res = ParserWebAfip.ParsearHtml(htmlEjemplo, "servicios", out cats, out meta, out error);

                        Assert.IsTrue(res);
                        Assert.AreEqual("", error);
                        Assert.IsTrue(meta.Contains("01/08/2024"));
                        Assert.AreEqual(11, cats.Count);

                        Assert.AreEqual("A", cats[0].Letra);
                        Assert.AreEqual(12009410.45m, cats[0].IngresosBrutosMaximos);
                        Assert.AreEqual(49527.18m, cats[0].CuotaServicios);
                        Assert.AreEqual(49527.18m, cats[0].CuotaBienes);

                        Assert.AreEqual("K", cats[10].Letra);
                        Assert.AreEqual(126610838.75m, cats[10].IngresosBrutosMaximos);
                        Assert.AreEqual(1614446.04m, cats[10].CuotaServicios);
                        Assert.AreEqual(702103.24m, cats[10].CuotaBienes);
                }

                [Test]
                public void ConsultaConstanciaAfip_ParsearPersonaFisicaMonotributo()
                {
                        string xmlAfip = @"<?xml version=""1.0"" encoding=""utf-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:a5=""http://a5.soap.ws.server.puc.sr/"">
  <soapenv:Body>
    <a5:getPersona_v2Response>
      <personaReturn>
        <datosGenerales>
          <tipoPersona>FISICA</tipoPersona>
          <tipoClave>CUIT</tipoClave>
          <idPersona>20354082050</idPersona>
          <apellido>PEREZ</apellido>
          <nombre>JUAN CARLOS</nombre>
          <estadoClave>ACTIVO</estadoClave>
          <domicilioFiscal>
            <direccion>SAN MARTIN 1234 PISO 2 DTO A</direccion>
            <localidad>ROSARIO</localidad>
            <codPostal>2000</codPostal>
            <idProvincia>21</idProvincia>
            <descripcionProvincia>SANTA FE</descripcionProvincia>
            <tipoDomicilio>FISCAL</tipoDomicilio>
          </domicilioFiscal>
        </datosGenerales>
        <datosMonotributo>
          <categoriaMonotributo>B</categoriaMonotributo>
          <actividadMonotributo>SERVICIOS JURIDICOS</actividadMonotributo>
        </datosMonotributo>
      </personaReturn>
    </a5:getPersona_v2Response>
  </soapenv:Body>
</soapenv:Envelope>";

                        var res = ConsultaConstanciaAfip.ParsearRespuestaPersonaA5(xmlAfip, "20354082050");

                        Assert.IsTrue(res.Exito);
                        Assert.AreEqual("FISICA", res.TipoPersona);
                        Assert.AreEqual("PEREZ", res.Apellido);
                        Assert.AreEqual("JUAN CARLOS", res.Nombre);
                        Assert.AreEqual("35408205", res.NumeroDocumento);
                        Assert.AreEqual("SAN MARTIN 1234 PISO 2 DTO A", res.Domicilio);
                        Assert.AreEqual("ROSARIO", res.Localidad);
                        Assert.AreEqual("2000", res.CodigoPostal);
                        Assert.AreEqual(21, res.IdProvincia);
                        Assert.AreEqual("SANTA FE", res.Provincia);
                        Assert.AreEqual("B", res.Categoria);
                        Assert.AreEqual(4, res.IdSituacionTributaria); // Responsable Monotributista
                        Assert.AreEqual("ACTIVO", res.EstadoClave);
                }

                [Test]
                public void ConsultaConstanciaAfip_ParsearCategoriaConTextoYActividad()
                {
                        string xmlAfip = @"<?xml version=""1.0"" encoding=""utf-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:a5=""http://a5.soap.ws.server.puc.sr/"">
  <soapenv:Body>
    <a5:getPersona_v2Response>
      <personaReturn>
        <datosGenerales>
          <tipoPersona>FISICA</tipoPersona>
          <tipoClave>CUIT</tipoClave>
          <idPersona>27354082050</idPersona>
          <apellido>GARCIA</apellido>
          <nombre>MARIA</nombre>
          <estadoClave>ACTIVO</estadoClave>
        </datosGenerales>
        <datosMonotributo>
          <categoriaMonotributo>E LOCACIONES DE SERVICIOS3920202602</categoriaMonotributo>
          <actividadMonotributo>LOCACIONES DE SERVICIOS</actividadMonotributo>
        </datosMonotributo>
      </personaReturn>
    </a5:getPersona_v2Response>
  </soapenv:Body>
</soapenv:Envelope>";

                        var res = ConsultaConstanciaAfip.ParsearRespuestaPersonaA5(xmlAfip, "27354082050");

                        Assert.IsTrue(res.Exito);
                        Assert.AreEqual("E", res.Categoria);
                        Assert.AreEqual(4, res.IdSituacionTributaria);
                }

                [Test]
                public void EscalasMonotributo_NormalizarLetraYObtenerCategoriaConTextoComplejo()
                {
                        Assert.AreEqual("E", EscalasMonotributo.Instancia.NormalizarLetra("E LOCACIONES DE SERVICIOS3920202602"));
                        Assert.AreEqual("E", EscalasMonotributo.Instancia.NormalizarLetra("CATEGORIA E"));
                        Assert.AreEqual("E", EscalasMonotributo.Instancia.NormalizarLetra("CATEGORÍA E"));
                        Assert.AreEqual("E", EscalasMonotributo.Instancia.NormalizarLetra("CAT. E - LOCACIONES"));
                        Assert.AreEqual("E", EscalasMonotributo.Instancia.NormalizarLetra("CAT E"));
                        Assert.AreEqual("E", EscalasMonotributo.Instancia.NormalizarLetra("E"));
                        Assert.AreEqual("A", EscalasMonotributo.Instancia.NormalizarLetra("A"));
                        Assert.AreEqual("K", EscalasMonotributo.Instancia.NormalizarLetra("CATEGORIA K"));

                        var cat = EscalasMonotributo.Instancia.ObtenerCategoria("E LOCACIONES DE SERVICIOS3920202602");
                        Assert.IsNotNull(cat);
                        Assert.AreEqual("E", cat.Letra);

                        var catAnt = EscalasMonotributo.Instancia.ObtenerCategoriaAnterior("E");
                        Assert.IsNotNull(catAnt);
                        Assert.AreEqual("D", catAnt.Letra);
                        Assert.IsNull(EscalasMonotributo.Instancia.ObtenerCategoriaAnterior("A"));
                }

                [Test]
                public void ResumenMonotributo_CalculoOportunidadBaja()
                {
                        var escalas = EscalasMonotributo.Instancia;
                        var catE = escalas.ObtenerCategoria("E");
                        var catD = escalas.ObtenerCategoria("D");
                        Assert.IsNotNull(catE);
                        Assert.IsNotNull(catD);

                        var resumen = new ResumenMonotributo();
                        resumen.CategoriaInscripta = catE;
                        resumen.CategoriaProyectada = catD;
                        resumen.FacturadoProyectado = 25000000m;
                        resumen.FacturadoPeriodoRecat = 20000000m;

                        resumen.HayOportunidadBaja = true;
                        resumen.CategoriaBaja = catD;
                        resumen.MargenParaPasarseDeBajaProyectado = catD.IngresosBrutosMaximos - resumen.FacturadoProyectado;
                        resumen.MargenParaPasarseDeBajaPeriodo = catD.IngresosBrutosMaximos - resumen.FacturadoPeriodoRecat;

                        resumen.DiasRestantes = 84;
                        resumen.MargenCategoriaInscripta = catE.IngresosBrutosMaximos - resumen.FacturadoPeriodoRecat;

                        Assert.IsTrue(resumen.HayOportunidadBaja);
                        Assert.AreEqual("D", resumen.CategoriaBaja.Letra);
                        Assert.AreEqual(catD.IngresosBrutosMaximos - 25000000m, resumen.MargenParaPasarseDeBajaProyectado);
                        Assert.AreEqual(3, resumen.MesesRestantes);
                        Assert.IsTrue(resumen.LimiteMensualCategoriaInscripta > 0m);
                        Assert.IsTrue(resumen.LimiteMensualBaja > 0m);
                        Assert.IsTrue(resumen.LimiteDiarioBaja > 0m);

                        string txt = resumen.GenerarTextoInformativo();
                        Assert.IsTrue(txt.Contains("OPORTUNIDAD DE BAJA DE CATEGORÍA"));
                        Assert.IsFalse(txt.Contains("ASESORAMIENTO"));
                        Assert.IsTrue(txt.Contains("AVISO INFORMATIVO"));
                }

                [Test]
                public void ConsultaConstanciaAfip_ParsearPersonaJuridicaResponsableInscripto()
                {
                        string xmlAfip = @"<?xml version=""1.0"" encoding=""utf-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:a5=""http://a5.soap.ws.server.puc.sr/"">
  <soapenv:Body>
    <a5:getPersona_v2Response>
      <personaReturn>
        <datosGenerales>
          <tipoPersona>JURIDICA</tipoPersona>
          <tipoClave>CUIT</tipoClave>
          <idPersona>30712345678</idPersona>
          <razonSocial>DISTRIBUIDORA DEL CENTRO S.A.</razonSocial>
          <estadoClave>ACTIVO</estadoClave>
          <domicilioFiscal>
            <direccion>AV CORRIENTES 500 PISO 4</direccion>
            <localidad>CIUDAD AUTONOMA BUENOS AIRES</localidad>
            <codPostal>1043</codPostal>
            <idProvincia>0</idProvincia>
            <descripcionProvincia>CIUDAD AUTONOMA BUENOS AIRES</descripcionProvincia>
            <tipoDomicilio>FISCAL</tipoDomicilio>
          </domicilioFiscal>
        </datosGenerales>
        <datosRegimenGeneral>
          <impuesto>
            <idImpuesto>30</idImpuesto>
            <descripcionImpuesto>IVA</descripcionImpuesto>
            <periodo>201801</periodo>
          </impuesto>
          <impuesto>
            <idImpuesto>10</idImpuesto>
            <descripcionImpuesto>GANANCIAS SOCIEDADES</descripcionImpuesto>
            <periodo>201801</periodo>
          </impuesto>
        </datosRegimenGeneral>
      </personaReturn>
    </a5:getPersona_v2Response>
  </soapenv:Body>
</soapenv:Envelope>";

                        var res = ConsultaConstanciaAfip.ParsearRespuestaPersonaA5(xmlAfip, "30712345678");

                        Assert.IsTrue(res.Exito);
                        Assert.AreEqual("JURIDICA", res.TipoPersona);
                        Assert.AreEqual("DISTRIBUIDORA DEL CENTRO S.A.", res.RazonSocial);
                        Assert.IsNull(res.Apellido);
                        Assert.IsNull(res.Nombre);
                        Assert.AreEqual("AV CORRIENTES 500 PISO 4", res.Domicilio);
                        Assert.AreEqual("CIUDAD AUTONOMA BUENOS AIRES", res.Localidad);
                        Assert.AreEqual("1043", res.CodigoPostal);
                        Assert.AreEqual(2, res.IdSituacionTributaria); // Responsable Inscripto
                        Assert.AreEqual("Responsable Inscripto", res.DescripcionSituacion);
                }

                [Test]
                public void ConsultaConstanciaAfip_ParsearErrorConstancia()
                {
                        string xmlAfip = @"<?xml version=""1.0"" encoding=""utf-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:a5=""http://a5.soap.ws.server.puc.sr/"">
  <soapenv:Body>
    <a5:getPersona_v2Response>
      <personaReturn>
        <errorConstancia>No existe persona con el CUIT indicado</errorConstancia>
      </personaReturn>
    </a5:getPersona_v2Response>
  </soapenv:Body>
</soapenv:Envelope>";

                        var res = ConsultaConstanciaAfip.ParsearRespuestaPersonaA5(xmlAfip, "20000000001");

                        Assert.IsFalse(res.Exito);
                        Assert.IsTrue(res.Mensaje.Contains("No existe persona"));
                }

                [Test]
                public void EscalasMonotributo_DetectarDiscrepancias_DetectaCambiosEnTopesYGeneraReporte()
                {
                        List<CategoriaMonotributo> actuales = new List<CategoriaMonotributo>()
                        {
                                new CategoriaMonotributo("A", 8992597.87m, 26600m, 26600m),
                                new CategoriaMonotributo("D", 22934610.05m, 45400m, 44300m)
                        };

                        List<CategoriaMonotributo> nuevas = new List<CategoriaMonotributo>()
                        {
                                new CategoriaMonotributo("A", 12009410.45m, 49527.18m, 49527.18m),
                                new CategoriaMonotributo("D", 30628651.43m, 84612.93m, 82564.81m)
                        };

                        string reporte;
                        int diferencias;
                        bool hayCambios = EscalasMonotributo.DetectarDiscrepancias(actuales, nuevas, out reporte, out diferencias);

                        Assert.IsTrue(hayCambios);
                        Assert.AreEqual(2, diferencias);
                        Assert.IsTrue(reporte.Contains("Cat. A:"));
                        Assert.IsTrue(reporte.Contains("8.992.598"));
                        Assert.IsTrue(reporte.Contains("12.009.410"));
                        Assert.IsTrue(reporte.Contains("Cat. D:"));
                        Assert.IsTrue(reporte.Contains("22.934.610"));
                        Assert.IsTrue(reporte.Contains("30.628.651"));
                        Assert.IsTrue(reporte.Contains("+33,5%"));
                }

                [Test]
                public void EscalasMonotributo_DetectarDiscrepancias_EscalasIguales_NoReportaDiferencias()
                {
                        List<CategoriaMonotributo> actuales = EscalasMonotributo.ObtenerEscalasPredeterminadas();
                        List<CategoriaMonotributo> nuevas = EscalasMonotributo.ObtenerEscalasPredeterminadas();

                        string reporte;
                        int diferencias;
                        bool hayCambios = EscalasMonotributo.DetectarDiscrepancias(actuales, nuevas, out reporte, out diferencias);

                        Assert.IsFalse(hayCambios);
                        Assert.AreEqual(0, diferencias);
                        Assert.AreEqual("", reporte);
                }
        }
}
