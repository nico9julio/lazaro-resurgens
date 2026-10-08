using System;
using System.Drawing;
using System.Windows.Forms;
using Lbl.Impuestos.Monotributo;

namespace Lazaro.WinMain.Principal
{
        public class FormDetalleMonotributo : Lui.Forms.Form
        {
                private Lfx.Data.IConnection m_Connection;
                private ResumenMonotributo m_Resumen;

                private Label LabelTitulo;
                private Label LabelAfipBanner;
                private PictureBox PicAfipBanner;
                private Label LabelPeriodoInfo;
                private Label LabelDiasInfo;
                private ProgressBar BarraProgresoDias;

                private Label ValMesActual;
                private Label ValMesAnterior;
                private Label ValPeriodo;
                private Label Val12Meses;
                private Label TituloCardPeriodo;

                private Label ValProyectado;
                private Label SubValProyectado;
                private Label TituloCardProy;
                private PictureBox IconoCardProy;

                private Label ValCategoriaAfip;
                private Label SubValCategoriaAfip;
                private Label TituloCardCategoriaAfip;
                private PictureBox IconoCardCategoriaAfip;
                private Panel CardCatAfipPanel;
                private Panel BarraConsumoAfipFondo;
                private Panel BarraConsumoAfipRelleno;

                private Label ValPlanificacion;
                private Label SubValPlanificacion;
                private Label TituloCardPlanificacion;
                private PictureBox IconoCardPlanificacion;
                private Panel CardPlanificacionPanel;

                private Label ValEncuadreProyectado;
                private Label SubValEncuadreProyectado;
                private Label TituloCardEncuadreProyectado;
                private PictureBox IconoCardEncuadreProyectado;
                private Panel CardEncuadrePanel;
                private Panel BarraEncuadreFondo;
                private Panel BarraEncuadreRelleno;
                private ToolTip ToolTipMetricas;

                private TabControl Pestañas;
                private DataGridView GrillaMensual;
                private DataGridView GrillaCategorias;
                private TextBox TxtDiagnostico;

                private Button BotonActualizar;
                private Button BotonConfigEscalas;
                private Button BotonConsultarAfip;
                private Button BotonCerrar;

                public FormDetalleMonotributo(Lfx.Data.IConnection connection)
                {
                        this.m_Connection = connection;
                        InitializeComponentCustom();
                        CargarDatos();
                        this.Shown += (s, e) =>
                        {
                                VerificarAfipAlAbrir();
                                VerificadorActualizacionesMonotributo.ComprobarEnSegundoPlano(this, forzar: false, onActualizado: () => CargarDatos());
                        };
                }

                private void InitializeComponentCustom()
                {
                        this.Text = "Análisis de Monotributo y Próxima Recategorización (ARCA / AFIP)";
                        this.Size = new Size(820, 690);
                        this.StartPosition = FormStartPosition.CenterParent;
                        this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
                        this.BackColor = Color.FromArgb(248, 249, 250);

                        // 1. Panel de Encabezado y Resumen
                        Panel panelHeader = new Panel();
                        panelHeader.Dock = DockStyle.Top;
                        panelHeader.Height = 252;
                        panelHeader.Padding = new Padding(14);
                        panelHeader.BackColor = Color.White;

                        LabelTitulo = new Label();
                        LabelTitulo.Text = "Control de Facturación y Recategorización de Monotributo";
                        LabelTitulo.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
                        LabelTitulo.ForeColor = Color.FromArgb(30, 45, 60);
                        LabelTitulo.Location = new Point(14, 8);
                        LabelTitulo.Size = new Size(770, 24);
                        panelHeader.Controls.Add(LabelTitulo);

                        LabelAfipBanner = new Label();
                        LabelAfipBanner.Font = new Font("Segoe UI", 8.75F, FontStyle.Bold);
                        LabelAfipBanner.Location = new Point(14, 34);
                        LabelAfipBanner.Size = new Size(774, 23);
                        LabelAfipBanner.TextAlign = ContentAlignment.MiddleLeft;
                        LabelAfipBanner.Padding = new Padding(26, 0, 6, 0);
                        LabelAfipBanner.BorderStyle = BorderStyle.FixedSingle;
                        LabelAfipBanner.AutoEllipsis = true;

                        PicAfipBanner = new PictureBox();
                        PicAfipBanner.Image = IconosMonotributo.Afip;
                        PicAfipBanner.SizeMode = PictureBoxSizeMode.CenterImage;
                        PicAfipBanner.Location = new Point(5, 3);
                        PicAfipBanner.Size = new Size(16, 16);
                        LabelAfipBanner.Controls.Add(PicAfipBanner);

                        panelHeader.Controls.Add(LabelAfipBanner);

                        LabelPeriodoInfo = new Label();
                        LabelPeriodoInfo.Font = new Font("Segoe UI", 8.75F, FontStyle.Regular);
                        LabelPeriodoInfo.ForeColor = Color.FromArgb(70, 80, 95);
                        LabelPeriodoInfo.Location = new Point(14, 60);
                        LabelPeriodoInfo.Size = new Size(770, 18);
                        panelHeader.Controls.Add(LabelPeriodoInfo);

                        LabelDiasInfo = new Label();
                        LabelDiasInfo.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular);
                        LabelDiasInfo.ForeColor = Color.FromArgb(100, 110, 120);
                        LabelDiasInfo.Location = new Point(14, 80);
                        LabelDiasInfo.Size = new Size(450, 18);
                        panelHeader.Controls.Add(LabelDiasInfo);

                        BarraProgresoDias = new ProgressBar();
                        BarraProgresoDias.Location = new Point(470, 80);
                        BarraProgresoDias.Size = new Size(318, 18);
                        BarraProgresoDias.Style = ProgressBarStyle.Continuous;
                        panelHeader.Controls.Add(BarraProgresoDias);

                        // Tarjetas de Métricas en panelHeader
                        int topCards = 104;
                        int cardW = 186;
                        int cardH = 64;
                        int gap = 10;

                        Label lblDummy;
                        Panel card1 = CrearTarjetaMetrica("Mes actual hasta hoy", out ValMesActual, out lblDummy, new Point(14, topCards), cardW, cardH, Color.FromArgb(240, 245, 255));
                        Panel card2 = CrearTarjetaMetrica("Mes anterior completo", out ValMesAnterior, out lblDummy, new Point(14 + (cardW + gap), topCards), cardW, cardH, Color.FromArgb(245, 245, 250));
                        Panel card3 = CrearTarjetaMetrica("Acumulado año (AFIP)", out ValPeriodo, out TituloCardPeriodo, new Point(14 + (cardW + gap) * 2, topCards), cardW, cardH, Color.FromArgb(240, 252, 245));
                        Panel card4 = CrearTarjetaMetrica("Últimos 12 meses móviles", out Val12Meses, out lblDummy, new Point(14 + (cardW + gap) * 3, topCards), cardW, cardH, Color.FromArgb(255, 250, 240));

