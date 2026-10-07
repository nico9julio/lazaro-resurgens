using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lazaro.Base.Controller
{
        public class ComprobanteController : BaseController, IConElemento, IConImprimir
        {
                protected static Afip.Ws.FacturaElectronica.ServicioFacturaElectronica CliFe = null;

                public Lbl.IElementoDeDatos Elemento { get; set; }

                public ComprobanteController() { }

                public ComprobanteController(System.Data.IDbTransaction trans)
                        : base(trans)
                { }

                public Lbl.Comprobantes.Comprobante Comprobante
                {
                        get
                        {
                                return this.Elemento as Lbl.Comprobantes.Comprobante;
                        }
                }

                public Lfx.Types.OperationResult Imprimir()
                {
                        return this.Imprimir(this.Comprobante, null);
                }


                public Lfx.Types.OperationResult ImprimirLote(Lbl.Comprobantes.Lote lote, Lbl.Impresion.Impresora impresora)
                {
                        return new Lfx.Types.CancelOperationResult();
                }

                public Lfx.Types.OperationResult Imprimir(Lbl.Comprobantes.Comprobante comprobante, Lbl.Impresion.Impresora impresora)
                {
                        if(impresora == null) {
                                impresora = Util.Comprobantes.Impresion.ObtenerImpresora(comprobante);
                        }

                        var Reimpresion = comprobante.Impreso;
                        var ClaseImpr = Util.Comprobantes.Impresion.ObtenerClaseImpresora(comprobante);

                        if (ClaseImpr != Lbl.Impresion.ClasesImpresora.FiscalAfip && ClaseImpr != Lbl.Impresion.ClasesImpresora.ElectronicaAfip && impresora != null) {
                                ClaseImpr = impresora.Clase;

                                if (comprobante.Tipo.EsFacturaNCoND && impresora.EsVistaPrevia) {
                                        return new Lfx.Types.FailureOperationResult("Este tipo de comprobante no puede ser previsualizado");
                                }
                        }

                        Lfx.Types.OperationResult ResultadoImprimir;
                        Lbl.Comprobantes.ComprobanteConArticulos ComprobConArt = comprobante as Lbl.Comprobantes.ComprobanteConArticulos;

                        switch (ClaseImpr) {
                                case Lbl.Impresion.ClasesImpresora.ElectronicaAfip:
                                        if(ComprobConArt == null) {
                                                throw new InvalidOperationException("El comprobante no es una factura");
                                        }
                                        if (Reimpresion) {
                                                this.GenerarPdf(ComprobConArt);
                                                return new Lfx.Types.SuccessOperationResult();
                                        } else {
                                                ResultadoImprimir = this.ImprimirFacturaElectronicaAfip(comprobante as Lbl.Comprobantes.ComprobanteConArticulos);
                                                if (ResultadoImprimir.Success == true) {
                                                        //Resto el stock si corresponde
                                                        ComprobConArt.MoverExistencias(false);

                                                        // Asentar pagos si corresponde
                                                        ComprobConArt.AsentarPago(false);
                                                }
                                                return ResultadoImprimir;
                                        }

                                case Lbl.Impresion.ClasesImpresora.FiscalAfip:
                                        if (Reimpresion)
                                                return new Lfx.Types.FailureOperationResult("No se permiten reimpresiones fiscales");

                                        // Primero hago un COMMIT, porque si no el otro proceso no va a poder hacer movimientos
                                        if (this.Transaction != null) {
                                                this.Transaction.Commit();
                                                this.Transaction.Dispose();
                                                this.Transaction = null;
                                        }

                                        // Lo mando a imprimir al servidor fiscal
                                        string Estacion = null;
                                        if (ClaseImpr == Lbl.Impresion.ClasesImpresora.FiscalAfip)
                                                Estacion = Lbl.Comprobantes.PuntoDeVenta.TodosPorNumero[comprobante.PV].Estacion;

                                        if (Estacion == null && impresora != null)
                                                Estacion = impresora.Estacion;

                                        if (Estacion != null)
                                                Lfx.Workspace.Master.DefaultScheduler.AddTask("IMPRIMIR " + comprobante.Id.ToString(), "fiscal" + comprobante.PV.ToString(), Estacion);
                                        else
                                                throw new Exception("No se ha definido el equipo al cual está conectada la impresora remota");

                                        //Espero hasta que la factura está impresa o hasta que pasen X segundos
                                        System.DateTime FinEsperaFiscal = System.DateTime.Now.AddSeconds(90);
                                        int NumeroFiscal = 0;
                                        while (System.DateTime.Now < FinEsperaFiscal && NumeroFiscal == 0) {
                                                System.Threading.Thread.Sleep(1000);
                                                NumeroFiscal = comprobante.Connection.FieldInt("SELECT numero FROM comprob WHERE impresa<>0 AND id_comprob=" + comprobante.Id.ToString());
                                        }
                                        if (NumeroFiscal == 0) {
                                                // Llegó el fin del tiempo de espera y no imprimió
                                                return new Lfx.Types.FailureOperationResult("Se superó el tiempo de espera para recibir respuesta del Servidor Fiscal.");
                                        } else {
                                                comprobante.Cargar();
                                                // Tengo número de factura. Imprimió Ok
                                                return new Lfx.Types.SuccessOperationResult();
                                        }

                                case Lbl.Impresion.ClasesImpresora.Nula:
                                        if (Reimpresion == false && comprobante.Tipo.NumerarAlImprimir) {
                                                new Lbl.Comprobantes.Numerador(comprobante).Numerar(true);
                                        }

                                        if (Reimpresion == false) {
                                                //Resto el stock si corresponde
                                                ComprobConArt.MoverExistencias(false);

                                                // Asentar pagos si corresponde
                                                ComprobConArt.AsentarPago(false);
                                        }

                                        return new Lfx.Types.SuccessOperationResult();

                                case Lbl.Impresion.ClasesImpresora.Papel:
                                        if (impresora == null || impresora.EsLocal) {
                                                var Impresor = Lazaro.Base.Util.Impresion.Instanciador.InstanciarImpresor(comprobante, this.Transaction);
                                                return Impresor.Imprimir();
                                        } else {
                                                if (Reimpresion)
                                                        throw new Lfx.Types.DomainException("No se permiten reimpresiones remotas");

                                                // Primero hago un COMMIT, porque si no el otro proceso no va a poder hacer movimientos
                                                if (this.Transaction != null) {
                                                        this.Transaction.Commit();
                                                        this.Transaction.Dispose();
                                                        this.Transaction = null;
                                                }

                                                // Lo mando a imprimir a la estación remota
                                                Lfx.Workspace.Master.DefaultScheduler.AddTask("IMPRIMIR " + comprobante.GetType().ToString() + " " + comprobante.Id.ToString() + " EN " + impresora.Dispositivo, "lazaro", impresora.Estacion);

                                                if (Reimpresion == false) {
                                                        //Espero hasta que la factura está impresa o hasta que pasen X segundos
                                                        System.DateTime FinEspera = System.DateTime.Now.AddSeconds(90);
                                                        int Impreso = 0;
                                                        while (System.DateTime.Now < FinEspera && Impreso == 0) {
                                                                System.Threading.Thread.Sleep(1000);
                                                                Impreso = comprobante.Connection.FieldInt("SELECT impresa FROM comprob WHERE impresa<>0 AND id_comprob=" + comprobante.Id.ToString());
                                                        }
                                                        if (Impreso == 0) {
                                                                // Llegó el fin del tiempo de espera y no imprimió
                                                                return new Lfx.Types.FailureOperationResult("Se superó el tiempo de espera para recibir respuesta del sistema remoto.");
                                                        } else {
                                                                comprobante.Cargar();

                                                                // Tengo número de factura. Imprimió Ok
                                                                return new Lfx.Types.SuccessOperationResult();
                                                        }
                                                }

                                                return new Lfx.Types.SuccessOperationResult();
                                        }

                                default:
                                        throw new NotImplementedException("No se reconoce el tipo de impresora " + ClaseImpr.ToString());
                        }
                }


                public Lfx.Types.OperationResult ImprimirFacturaElectronicaAfip(Lbl.Comprobantes.ComprobanteConArticulos comprobante)
                {
                        Lfx.Types.OperationResult Res = this.IniciarWsAfip();
                        if(Res.Success == false) {
                                return Res;
                        }

                        bool esHomo = this.EsHomologacionAfip();

                        var TipoComprob = Afip.Ws.FacturaElectronica.Tablas.ComprobantesTiposPorLetra[comprobante.Tipo.Nomenclatura];
                        var UltimoComprob = CliFe.UltimoComprobante(comprobante.PV, TipoComprob);

                        int ProximoNumero = UltimoComprob.CbteNro + 1;

                        // Salvaguardas de seguridad en Modo Homologación
                        if (esHomo) {
                                // 1. Detección de Colisión Numérica Inminente en la base de datos local
                                string sqlColision = string.Format(
                                        "SELECT COUNT(*) FROM comprob WHERE pv = {0} AND tipo_fac = '{1}' AND numero = {2} AND id_comprob != {3}",
                                        comprobante.PV,
                                        comprobante.Tipo.Nomenclatura,
                                        ProximoNumero,
                                        comprobante.Id
                                );
                                int colisiones = comprobante.Connection.FieldInt(sqlColision);
                                if (colisiones > 0) {
                                        return new Lfx.Types.FailureOperationResult(string.Format(
                                                "BLOQUEO DE SEGURIDAD (MODO HOMOLOGACIÓN): El servidor de prueba de AFIP asignará el comprobante Nº {0} al Punto de Venta {1}, " +
                                                "pero dicho número YA EXISTE en su base de datos local para este tipo de comprobante ({2}).\n\n" +
                                                "Para evitar sobreescritura de comprobantes reales e inconsistencias en la numeración, utilice un Punto de Venta exclusivo de pruebas (ej: PV 99) " +
                                                "o trabaje sobre una base de datos de pruebas/clon.",
                                                ProximoNumero, comprobante.PV, comprobante.Tipo.Nomenclatura
                                        ));
                                }

                                // 2. Advertencia si el Punto de Venta posee historial productivo real previo
                                string sqlHistorialProd = string.Format(
                                        "SELECT COUNT(*) FROM comprob WHERE pv = {0} AND cae_numero IS NOT NULL AND cae_numero != '' AND (obs IS NULL OR obs NOT LIKE '%HOMOLOGACIÓN%')",
                                        comprobante.PV
                                );
                                int cantProd = comprobante.Connection.FieldInt(sqlHistorialProd);
                                if (cantProd > 0) {
                                        bool permitirPvProd = Environment.GetEnvironmentVariable("LAZARO_PERMITIR_HOMO_PV_PROD") == "1"
                                                || Lfx.Workspace.Master.CurrentConfig.ReadGlobalSetting<string>("AFIP.Homologacion.PermitirPvProd", "0") == "1";

                                        if (!permitirPvProd) {
                                                return new Lfx.Types.FailureOperationResult(string.Format(
                                                        "ADVERTENCIA DE SEGURIDAD (MODO HOMOLOGACIÓN): El Punto de Venta {0} posee {1} comprobante(s) emitidos previamente en Producción.\n\n" +
                                                        "Emitir en Homologación sobre un Punto de Venta productivo puede alterar la correlatividad de la empresa.\n\n" +
                                                        "Recomendaciones:\n" +
                                                        "1. Asigne a sus pruebas un Punto de Venta exclusivo (ej: PV 99).\n" +
                                                        "2. O configure la opción 'AFIP.Homologacion.PermitirPvProd=1' si desea forzar la emisión sobre este PV.",
                                                        comprobante.PV, cantProd
                                                ));
                                        }
                                }
                        }

                        var SolCae = Util.Comprobantes.ClienteAfipWsfe.CrearSolicitudCae(comprobante, ProximoNumero);

                        var CantidadImpresos = CliFe.SolictarCae(SolCae);
                        // TODO: tratamiento de errores

                        foreach(var Comprob in SolCae.Comprobantes) {
                                if(Comprob.Cae != null && string.IsNullOrWhiteSpace(Comprob.Cae.CodigoCae) == false) {
                                        // 3. Marca indeleble de Homologación en la base de datos
                                        if (esHomo) {
                                                string marcaHomo = "[AFIP HOMOLOGACIÓN - COMPROBANTE NO FISCAL]";
                                                if (string.IsNullOrWhiteSpace(comprobante.Obs)) {
                                                        comprobante.Obs = marcaHomo;
                                                } else if (!comprobante.Obs.Contains("[AFIP HOMOLOGACIÓN")) {
                                                        comprobante.Obs = marcaHomo + " " + comprobante.Obs;
                                                }
                                                comprobante.Registro["obs"] = comprobante.Obs;
                                        }

                                        new Lbl.Comprobantes.Numerador(comprobante).Numerar(Comprob.Numero, Comprob.Cae.CodigoCae, Comprob.Cae.Vencimiento, true);

                                        if (esHomo) {
                                                qGen.Update updObs = new qGen.Update(comprobante.TablaDatos);
                                                updObs.ColumnValues.AddWithValue("obs", comprobante.Obs);
                                                updObs.WhereClause = new qGen.Where(comprobante.CampoId, comprobante.Id);
                                                comprobante.Connection.ExecuteNonQuery(updObs);
                                        }

                                        this.GenerarPdf(comprobante);
                                }
                        }

                        
                        if(CantidadImpresos > 0) {
                                // La solicitud tuvo éxito (total o parcial)
                                return new Lfx.Types.SuccessOperationResult();
                        } else {
                                // La solicitud de CAE fue rechazada
                                if (SolCae.Observaciones.Count > 0) {
                                        return new Lfx.Types.FailureOperationResult(SolCae.Observaciones[0].Mensaje);
                                } else {
                                        return new Lfx.Types.FailureOperationResult("La solicitud de CAE fue rechazada");
                                }
                        }
                }

                /// <summary>
                /// Generar un PDF a partir de un comprobante y guardarlo en la carpeta predeterminada de comprobantes.
                /// </summary>
                public void GenerarPdf(Lbl.Comprobantes.ComprobanteConArticulos comprobante)
                {
                        var Generador = new Util.Comprobantes.GeneradorPdf(comprobante);

                        var VariantePv = Lbl.Comprobantes.PuntoDeVenta.TodosPorNumero[comprobante.PV].Variante;
                        if (VariantePv > 0) {
                                Generador.Variante = (Util.Comprobantes.Variantes)VariantePv;
                        }

                        var Carpeta = System.IO.Path.Combine(Lbl.Sys.Config.CarpetaEmpresa, "Comprobantes", "PV" + comprobante.PV.ToString());

                        Lfx.Environment.Folders.EnsurePathExists(Carpeta);

                        string NombreArchivo = System.IO.Path.Combine(Carpeta, comprobante.ToString().Replace("\"", "").Replace("\\", "").Replace("/", "").Replace(":", "").Replace("?", "").Replace("*", ""));
                        Generador.GenerarYGuardar(NombreArchivo + ".pdf");
                }


                /// <summary>
                /// Determina si AFIP debe operar en entorno de homologación.
                /// Prioridad: variable de entorno > configuración en sys_config (AFIP.Homologacion).
                /// </summary>
                protected bool EsHomologacionAfip()
                {
                        return Lbl.Sys.Config.AfipHomologacion;
                }

                /// <summary>
                /// Resuelve la ruta del certificado .p12 para la empresa y entorno actual.
                /// En homologación busca Certificado_homo.p12 y fallback a Certificado.p12.
                /// En producción busca Certificado_prod.p12 y fallback a Certificado.p12.
                /// </summary>
                protected string ObtenerRutaCertificadoAfip(bool esHomo)
                {
                        string carpetaAfip = System.IO.Path.Combine(Lbl.Sys.Config.CarpetaEmpresa, "AFIP");
                        if (esHomo) {
                                string certHomo = System.IO.Path.Combine(carpetaAfip, "Certificado_homo.p12");
                                if (System.IO.File.Exists(certHomo)) {
                                        return certHomo;
                                }
                        } else {
                                string certProd = System.IO.Path.Combine(carpetaAfip, "Certificado_prod.p12");
                                if (System.IO.File.Exists(certProd)) {
                                        return certProd;
                                }
                        }

                        return System.IO.Path.Combine(carpetaAfip, "Certificado.p12");
                }

                /// <summary>
                /// Resuelve la ruta de archivo local de ticket de acceso aislada por empresa y entorno.
                /// </summary>
                protected string ObtenerRutaArchivoTicketAcceso(bool esHomo)
                {
                        string sufijo = esHomo ? "homo" : "prod";
                        string carpetaAfip = System.IO.Path.Combine(Lbl.Sys.Config.CarpetaEmpresa, "AFIP");
                        if (!System.IO.Directory.Exists(carpetaAfip)) {
                                try {
                                        Lfx.Environment.Folders.EnsurePathExists(carpetaAfip);
                                } catch { }
                        }
                        return System.IO.Path.Combine(carpetaAfip, string.Format("ticketacceso_{0}.dat", sufijo));
                }

                /// <summary>
                /// Prepara el cliente de WS de AFIP, prueba el estado de los servicios y obtiene un ticket de acceso.
                /// </summary>
                /// <returns>SuccessOperationResult si todo salió bien.</returns>
                protected Lfx.Types.OperationResult IniciarWsAfip()
                {
                        bool esHomologacion = this.EsHomologacionAfip();

                        // Inicio un cliente si es necesario
                        if (CliFe == null) {
                                CliFe = new Afip.Ws.FacturaElectronica.ServicioFacturaElectronica();
                        }

                        CliFe.Homologacion = esHomologacion;

                        // Si no estoy autenticado o la autenticación está vencida, pido un TA
                        if (CliFe.TieneTicketDeAccesoValido() == false) {
                                var TicketAcceso = this.ObtenerTicketDeAcceso();

                                if (TicketAcceso == null) {
                                        return new Lfx.Types.FailureOperationResult("Error solicitando Ticket de Acceso a los servicios web de AFIP");
                                } else {
                                        CliFe.Cuit = Lbl.Sys.Config.Empresa.ClaveTributaria.ToString();
                                        CliFe.TicketAcceso = TicketAcceso;
                                }
                        }

                        return new Lfx.Types.SuccessOperationResult();
                }

                /// <summary>
                /// Guarda un ticket de acceso en la carpeta de la empresa y en la base de datos, para reusarlo mientras sea válido.
                /// </summary>
                protected void GuardarTicketDeAcceso(Afip.Ws.Autenticacion.TicketAcceso ta, bool esHomo, string cuit)
                {
                        try {
                                string cuitLimpio = cuit != null ? cuit.Replace("-", "").Replace(" ", "").Replace(".", "").Trim() : "";
                                // Generar una cadena con el TA (incluye el CUIT como 5to token para evitar colisiones multi-instancia)
                                var CadenaTa = ta.Token + "|lazaro_separador|" + ta.Sign + "|lazaro_separador|" + Lfx.Types.Formatting.FormatDateTimeSql(ta.GenerationTime) + "|lazaro_separador|" + Lfx.Types.Formatting.FormatDateTimeSql(ta.ExpirationTime) + "|lazaro_separador|" + cuitLimpio;

                                // Escribirlo en un archivo en el disco en la carpeta de la empresa
                                var RutaTa = this.ObtenerRutaArchivoTicketAcceso(esHomo);
                                System.IO.File.WriteAllText(RutaTa, CadenaTa);

                                // Guardarlo en la base de datos con clave por entorno
                                string claveDb = esHomo ? "AFIP.TicketAcceso.Homo" : "AFIP.TicketAcceso.Prod";
                                Lfx.Workspace.Master.CurrentConfig.WriteGlobalSetting(claveDb, CadenaTa);

                                // Si es producción, actualizar también la clave histórica por retrocompatibilidad
                                if (!esHomo) {
                                        Lfx.Workspace.Master.CurrentConfig.WriteGlobalSetting("AFIP.TicketAcceso", CadenaTa);
                                }
                        } catch { 
                                // Nada
                        }
                }

                protected void GuardarTicketDeAcceso(Afip.Ws.Autenticacion.TicketAcceso ta)
                {
                        bool esHomo = this.EsHomologacionAfip();
                        string cuit = Lbl.Sys.Config.Empresa.ClaveTributaria != null ? Lbl.Sys.Config.Empresa.ClaveTributaria.ToString() : "";
                        this.GuardarTicketDeAcceso(ta, esHomo, cuit);
                }

                /// <summary>
                /// Obtiene un ticket de acceso guardado en la base de datos o solicita uno nuevo a AFIP.
                /// </summary>
                /// <returns>Un ticket de acceso válido o null en caso de error.</returns>
                protected Afip.Ws.Autenticacion.TicketAcceso ObtenerTicketDeAcceso()
                {
                        bool esHomo = this.EsHomologacionAfip();
                        string cuit = Lbl.Sys.Config.Empresa.ClaveTributaria != null ? Lbl.Sys.Config.Empresa.ClaveTributaria.ToString() : "";

                        // 1. Buscar un ticket guardado en el archivo local de la empresa
                        var RutaTa = this.ObtenerRutaArchivoTicketAcceso(esHomo);
                        if (System.IO.File.Exists(RutaTa)) {
                                var CadenaTaArchivo = System.IO.File.ReadAllText(RutaTa);
                                var TicketGuardadoEnArchivo = this.DecodificarTicketDeAcceso(CadenaTaArchivo, cuit);

                                if (TicketGuardadoEnArchivo != null && TicketGuardadoEnArchivo.EsValido()) {
                                        return TicketGuardadoEnArchivo;
                                }
                        }

                        // 2. Buscar un ticket guardado en la base de datos para este entorno
                        string claveDb = esHomo ? "AFIP.TicketAcceso.Homo" : "AFIP.TicketAcceso.Prod";
                        var CadenaDb = Lfx.Workspace.Master.CurrentConfig.ReadGlobalSetting<string>(claveDb, null);

                        // Fallback retrocompatible para producción si no se encontró en AFIP.TicketAcceso.Prod
                        if (CadenaDb == null && !esHomo) {
                                CadenaDb = Lfx.Workspace.Master.CurrentConfig.ReadGlobalSetting<string>("AFIP.TicketAcceso", null);
                        }

                        var TicketGuardadoEnDb = this.DecodificarTicketDeAcceso(CadenaDb, cuit);
                        if (TicketGuardadoEnDb != null && TicketGuardadoEnDb.EsValido()) {
                                return TicketGuardadoEnDb;
                        }

                        // 3. Parece que no hay un ticket o ya no es válido. Pedir uno nuevo a AFIP (WSAA).
                        var CliWsass = new Afip.Ws.Autenticacion.ServicioAutenticacion();
                        CliWsass.Homologacion = esHomo;
                        CliWsass.RutaCertificado = this.ObtenerRutaCertificadoAfip(esHomo);
                        var Ta = CliWsass.Autenticar();

                        // Guardar el ticket para reusar
                        this.GuardarTicketDeAcceso(Ta, esHomo, cuit);

                        return Ta;
                }

                /// <summary>
                /// Decodifica la cadena serializada del ticket de acceso.
                /// Valida que el formato sea correcto y que, si posee CUIT registrado, coincida con el CUIT esperado.
                /// </summary>
                protected Afip.Ws.Autenticacion.TicketAcceso DecodificarTicketDeAcceso(string cadenaTa, string cuitEsperado = null)
                {
                        try {
                                if (string.IsNullOrWhiteSpace(cadenaTa) == false) {
                                        var Partes = cadenaTa.Split(new string[] { "|lazaro_separador|" }, StringSplitOptions.None);
                                        if (Partes.Length >= 4) {
                                                // Si incluye el CUIT (5 partes), validar coincidencia con el negocio actual
                                                if (Partes.Length >= 5 && !string.IsNullOrWhiteSpace(cuitEsperado)) {
                                                        string cuitGuardado = Partes[4].Trim();
                                                        string cuitEsp = cuitEsperado.Replace("-", "").Replace(" ", "").Replace(".", "").Trim();
                                                        if (!string.IsNullOrEmpty(cuitGuardado) && !string.Equals(cuitGuardado, cuitEsp, StringComparison.OrdinalIgnoreCase)) {
                                                                // Pertenece a otra empresa o CUIT
                                                                return null;
                                                        }
                                                }

                                                var Ta = new Afip.Ws.Autenticacion.TicketAcceso();
                                                Ta.Token = Partes[0];
                                                Ta.Sign = Partes[1];
                                                Ta.GenerationTime = Lfx.Types.Parsing.ParseSqlDateTime(Partes[2]);
                                                Ta.ExpirationTime = Lfx.Types.Parsing.ParseSqlDateTime(Partes[3]);

                                                return Ta;
                                        }
                                }
                        } catch {
                                // Nada...
                        }

                        return null;
                }
        }
}