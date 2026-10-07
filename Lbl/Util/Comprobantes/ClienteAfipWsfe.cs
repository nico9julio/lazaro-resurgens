using System;
using System.Collections.Generic;
using System.Text;
using Afip.Ws.FacturaElectronica;

namespace Lazaro.Base.Util.Comprobantes
{
        public static class ClienteAfipWsfe
        {
                public static Afip.Ws.FacturaElectronica.SolicitudCae CrearSolicitudCae(Lbl.Comprobantes.ComprobanteConArticulos comprobante, int numero)
                {
                        // Crear la solicitud de CAE
                        var SolCae = new Afip.Ws.FacturaElectronica.SolicitudCae()
                        {
                                PuntoDeVenta = comprobante.PV,
                                TipoComprobante = Afip.Ws.FacturaElectronica.Tablas.ComprobantesTiposPorLetra[comprobante.Tipo.Nomenclatura]
                        };

                        // Crear el comprobante asociado
                        var ComprobanteSolCae = new Afip.Ws.FacturaElectronica.ComprobanteSolicitud()
                        {
                                Conceptos = (Afip.Ws.FacturaElectronica.Tablas.Conceptos)comprobante.Articulos.ConceptosAfip(),
                                Numero = numero,
                        };

                        if ((ComprobanteSolCae.Conceptos | Tablas.Conceptos.Servicios) == Tablas.Conceptos.Servicios) {
                                foreach (var Art in comprobante.Articulos) {
                                        if (Art.Articulo != null) {
                                                switch (Art.Articulo.Periodicidad) {
                                                        case Lbl.Articulos.Periodicidad.PorSemana:
                                                                DateTime SemanaPasada = DateTime.Now;
                                                                SemanaPasada.AddDays(-7);
                                                                ComprobanteSolCae.ServicioFechaDesde = new DateTime(SemanaPasada.Year, SemanaPasada.Month, SemanaPasada.Day);
                                                                ComprobanteSolCae.ServicioFechaHasta = DateTime.Now;
                                                                break;

                                                        case Lbl.Articulos.Periodicidad.PorMes:
                                                                DateTime MesPasado = DateTime.Now;
                                                                MesPasado.AddMonths(-1);
                                                                ComprobanteSolCae.ServicioFechaDesde = new DateTime(MesPasado.Year, MesPasado.Month, 1);
                                                                ComprobanteSolCae.ServicioFechaHasta = new DateTime(MesPasado.Year, MesPasado.Month, DateTime.DaysInMonth(MesPasado.Year, MesPasado.Month));
                                                                break;

                                                        case Lbl.Articulos.Periodicidad.PorBimestre:
                                                                DateTime MesPasado1 = DateTime.Now;
                                                                MesPasado1.AddMonths(-1);
                                                                DateTime MesPasado2 = DateTime.Now;
                                                                MesPasado2.AddMonths(-2);
                                                                ComprobanteSolCae.ServicioFechaDesde = new DateTime(MesPasado2.Year, MesPasado2.Month, 1);
                                                                ComprobanteSolCae.ServicioFechaHasta = new DateTime(MesPasado1.Year, MesPasado1.Month, DateTime.DaysInMonth(MesPasado1.Year, MesPasado1.Month));
                                                                break;

                                                        case Lbl.Articulos.Periodicidad.PorOcasion:
                                                        case Lbl.Articulos.Periodicidad.PorMinuto:
                                                        case Lbl.Articulos.Periodicidad.PorHora:
                                                        case Lbl.Articulos.Periodicidad.PorDia:
                                                        default:
                                                                ComprobanteSolCae.ServicioFechaDesde = DateTime.Now;
                                                                ComprobanteSolCae.ServicioFechaHasta = ComprobanteSolCae.ServicioFechaDesde;
                                                                break;
                                                }
                                                break;
                                        }
                                }
                                
                                ComprobanteSolCae.FechaVencimientoPago = DateTime.Now;
                        }

                        // Asignar cliente al comprobante
                        if (comprobante.Cliente.SituacionTributaria == null || comprobante.Cliente.SituacionTributaria.EsConsumidorFinal) {
                                if (string.IsNullOrEmpty(comprobante.Cliente.NumeroDocumento)) {
                                        ComprobanteSolCae.Cliente = new Afip.Ws.FacturaElectronica.Cliente()
                                        {
                                                DocumentoTipo = Afip.Ws.FacturaElectronica.Tablas.DocumentoTipos.SinIdentificar,
                                                DocumentoNumero = Lfx.Types.Parsing.ParseInt(comprobante.Cliente.NumeroDocumento)
                                        };
                                } else {
                                        ComprobanteSolCae.Cliente = new Afip.Ws.FacturaElectronica.Cliente()
                                        {
                                                DocumentoTipo = Afip.Ws.FacturaElectronica.Tablas.DocumentoTipos.Dni,
                                                DocumentoNumero = Lfx.Types.Parsing.ParseInt(comprobante.Cliente.NumeroDocumento)
                                        };
                                }
                        } else {
                                long DocNro = 0;
                                long.TryParse(comprobante.Cliente.Cuit.ToString().Replace("-", "").Replace(" ", "").Replace("/", "").Replace(".", ""), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out DocNro);
                                ComprobanteSolCae.Cliente = new Afip.Ws.FacturaElectronica.Cliente()
                                {
                                        DocumentoTipo = Afip.Ws.FacturaElectronica.Tablas.DocumentoTipos.Cuit,
                                        DocumentoNumero = DocNro
                                };
                                
                        }

                        // RG 5616: Asignar condición frente al IVA del receptor
                        if (ComprobanteSolCae.Cliente != null) {
                                ComprobanteSolCae.Cliente.CondicionIvaReceptorId = MapearCondicionIvaReceptor(comprobante.Cliente);
                        }

                        // Agregar conceptos al comprobante, agrupados por alícuota
                        if (comprobante.Tipo.Letra == "C") {
                                // El comprobante C lleva características especiales
                                ComprobanteSolCae.ImporteNetoGravado = Math.Round(comprobante.Total, 2, MidpointRounding.AwayFromZero);
                        } else if (comprobante.Cliente.ObtenerSituacionIva() == Lbl.Impuestos.SituacionIva.Exento) {
                                // Cliente exento... una sóla alícuota al 0% por el total
                                ComprobanteSolCae.ImportesAlicuotas.Add(new Afip.Ws.FacturaElectronica.ImporteAlicuota()
                                {
                                        Alicuota = Afip.Ws.FacturaElectronica.Tablas.Alicuotas.Iva0,
                                        BaseImponible = Math.Round(comprobante.Total, 2, MidpointRounding.AwayFromZero),
                                        Importe = 0m
                                });
                                ComprobanteSolCae.ImporteNetoGravado = ComprobanteSolCae.ImportesAlicuotas.ImporteNetoGravado();
                        } else {
                                // Agregar una o más alícuotas de IVA
                                var Alicuotas = comprobante.AlicuotasUsadas();
                                foreach (Lbl.Impuestos.Alicuota Alic in Alicuotas.Values) {
                                        decimal ImporteIva = Math.Round(comprobante.TotalIvaAlicuota(Alic.Id), 2, MidpointRounding.AwayFromZero);
                                        decimal ImporteGravado = Math.Round(comprobante.ImporteGravadoAlicuota(Alic.Id), 2, MidpointRounding.AwayFromZero);

                                        if (ImporteGravado == 0m && ImporteIva == 0m)
                                                continue;

                                        int codigoAlicAfip = Lbl.Archivos.Salida.CitiTablas.Alicuotas.ContainsKey(Alic.Id)
                                                ? Lbl.Archivos.Salida.CitiTablas.Alicuotas[Alic.Id]
                                                : (int)Afip.Ws.FacturaElectronica.Tablas.Alicuotas.Iva21;

                                        ComprobanteSolCae.ImportesAlicuotas.Add(new Afip.Ws.FacturaElectronica.ImporteAlicuota()
                                        {
                                                Alicuota = (Afip.Ws.FacturaElectronica.Tablas.Alicuotas)codigoAlicAfip,
                                                BaseImponible = ImporteGravado,
                                                Importe = ImporteIva
                                        });
                                }
                                ComprobanteSolCae.ImporteNetoGravado = ComprobanteSolCae.ImportesAlicuotas.ImporteNetoGravado();
                        }

                        if (comprobante.ComprobanteOriginal != null)
                        {
                                ComprobanteSolCae.ComprobantesAsociados = new List<ComprobanteAsociado>()
                                {
                                        new ComprobanteAsociado
                                        {
                                                PuntoDeVenta = comprobante.ComprobanteOriginal.PV,
                                                Tipo = Afip.Ws.FacturaElectronica.Tablas.ComprobantesTiposPorLetra[comprobante.ComprobanteOriginal.Tipo.Nomenclatura],
                                                Cuit= comprobante.ComprobanteOriginal.Cliente.Cuit.ToString().Replace("-", "").Replace(" ", "").Replace(".", ""),
                                                Fecha = comprobante.ComprobanteOriginal.Fecha,
                                                Numero = comprobante.ComprobanteOriginal.Numero
                                        }
                                };
                        }

                        // Agregar el comprobante asociado a la solicitud de CAE
                        SolCae.Comprobantes = new Afip.Ws.FacturaElectronica.ColeccionComprobantesSolicitud()
                        {
                                ComprobanteSolCae
                        };

                        return SolCae;
                }