                        panelHeader.Controls.Add(card1);
                        panelHeader.Controls.Add(card2);
                        panelHeader.Controls.Add(card3);
                        panelHeader.Controls.Add(card4);

                        // Segunda fila de tarjetas (Proyección, Situación AFIP unificada, Límite/Ahorro y Próxima Escala/Baja)
                        int topCards2 = 174;
                        int cardH2 = 68;

                        Panel cardProy = CrearTarjetaMetricaConSubtitulo("PROYECCIÓN ANUAL", out ValProyectado, out SubValProyectado, out TituloCardProy, out IconoCardProy, new Point(14, topCards2), cardW, cardH2, Color.FromArgb(235, 245, 255));
                        CardCatAfipPanel = CrearTarjetaMetricaConSubtitulo("CATEGORÍA ACTUAL (AFIP)", out ValCategoriaAfip, out SubValCategoriaAfip, out TituloCardCategoriaAfip, out IconoCardCategoriaAfip, new Point(14 + (cardW + gap), topCards2), cardW, cardH2, Color.FromArgb(240, 253, 244));
                        CardPlanificacionPanel = CrearTarjetaMetricaConSubtitulo("LÍMITE Y AHORRO (CAT. AFIP)", out ValPlanificacion, out SubValPlanificacion, out TituloCardPlanificacion, out IconoCardPlanificacion, new Point(14 + (cardW + gap) * 2, topCards2), cardW, cardH2, Color.FromArgb(255, 252, 240));
                        CardEncuadrePanel = CrearTarjetaMetricaConSubtitulo("PRÓXIMA ESCALA / PROY.", out ValEncuadreProyectado, out SubValEncuadreProyectado, out TituloCardEncuadreProyectado, out IconoCardEncuadreProyectado, new Point(14 + (cardW + gap) * 3, topCards2), cardW, cardH2, Color.FromArgb(245, 243, 255));

                        // Barrita fina visual de porcentaje debajo de Categoría Actual AFIP
                        BarraConsumoAfipFondo = new Panel();
                        BarraConsumoAfipFondo.Location = new Point(5, 39);
                        BarraConsumoAfipFondo.Size = new Size(cardW - 10, 4);
                        BarraConsumoAfipFondo.BackColor = Color.FromArgb(220, 225, 230);
                        BarraConsumoAfipFondo.Visible = false;

                        BarraConsumoAfipRelleno = new Panel();
                        BarraConsumoAfipRelleno.Location = new Point(0, 0);
                        BarraConsumoAfipRelleno.Size = new Size(0, 4);
                        BarraConsumoAfipRelleno.BackColor = Color.FromArgb(0, 160, 60);
                        BarraConsumoAfipFondo.Controls.Add(BarraConsumoAfipRelleno);

                        CardCatAfipPanel.Controls.Add(BarraConsumoAfipFondo);

                        // Barrita fina visual de porcentaje debajo de Próxima Escala / Objetivo de Baja
                        BarraEncuadreFondo = new Panel();
                        BarraEncuadreFondo.Location = new Point(5, 39);
                        BarraEncuadreFondo.Size = new Size(cardW - 10, 4);
                        BarraEncuadreFondo.BackColor = Color.FromArgb(220, 225, 230);
                        BarraEncuadreFondo.Visible = false;

                        BarraEncuadreRelleno = new Panel();
                        BarraEncuadreRelleno.Location = new Point(0, 0);
                        BarraEncuadreRelleno.Size = new Size(0, 4);
                        BarraEncuadreRelleno.BackColor = Color.FromArgb(0, 160, 60);
                        BarraEncuadreFondo.Controls.Add(BarraEncuadreRelleno);

                        CardEncuadrePanel.Controls.Add(BarraEncuadreFondo);

                        ToolTipMetricas = new ToolTip();
                        ToolTipMetricas.InitialDelay = 200;
                        ToolTipMetricas.ReshowDelay = 100;
                        ToolTipMetricas.AutoPopDelay = 12000;

                        panelHeader.Controls.Add(cardProy);
                        panelHeader.Controls.Add(CardCatAfipPanel);
                        panelHeader.Controls.Add(CardPlanificacionPanel);
                        panelHeader.Controls.Add(CardEncuadrePanel);

                        this.Controls.Add(panelHeader);

                        // 2. Panel Inferior con Botones
                        Panel panelBottom = new Panel();
                        panelBottom.Dock = DockStyle.Bottom;
                        panelBottom.Height = 50;
                        panelBottom.BackColor = Color.FromArgb(240, 242, 245);
                        panelBottom.Padding = new Padding(12);

                        BotonActualizar = new Button();
                        BotonActualizar.Text = "Actualizar Datos";
                        BotonActualizar.Location = new Point(14, 11);
                        BotonActualizar.Size = new Size(120, 28);
                        BotonActualizar.Click += (s, e) =>
                        {
                                CargarDatos();
                                VerificadorActualizacionesMonotributo.ComprobarEnSegundoPlano(this, forzar: true, onActualizado: () => CargarDatos());
                        };
                        panelBottom.Controls.Add(BotonActualizar);

                        BotonConfigEscalas = new Button();
                        BotonConfigEscalas.Text = "Configurar Escalas...";
                        BotonConfigEscalas.Location = new Point(140, 11);
                        BotonConfigEscalas.Size = new Size(145, 28);
                        BotonConfigEscalas.Click += (s, e) =>
                        {
                                using (var dlg = new FormConfigurarEscalasMonotributo())
                                {
                                        if (dlg.ShowDialog(this) == DialogResult.OK)
                                        {
                                                CargarDatos();
                                        }
                                }
                        };
                        panelBottom.Controls.Add(BotonConfigEscalas);

                        BotonConsultarAfip = new Button();
                        BotonConsultarAfip.Text = "  Consultar en ARCA / AFIP...";
                        BotonConsultarAfip.Image = IconosMonotributo.Afip;
                        BotonConsultarAfip.ImageAlign = ContentAlignment.MiddleLeft;
                        BotonConsultarAfip.TextImageRelation = TextImageRelation.ImageBeforeText;
                        BotonConsultarAfip.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                        BotonConsultarAfip.Location = new Point(292, 11);
                        BotonConsultarAfip.Size = new Size(205, 28);
                        BotonConsultarAfip.Cursor = Cursors.Hand;
                        BotonConsultarAfip.Click += (s, e) => EjecutarConsultaAfipManual();
                        panelBottom.Controls.Add(BotonConsultarAfip);

                        BotonCerrar = new Button();
                        BotonCerrar.Text = "Cerrar";
                        BotonCerrar.Location = new Point(700, 11);
                        BotonCerrar.Size = new Size(88, 28);
                        BotonCerrar.Click += (s, e) => this.Close();
                        panelBottom.Controls.Add(BotonCerrar);

                        this.Controls.Add(panelBottom);

                        // 3. Pestañas Centrales
                        Pestañas = new TabControl();
                        Pestañas.Dock = DockStyle.Fill;
                        Pestañas.Padding = new Point(12, 6);

