using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Lbl.Impuestos.Monotributo;

namespace Lazaro.WinMain.Principal
{
	public class FormConfigurarEscalasMonotributo : Lui.Forms.Form
	{
		private DataGridView GrillaEscalas;
		private Button BotonSincronizarWebAfip;
		private TextBox EntradaApiKey;
		private ComboBox ComboActividad;
		private Button BotonSincronizarServidos;
		private LinkLabel LinkServidosDev;
		private Label LabelEstadoSync;

		private TextBox EntradaUrl;
		private Button BotonDescargarUrl;

		private Button BotonRestablecer;
		private Button BotonGuardar;
		private Button BotonCancelar;
		private Label EtiquetaExplicacion;

		public FormConfigurarEscalasMonotributo()
		{
			InitializeComponentCustom();
			CargarDatos();
		}

		private void InitializeComponentCustom()
		{
			this.Text = "Configuración de Escalas de Monotributo (ARCA / AFIP)";
			this.Size = new Size(760, 680);
			this.StartPosition = FormStartPosition.CenterParent;
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
			this.BackColor = Color.White;

			// Panel Superior de Sincronización e Información
			Panel panelTop = new Panel();
			panelTop.Dock = DockStyle.Top;
			panelTop.Height = 252;
			panelTop.Padding = new Padding(12);
			panelTop.BackColor = Color.FromArgb(246, 248, 252);

			EtiquetaExplicacion = new Label();
			EtiquetaExplicacion.Text = "ARCA (ex AFIP) actualiza semestralmente los límites máximos de facturación y cuotas del Monotributo según el IPC. " +
				"Puede sincronizarlas de forma manual en 1 clic desde la web oficial de AFIP (sin clave), vía Servidos API, o editarlas en la grilla.";
			EtiquetaExplicacion.Location = new Point(12, 8);
			EtiquetaExplicacion.Size = new Size(715, 30);
			EtiquetaExplicacion.ForeColor = Color.FromArgb(50, 60, 75);
			panelTop.Controls.Add(EtiquetaExplicacion);

			// GroupBox 1: Sincronización Oficial Directa Web AFIP / ARCA (Recomendada - Sin clave)
			GroupBox gbWebAfip = new GroupBox();
			gbWebAfip.Text = " Sincronización oficial directa desde la Web de ARCA / AFIP (Recomendado — Sin API Key) ";
			gbWebAfip.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
			gbWebAfip.ForeColor = Color.FromArgb(20, 80, 45);
			gbWebAfip.Location = new Point(12, 40);
			gbWebAfip.Size = new Size(715, 68);

			BotonSincronizarWebAfip = new Button();
			BotonSincronizarWebAfip.Text = "🌐 Sincronizar desde Web Oficial AFIP";
			BotonSincronizarWebAfip.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
			BotonSincronizarWebAfip.Location = new Point(12, 22);
			BotonSincronizarWebAfip.Size = new Size(310, 34);
			BotonSincronizarWebAfip.BackColor = Color.FromArgb(28, 120, 60);
			BotonSincronizarWebAfip.ForeColor = Color.White;
			BotonSincronizarWebAfip.UseVisualStyleBackColor = false;
			BotonSincronizarWebAfip.Cursor = Cursors.Hand;
			BotonSincronizarWebAfip.Click += new EventHandler(BotonSincronizarWebAfip_Click);
			gbWebAfip.Controls.Add(BotonSincronizarWebAfip);

			Label lblWebInfo = new Label();
			lblWebInfo.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular);
			lblWebInfo.ForeColor = Color.FromArgb(60, 75, 70);
			lblWebInfo.Location = new Point(332, 22);
			lblWebInfo.Size = new Size(370, 36);
			lblWebInfo.Text = "Descarga en un clic la tabla oficial de afip.gob.ar/monotributo/categorias.asp.\r\nActualización manual y directa sin requerir registro ni API Key.";
			gbWebAfip.Controls.Add(lblWebInfo);

			panelTop.Controls.Add(gbWebAfip);

			// GroupBox 2: Integración alternativa con Servidos Developers API o URL
			GroupBox gbServidos = new GroupBox();
			gbServidos.Text = " Alternativas: Servidos Developers Tax API (servidos.ar) o URL externa ";
			gbServidos.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
			gbServidos.ForeColor = Color.FromArgb(40, 65, 110);
			gbServidos.Location = new Point(12, 112);
			gbServidos.Size = new Size(715, 104);

