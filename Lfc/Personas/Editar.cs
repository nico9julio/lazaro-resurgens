using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Lfc.Personas
{
        public partial class Editar : Lcc.Edicion.ControlEdicion
        {
                public Editar()
                {
                        ElementoTipo = typeof(Lbl.Personas.Persona);

                        InitializeComponent();

                        if (Lbl.Sys.Config.Pais.ClaveBancaria != null)
                                EtiquetaClaveBancaria.Text = Lbl.Sys.Config.Pais.ClaveBancaria.Nombre;


                        // Lleno el listado de tipos de factura
                        List<Lbl.Comprobantes.Tipo> TiposFacturaVenta = new List<Lbl.Comprobantes.Tipo>();
                        foreach (Lbl.Comprobantes.Tipo Tp in Lbl.Comprobantes.Tipo.TodosPorLetra.Values) {
                                if (Tp.EsFacturaOTicket && Tp.PermiteVenta) {
                                        TiposFacturaVenta.Add(Tp);
                                }
                        }
                        string[] ListaFactVenta = new string[TiposFacturaVenta.Count + 1];
                        ListaFactVenta[0] = "Predeterminada|*";
                        int i = 1;
                        foreach (Lbl.Comprobantes.Tipo Tp in TiposFacturaVenta) {
                                ListaFactVenta[i++] = Tp.NombreLargo + "|" + Tp.LetraONomenclatura;
                        }

                        EntradaTipoFac.SetData = ListaFactVenta; 
                }


                public override Lfx.Types.OperationResult ValidarControl()
                {
                        if (EntradaTipo.ValueInt <= 0)
                                return new Lfx.Types.FailureOperationResult("Seleccione el tipo de cliente");

                        Lbl.Personas.Persona Cliente = this.Elemento as Lbl.Personas.Persona;

                        if (EntradaNombreFantasia.Text.Length > 0 && EntradaRazonSocial.Text.Length == 0) {
                                return new Lfx.Types.FailureOperationResult("Escriba la razón social");
                        }

                        if (EntradaRazonSocial.Text.Length == 0 && EntradaNombre.Text.Length == 0 && EntradaApellido.Text.Length == 0) {
                                return new Lfx.Types.FailureOperationResult("Escriba el nombre y el apellido o la razón social");
                        } else {
                                //Busco un cliente con datos similares
                                Lfx.Data.Row ClienteDup = null;
                                string Sql = @"SELECT id_persona, nombre_visible, domicilio, telefono, cuit, email FROM personas WHERE (";
                                if (this.Text.Length > 0)
                                        Sql += @"nombre_visible LIKE '%" + this.Connection.EscapeString(this.Text.Replace("%", "").Replace("_", "")) + @"%'";
                                if (EntradaDomicilio.Text.Length > 0)
                                        Sql += @" OR domicilio LIKE '%" + Lfx.Workspace.Master.MasterConnection.EscapeString(EntradaDomicilio.Text) + @"%'";

                                if (EntradaNumDoc.Text.Length > 0)
                                        Sql += @" OR REPLACE(num_doc, '.', '') LIKE '%" + Lfx.Workspace.Master.MasterConnection.EscapeString(EntradaNumDoc.Text.Replace(".", "")) + @"%'";

                                if (EntradaTelefono.Text.Length > 0) {
                                        string Telefono = EntradaTelefono.Text.Replace(" -", "").Replace("- ", "").Replace("/", " ").Replace(",", " ").Replace(".", " ").Replace("  ", " ").Replace("%", "").Replace("_", "");
                                        IList<string> Telefonos = Lfx.Types.Strings.SplitDelimitedString(Telefono, ";");
                                        if (Telefonos != null && Telefonos.Count > 0) {
                                                foreach (string Tel in Telefonos) {
                                                        if (Tel != null && Tel.Length > 4)
                                                                Sql += @" OR telefono LIKE '%" + Lfx.Workspace.Master.MasterConnection.EscapeString(Tel.Replace("%", "").Replace("_", "")) + @"%'";
                                                }
                                        }
                                }
                                if (EntradaEmail.Text.Length > 0)
                                        Sql += @" OR email LIKE '%" + Lfx.Workspace.Master.MasterConnection.EscapeString(EntradaEmail.Text.Replace("%", "").Replace("_", "")) + @"%'";
                                if (EntradaClaveTributaria.Text.Length > 0)
                                        Sql += @" OR cuit='" + Lfx.Workspace.Master.MasterConnection.EscapeString(EntradaClaveTributaria.Text.Replace("%", "").Replace("_", "")) + @"'";
                                Sql += @") AND id_persona<>" + this.Elemento.Id.ToString();

                                ClienteDup = this.Connection.FirstRowFromSelect(Sql);
                                if (ClienteDup != null) {
                                        if (Cliente != null && Cliente.Existe == false) {
                                                AltaDuplicada FormAltaDuplicada = new AltaDuplicada();
                                                ListViewItem itm;
                                                itm = FormAltaDuplicada.ListaComparacion.Items.Add("Nombre");
                                                itm.SubItems.Add(ClienteDup["nombre_visible"].ToString());
                                                itm.SubItems.Add(this.Text);
                                                itm = FormAltaDuplicada.ListaComparacion.Items.Add("Domicilio");
                                                itm.SubItems.Add(ClienteDup["domicilio"].ToString());
                                                itm.SubItems.Add(EntradaDomicilio.Text);
                                                itm = FormAltaDuplicada.ListaComparacion.Items.Add("Teléfono");
                                                itm.SubItems.Add(ClienteDup["telefono"].ToString());
                                                itm.SubItems.Add(EntradaTelefono.Text);
                                                itm = FormAltaDuplicada.ListaComparacion.Items.Add(Lbl.Sys.Config.Pais.ClavePersonasJuridicas.Nombre);
                                                if (ClienteDup["cuit"] != null)
                                                        itm.SubItems.Add(ClienteDup["cuit"].ToString());
                                                else
                                                        itm.SubItems.Add("");
                                                itm.SubItems.Add(EntradaClaveTributaria.Text);
                                                itm = FormAltaDuplicada.ListaComparacion.Items.Add("E-mail");
                                                if (ClienteDup["email"] != null)
                                                        itm.SubItems.Add(ClienteDup["email"].ToString());
                                                else
                                                        itm.SubItems.Add("");
                                                itm.SubItems.Add(EntradaEmail.Text);

                                                switch (FormAltaDuplicada.ShowDialog()) {
                                                        case DialogResult.Yes:
                                                                //Crear uno nuevo
                                                                return new Lfx.Types.SuccessOperationResult();
                                                        case DialogResult.No:
                                                                //Actualizar
                                                                this.Elemento = new Lbl.Personas.Persona(this.Elemento.Connection, System.Convert.ToInt32(ClienteDup["id_persona"]));
                                                                return new Lfx.Types.SuccessOperationResult();
                                                        case DialogResult.Cancel:
                                                                //Volver a la edición
                                                                return new Lfx.Types.CancelOperationResult();
                                                }
                                        }
                                }
                        }

                        switch (Lbl.Sys.Config.Pais.ClaveBancaria.Nombre) {
                                case "CBU":
                                        if (EntradaClaveBancaria.Text.Length > 0 && Lbl.Bancos.Claves.Cbu.EsValido(EntradaClaveBancaria.Text) == false) {
                                                return new Lfx.Types.FailureOperationResult("La CBU es incorrecta.");
                                        }
                                        break;
                        }


                        if (Cliente.Existe == false && Cliente.Grupo != null && Cliente.Grupo.Id == 2 && (EntradaClaveBancaria.Text.Length > 0 || EntradaNumeroCuenta.Text.Length > 0) && EntradaTipoCuenta.TextKey == "0")
                        {
                                return new Lfx.Types.FailureOperationResult("Por favor seleccione el tipo de cuenta bancaria.");
                        }


                        if (Lbl.Sys.Config.Pais.ClavePersonasJuridicas != null) {
                                switch (Lbl.Sys.Config.Pais.ClavePersonasJuridicas.Nombre) {
                                        case "CUIT":
                                                if (EntradaClaveTributaria.Text.Length > 0) {
                                                        if (EntradaSituacion.ValueInt == 1) {
                                                                return new Lfx.Types.FailureOperationResult(@"Un Cliente con CUIT no debe estar en Situación de ""Consumidor Final"".");
                                                        }
                                                        if (System.Text.RegularExpressions.Regex.IsMatch(EntradaClaveTributaria.Text, @"^\d{11}$")) {
                                                                EntradaClaveTributaria.Text = EntradaClaveTributaria.Text.Substring(0, 2) + "-" + EntradaClaveTributaria.Text.Substring(2, 8) + "-" + EntradaClaveTributaria.Text.Substring(10, 1);
                                                        }

                                                        //Agrego los guiones si no los tiene
                                                        if (EntradaClaveTributaria.Text.Length == 11)
                                                                EntradaClaveTributaria.Text = EntradaClaveTributaria.Text.Substring(0, 2) + "-" + EntradaClaveTributaria.Text.Substring(2, 8) + "-" + EntradaClaveTributaria.Text.Substring(10, 1);

                                                        if (Lbl.Personas.Claves.Cuit.EsValido(EntradaClaveTributaria.Text) == false) {
                                                                return new Lfx.Types.FailureOperationResult("La CUIT no es correcta." + Environment.NewLine + "El sistema le permite dejar la CUIT en blanco, pero no aceptará una incorrecta.");
                                                        }
                                                }
                                                break;
                                }
                        }

                        if (EntradaClaveTributaria.Text.Length > 0) {
                                Lfx.Data.Row RowPersMismaClave = this.Connection.FirstRowFromSelect("SELECT id_persona FROM personas WHERE cuit='" + EntradaClaveTributaria.Text + "' AND id_persona<>" + this.Elemento.Id.ToString());
                                if (RowPersMismaClave != null) {
                                        if (Cliente.Existe == false || System.Convert.ToInt32(RowPersMismaClave["id_persona"]) != this.Elemento.Id) {
                                                Lui.Forms.YesNoDialog Pregunta = new Lui.Forms.YesNoDialog("Ya existe una empresa o persona con esa clave tributaria (" + Lbl.Sys.Config.Pais.ClavePersonasJuridicas.Nombre + ") en la base de datos. ¿Desea continuar y crear una nueva de todos modos?", "Clave tributaria duplicada");
                                                Pregunta.DialogButtons = Lui.Forms.DialogButtons.YesNo;
                                                if (Pregunta.ShowDialog() != DialogResult.OK) {
                                                        return new Lfx.Types.FailureOperationResult("Cambie la Clave tributaria (" + Lbl.Sys.Config.Pais.ClavePersonasJuridicas.Nombre + ") para antes de continuar.");
                                                }
                                        }
                                }
                        }

                        return new Lfx.Types.SuccessOperationResult();
                }

                public override void ActualizarControl()
                {
                        Lbl.Personas.Persona Cliente = this.Elemento as Lbl.Personas.Persona;

                        bool PermitirEdicionAvanzada = Lbl.Sys.Config.Actual.UsuarioConectado.TienePermiso(Cliente, Lbl.Sys.Permisos.Operaciones.EditarAvanzado);
                        bool PermitirEdicionCredito = Lbl.Sys.Config.Actual.UsuarioConectado.TienePermiso(Cliente, Lbl.Sys.Permisos.Operaciones.Extra1);

                        EntradaNombre.Text = Cliente.Nombres;
                        EntradaNombre.Enabled = PermitirEdicionAvanzada;
                        EntradaApellido.Text = Cliente.Apellido;
                        EntradaApellido.Enabled = PermitirEdicionAvanzada;
                        this.Text = Cliente.Nombre;
                        EntradaTipoDoc.Elemento = Cliente.TipoDocumento;
                        EntradaTipoDoc.Enabled = PermitirEdicionAvanzada;
                        EntradaNumDoc.Text = Cliente.NumeroDocumento;
                        EntradaNumDoc.Enabled = PermitirEdicionAvanzada;
                        EntradaGenero.ValueInt = Cliente.Genero;

                        EntradaNombreFantasia.Text = Cliente.NombreFantasia;
                        EntradaNombreFantasia.Enabled = PermitirEdicionAvanzada;
                        EntradaRazonSocial.Text = Cliente.RazonSocial;
                        EntradaRazonSocial.Enabled = PermitirEdicionAvanzada;

                        if (Cliente.TipoClaveTributaria != null)
                                EtiquetaClaveTributaria.Text = Cliente.TipoClaveTributaria.Nombre;

                        if (Cliente.ClaveTributaria != null)
                                EntradaClaveTributaria.Text = Cliente.ClaveTributaria.ToString();
                        else
                                EntradaClaveTributaria.Text = "";
                        EntradaClaveTributaria.Enabled = PermitirEdicionAvanzada;
                        BotonConsultarAfip.Enabled = PermitirEdicionAvanzada;
                        EntradaSituacion.Elemento = Cliente.SituacionTributaria;
                        EntradaSituacion.Enabled = PermitirEdicionAvanzada;
                        if (Cliente.FacturaPreferida == null || Cliente.FacturaPreferida.Length == 0)
                                EntradaTipoFac.TextKey = "*";
                        else
                                EntradaTipoFac.TextKey = Cliente.FacturaPreferida;
                        EntradaTipoFac.Enabled = PermitirEdicionAvanzada;

                        EntradaTipo.ValueInt = Cliente.Tipo;
                        EntradaTipo.Enabled = PermitirEdicionAvanzada;
                        EntradaGrupo.Elemento = Cliente.Grupo;
                        EntradaSubGrupo.Elemento = Cliente.SubGrupo;
                        EntradaGrupo.Enabled = PermitirEdicionAvanzada;
                        EntradaDomicilio.Text = Cliente.Domicilio;
                        EntradaDomicilioTrabajo.Text = Cliente.DomicilioLaboral;
                        EntradaLocalidad.Elemento = Cliente.Localidad;
                        EntradaTelefono.Text = Cliente.Telefono;
                        EntradaEmail.Text = Cliente.Email;
                        EntradaVendedor.Elemento = Cliente.Vendedor;
                        EntradaLimiteCredito.Text = Lfx.Types.Formatting.FormatCurrency(Cliente.LimiteCredito, Lfx.Workspace.Master.CurrentConfig.Moneda.Decimales);
                        EntradaLimiteCredito.Enabled = PermitirEdicionCredito;
                        if (Cliente.FechaNacimiento == null)
                                EntradaFechaNac.Text = "";
                        else
                                EntradaFechaNac.Text = Lfx.Types.Formatting.FormatDate(Cliente.FechaNacimiento);
                        EntradaFechaNac.Enabled = PermitirEdicionAvanzada;

                        EntradaEstadoCredito.TextKey = ((int)(Cliente.EstadoCredito)).ToString();
                        EntradaEstadoCredito.Enabled = PermitirEdicionCredito;
                        EntradaNumeroCuenta.Text = Cliente.NumeroCuenta;
                        EntradaTipoCuenta.TextKey = ((int)(Cliente.TipoCuenta)).ToString();
                        EntradaTipoCuenta.Enabled = PermitirEdicionAvanzada;
                        EntradaNumeroCuenta.Enabled = PermitirEdicionAvanzada;

                        string CBU = Cliente.GetFieldValue<string>("cbu");
                        if (CBU == null)
                                EntradaClaveBancaria.Text = "";
                        else if (CBU.Length == 22)
                                EntradaClaveBancaria.Text = CBU.Substring(0, 8) + "-" + CBU.Substring(8, 14);
                        else
                                EntradaClaveBancaria.Text = CBU;
                        EntradaClaveBancaria.Enabled = PermitirEdicionAvanzada;
                        EntradaObs.Text = Cliente.Obs;
                        EntradaObs.Enabled = PermitirEdicionAvanzada;

                        EntradaEstado.TextKey = Cliente.Estado.ToString();

                        if (Cliente.FechaAlta == null)
                                EntradaFechaAlta.Text = "";
                        else
                                EntradaFechaAlta.Text = Cliente.FechaAlta.Value.ToString(Lfx.Types.Formatting.DateTime.ShortDatePattern);

                        EntradaFechaAlta.TemporaryReadOnly = true;
                        EntradaFechaBaja.TemporaryReadOnly = true;

                        if (Cliente.FechaBaja == null)
                                EntradaFechaBaja.Text = "";
                        else
                                EntradaFechaBaja.Text = Cliente.FechaBaja.Value.ToString(Lfx.Types.Formatting.DateTime.ShortDatePattern);

                        // TODO: pasar esto al formulario parent
                        //EntradaImagen.Enabled = PermitirEdicionAvanzada;
                        //EntradaTags.Enabled = PermitirEdicionAvanzada;

                        EntradaEstado.Enabled = Lbl.Sys.Config.Actual.UsuarioConectado.TienePermiso(Cliente, Lbl.Sys.Permisos.Operaciones.Eliminar);
                        EntradaEstado.ReadOnly = !Lbl.Sys.Config.Actual.UsuarioConectado.TienePermiso(Cliente, Lbl.Sys.Permisos.Operaciones.Eliminar);

                        base.ActualizarControl();
                }

                private void GenerarNombreVisible(System.Object sender, System.EventArgs e)
                {
                        string Res = "";
                        if (EntradaNombreFantasia.Text.Length > 0) {
                                Res += EntradaNombreFantasia.Text.Trim().ToTitleCase();
                        }
                        else if(EntradaRazonSocial.Text.Length > 0) {
                                Res += EntradaRazonSocial.Text.Trim().ToTitleCase();
                        } else {
                                if (EntradaApellido.Text.Length > 0) {
                                        Res += EntradaApellido.Text.Trim();
                                        if (EntradaNombre.Text.Length > 0)
                                                Res += ", " + EntradaNombre.Text.Trim();
                                } else {
                                        if (EntradaNombre.Text.Length > 0)
                                                Res += EntradaNombre.Text.Trim();
                                }
                        }
                        this.Text = Res;
                }


                private void EntradaClaveTributaria_Leave(object sender, System.EventArgs e)
                {
                        if (Lbl.Sys.Config.Pais.ClavePersonasJuridicas != null && Lbl.Sys.Config.Pais.ClavePersonasJuridicas.Id == 6) {
                                if (EntradaClaveTributaria.Text.Length == 11)
                                        EntradaClaveTributaria.Text = EntradaClaveTributaria.Text.Substring(0, 2) + "-" + EntradaClaveTributaria.Text.Substring(2, 8) + "-" + EntradaClaveTributaria.Text.Substring(10, 1);

                                if (EntradaClaveTributaria.Text.Length == 13 && Lbl.Personas.Claves.Cuit.EsValido(EntradaClaveTributaria.Text) == false)
                                        EntradaClaveTributaria.ErrorText = "La CUIT ingresada no es válida.";
                                else if (EntradaClaveTributaria.Text.Length > 0 && EntradaClaveTributaria.Text.Length != 13)
                                        EntradaClaveTributaria.ErrorText = "La CUIT ingresada no es válida.";
                                else
                                        EntradaClaveTributaria.ErrorText = null;
                        }
                }


                private void BotonConsultarAfip_Click(object sender, EventArgs e)
                {
                        string cuitTexto = EntradaClaveTributaria.Text != null ? EntradaClaveTributaria.Text.Trim() : "";
                        if (string.IsNullOrEmpty(cuitTexto))
                        {
                                Lui.Forms.MessageBox.Show("Por favor ingrese un número de CUIT para consultar en ARCA / AFIP.", "Consulta AFIP");
                                EntradaClaveTributaria.Focus();
                                return;
                        }

                        string cuitLimpio = cuitTexto.Replace("-", "").Replace(".", "").Replace(" ", "").Replace("/", "").Trim();
                        if (cuitLimpio.Length != 11)
                        {
                                Lui.Forms.MessageBox.Show("El número de CUIT debe tener 11 dígitos numéricos.", "Consulta AFIP");
                                EntradaClaveTributaria.Focus();
                                return;
                        }

                        if (!Lbl.Personas.Claves.Cuit.EsValido(cuitLimpio))
                        {
                                Lui.Forms.MessageBox.Show("El número de CUIT ingresado no es válido según su dígito verificador.", "Consulta AFIP");
                                EntradaClaveTributaria.Focus();
                                return;
                        }

                        Cursor prevCursor = this.Cursor;
                        this.Cursor = Cursors.WaitCursor;
                        try
                        {
                                var res = Lbl.Impuestos.Monotributo.ConsultaConstanciaAfip.ConsultarContribuyente(cuitLimpio);

                                if (!res.Exito)
                                {
                                        if (res.RequierePermisoWebservice)
                                        {
                                                Lui.Forms.MessageBox.Show(
                                                        res.Mensaje + "\r\n\r\n" + Lbl.Impuestos.Monotributo.ResultadoConsultaConstancia.ObtenerGuiaConfiguracionPermiso(),
                                                        "Permiso en AFIP requerido");
                                        }
                                        else
                                        {
                                                Lui.Forms.MessageBox.Show(
                                                        res.Mensaje ?? "No se obtuvieron datos para el CUIT consultado en ARCA / AFIP.",
                                                        "Consulta AFIP");
                                        }
                                        return;
                                }

                                // 1. Formatear y asignar CUIT con guiones
                                EntradaClaveTributaria.Text = cuitLimpio.Substring(0, 2) + "-" + cuitLimpio.Substring(2, 8) + "-" + cuitLimpio.Substring(10, 1);
                                EntradaClaveTributaria.ErrorText = null;

                                // 2. Completar datos según tipo de persona (Física vs Jurídica)
                                bool esJuridica = string.Equals(res.TipoPersona, "JURIDICA", StringComparison.OrdinalIgnoreCase);

                                if (esJuridica)
                                {
                                        EntradaRazonSocial.Text = (res.RazonSocial ?? "").Trim();
                                        EntradaApellido.Text = "";
                                        EntradaNombre.Text = "";
                                        EntradaNumDoc.Text = "";
                                }
                                else
                                {
                                        EntradaRazonSocial.Text = "";
                                        EntradaApellido.Text = (res.Apellido ?? "").Trim();
                                        EntradaNombre.Text = (res.Nombre ?? "").Trim();

                                        if (Lbl.Sys.Config.Pais.ClavePersonasFisicas != null)
                                        {
                                                EntradaTipoDoc.Elemento = Lbl.Sys.Config.Pais.ClavePersonasFisicas;
                                        }

                                        if (!string.IsNullOrWhiteSpace(res.NumeroDocumento))
                                        {
                                                EntradaNumDoc.Text = res.NumeroDocumento;
                                        }
                                        else if (cuitLimpio.Length == 11)
                                        {
                                                EntradaNumDoc.Text = cuitLimpio.Substring(2, 8).TrimStart('0');
                                        }
                                }

                                // Regenerar título/nombre visible del formulario
                                GenerarNombreVisible(null, null);

                                // 3. Domicilio fiscal
                                if (!string.IsNullOrWhiteSpace(res.Domicilio))
                                {
                                        EntradaDomicilio.Text = res.Domicilio.Trim();
                                }

                                // 4. Localidad
                                if (this.Connection != null)
                                {
                                        int idCiudad = 0;
                                        if (!string.IsNullOrWhiteSpace(res.CodigoPostal))
                                        {
                                                string cpSoloNum = System.Text.RegularExpressions.Regex.Replace(res.CodigoPostal, @"\D", "");
                                                string sqlCp = "SELECT id_ciudad FROM ciudades WHERE cp = '" + this.Connection.EscapeString(res.CodigoPostal.Trim()) + "'";
                                                if (!string.IsNullOrEmpty(cpSoloNum) && cpSoloNum != res.CodigoPostal.Trim())
                                                {
                                                        sqlCp += " OR cp = '" + this.Connection.EscapeString(cpSoloNum) + "'";
                                                }
                                                sqlCp += " LIMIT 1";

                                                var rowCp = this.Connection.FirstRowFromSelect(sqlCp);
                                                if (rowCp != null && rowCp["id_ciudad"] != null)
                                                {
                                                        idCiudad = System.Convert.ToInt32(rowCp["id_ciudad"]);
                                                }
                                        }

                                        if (idCiudad == 0 && !string.IsNullOrWhiteSpace(res.Localidad))
                                        {
                                                string sqlLoc = "SELECT id_ciudad FROM ciudades WHERE nombre LIKE '%" + this.Connection.EscapeString(res.Localidad.Trim()) + "%' LIMIT 1";
                                                var rowLoc = this.Connection.FirstRowFromSelect(sqlLoc);
                                                if (rowLoc != null && rowLoc["id_ciudad"] != null)
                                                {
                                                        idCiudad = System.Convert.ToInt32(rowLoc["id_ciudad"]);
                                                }
                                        }

                                        if (idCiudad > 0)
                                        {
                                                EntradaLocalidad.Elemento = new Lbl.Entidades.Localidad(this.Connection, idCiudad);
                                        }
                                }

                                // 5. Condición fiscal / Situación tributaria
                                if (res.IdSituacionTributaria > 0 && this.Connection != null)
                                {
                                        EntradaSituacion.Elemento = new Lbl.Impuestos.SituacionTributaria(this.Connection, res.IdSituacionTributaria);
                                        EntradaSituacion.ErrorText = "";
                                }

                                // Notificar resultado amigable al usuario
                                string detalle = esJuridica ?
                                        ("Razón Social: " + (res.RazonSocial ?? "")) :
                                        ("Nombre: " + ((res.Apellido ?? "") + " " + (res.Nombre ?? "")).Trim());
                                if (!string.IsNullOrEmpty(res.DescripcionSituacion))
                                        detalle += "\r\nCondición: " + res.DescripcionSituacion;
                                if (!string.IsNullOrEmpty(res.Domicilio))
                                        detalle += "\r\nDomicilio: " + res.Domicilio;

                                Lui.Forms.MessageBox.Show(
                                        "Datos obtenidos de ARCA / AFIP:\r\n\r\n" + detalle,
                                        "Consulta AFIP");
                        }
                        catch (Exception ex)
                        {
                                Lui.Forms.MessageBox.Show("Error al consultar datos en ARCA / AFIP: " + ex.Message, "Error AFIP");
                        }
                        finally
                        {
                                this.Cursor = prevCursor;
                        }
                }


                private void EntradaSituacion_Leave(object sender, System.EventArgs e)
                {
                        if (Lbl.Sys.Config.Pais.ClavePersonasJuridicas != null && Lbl.Sys.Config.Pais.ClavePersonasJuridicas.Id == 6) {
                                if (EntradaClaveTributaria.Text.Length > 0) {
                                        if (EntradaSituacion.ValueInt == 1)
                                                EntradaSituacion.ErrorText = "La situación tributaria del cliente no se corresponde con la CUIT.";
                                        else
                                                EntradaSituacion.ErrorText = "";
                                }
                        }
                }


                private Lfx.Types.OperationResult EditarPermisos()
                {
                        if (Lui.LogOn.LogOnData.ValidateAccess(this.Elemento, Lbl.Sys.Permisos.Operaciones.Administrar)) {
                                if (this.Changed || this.Elemento.Existe == false) {
                                        Lui.Forms.YesNoDialog Pregunta = new Lui.Forms.YesNoDialog("Antes de editar los permisos debe guardar los cambios en este formulario. ¿Desea guardar ahora?", "Editar Permisos");
                                        Pregunta.DialogButtons = Lui.Forms.DialogButtons.YesNo;
                                        if (this.Elemento.Existe)
                                                this.ShowChanged = true;
                                        DialogResult Respuesta = Pregunta.ShowDialog();
                                        this.ShowChanged = false;
                                        if (Respuesta != DialogResult.OK) {
                                                return new Lfx.Types.CancelOperationResult();
                                        } else {
                                                Lfx.Types.OperationResult ResultadoGuardar = this.Save();
                                                if (ResultadoGuardar.Success == false)
                                                        return ResultadoGuardar;
                                        }
                                }
                                Lfx.Workspace.Master.RunTime.Execute("EDITAR Lbl.Personas.Usuario " + this.Elemento.Id.ToString());
                                return new Lfx.Types.SuccessOperationResult();
                        } else {
                                return new Lfx.Types.NoAccessOperationResult();
                        }
                }

                public override void ActualizarElemento()
                {
                        Lbl.Personas.Persona Res = this.Elemento as Lbl.Personas.Persona;

                        Res.Tipo = EntradaTipo.ValueInt;
                        Res.Grupo = EntradaGrupo.Elemento as Lbl.Personas.Grupo;
                        Res.SubGrupo = EntradaSubGrupo.Elemento as Lbl.Personas.Grupo;
                        Res.Nombres = EntradaNombre.Text.Trim();
                        Res.Apellido = EntradaApellido.Text.Trim();
                        Res.NombreFantasia = EntradaNombreFantasia.Text.Trim();
                        Res.RazonSocial = EntradaRazonSocial.Text.Trim();
                        Res.Nombre = this.Text;
                        Res.Genero = EntradaGenero.ValueInt;
                        Res.TipoDocumento = EntradaTipoDoc.Elemento as Lbl.Entidades.ClaveUnica;
                        Res.NumeroDocumento = EntradaNumDoc.Text;
                        if (EntradaClaveTributaria.Text.Length > 0)
                                Res.ClaveTributaria = new Lbl.Personas.Claves.Cuit(EntradaClaveTributaria.Text);
                        else
                                Res.ClaveTributaria = null;
                        Res.Estado = Lfx.Types.Parsing.ParseInt(EntradaEstado.TextKey);
                        Res.SituacionTributaria = EntradaSituacion.Elemento as Lbl.Impuestos.SituacionTributaria;

                        if (EntradaTipoFac.TextKey == "*")
                                Res.FacturaPreferida = null;
                        else
                                Res.FacturaPreferida = EntradaTipoFac.TextKey;

                        Res.Domicilio = EntradaDomicilio.Text;
                        Res.DomicilioLaboral = EntradaDomicilioTrabajo.Text;
                        Res.Localidad = EntradaLocalidad.Elemento as Lbl.Entidades.Localidad;
                        Res.Telefono = EntradaTelefono.Text;
                        Res.Email = EntradaEmail.Text;
                        Res.Vendedor = EntradaVendedor.Elemento as Lbl.Personas.Persona;
                        Res.Obs = EntradaObs.Text;
                        Res.LimiteCredito = Lfx.Types.Parsing.ParseCurrency(EntradaLimiteCredito.Text);
                        Res.FechaNacimiento = Lfx.Types.Parsing.ParseDate(EntradaFechaNac.Text);
                        Res.NumeroCuenta = EntradaNumeroCuenta.Text;
                        Res.TipoCuenta = (Lbl.Personas.TiposCuenta)(Lfx.Types.Parsing.ParseInt(EntradaTipoCuenta.TextKey));
                        Res.ClaveBancaria = EntradaClaveBancaria.Text.Replace("-", "").Replace(" ", "").Replace("/", "").Replace(".", "");
                        Res.EstadoCredito = ((Lbl.Personas.EstadoCredito)(Lfx.Types.Parsing.ParseInt(EntradaEstadoCredito.TextKey)));
                        Res.Estado = Lfx.Types.Parsing.ParseInt(EntradaEstado.TextKey);

                        base.ActualizarElemento();
                }

                private void EntradaGrupo_TextChanged(object sender, EventArgs e)
                {
                        if (EntradaGrupo.ValueInt == 0) {
                                EntradaSubGrupo.ValueInt = 0;
                                EntradaSubGrupo.Enabled = false;
                        } else {
                                EntradaSubGrupo.Enabled = true;
                                EntradaSubGrupo.Filter = "parent=" + EntradaGrupo.ValueInt.ToString();
                        }
                }

                private void EntradaEmail_Leave(object sender, EventArgs e)
                {
                        EntradaEmail.Text = EntradaEmail.Text.Replace(" ", "").Replace("/", ", ").Replace(";", ", ");
                }

                private void EntradaTipo_TextChanged(object sender, EventArgs e)
                {
                        this.FireFormActionsChanged();
                }


                public override Lazaro.Pres.Forms.FormActionCollection GetFormActions()
                {
                        Lazaro.Pres.Forms.FormActionCollection Res = base.GetFormActions();
                        if (this.Elemento != null && Lbl.Sys.Config.Actual.UsuarioConectado.TienePermiso(this.Elemento, Lbl.Sys.Permisos.Operaciones.Administrar) && (EntradaTipo.ValueInt & 4) == 4)
                                Res.Add(new Lazaro.Pres.Forms.FormAction("Permisos", "F2", "permisos", 10, Lazaro.Pres.Forms.FormActionVisibility.Secondary));
                        return Res;
                }


                public override Lfx.Types.OperationResult PerformFormAction(string name)
                {
                        switch (name)
                        {
                                case "permisos":
                                        return EditarPermisos();
                                default:
                                        return base.PerformFormAction(name);
                        }
                }


                public override Lazaro.Pres.DisplayStyles.IDisplayStyle HeaderDisplayStyle
                {
                        get
                        {
                                return Lazaro.Pres.DisplayStyles.Template.Current.Personas;
                        }
                }


                protected override void OnSizeChanged(EventArgs e)
                {
                        base.OnSizeChanged(e);
                        if (this.Created) {
                                this.SuspendLayout();
                                PanelI1.Width = (this.ClientRectangle.Width - 16) / 2;
                                PanelI2.Width = PanelI1.Width;
                                PanelD1.Width = PanelI1.Width;
                                PanelD2.Width = PanelI1.Width;

                                PanelD1.Left = PanelI1.Width + 16;
                                PanelD2.Left = PanelD1.Left;
                                this.ResumeLayout(true);
                        }
                }
        }
}