                        // Pestaña 1: Desglose Mensual
                        TabPage tabMensual = new TabTabPageCustom("Evolución Mensual (Últimos 12 Meses)");
                        GrillaMensual = new DataGridView();
                        GrillaMensual.Dock = DockStyle.Fill;
                        GrillaMensual.AllowUserToAddRows = false;
                        GrillaMensual.AllowUserToDeleteRows = false;
                        GrillaMensual.ReadOnly = true;
                        GrillaMensual.RowHeadersVisible = false;
                        GrillaMensual.BackgroundColor = Color.White;
                        GrillaMensual.BorderStyle = BorderStyle.None;
                        GrillaMensual.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

                        GrillaMensual.Columns.Add("Mes", "Mes / Período");
                        GrillaMensual.Columns["Mes"].Width = 180;

                        GrillaMensual.Columns.Add("Fac", "Facturas y ND ($)");
                        GrillaMensual.Columns["Fac"].Width = 180;
                        GrillaMensual.Columns["Fac"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                        GrillaMensual.Columns["Fac"].DefaultCellStyle.Format = "N0";

                        GrillaMensual.Columns.Add("NC", "Notas de Crédito ($)");
                        GrillaMensual.Columns["NC"].Width = 180;
                        GrillaMensual.Columns["NC"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                        GrillaMensual.Columns["NC"].DefaultCellStyle.Format = "N0";

                        GrillaMensual.Columns.Add("Neto", "Total Facturado Neto ($)");
                        GrillaMensual.Columns["Neto"].Width = 200;
                        GrillaMensual.Columns["Neto"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                        GrillaMensual.Columns["Neto"].DefaultCellStyle.Format = "N0";
                        GrillaMensual.Columns["Neto"].DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

                        tabMensual.Controls.Add(GrillaMensual);
                        Pestañas.TabPages.Add(tabMensual);

                        // Pestaña 2: Escalas Oficiales ARCA
                        TabPage tabEscalas = new TabTabPageCustom("Escalas de Categorías ARCA");
                        GrillaCategorias = new DataGridView();
                        GrillaCategorias.Dock = DockStyle.Fill;
                        GrillaCategorias.AllowUserToAddRows = false;
                        GrillaCategorias.AllowUserToDeleteRows = false;
                        GrillaCategorias.ReadOnly = true;
                        GrillaCategorias.RowHeadersVisible = false;
                        GrillaCategorias.BackgroundColor = Color.White;
                        GrillaCategorias.BorderStyle = BorderStyle.None;
                        GrillaCategorias.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

                        GrillaCategorias.Columns.Add("Letra", "Categoría");
                        GrillaCategorias.Columns["Letra"].Width = 70;
                        GrillaCategorias.Columns["Letra"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        GrillaCategorias.Columns["Letra"].DefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

                        GrillaCategorias.Columns.Add("Tope", "Ingresos Brutos Máximos ($)");
                        GrillaCategorias.Columns["Tope"].Width = 190;
                        GrillaCategorias.Columns["Tope"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                        GrillaCategorias.Columns["Tope"].DefaultCellStyle.Format = "N0";

                        GrillaCategorias.Columns.Add("Consumo", "% Consumido");
                        GrillaCategorias.Columns["Consumo"].Width = 110;
                        GrillaCategorias.Columns["Consumo"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                        GrillaCategorias.Columns["Consumo"].DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

                        GrillaCategorias.Columns.Add("Serv", "Cuota Servicios ($)");
                        GrillaCategorias.Columns["Serv"].Width = 130;
                        GrillaCategorias.Columns["Serv"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                        GrillaCategorias.Columns["Serv"].DefaultCellStyle.Format = "N0";

                        GrillaCategorias.Columns.Add("Bien", "Cuota Venta Bienes ($)");
                        GrillaCategorias.Columns["Bien"].Width = 130;
                        GrillaCategorias.Columns["Bien"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                        GrillaCategorias.Columns["Bien"].DefaultCellStyle.Format = "N0";

                        GrillaCategorias.Columns.Add("Estado", "Encuadre / Situación");
                        GrillaCategorias.Columns["Estado"].Width = 140;
                        GrillaCategorias.Columns["Estado"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                        tabEscalas.Controls.Add(GrillaCategorias);
                        Pestañas.TabPages.Add(tabEscalas);

                        // Pestaña 3: Diagnóstico y Situación Fiscal
                        TabPage tabDiagnostico = new TabTabPageCustom("Diagnóstico y Estimación Fiscal");
                        TxtDiagnostico = new TextBox();
                        TxtDiagnostico.Multiline = true;
                        TxtDiagnostico.ReadOnly = true;
                        TxtDiagnostico.ScrollBars = ScrollBars.Vertical;
                        TxtDiagnostico.Dock = DockStyle.Fill;
                        TxtDiagnostico.BackColor = Color.White;
                        TxtDiagnostico.BorderStyle = BorderStyle.None;
                        TxtDiagnostico.Font = new Font("Consolas", 9.5F, FontStyle.Regular);
                        tabDiagnostico.Controls.Add(TxtDiagnostico);
                        Pestañas.TabPages.Add(tabDiagnostico);

                        this.Controls.Add(Pestañas);
                        Pestañas.BringToFront();
                }

                private Panel CrearTarjetaMetrica(string titulo, out Label lblValor, Point location, int width, int height, Color backColor)
                {
                        Label lblDummy;
                        return CrearTarjetaMetrica(titulo, out lblValor, out lblDummy, location, width, height, backColor);
                }

                private Panel CrearTarjetaMetrica(string titulo, out Label lblValor, out Label lblTit, Point location, int width, int height, Color backColor)
                {
                        Panel panel = new Panel();
                        panel.Location = location;
                        panel.Size = new Size(width, height);
                        panel.BackColor = backColor;
                        panel.BorderStyle = BorderStyle.FixedSingle;

                        lblTit = new Label();
                        lblTit.Text = titulo;
                        lblTit.Font = new Font("Segoe UI", 7.5F, FontStyle.Regular);
                        lblTit.ForeColor = Color.FromArgb(90, 100, 110);
                        lblTit.Location = new Point(4, 4);
                        lblTit.Size = new Size(width - 8, 14);
                        lblTit.AutoEllipsis = true;
                        panel.Controls.Add(lblTit);

                        lblValor = new Label();
                        lblValor.Text = "$ 0";
                        lblValor.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
                        lblValor.ForeColor = Color.FromArgb(20, 30, 45);
                        lblValor.Location = new Point(4, 22);
                        lblValor.Size = new Size(width - 8, 30);
                        lblValor.AutoEllipsis = true;
                        panel.Controls.Add(lblValor);

                        return panel;
                }

                private Panel CrearTarjetaMetricaConSubtitulo(string titulo, out Label lblValor, out Label lblSub, out Label lblTit, out PictureBox picIcon, Point location, int width, int height, Color backColor)
                {
                        Panel panel = new Panel();
                        panel.Location = location;
                        panel.Size = new Size(width, height);
                        panel.BackColor = backColor;
                        panel.BorderStyle = BorderStyle.FixedSingle;

                        picIcon = new PictureBox();
                        picIcon.Location = new Point(4, 3);
                        picIcon.Size = new Size(14, 14);
                        picIcon.SizeMode = PictureBoxSizeMode.CenterImage;
                        picIcon.BackColor = Color.Transparent;
                        panel.Controls.Add(picIcon);

                        lblTit = new Label();
                        lblTit.Text = titulo;
                        lblTit.Font = new Font("Segoe UI", 7F, FontStyle.Bold);
                        lblTit.ForeColor = Color.FromArgb(70, 80, 95);
                        lblTit.Location = new Point(21, 3);
                        lblTit.Size = new Size(width - 24, 14);
                        lblTit.AutoEllipsis = true;
                        panel.Controls.Add(lblTit);

                        lblValor = new Label();
                        lblValor.Text = "$ 0";
                        lblValor.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                        lblValor.ForeColor = Color.FromArgb(20, 30, 45);
                        lblValor.Location = new Point(4, 17);
                        lblValor.Size = new Size(width - 8, 20);
                        lblValor.AutoEllipsis = true;
                        panel.Controls.Add(lblValor);

                        lblSub = new Label();
                        lblSub.Text = "";
                        lblSub.Font = new Font("Segoe UI", 7F, FontStyle.Regular);
                        lblSub.ForeColor = Color.FromArgb(90, 100, 110);
                        lblSub.Location = new Point(4, 46);
                        lblSub.Size = new Size(width - 8, 17);
                        lblSub.AutoEllipsis = true;
                        panel.Controls.Add(lblSub);

                        return panel;
                }

                private void CargarDatos()
                {
                        Cursor = Cursors.WaitCursor;
                        try
                        {
                                m_Resumen = CalculadorMonotributo.Calcular(m_Connection);

                                if (m_Resumen.PeriodoRecat != null)
                                {
                                        LabelPeriodoInfo.Text = string.Format("Próxima recategorización: {0} | Período evaluado: {1:dd/MM/yyyy} al {2:dd/MM/yyyy} (Vence aprox. {3:dd/MM/yyyy})",
                                                m_Resumen.PeriodoRecat.Nombre,
                                                m_Resumen.PeriodoRecat.FechaInicioPeriodo,
                                                m_Resumen.PeriodoRecat.FechaFinPeriodo,
                                                m_Resumen.PeriodoRecat.FechaVencimientoTramite);

                                        LabelDiasInfo.Text = string.Format("Días transcurridos: {0} de {1} ({2:N0}% del período de 12 meses). Días restantes: {3}",
                                                m_Resumen.DiasTranscurridos,
                                                m_Resumen.DiasTotales,
                                                m_Resumen.PeriodoRecat.PorcentajeTranscurrido(m_Resumen.FechaCalculo),
                                                m_Resumen.DiasRestantes);

                                        BarraProgresoDias.Maximum = m_Resumen.DiasTotales;
                                        BarraProgresoDias.Value = Math.Min(m_Resumen.DiasTotales, Math.Max(0, m_Resumen.DiasTranscurridos));

                                        if (TituloCardPeriodo != null)
                                        {
                                                TituloCardPeriodo.Text = (m_Resumen.PeriodoRecat.FechaInicioPeriodo.Month == 1) ?
                                                        string.Format("Facturado año {0} (AFIP)", m_Resumen.PeriodoRecat.FechaInicioPeriodo.Year) :
                                                        "Facturado en período (AFIP)";
                                        }
                                }

                                ValMesActual.Text = FormatoMoneda.Formatear(m_Resumen.FacturadoMesActual);
                                ValMesAnterior.Text = FormatoMoneda.Formatear(m_Resumen.FacturadoMesAnterior);
                                ValPeriodo.Text = FormatoMoneda.Formatear(m_Resumen.FacturadoPeriodoRecat);
                                Val12Meses.Text = FormatoMoneda.Formatear(m_Resumen.FacturadoUltimos12Meses);
                                ValProyectado.Text = FormatoMoneda.Formatear(m_Resumen.FacturadoProyectado);

                                if (m_Resumen.CategoriaInscripta != null)
                                {
                                        string textoBajaBanner = "";
                                        if (m_Resumen.HayOportunidadBaja && m_Resumen.CategoriaBaja != null)
                                        {
                                                textoBajaBanner = string.Format(" • Baja a Cat. {0} (resta {1} p/ tope)",
                                                        m_Resumen.CategoriaBaja.Letra, FormatoMoneda.Formatear(m_Resumen.MargenParaPasarseDeBajaProyectado));
                                        }

                                        LabelAfipBanner.Text = string.Format("Situación oficial ARCA / AFIP: Categoría {0} (Tope anual: {1}) • Facturado: {2:N1}% consumido • {3}{4}",
                                                m_Resumen.CategoriaInscripta.Letra,
                                                FormatoMoneda.Formatear(m_Resumen.CategoriaInscripta.IngresosBrutosMaximos),
                                                m_Resumen.PorcentajeCategoriaInscripta,
                                                m_Resumen.SuperoCategoriaInscripta ?
                                                        ("SUPERADO por " + FormatoMoneda.Formatear(Math.Abs(m_Resumen.MargenCategoriaInscripta))) :
                                                        ("Resta " + FormatoMoneda.Formatear(m_Resumen.MargenCategoriaInscripta) + " antes de superar"),
                                                textoBajaBanner);
                                        LabelAfipBanner.BackColor = m_Resumen.SuperoCategoriaInscripta ? Color.FromArgb(255, 235, 235) : Color.FromArgb(238, 248, 242);
                                        LabelAfipBanner.ForeColor = m_Resumen.SuperoCategoriaInscripta ? Color.FromArgb(180, 20, 20) : Color.FromArgb(20, 85, 45);
                                        if (PicAfipBanner != null) PicAfipBanner.BackColor = LabelAfipBanner.BackColor;
                                }
                                else
                                {
                                        LabelAfipBanner.Text = "Categoría oficial en ARCA / AFIP: No detectada • Presione 'Consultar en ARCA / AFIP' abajo para verificarla con su certificado fiscal.";
                                        LabelAfipBanner.BackColor = Color.FromArgb(254, 248, 235);
                                        LabelAfipBanner.ForeColor = Color.FromArgb(140, 80, 10);
                                        if (PicAfipBanner != null) PicAfipBanner.BackColor = LabelAfipBanner.BackColor;
                                }

                                ValMesActual.Text = FormatoMoneda.Formatear(m_Resumen.FacturadoMesActual);
                                ValMesAnterior.Text = FormatoMoneda.Formatear(m_Resumen.FacturadoMesAnterior);
                                ValPeriodo.Text = FormatoMoneda.Formatear(m_Resumen.FacturadoPeriodoRecat);
                                Val12Meses.Text = FormatoMoneda.Formatear(m_Resumen.FacturadoUltimos12Meses);

                                // Fila 2 - Tarjeta 1: Proyección Anual
                                IconoCardProy.Image = IconosMonotributo.Proyeccion;
                                ValProyectado.Text = FormatoMoneda.Formatear(m_Resumen.FacturadoProyectado);
                                SubValProyectado.Text = string.Format("Promedio: {0}/mes ({1}/día)", FormatoMoneda.Formatear(m_Resumen.PromedioMensual), FormatoMoneda.Formatear(m_Resumen.PromedioDiario));

                                // Fila 2 - Tarjeta 2: Categoría Actual AFIP (Unificada con consumo y barra)
                                IconoCardCategoriaAfip.Image = IconosMonotributo.Afip;
                                if (m_Resumen.CategoriaInscripta != null)
                                {
                                        decimal pctAfip = m_Resumen.PorcentajeCategoriaInscripta;
                                        string diasTxt = m_Resumen.DiasRestantes > 0 ? string.Format(" ({0}d)", m_Resumen.DiasRestantes) : "";
                                        TituloCardCategoriaAfip.Text = "AFIP: CAT. " + m_Resumen.CategoriaInscripta.Letra + diasTxt;

                                        if (m_Resumen.SuperoCategoriaInscripta)
                                        {
                                                ValCategoriaAfip.Text = string.Format("{0:N1}% (SUPERADO)", pctAfip);
                                                ValCategoriaAfip.ForeColor = Color.Red;
                                                SubValCategoriaAfip.Text = "Exceso: " + FormatoMoneda.Formatear(Math.Abs(m_Resumen.MargenCategoriaInscripta));
                                                SubValCategoriaAfip.ForeColor = Color.Red;
                                        }
                                        else
                                        {
                                                ValCategoriaAfip.Text = string.Format("{0:N1}% consumido", pctAfip);
                                                ValCategoriaAfip.ForeColor = (pctAfip > 85m) ? Color.FromArgb(200, 80, 0) : Color.FromArgb(0, 100, 40);
                                                SubValCategoriaAfip.Text = string.Format("Tope: {0} • Resta {1}",
                                                        FormatoMoneda.Formatear(m_Resumen.CategoriaInscripta.IngresosBrutosMaximos),
                                                        FormatoMoneda.Formatear(m_Resumen.MargenCategoriaInscripta));
                                                SubValCategoriaAfip.ForeColor = Color.FromArgb(70, 80, 95);
                                        }

                                        if (BarraConsumoAfipFondo != null && BarraConsumoAfipRelleno != null)
                                        {
                                                int trackW = BarraConsumoAfipFondo.Width;
                                                int fillW = (int)Math.Round(Math.Min(100m, Math.Max(0m, pctAfip)) * trackW / 100m);
                                                BarraConsumoAfipRelleno.Width = fillW;

                                                if (pctAfip >= 100m || m_Resumen.SuperoCategoriaInscripta)
                                                {
                                                        BarraConsumoAfipRelleno.BackColor = Color.FromArgb(220, 30, 30);
                                                }
                                                else if (pctAfip > 85m)
                                                {
                                                        BarraConsumoAfipRelleno.BackColor = Color.FromArgb(230, 110, 0);
                                                }
                                                else
                                                {
                                                        BarraConsumoAfipRelleno.BackColor = Color.FromArgb(0, 160, 60);
                                                }
                                                BarraConsumoAfipFondo.Visible = true;
                                        }

                                        if (ToolTipMetricas != null && CardCatAfipPanel != null)
                                        {
                                                ToolTipMetricas.SetToolTip(CardCatAfipPanel, string.Format(
                                                        "Situación Oficial en ARCA / AFIP:\r\n" +
                                                        "• Categoría registrada: Categoría {0}\r\n" +
                                                        "• Tope anual máximo: {1}\r\n" +
                                                        "• Facturado acumulado en el período: {2} ({3:N1}% consumido)\r\n" +
                                                        "• Margen disponible antes de superar Cat. {0}: {4}\r\n" +
                                                        "• Días restantes del período: {5} días",
                                                        m_Resumen.CategoriaInscripta.Letra,
                                                        FormatoMoneda.Formatear(m_Resumen.CategoriaInscripta.IngresosBrutosMaximos),
                                                        FormatoMoneda.Formatear(m_Resumen.FacturadoPeriodoRecat),
                                                        pctAfip,
                                                        FormatoMoneda.Formatear(m_Resumen.MargenCategoriaInscripta),
                                                        m_Resumen.DiasRestantes));
                                        }
                                }
                                else
                                {
                                        TituloCardCategoriaAfip.Text = "CATEGORÍA ACTUAL (AFIP)";
                                        ValCategoriaAfip.Text = "Sin verificar";
                                        ValCategoriaAfip.ForeColor = Color.FromArgb(130, 130, 130);
                                        SubValCategoriaAfip.Text = "Click en Consultar abajo";
                                        SubValCategoriaAfip.ForeColor = Color.FromArgb(130, 80, 10);
                                        if (BarraConsumoAfipFondo != null)
                                                BarraConsumoAfipFondo.Visible = false;
                                }

                                // Fila 2 - Tarjeta 3: Límite de Ventas y Ahorro (Tarjeta Liberada - Opción 3.1)
                                IconoCardPlanificacion.Image = IconosMonotributo.Escudo;
                                if (m_Resumen.CategoriaInscripta != null)
                                {
                                        TituloCardPlanificacion.Text = "LÍMITE Y AHORRO (CAT. " + m_Resumen.CategoriaInscripta.Letra + ")";
                                        if (m_Resumen.SuperoCategoriaInscripta)
                                        {
                                                ValPlanificacion.Text = "Tope superado";
                                                ValPlanificacion.ForeColor = Color.Red;
                                                SubValPlanificacion.Text = "Ritmo: " + FormatoMoneda.Formatear(m_Resumen.PromedioDiario) + "/día";
                                                SubValPlanificacion.ForeColor = Color.Red;
                                        }
                                        else
                                        {
                                                ValPlanificacion.Text = "Tope: " + FormatoMoneda.Formatear(m_Resumen.LimiteMensualCategoriaInscripta) + " / mes";
                                                ValPlanificacion.ForeColor = Color.FromArgb(0, 100, 40);

                                                string textoAhorro = "";
                                                if (m_Resumen.HayOportunidadBaja && m_Resumen.CategoriaBaja != null && m_Resumen.AhorroEstimadoMensualBaja > 0m)
                                                {
                                                        textoAhorro = string.Format(" • Si bajas a {0} ahorrarías: {1}/mes",
                                                                m_Resumen.CategoriaBaja.Letra,
                                                                FormatoMoneda.Formatear(m_Resumen.AhorroEstimadoMensualBaja));
                                                }
                                                else
                                                {
                                                        textoAhorro = string.Format(" (máx {0}/d)", FormatoMoneda.Formatear(m_Resumen.LimiteDiarioCategoriaInscripta));
                                                }

                                                SubValPlanificacion.Text = string.Format("Hoy: {0}/d{1}",
                                                        FormatoMoneda.Formatear(m_Resumen.PromedioDiario),
                                                        textoAhorro);
                                                SubValPlanificacion.ForeColor = Color.FromArgb(70, 80, 95);
                                        }

                                        if (ToolTipMetricas != null && CardPlanificacionPanel != null)
                                        {
                                                string extraAhorroTip = "";
                                                if (m_Resumen.HayOportunidadBaja && m_Resumen.CategoriaBaja != null && m_Resumen.AhorroEstimadoMensualBaja > 0m)
                                                {
                                                        extraAhorroTip = string.Format("\r\n• Beneficio económico: Si bajas a Cat. {0} ahorrarías {1} por mes en la cuota fija ({2} al año).",
                                                                m_Resumen.CategoriaBaja.Letra,
                                                                FormatoMoneda.Formatear(m_Resumen.AhorroEstimadoMensualBaja),
                                                                FormatoMoneda.Formatear(m_Resumen.AhorroEstimadoMensualBaja * 12m));
                                                }

                                                ToolTipMetricas.SetToolTip(CardPlanificacionPanel, string.Format(
                                                        "Control de Ventas y Límite Operativo:\r\n" +
                                                        "• Para permanecer en Cat. {0} (no subir de escala):\r\n" +
                                                        "  - Límite mensual seguro: hasta {1} / mes\r\n" +
                                                        "  - Límite diario seguro: hasta {2} / día\r\n" +
                                                        "• Ritmo de venta actual: {3} / día ({4} / mes){5}",
                                                        m_Resumen.CategoriaInscripta.Letra,
                                                        FormatoMoneda.Formatear(m_Resumen.LimiteMensualCategoriaInscripta),
                                                        FormatoMoneda.Formatear(m_Resumen.LimiteDiarioCategoriaInscripta),
                                                        FormatoMoneda.Formatear(m_Resumen.PromedioDiario),
                                                        FormatoMoneda.Formatear(m_Resumen.PromedioMensual),
                                                        extraAhorroTip));
                                        }
                                }
                                else
                                {
                                        TituloCardPlanificacion.Text = "LÍMITE DE VENTAS Y AHORRO";
                                        ValPlanificacion.Text = "-";
                                        ValPlanificacion.ForeColor = Color.FromArgb(120, 120, 120);
                                        SubValPlanificacion.Text = "Requiere verificar Cat. AFIP";
                                        SubValPlanificacion.ForeColor = Color.FromArgb(120, 120, 120);
                                }

                                // Fila 2 - Tarjeta 4: Próxima Escala / Proyección (con objetivo de baja y cupo en meses restantes)
                                if (m_Resumen.ExcluidoProyectado)
                                {
                                        IconoCardEncuadreProyectado.Image = IconosMonotributo.Alerta;
                                        TituloCardEncuadreProyectado.Text = "PRÓXIMA ESCALA (AFIP)";
                                        ValEncuadreProyectado.Text = "EXCLUSIÓN (R.G.)";
                                        ValEncuadreProyectado.ForeColor = Color.Red;
                                        SubValEncuadreProyectado.Text = "Supera límite máximo Cat. K";
                                        SubValEncuadreProyectado.ForeColor = Color.Red;
                                        if (BarraEncuadreFondo != null) BarraEncuadreFondo.Visible = false;
                                }
                                else if (m_Resumen.CategoriaProyectada != null)
                                {
                                        if (m_Resumen.EsUltimaCategoria)
                                        {
                                                IconoCardEncuadreProyectado.Image = IconosMonotributo.Alerta;
                                                TituloCardEncuadreProyectado.Text = "ESCALA FINAL (K)";
                                                ValEncuadreProyectado.Text = "Proyecta Cat. " + m_Resumen.CategoriaProyectada.Letra + " (Máx)";
                                                ValEncuadreProyectado.ForeColor = Color.FromArgb(200, 50, 0);
                                                SubValEncuadreProyectado.Text = "Resta " + FormatoMoneda.Formatear(m_Resumen.MargenProyectado) + " a exclusión";
                                                SubValEncuadreProyectado.ForeColor = Color.FromArgb(180, 40, 0);
                                                if (BarraEncuadreFondo != null) BarraEncuadreFondo.Visible = false;
                                        }
                                        else if (m_Resumen.CategoriaInscripta != null)
                                        {
                                                int comp = string.Compare(m_Resumen.CategoriaProyectada.Letra, m_Resumen.CategoriaInscripta.Letra, StringComparison.OrdinalIgnoreCase);
                                                if (comp < 0)
                                                {
                                                        IconoCardEncuadreProyectado.Image = IconosMonotributo.Baja;
                                                        string diasTxt = m_Resumen.DiasRestantes > 0 ? string.Format(" ({0} DÍAS)", m_Resumen.DiasRestantes) : "";
                                                        TituloCardEncuadreProyectado.Text = "OBJETIVO BAJA CAT. " + m_Resumen.CategoriaProyectada.Letra + diasTxt;

                                                        decimal limMensualBaja = m_Resumen.LimiteMensualBaja;
                                                        ValEncuadreProyectado.Text = limMensualBaja > 0m ?
                                                                ("Máx. " + FormatoMoneda.Formatear(limMensualBaja) + " / mes") :
                                                                (string.Format("{0:N1}% de Cat. {1}", m_Resumen.PorcentajeConsumoBajaPeriodo, m_Resumen.CategoriaProyectada.Letra));
                                                        ValEncuadreProyectado.ForeColor = Color.FromArgb(0, 110, 60);

                                                        int mesesR = m_Resumen.MesesRestantes;
                                                        string txtTiempo = mesesR > 1 ? string.Format("en estos {0} meses", mesesR) : (m_Resumen.DiasRestantes > 0 ? string.Format("en los {0} días restantes", m_Resumen.DiasRestantes) : "en el período");
                                                        SubValEncuadreProyectado.Text = string.Format("Puedes facturar hasta {0} {1}",
                                                                FormatoMoneda.Formatear(m_Resumen.MargenParaPasarseDeBajaPeriodo),
                                                                txtTiempo);
                                                        SubValEncuadreProyectado.ForeColor = Color.FromArgb(0, 110, 60);

                                                        // Barra fina visual de avance hacia el tope de la categoría baja
                                                        if (BarraEncuadreFondo != null && BarraEncuadreRelleno != null)
                                                        {
                                                                decimal pctBaja = m_Resumen.PorcentajeConsumoBajaPeriodo;
                                                                int trackW = BarraEncuadreFondo.Width;
                                                                int fillW = (int)Math.Round(Math.Min(100m, Math.Max(0m, pctBaja)) * trackW / 100m);
                                                                BarraEncuadreRelleno.Width = fillW;
                                                                BarraEncuadreRelleno.BackColor = (pctBaja > 85m) ? Color.FromArgb(230, 110, 0) : Color.FromArgb(0, 160, 60);
                                                                BarraEncuadreFondo.Visible = true;
                                                        }

                                                        if (ToolTipMetricas != null && CardEncuadrePanel != null)
                                                        {
                                                                ToolTipMetricas.SetToolTip(CardEncuadrePanel, string.Format(
                                                                        "Oportunidad de Recategorización a la Baja:\r\n" +
                                                                        "• Categoría a la que calificarías: Categoría {0}\r\n" +
                                                                        "• Tope anual máximo de Cat. {0}: {1}\r\n" +
                                                                        "• Facturación anual proyectada: {2} ({3:N1}% consumido de Cat. {0})\r\n" +
                                                                        "• Facturado acumulado en el período: {4} ({5:N1}% consumido)\r\n" +
                                                                        "• Puedes facturar hasta {6} {7} (promedio máx. {8} / mes)\r\n" +
                                                                        "  (Si facturas más que eso, perderás la baja a Cat. {0} y quedarás en Cat. {9})",
                                                                        m_Resumen.CategoriaProyectada.Letra,
                                                                        FormatoMoneda.Formatear(m_Resumen.CategoriaProyectada.IngresosBrutosMaximos),
                                                                        FormatoMoneda.Formatear(m_Resumen.FacturadoProyectado),
                                                                        m_Resumen.PorcentajeConsumoBajaProyectado,
                                                                        FormatoMoneda.Formatear(m_Resumen.FacturadoPeriodoRecat),
                                                                        m_Resumen.PorcentajeConsumoBajaPeriodo,
                                                                        FormatoMoneda.Formatear(m_Resumen.MargenParaPasarseDeBajaPeriodo),
                                                                        txtTiempo,
                                                                        FormatoMoneda.Formatear(limMensualBaja),
                                                                        m_Resumen.CategoriaInscripta.Letra));
                                                        }
                                                }
                                                else if (comp > 0)
                                                {
                                                        IconoCardEncuadreProyectado.Image = IconosMonotributo.Sube;
                                                        string diasTxt = m_Resumen.DiasRestantes > 0 ? string.Format(" ({0} DÍAS)", m_Resumen.DiasRestantes) : "";
                                                        TituloCardEncuadreProyectado.Text = "SUBE A CAT. " + m_Resumen.CategoriaProyectada.Letra + diasTxt;
                                                        ValEncuadreProyectado.Text = "Sube a Cat. " + m_Resumen.CategoriaProyectada.Letra;
                                                        ValEncuadreProyectado.ForeColor = Color.FromArgb(210, 80, 0);
                                                        SubValEncuadreProyectado.Text = "Resta " + FormatoMoneda.Formatear(m_Resumen.MargenProyectado) + " a Cat. " + m_Resumen.SiguienteCategoriaLetra;
                                                        SubValEncuadreProyectado.ForeColor = Color.FromArgb(90, 100, 110);
                                                        if (BarraEncuadreFondo != null) BarraEncuadreFondo.Visible = false;
                                                }
                                                else
                                                {
                                                        IconoCardEncuadreProyectado.Image = IconosMonotributo.Proyeccion;
                                                        string diasTxt = m_Resumen.DiasRestantes > 0 ? string.Format(" ({0} DÍAS)", m_Resumen.DiasRestantes) : "";
                                                        TituloCardEncuadreProyectado.Text = "PRÓXIMA ESCALA / PROY." + diasTxt;
                                                        ValEncuadreProyectado.Text = "Mantiene Cat. " + m_Resumen.CategoriaProyectada.Letra;
                                                        ValEncuadreProyectado.ForeColor = Color.FromArgb(0, 80, 160);
                                                        SubValEncuadreProyectado.Text = "Resta " + FormatoMoneda.Formatear(m_Resumen.MargenProyectado) + " a Cat. " + m_Resumen.SiguienteCategoriaLetra;
                                                        SubValEncuadreProyectado.ForeColor = Color.FromArgb(90, 100, 110);
                                                        if (BarraEncuadreFondo != null) BarraEncuadreFondo.Visible = false;
                                                }
                                        }
                                        else
                                        {
                                                IconoCardEncuadreProyectado.Image = IconosMonotributo.Proyeccion;
                                                TituloCardEncuadreProyectado.Text = "PRÓXIMA ESCALA / PROY.";
                                                ValEncuadreProyectado.Text = "Proy. Cat. " + m_Resumen.CategoriaProyectada.Letra;
                                                ValEncuadreProyectado.ForeColor = Color.FromArgb(0, 80, 160);
                                                SubValEncuadreProyectado.Text = "Resta " + FormatoMoneda.Formatear(m_Resumen.MargenProyectado) + " a Cat. " + m_Resumen.SiguienteCategoriaLetra;
                                                SubValEncuadreProyectado.ForeColor = Color.FromArgb(90, 100, 110);
                                                if (BarraEncuadreFondo != null) BarraEncuadreFondo.Visible = false;
                                        }
                                }
                                else
                                {
                                        IconoCardEncuadreProyectado.Image = null;
                                        ValEncuadreProyectado.Text = "Sin datos";
                                        ValEncuadreProyectado.ForeColor = Color.FromArgb(120, 120, 120);
                                        SubValEncuadreProyectado.Text = "-";
                                        SubValEncuadreProyectado.ForeColor = Color.FromArgb(120, 120, 120);
                                }

                                // Cargar Grilla Mensual
                                GrillaMensual.Rows.Clear();
                                foreach (var mes in m_Resumen.DetalleMensual)
                                {
                                        GrillaMensual.Rows.Add(
                                                mes.NombreMes,
                                                FormatoMoneda.Formatear(mes.Facturas),
                                                FormatoMoneda.Formatear(mes.NotasCredito),
                                                FormatoMoneda.Formatear(mes.TotalNeto)
                                        );
                                }

                                // Cargar Grilla Categorías
                                GrillaCategorias.Rows.Clear();
                                var categorias = EscalasMonotributo.Instancia.Categorias;
                                foreach (var cat in categorias)
                                {
                                        string estado = "-";
                                        bool esInscripta = (m_Resumen.CategoriaInscripta != null && m_Resumen.CategoriaInscripta.Letra == cat.Letra);
                                        bool esProyectada = (m_Resumen.CategoriaProyectada != null && m_Resumen.CategoriaProyectada.Letra == cat.Letra);
                                        bool esActual = (m_Resumen.CategoriaActual != null && m_Resumen.CategoriaActual.Letra == cat.Letra);
                                        bool esUltima = EscalasMonotributo.Instancia.EsUltimaCategoria(cat.Letra);

                                        if (esInscripta && esProyectada)
                                                estado = "★ INSCRIPTA Y PROYECTADA";
                                        else if (esInscripta)
                                                estado = "★ INSCRIPTA EN AFIP";
                                        else if (esProyectada && m_Resumen.HayOportunidadBaja)
                                                estado = "BAJA PROY. (Resta " + FormatoMoneda.Formatear(m_Resumen.MargenParaPasarseDeBajaProyectado) + ")";
                                        else if (esProyectada && esActual)
                                                estado = esUltima ? "➔ ACTUAL Y PROY. (ÚLTIMA)" : "➔ ACTUAL Y PROY.";
                                        else if (esProyectada)
                                                estado = esUltima ? "➔ PROYECTADA (ÚLTIMA)" : "➔ PROYECTADA";
                                        else if (esActual)
                                                estado = esUltima ? "✓ FACT. ACUM. (ÚLTIMA)" : "✓ FACT. ACUM.";
                                        else if (esUltima)
                                                estado = "[ÚLTIMA CATEGORÍA]";

                                        decimal pctCat = cat.IngresosBrutosMaximos > 0 ? (m_Resumen.FacturadoPeriodoRecat / cat.IngresosBrutosMaximos) * 100m : 0m;
                                        string pctTexto = string.Format("{0:N1}%", pctCat);
                                        if (pctCat > 100m)
                                        {
                                                pctTexto = string.Format("{0:N0}% (Superado)", pctCat);
                                        }

                                        int rowIdx = GrillaCategorias.Rows.Add(
                                                cat.Letra,
                                                FormatoMoneda.Formatear(cat.IngresosBrutosMaximos),
                                                pctTexto,
                                                FormatoMoneda.Formatear(cat.CuotaServicios),
                                                FormatoMoneda.Formatear(cat.CuotaBienes),
                                                estado
                                        );

                                        if (esInscripta)
                                        {
                                                GrillaCategorias.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(235, 250, 235);
                                                GrillaCategorias.Rows[rowIdx].DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                                        }
                                        else if (esProyectada && m_Resumen.HayOportunidadBaja)
                                        {
                                                GrillaCategorias.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(235, 255, 242);
                                                GrillaCategorias.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.FromArgb(0, 110, 50);
                                                GrillaCategorias.Rows[rowIdx].DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                                        }
                                        else if (esProyectada)
                                        {
                                                GrillaCategorias.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(240, 245, 255);
                                                GrillaCategorias.Rows[rowIdx].DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                                        }
                                        else if (esActual)
                                        {
                                                GrillaCategorias.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250);
                                        }
                                }

                                if (TxtDiagnostico != null)
                                {
                                        TxtDiagnostico.Text = m_Resumen.GenerarTextoInformativo();
                                }
                        }
                        finally
                        {
                                Cursor = Cursors.Default;
                        }
                }

                private void EjecutarConsultaAfipManual()
                {
                        Cursor = Cursors.WaitCursor;
                        try
                        {
                                var res = Lbl.Impuestos.Monotributo.ConsultaConstanciaAfip.Consultar();
                                if (res.Exito && !string.IsNullOrEmpty(res.Categoria))
                                {
                                        string catLimpia = EscalasMonotributo.Instancia.NormalizarLetra(res.Categoria);
                                        if (string.IsNullOrEmpty(catLimpia)) catLimpia = res.Categoria;

                                        if (Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null)
                                        {
                                                Lfx.Workspace.Master.CurrentConfig.WriteGlobalSetting("Sistema.Monotributo.CategoriaInscripta", catLimpia);
                                        }
                                        CargarDatos();

                                        string titular = !string.IsNullOrEmpty(res.RazonSocial) ? (" de " + res.RazonSocial) : "";
                                        MessageBox.Show(this,
                                                string.Format("Se consultó exitosamente el Web Service de ARCA / AFIP.\r\n\r\nCategoría actual detectada: Categoría {0}{1}\r\nCUIT: {2}\r\n\r\nLos datos y topes en pantalla han sido actualizados.",
                                                        catLimpia, titular, res.CuitConsultado),
                                                "Categoría Verificada en ARCA / AFIP", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                                else
                                {
                                        if (res.RequierePermisoWebservice)
                                        {
                                                MessageBox.Show(this,
                                                        res.Mensaje + "\r\n\r\n" + Lbl.Impuestos.Monotributo.ResultadoConsultaConstancia.ObtenerGuiaConfiguracionPermiso(),
                                                        "Permiso Requerido en ARCA / AFIP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        }
                                        else
                                        {
                                                MessageBox.Show(this,
                                                        res.Mensaje + "\r\n\r\nPuede ingresar la categoría manualmente en 'Configurar Escalas...' o desde el menú contextual del widget.",
                                                        "Consulta Web Service AFIP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        }
                                }
                        }
                        finally
                        {
                                Cursor = Cursors.Default;
                        }
                }

                private void VerificarAfipAlAbrir()
                {
                        bool autoCheck = Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null ?
                                Lfx.Workspace.Master.CurrentConfig.ReadGlobalSetting<bool>("Sistema.Monotributo.ConsultarAfipAlAbrir", true) : true;
                        bool sinCategoria = m_Resumen == null || m_Resumen.CategoriaInscripta == null;

                        if (autoCheck || sinCategoria)
                        {
                                System.Threading.ThreadPool.QueueUserWorkItem(state =>
                                {
                                        try
                                        {
                                                var res = Lbl.Impuestos.Monotributo.ConsultaConstanciaAfip.Consultar();
                                                if (res.Exito && !string.IsNullOrEmpty(res.Categoria))
                                                {
                                                        string catLimpia = EscalasMonotributo.Instancia.NormalizarLetra(res.Categoria);
                                                        if (string.IsNullOrEmpty(catLimpia)) catLimpia = res.Categoria;

                                                        if (this.IsHandleCreated && !this.IsDisposed)
                                                        {
                                                                this.BeginInvoke((MethodInvoker)delegate
                                                                {
                                                                        string catActual = (m_Resumen != null && m_Resumen.CategoriaInscripta != null) ? m_Resumen.CategoriaInscripta.Letra : "";
                                                                        if (!string.Equals(catActual, catLimpia, StringComparison.OrdinalIgnoreCase))
                                                                        {
                                                                                if (Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null)
                                                                                {
                                                                                	Lfx.Workspace.Master.CurrentConfig.WriteGlobalSetting("Sistema.Monotributo.CategoriaInscripta", catLimpia);
                                                                                }
                                                                                CargarDatos();
                                                                        }
                                                                });
                                                        }
                                                }
                                                else if (res.RequierePermisoWebservice)
                                                {
                                                        if (this.IsHandleCreated && !this.IsDisposed)
                                                        {
                                                                this.BeginInvoke((MethodInvoker)delegate
                                                                {
                                                                        if (TxtDiagnostico != null && !TxtDiagnostico.IsDisposed)
                                                                        {
                                                                                TxtDiagnostico.AppendText("\r\n--------------------------------------------------\r\n" +
                                                                                        "ℹ NOTA WEBSERVICE AFIP: No se pudo verificar la categoría actual automáticamente.\r\n" +
                                                                                        res.Mensaje + "\r\nPara autorizar el certificado existente, use el botón 'Consultar en ARCA / AFIP' o revise la configuración.\r\n");
                                                                        }
                                                                });
                                                        }
                                                }
                                        }
                                        catch { }
                                });
                        }
                }
        }

        internal class TabTabPageCustom : TabPage
        {
                public TabTabPageCustom(string title) : base(title)
                {
                        this.BackColor = Color.White;
                        this.Padding = new Padding(8);
                }
        }
}
