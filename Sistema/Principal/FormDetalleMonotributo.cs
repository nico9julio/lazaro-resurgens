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
                private Label LabelPeriodoInfo;
                private Label LabelDiasInfo;
                private ProgressBar BarraProgresoDias;

                private Label ValMesActual;
                private Label ValMesAnterior;
                private Label ValPeriodo;
                private Label Val12Meses;
                private Label ValProyectado;
                private Label ValCategoria;
                private Label ValMargen;

                private TabControl Pestañas;
                private DataGridView GrillaMensual;
                private DataGridView GrillaCategorias;

                private Button BotonActualizar;
                private Button BotonConfigEscalas;
                private Button BotonCerrar;

                public FormDetalleMonotributo(Lfx.Data.IConnection connection)
                {
                        this.m_Connection = connection;
                        InitializeComponentCustom();
                        CargarDatos();
                }

                private void InitializeComponentCustom()
                {
                        this.Text = "Análisis de Monotributo y Próxima Recategorización (ARCA / AFIP)";
                        this.Size = new Size(820, 680);
                        this.StartPosition = FormStartPosition.CenterParent;
                        this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
                        this.BackColor = Color.FromArgb(248, 249, 250);

                        // 1. Panel de Encabezado y Resumen
                        Panel panelHeader = new Panel();
                        panelHeader.Dock = DockStyle.Top;
                        panelHeader.Height = 230;
                        panelHeader.Padding = new Padding(14);
                        panelHeader.BackColor = Color.White;

                        LabelTitulo = new Label();
                        LabelTitulo.Text = "Control de Facturación y Recategorización de Monotributo";
                        LabelTitulo.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
                        LabelTitulo.ForeColor = Color.FromArgb(30, 45, 60);
                        LabelTitulo.Location = new Point(14, 10);
                        LabelTitulo.Size = new Size(770, 26);
                        panelHeader.Controls.Add(LabelTitulo);

                        LabelPeriodoInfo = new Label();
                        LabelPeriodoInfo.Font = new Font("Segoe UI", 9.25F, FontStyle.Regular);
                        LabelPeriodoInfo.ForeColor = Color.FromArgb(70, 80, 95);
                        LabelPeriodoInfo.Location = new Point(14, 38);
                        LabelPeriodoInfo.Size = new Size(770, 20);
                        panelHeader.Controls.Add(LabelPeriodoInfo);

                        LabelDiasInfo = new Label();
                        LabelDiasInfo.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
                        LabelDiasInfo.ForeColor = Color.FromArgb(100, 110, 120);
                        LabelDiasInfo.Location = new Point(14, 60);
                        LabelDiasInfo.Size = new Size(450, 18);
                        panelHeader.Controls.Add(LabelDiasInfo);

                        BarraProgresoDias = new ProgressBar();
                        BarraProgresoDias.Location = new Point(470, 58);
                        BarraProgresoDias.Size = new Size(314, 18);
                        BarraProgresoDias.Style = ProgressBarStyle.Continuous;
                        panelHeader.Controls.Add(BarraProgresoDias);

                        // Tarjetas de Métricas en panelHeader
                        int topCards = 88;
                        int cardW = 186;
                        int cardH = 62;
                        int gap = 10;

                        Panel card1 = CrearTarjetaMetrica("Mes actual hasta hoy", out ValMesActual, new Point(14, topCards), cardW, cardH, Color.FromArgb(240, 245, 255));
                        Panel card2 = CrearTarjetaMetrica("Mes anterior completo", out ValMesAnterior, new Point(14 + (cardW + gap), topCards), cardW, cardH, Color.FromArgb(245, 245, 250));
                        Panel card3 = CrearTarjetaMetrica("Acumulado período recat.", out ValPeriodo, new Point(14 + (cardW + gap) * 2, topCards), cardW, cardH, Color.FromArgb(240, 252, 245));
                        Panel card4 = CrearTarjetaMetrica("Últimos 12 meses móviles", out Val12Meses, new Point(14 + (cardW + gap) * 3, topCards), cardW, cardH, Color.FromArgb(255, 250, 240));

                        panelHeader.Controls.Add(card1);
                        panelHeader.Controls.Add(card2);
                        panelHeader.Controls.Add(card3);
                        panelHeader.Controls.Add(card4);

                        // Segunda fila de tarjetas (Proyección y Categoría)
                        int topCards2 = topCards + cardH + 10;
                        int cardW2 = 252;
                        int cardH2 = 60;

                        Panel cardProy = CrearTarjetaMetrica("PROYECCIÓN PRÓX. RECATEGORIZACIÓN", out ValProyectado, new Point(14, topCards2), cardW2, cardH2, Color.FromArgb(235, 245, 255));
                        Panel cardCat = CrearTarjetaMetrica("CATEGORÍA PROYECTADA", out ValCategoria, new Point(14 + cardW2 + gap, topCards2), cardW2, cardH2, Color.FromArgb(240, 255, 240));
                        Panel cardMarg = CrearTarjetaMetrica("MARGEN RESTANTE EN CATEGORÍA", out ValMargen, new Point(14 + (cardW2 + gap) * 2, topCards2), cardW2, cardH2, Color.FromArgb(255, 245, 240));

                        panelHeader.Controls.Add(cardProy);
                        panelHeader.Controls.Add(cardCat);
                        panelHeader.Controls.Add(cardMarg);

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
                        BotonActualizar.Size = new Size(130, 28);
                        BotonActualizar.Click += (s, e) => CargarDatos();
                        panelBottom.Controls.Add(BotonActualizar);

                        BotonConfigEscalas = new Button();
                        BotonConfigEscalas.Text = "Configurar Escalas...";
                        BotonConfigEscalas.Location = new Point(152, 11);
                        BotonConfigEscalas.Size = new Size(160, 28);
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
                        GrillaCategorias.Columns["Letra"].Width = 90;
                        GrillaCategorias.Columns["Letra"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        GrillaCategorias.Columns["Letra"].DefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

                        GrillaCategorias.Columns.Add("Tope", "Ingresos Brutos Máximos Anuales ($)");
                        GrillaCategorias.Columns["Tope"].Width = 240;
                        GrillaCategorias.Columns["Tope"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                        GrillaCategorias.Columns["Tope"].DefaultCellStyle.Format = "N0";

                        GrillaCategorias.Columns.Add("Serv", "Cuota Servicios ($)");
                        GrillaCategorias.Columns["Serv"].Width = 150;
                        GrillaCategorias.Columns["Serv"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                        GrillaCategorias.Columns["Serv"].DefaultCellStyle.Format = "N0";

                        GrillaCategorias.Columns.Add("Bien", "Cuota Venta Bienes ($)");
                        GrillaCategorias.Columns["Bien"].Width = 150;
                        GrillaCategorias.Columns["Bien"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                        GrillaCategorias.Columns["Bien"].DefaultCellStyle.Format = "N0";

                        GrillaCategorias.Columns.Add("Estado", "Encuadre / Situación");
                        GrillaCategorias.Columns["Estado"].Width = 140;
                        GrillaCategorias.Columns["Estado"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                        tabEscalas.Controls.Add(GrillaCategorias);
                        Pestañas.TabPages.Add(tabEscalas);

                        this.Controls.Add(Pestañas);
                        Pestañas.BringToFront();
                }

                private Panel CrearTarjetaMetrica(string titulo, out Label lblValor, Point location, int width, int height, Color backColor)
                {
                        Panel panel = new Panel();
                        panel.Location = location;
                        panel.Size = new Size(width, height);
                        panel.BackColor = backColor;
                        panel.BorderStyle = BorderStyle.FixedSingle;

                        Label lblTit = new Label();
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

                                        LabelDiasInfo.Text = string.Format("Días transcurridos: {0} de {1} ({2:N0}% del semestre). Días restantes: {3}",
                                                m_Resumen.DiasTranscurridos,
                                                m_Resumen.DiasTotales,
                                                m_Resumen.PeriodoRecat.PorcentajeTranscurrido(m_Resumen.FechaCalculo),
                                                m_Resumen.DiasRestantes);

                                        BarraProgresoDias.Maximum = m_Resumen.DiasTotales;
                                        BarraProgresoDias.Value = Math.Min(m_Resumen.DiasTotales, Math.Max(0, m_Resumen.DiasTranscurridos));
                                }

                                ValMesActual.Text = FormatoMoneda.Formatear(m_Resumen.FacturadoMesActual);
                                ValMesAnterior.Text = FormatoMoneda.Formatear(m_Resumen.FacturadoMesAnterior);
                                ValPeriodo.Text = FormatoMoneda.Formatear(m_Resumen.FacturadoPeriodoRecat);
                                Val12Meses.Text = FormatoMoneda.Formatear(m_Resumen.FacturadoUltimos12Meses);
                                ValProyectado.Text = FormatoMoneda.Formatear(m_Resumen.FacturadoProyectado);

                                if (m_Resumen.ExcluidoProyectado)
                                {
                                        ValCategoria.Text = "EXCLUSIÓN (R.G.)";
                                        ValCategoria.ForeColor = Color.Red;
                                        ValMargen.Text = "Supera Cat. K";
                                        ValMargen.ForeColor = Color.Red;
                                }
                                else if (m_Resumen.CategoriaProyectada != null)
                                {
                                        ValCategoria.Text = "Categoría " + m_Resumen.CategoriaProyectada.Letra + string.Format(" ({0:N0}% tramo)", m_Resumen.PorcentajeTramoActual);
                                        if (m_Resumen.EsUltimaCategoria)
                                        {
                                                ValCategoria.ForeColor = Color.FromArgb(200, 60, 0);
                                                ValMargen.Text = FormatoMoneda.Formatear(m_Resumen.MargenProyectado) + " (a exclusión)";
                                                ValMargen.ForeColor = Color.FromArgb(200, 40, 0);
                                        }
                                        else
                                        {
                                                ValCategoria.ForeColor = Color.FromArgb(0, 100, 0);
                                                ValMargen.Text = FormatoMoneda.Formatear(m_Resumen.MargenProyectado) + " (a Cat. " + m_Resumen.SiguienteCategoriaLetra + ")";
                                                ValMargen.ForeColor = Color.FromArgb(0, 80, 160);
                                        }
                                }
                                else
                                {
                                        ValCategoria.Text = "Sin datos";
                                        ValMargen.Text = "$ 0";
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
                                        bool esProyectada = (m_Resumen.CategoriaProyectada != null && m_Resumen.CategoriaProyectada.Letra == cat.Letra);
                                        bool esActual = (m_Resumen.CategoriaActual != null && m_Resumen.CategoriaActual.Letra == cat.Letra);
                                        bool esUltima = EscalasMonotributo.Instancia.EsUltimaCategoria(cat.Letra);

                                        if (esProyectada && esActual)
                                                estado = esUltima ? "★ ACTUAL Y PROY. (ÚLTIMA)" : "★ ACTUAL Y PROY.";
                                        else if (esProyectada)
                                                estado = esUltima ? "➔ PROYECTADA (ÚLTIMA)" : "➔ PROYECTADA";
                                        else if (esActual)
                                                estado = esUltima ? "✓ ACTUAL (ÚLTIMA)" : "✓ ACTUAL";
                                        else if (esUltima)
                                                estado = "[ÚLTIMA CATEGORÍA]";

                                        int rowIdx = GrillaCategorias.Rows.Add(
                                                cat.Letra,
                                                FormatoMoneda.Formatear(cat.IngresosBrutosMaximos),
                                                FormatoMoneda.Formatear(cat.CuotaServicios),
                                                FormatoMoneda.Formatear(cat.CuotaBienes),
                                                estado
                                        );

                                        if (esProyectada)
                                        {
                                                GrillaCategorias.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(235, 250, 235);
                                                GrillaCategorias.Rows[rowIdx].DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                                        }
                                        else if (esActual)
                                        {
                                                GrillaCategorias.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(240, 245, 255);
                                        }
                                }
                        }
                        finally
                        {
                                Cursor = Cursors.Default;
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