			LinkServidosDev = new LinkLabel();
			LinkServidosDev.Text = "Obtener API Key gratuita (500 peticiones/mes) en servidos.ar/developers";
			LinkServidosDev.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular);
			LinkServidosDev.Location = new Point(12, 20);
			LinkServidosDev.Size = new Size(420, 16);
			LinkServidosDev.LinkClicked += (s, e) =>
			{
				try
				{
					Process.Start(new ProcessStartInfo("https://servidos.ar/developers") { UseShellExecute = true });
				}
				catch { }
			};
			gbServidos.Controls.Add(LinkServidosDev);

			Label lblKey = new Label();
			lblKey.Text = "API Key:";
			lblKey.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular);
			lblKey.ForeColor = Color.FromArgb(40, 50, 60);
			lblKey.Location = new Point(12, 40);
			lblKey.Size = new Size(55, 22);
			lblKey.TextAlign = ContentAlignment.MiddleLeft;
			gbServidos.Controls.Add(lblKey);

			EntradaApiKey = new TextBox();
			EntradaApiKey.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular);
			EntradaApiKey.Location = new Point(68, 40);
			EntradaApiKey.Size = new Size(170, 22);
			EntradaApiKey.Text = Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null ?
				Lfx.Workspace.Master.CurrentConfig.ReadGlobalSetting<string>("Sistema.Monotributo.ServidosApiKey", "") : "";
			gbServidos.Controls.Add(EntradaApiKey);

			Label lblAct = new Label();
			lblAct.Text = "Actividad:";
			lblAct.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular);
			lblAct.ForeColor = Color.FromArgb(40, 50, 60);
			lblAct.Location = new Point(244, 40);
			lblAct.Size = new Size(60, 22);
			lblAct.TextAlign = ContentAlignment.MiddleLeft;
			gbServidos.Controls.Add(lblAct);

			ComboActividad = new ComboBox();
			ComboActividad.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular);
			ComboActividad.DropDownStyle = ComboBoxStyle.DropDownList;
			ComboActividad.Items.Add("Servicios");
			ComboActividad.Items.Add("Comercio / Cosas Muebles");
			string tipoGuardado = Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null ?
				Lfx.Workspace.Master.CurrentConfig.ReadGlobalSetting<string>("Sistema.Monotributo.TipoActividad", "servicios") : "servicios";
			ComboActividad.SelectedIndex = (tipoGuardado == "comercio") ? 1 : 0;
			ComboActividad.Location = new Point(306, 40);
			ComboActividad.Size = new Size(140, 22);
			gbServidos.Controls.Add(ComboActividad);

			BotonSincronizarServidos = new Button();
			BotonSincronizarServidos.Text = "Sincronizar Servidos";
			BotonSincronizarServidos.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
			BotonSincronizarServidos.Location = new Point(454, 38);
			BotonSincronizarServidos.Size = new Size(135, 26);
			BotonSincronizarServidos.BackColor = Color.FromArgb(41, 128, 185);
			BotonSincronizarServidos.ForeColor = Color.White;
			BotonSincronizarServidos.UseVisualStyleBackColor = false;
			BotonSincronizarServidos.Click += new EventHandler(BotonSincronizarServidos_Click);
			gbServidos.Controls.Add(BotonSincronizarServidos);

			Label lblUrl = new Label();
			lblUrl.Text = "URL JSON:";
			lblUrl.Font = new Font("Segoe UI", 8F);
			lblUrl.ForeColor = Color.FromArgb(80, 90, 100);
			lblUrl.Location = new Point(12, 70);
			lblUrl.Size = new Size(65, 22);
			lblUrl.TextAlign = ContentAlignment.MiddleLeft;
			gbServidos.Controls.Add(lblUrl);

			EntradaUrl = new TextBox();
			EntradaUrl.Font = new Font("Segoe UI", 8F);
			EntradaUrl.Location = new Point(78, 70);
			EntradaUrl.Size = new Size(510, 22);
			EntradaUrl.Text = Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null ?
				Lfx.Workspace.Master.CurrentConfig.ReadGlobalSetting<string>("Sistema.Monotributo.UrlApiEscalas", "") : "";
			gbServidos.Controls.Add(EntradaUrl);

			BotonDescargarUrl = new Button();
			BotonDescargarUrl.Text = "Cargar URL";
			BotonDescargarUrl.Font = new Font("Segoe UI", 8F);
			BotonDescargarUrl.Location = new Point(598, 69);
			BotonDescargarUrl.Size = new Size(105, 24);
			BotonDescargarUrl.BackColor = Color.FromArgb(235, 240, 248);
			BotonDescargarUrl.Click += new EventHandler(BotonDescargarUrl_Click);
			gbServidos.Controls.Add(BotonDescargarUrl);

			panelTop.Controls.Add(gbServidos);

			LabelEstadoSync = new Label();
			LabelEstadoSync.Font = new Font("Segoe UI", 8.25F, FontStyle.Italic);
			LabelEstadoSync.ForeColor = Color.FromArgb(70, 85, 100);
			LabelEstadoSync.Location = new Point(12, 222);
			LabelEstadoSync.Size = new Size(715, 22);
			string vigencia = EscalasMonotributo.UltimaVigenciaMetadata;
			LabelEstadoSync.Text = !string.IsNullOrEmpty(vigencia) ? ("Estado: " + vigencia) : "Estado: Escalas vigentes oficiales de ARCA cargadas.";
			panelTop.Controls.Add(LabelEstadoSync);

			this.Controls.Add(panelTop);

			// Grilla Central de Categorías
			GrillaEscalas = new DataGridView();
			GrillaEscalas.Dock = DockStyle.Fill;
			GrillaEscalas.AllowUserToAddRows = false;
			GrillaEscalas.AllowUserToDeleteRows = false;
			GrillaEscalas.RowHeadersVisible = false;
			GrillaEscalas.BackgroundColor = Color.White;
			GrillaEscalas.BorderStyle = BorderStyle.None;
			GrillaEscalas.SelectionMode = DataGridViewSelectionMode.CellSelect;
			GrillaEscalas.MultiSelect = false;

			DataGridViewTextBoxColumn colCat = new DataGridViewTextBoxColumn();
			colCat.Name = "Letra";
			colCat.HeaderText = "Categoría";
			colCat.Width = 90;
			colCat.ReadOnly = true;
			colCat.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
			colCat.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
			GrillaEscalas.Columns.Add(colCat);

			DataGridViewTextBoxColumn colTope = new DataGridViewTextBoxColumn();
			colTope.Name = "Tope";
			colTope.HeaderText = "Ingresos Brutos Máximos Anuales ($)";
			colTope.Width = 260;
			colTope.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
			colTope.DefaultCellStyle.Format = "N0";
			GrillaEscalas.Columns.Add(colTope);

			DataGridViewTextBoxColumn colServ = new DataGridViewTextBoxColumn();
			colServ.Name = "CuotaServicios";
			colServ.HeaderText = "Cuota Servicios ($)";
			colServ.Width = 170;
			colServ.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
			colServ.DefaultCellStyle.Format = "N0";
			GrillaEscalas.Columns.Add(colServ);

			DataGridViewTextBoxColumn colBien = new DataGridViewTextBoxColumn();
			colBien.Name = "CuotaBienes";
			colBien.HeaderText = "Cuota Venta Bienes ($)";
			colBien.Width = 175;
			colBien.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
			colBien.DefaultCellStyle.Format = "N0";
			GrillaEscalas.Columns.Add(colBien);

			this.Controls.Add(GrillaEscalas);
			GrillaEscalas.BringToFront();

			// Panel Inferior de Acciones
			Panel panelBottom = new Panel();
			panelBottom.Dock = DockStyle.Bottom;
			panelBottom.Height = 52;
			panelBottom.BackColor = Color.FromArgb(245, 246, 248);

			BotonRestablecer = new Button();
			BotonRestablecer.Text = "Restablecer Oficiales";
			BotonRestablecer.Location = new Point(12, 12);
			BotonRestablecer.Size = new Size(160, 28);
			BotonRestablecer.Click += new EventHandler(BotonRestablecer_Click);
			panelBottom.Controls.Add(BotonRestablecer);

			BotonGuardar = new Button();
			BotonGuardar.Text = "Guardar";
			BotonGuardar.Location = new Point(515, 12);
			BotonGuardar.Size = new Size(100, 28);
			BotonGuardar.BackColor = Color.FromArgb(41, 128, 185);
			BotonGuardar.ForeColor = Color.White;
			BotonGuardar.UseVisualStyleBackColor = false;
			BotonGuardar.Click += new EventHandler(BotonGuardar_Click);
			panelBottom.Controls.Add(BotonGuardar);

			BotonCancelar = new Button();
			BotonCancelar.Text = "Cancelar";
			BotonCancelar.Location = new Point(622, 12);
			BotonCancelar.Size = new Size(95, 28);
			BotonCancelar.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
			panelBottom.Controls.Add(BotonCancelar);

			this.Controls.Add(panelBottom);
		}

		private void CargarDatos()
		{
			GrillaEscalas.Rows.Clear();
			var categorias = EscalasMonotributo.Instancia.Categorias;
			foreach (var cat in categorias)
			{
				GrillaEscalas.Rows.Add(cat.Letra, Math.Round(cat.IngresosBrutosMaximos, 0), Math.Round(cat.CuotaServicios, 0), Math.Round(cat.CuotaBienes, 0));
			}

			string meta = EscalasMonotributo.UltimaVigenciaMetadata;
			if (!string.IsNullOrEmpty(meta))
			{
				LabelEstadoSync.Text = "Estado: " + meta;
			}
		}

		private void BotonSincronizarWebAfip_Click(object sender, EventArgs e)
		{
			string tipo = ComboActividad.SelectedIndex == 1 ? "comercio" : "servicios";

			Cursor = Cursors.WaitCursor;
			try
			{
				string infoMeta;
				string error;
				bool ok = EscalasMonotributo.Instancia.SincronizarDesdeWebAfip(tipo, out infoMeta, out error);
				if (ok)
				{
					CargarDatos();
					LabelEstadoSync.Text = "Sincronizado: " + (!string.IsNullOrEmpty(infoMeta) ? infoMeta : "Escalas oficiales actualizadas desde web de AFIP");
					LabelEstadoSync.ForeColor = Color.FromArgb(0, 120, 0);

					MessageBox.Show("Las escalas de categorías de Monotributo se sincronizaron exitosamente desde el portal oficial de ARCA / AFIP (afip.gob.ar).\r\n\r\n" +
						infoMeta + "\r\n\r\nLos nuevos topes y cuotas vigentes ya se encuentran cargados en la grilla.",
						"Sincronización Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				else
				{
					MessageBox.Show("No se pudieron sincronizar las escalas desde la página web de AFIP:\r\n\r\n" + error +
						"\r\n\r\nPuede intentar sincronizar mediante Servidos API, ingresar los valores manualmente en la grilla, o verificar su conexión a Internet.",
						"Aviso de Sincronización Web", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private void BotonSincronizarServidos_Click(object sender, EventArgs e)
		{
			string apiKey = EntradaApiKey.Text.Trim();
			if (string.IsNullOrEmpty(apiKey))
			{
				MessageBox.Show("Por favor ingrese su API Key de Servidos Developers.\r\nPuede obtener una de forma gratuita (500 peticiones/mes) registrándose en https://servidos.ar/developers.",
					"API Key Requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				EntradaApiKey.Focus();
				return;
			}

			string tipo = ComboActividad.SelectedIndex == 1 ? "comercio" : "servicios";

			Cursor = Cursors.WaitCursor;
			try
			{
				string infoMeta;
				string error;
				bool ok = EscalasMonotributo.Instancia.SincronizarServidosApi(apiKey, tipo, out infoMeta, out error);
				if (ok)
				{
					if (Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null)
					{
						Lfx.Workspace.Master.CurrentConfig.WriteGlobalSetting("Sistema.Monotributo.ServidosApiKey", apiKey);
						Lfx.Workspace.Master.CurrentConfig.WriteGlobalSetting("Sistema.Monotributo.TipoActividad", tipo);
					}

					CargarDatos();
					LabelEstadoSync.Text = "Sincronizado: " + (string.IsNullOrEmpty(infoMeta) ? "Escalas ARCA actualizadas" : infoMeta);
					LabelEstadoSync.ForeColor = Color.FromArgb(0, 120, 0);

					MessageBox.Show("Las escalas de categorías de Monotributo se actualizaron exitosamente desde Servidos API.\r\n\r\n" + infoMeta,
						"Sincronización Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				else
				{
					MessageBox.Show("No se pudieron sincronizar las escalas:\r\n\r\n" + error,
						"Error de Sincronización", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private void BotonDescargarUrl_Click(object sender, EventArgs e)
		{
			string url = EntradaUrl.Text.Trim();
			if (string.IsNullOrEmpty(url))
			{
				MessageBox.Show("Por favor especifique la URL del endpoint o servicio JSON para descargar las escalas.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			Cursor = Cursors.WaitCursor;
			try
			{
				bool ok = EscalasMonotributo.Instancia.ActualizarDesdeUrl(url);
				if (ok)
				{
					if (Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null)
					{
						Lfx.Workspace.Master.CurrentConfig.WriteGlobalSetting("Sistema.Monotributo.UrlApiEscalas", url);
					}
					CargarDatos();
					LabelEstadoSync.Text = "Sincronizado desde URL JSON personalizada";
					LabelEstadoSync.ForeColor = Color.FromArgb(0, 120, 0);
					MessageBox.Show("Escalas sincronizadas exitosamente desde la URL proporcionada.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				else
				{
					MessageBox.Show("No se pudieron obtener o procesar las escalas desde la URL indicada.\r\nVerifique la conexión o el formato JSON.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private void BotonRestablecer_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("¿Desea restablecer todas las categorías a los valores oficiales predeterminados?",
				"Confirmación", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				EscalasMonotributo.Instancia.RestablecerPredeterminados();
				CargarDatos();
				LabelEstadoSync.Text = "Estado: Valores oficiales predeterminados vigentes.";
				LabelEstadoSync.ForeColor = Color.FromArgb(70, 85, 100);
				MessageBox.Show("Las escalas han sido restablecidas a los valores oficiales vigentes.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private decimal ParsearMonto(object val)
		{
			if (val == null) return 0m;
			string s = Convert.ToString(val).Replace("$", "").Trim();
			if (string.IsNullOrEmpty(s)) return 0m;

			if (decimal.TryParse(s, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, new CultureInfo("es-AR"), out decimal res))
				return Math.Round(res, 0);

			if (decimal.TryParse(s, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.InvariantCulture, out res))
				return Math.Round(res, 0);

			return 0m;
		}

		private void BotonGuardar_Click(object sender, EventArgs e)
		{
			List<CategoriaMonotributo> nuevas = new List<CategoriaMonotributo>();

			foreach (DataGridViewRow row in GrillaEscalas.Rows)
			{
				if (row.IsNewRow) continue;
				string letra = Convert.ToString(row.Cells["Letra"].Value);
				decimal tope = ParsearMonto(row.Cells["Tope"].Value);
				decimal serv = ParsearMonto(row.Cells["CuotaServicios"].Value);
				decimal bien = ParsearMonto(row.Cells["CuotaBienes"].Value);

				if (!string.IsNullOrEmpty(letra) && tope > 0)
				{
					nuevas.Add(new CategoriaMonotributo(letra, tope, serv, bien));
				}
			}

			if (nuevas.Count == 0)
			{
				MessageBox.Show("Debe especificar al menos una categoría con tope mayor a cero.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			EscalasMonotributo.Instancia.Guardar(nuevas);

			if (Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null)
			{
				Lfx.Workspace.Master.CurrentConfig.WriteGlobalSetting("Sistema.Monotributo.ServidosApiKey", EntradaApiKey.Text.Trim());
				Lfx.Workspace.Master.CurrentConfig.WriteGlobalSetting("Sistema.Monotributo.TipoActividad", ComboActividad.SelectedIndex == 1 ? "comercio" : "servicios");
				Lfx.Workspace.Master.CurrentConfig.WriteGlobalSetting("Sistema.Monotributo.UrlApiEscalas", EntradaUrl.Text.Trim());
			}

			this.DialogResult = DialogResult.OK;
			this.Close();
		}
	}
}