                /// <summary>
                /// Mapea la condición fiscal del contacto al código CondicionIVAReceptorId requerido por AFIP según RG 5616.
                /// Códigos AFIP (consultables vía FEParamGetCondicionIvaReceptor):
                /// 1: IVA Responsable Inscripto
                /// 4: IVA Sujeto Exento
                /// 5: Consumidor Final
                /// 6: Responsable Monotributo
                /// 7: Sujeto No Categorizado
                /// 15: IVA No Alcanzado
                /// </summary>
                /// <param name="cliente">El contacto / cliente receptor.</param>
                /// <returns>El código numérico correspondiente de AFIP.</returns>
                public static int MapearCondicionIvaReceptor(Lbl.Personas.Persona cliente)
                {
                        if (cliente == null || cliente.SituacionTributaria == null) {
                                return 5; // Predeterminado: Consumidor Final
                        }

                        // Mapeo por ID de situación en la BD local de Lázaro (tabla 'situaciones')
                        switch (cliente.SituacionTributaria.Id) {
                                case 1:
                                        // Consumidor Final
                                        return 5;
                                case 2:
                                        // Responsable Inscripto
                                        return 1;
                                case 3:
                                        // Responsable No Inscripto
                                        return 7;
                                case 4:
                                        // Responsable Monotributista
                                        return 6;
                                case 5:
                                        // Exento
                                        return 4;
                                case 6:
                                        // No Responsable
                                        return 15;
                                case 7:
                                        // No Categorizado
                                        return 7;
                        }

                        // Salvaguarda por abreviatura de situación tributaria
                        var abrev = (cliente.SituacionTributaria.Abreviatura ?? "").Trim().ToUpperInvariant();
                        switch (abrev) {
                                case "RI":
                                        return 1;
                                case "EX":
                                        return 4;
                                case "CF":
                                        return 5;
                                case "M":
                                        return 6;
                                case "NI":
                                case "NC":
                                        return 7;
                                case "NR":
                                        return 15;
                        }

                        return 5;
                }
        }
}
