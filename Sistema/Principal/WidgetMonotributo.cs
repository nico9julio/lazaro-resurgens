using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;
using Lbl.Impuestos.Monotributo;

namespace Lazaro.WinMain.Principal
{
        /// <summary>
        /// Widget informativo para la barra inferior que monitorea la facturación
        /// y la proyección de recategorización semestral para contribuyentes Monotributistas (ARCA / AFIP).
        /// </summary>
        public class WidgetMonotributo : UserControl
        {
                private ResumenMonotributo m_Resumen;
                private bool m_Cargando = false;
                private bool m_MouseEncima = false;
                private ToolTip m_ToolTip;

                // Opciones configurables mediante clic derecho
                public bool MostrarMesActual { get; set; }
                public bool MostrarMesAnterior { get; set; }
                public bool MostrarPeriodoRecat { get; set; }
                public bool MostrarUltimos12Meses { get; set; }
                public bool MostrarProyeccion { get; set; }
                public bool MostrarCategoria { get; set; }

                private ContextMenuStrip MenuContextual;
                private ToolStripMenuItem ItemMesActual;
                private ToolStripMenuItem ItemMesAnterior;
                private ToolStripMenuItem ItemPeriodoRecat;
                private ToolStripMenuItem ItemUltimos12Meses;
                private ToolStripMenuItem ItemProyeccion;
                private ToolStripMenuItem ItemCategoria;
                private ToolStripMenuItem ItemActualizar;
                private ToolStripMenuItem ItemVerDetalle;
                private ToolStripMenuItem ItemConfigEscalas;

                public event EventHandler SolicitudReajusteLayout;

                public WidgetMonotributo()
                {
                        this.SetStyle(ControlStyles.DoubleBuffer |
                                      ControlStyles.UserPaint |
                                      ControlStyles.AllPaintingInWmPaint |
                                      ControlStyles.ResizeRedraw, true);

                        this.Height = 48;
                        this.Cursor = Cursors.Hand;
                        this.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point);

                        m_ToolTip = new ToolTip();
                        m_ToolTip.InitialDelay = 300;
                        m_ToolTip.ReshowDelay = 100;
                        m_ToolTip.AutoPopDelay = 15000;

                        CargarPreferencias();
                        CrearMenuContextual();
                        CalcularAnchoRequerido();
                }

                public void CargarPreferencias()
                {
                        if (Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null)
                        {
                                MostrarMesActual = Lfx.Workspace.Master.CurrentConfig.ReadLocalSettingInt("Sistema", "WidgetMonotributo.MostrarMesActual", 1) != 0;
                                MostrarMesAnterior = Lfx.Workspace.Master.CurrentConfig.ReadLocalSettingInt("Sistema", "WidgetMonotributo.MostrarMesAnterior", 1) != 0;
                                MostrarPeriodoRecat = Lfx.Workspace.Master.CurrentConfig.ReadLocalSettingInt("Sistema", "WidgetMonotributo.MostrarPeriodoRecat", 1) != 0;
                                MostrarUltimos12Meses = Lfx.Workspace.Master.CurrentConfig.ReadLocalSettingInt("Sistema", "WidgetMonotributo.MostrarUltimos12Meses", 0) != 0;
                                MostrarProyeccion = Lfx.Workspace.Master.CurrentConfig.ReadLocalSettingInt("Sistema", "WidgetMonotributo.MostrarProyeccion", 1) != 0;
                                MostrarCategoria = Lfx.Workspace.Master.CurrentConfig.ReadLocalSettingInt("Sistema", "WidgetMonotributo.MostrarCategoria", 1) != 0;
                        }
                        else
                        {
                                MostrarMesActual = true;
                                MostrarMesAnterior = true;
                                MostrarPeriodoRecat = true;
                                MostrarUltimos12Meses = false;
                                MostrarProyeccion = true;
                                MostrarCategoria = true;
                        }
                }

                public void GuardarPreferencias()
                {
                        if (Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null)
                        {
                                Lfx.Workspace.Master.CurrentConfig.WriteLocalSetting("Sistema", "WidgetMonotributo.MostrarMesActual", MostrarMesActual ? 1 : 0);
                                Lfx.Workspace.Master.CurrentConfig.WriteLocalSetting("Sistema", "WidgetMonotributo.MostrarMesAnterior", MostrarMesAnterior ? 1 : 0);
                                Lfx.Workspace.Master.CurrentConfig.WriteLocalSetting("Sistema", "WidgetMonotributo.MostrarPeriodoRecat", MostrarPeriodoRecat ? 1 : 0);
                                Lfx.Workspace.Master.CurrentConfig.WriteLocalSetting("Sistema", "WidgetMonotributo.MostrarUltimos12Meses", MostrarUltimos12Meses ? 1 : 0);
                                Lfx.Workspace.Master.CurrentConfig.WriteLocalSetting("Sistema", "WidgetMonotributo.MostrarProyeccion", MostrarProyeccion ? 1 : 0);
                                Lfx.Workspace.Master.CurrentConfig.WriteLocalSetting("Sistema", "WidgetMonotributo.MostrarCategoria", MostrarCategoria ? 1 : 0);
                        }
                }

                private void CrearMenuContextual()
                {
                        MenuContextual = new ContextMenuStrip();
                        MenuContextual.Font = new Font("Segoe UI", 9F);

                        ToolStripMenuItem headerItem = new ToolStripMenuItem("Información visible en widget:");
                        headerItem.Enabled = false;
                        headerItem.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                        MenuContextual.Items.Add(headerItem);

                        ItemMesActual = new ToolStripMenuItem("Total facturado mes actual", null, (s, e) => ToggleOpcion(1));
                        ItemMesActual.CheckOnClick = true;
                        ItemMesActual.Checked = MostrarMesActual;
                        MenuContextual.Items.Add(ItemMesActual);

                        ItemMesAnterior = new ToolStripMenuItem("Total facturado mes anterior", null, (s, e) => ToggleOpcion(2));
                        ItemMesAnterior.CheckOnClick = true;
                        ItemMesAnterior.Checked = MostrarMesAnterior;
                        MenuContextual.Items.Add(ItemMesAnterior);

                        ItemPeriodoRecat = new ToolStripMenuItem("Acumulado período recategorización", null, (s, e) => ToggleOpcion(3));
                        ItemPeriodoRecat.CheckOnClick = true;
                        ItemPeriodoRecat.Checked = MostrarPeriodoRecat;
                        MenuContextual.Items.Add(ItemPeriodoRecat);

                        ItemUltimos12Meses = new ToolStripMenuItem("Últimos 12 meses móviles", null, (s, e) => ToggleOpcion(4));
                        ItemUltimos12Meses.CheckOnClick = true;
                        ItemUltimos12Meses.Checked = MostrarUltimos12Meses;
                        MenuContextual.Items.Add(ItemUltimos12Meses);

                        ItemProyeccion = new ToolStripMenuItem("Total proyectado próxima recategorización", null, (s, e) => ToggleOpcion(5));
                        ItemProyeccion.CheckOnClick = true;
                        ItemProyeccion.Checked = MostrarProyeccion;
                        MenuContextual.Items.Add(ItemProyeccion);

                        ItemCategoria = new ToolStripMenuItem("Siguiente categoría / margen restante", null, (s, e) => ToggleOpcion(6));
                        ItemCategoria.CheckOnClick = true;
                        ItemCategoria.Checked = MostrarCategoria;
                        MenuContextual.Items.Add(ItemCategoria);

                        MenuContextual.Items.Add(new ToolStripSeparator());

                        ItemActualizar = new ToolStripMenuItem("Actualizar datos ahora", null, (s, e) => IniciarCargaDatos());
                        MenuContextual.Items.Add(ItemActualizar);

                        ItemVerDetalle = new ToolStripMenuItem("Ver detalle y diagnóstico...", null, (s, e) => AbrirDetalle());
                        ItemVerDetalle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                        MenuContextual.Items.Add(ItemVerDetalle);

                        ItemConfigEscalas = new ToolStripMenuItem("Configurar escalas de categorías...", null, (s, e) => AbrirConfigEscalas());
                        MenuContextual.Items.Add(ItemConfigEscalas);

                        this.ContextMenuStrip = MenuContextual;
                }

                private void ToggleOpcion(int opcion)
                {
                        switch (opcion)
                        {
                                case 1: MostrarMesActual = ItemMesActual.Checked; break;
                                case 2: MostrarMesAnterior = ItemMesAnterior.Checked; break;
                                case 3: MostrarPeriodoRecat = ItemPeriodoRecat.Checked; break;
                                case 4: MostrarUltimos12Meses = ItemUltimos12Meses.Checked; break;
                                case 5: MostrarProyeccion = ItemProyeccion.Checked; break;
                                case 6: MostrarCategoria = ItemCategoria.Checked; break;
                        }

                        GuardarPreferencias();
                        CalcularAnchoRequerido();
                        if (SolicitudReajusteLayout != null)
                                SolicitudReajusteLayout(this, EventArgs.Empty);

                        this.Invalidate();
                }

                private void CalcularAnchoRequerido()
                {
                        int itemsActivos = 0;
                        if (MostrarMesActual) itemsActivos++;
                        if (MostrarMesAnterior) itemsActivos++;
                        if (MostrarPeriodoRecat) itemsActivos++;
                        if (MostrarUltimos12Meses) itemsActivos++;
                        if (MostrarProyeccion) itemsActivos++;
                        if (MostrarCategoria) itemsActivos++;

                        // Si no hay ninguno activo, dejamos al menos un badge mínimo
                        if (itemsActivos == 0)
                        {
                                this.Width = 110;
                                return;
                        }

                        // Colocamos en pares (2 filas de métricas)
                        int columnas = (itemsActivos + 1) / 2;
                        int anchoBadge = 92;
                        int anchoColumna = 126;

                        this.Width = anchoBadge + (columnas * anchoColumna) + 12;
                }

                public void IniciarCargaDatos()
                {
                        if (m_Cargando)
                                return;

                        m_Cargando = true;
                        this.Invalidate();

                        ThreadPool.QueueUserWorkItem(state =>
                        {
                                try
                                {
                                        Lfx.Data.IConnection conn = null;
                                        if (Lfx.Workspace.Master != null)
                                                conn = Lfx.Workspace.Master.MasterConnection;

                                        ResumenMonotributo res = CalculadorMonotributo.Calcular(conn);

                                        if (this.IsHandleCreated && !this.IsDisposed)
                                        {
                                                this.BeginInvoke((MethodInvoker)delegate
                                                {
                                                        m_Resumen = res;
                                                        m_Cargando = false;
                                                        ActualizarToolTip();
                                                        this.Invalidate();
                                                });
                                        }
                                }
                                catch
                                {
                                        if (this.IsHandleCreated && !this.IsDisposed)
                                        {
                                                this.BeginInvoke((MethodInvoker)delegate
                                                {
                                                        m_Cargando = false;
                                                        this.Invalidate();
                                                });
                                        }
                                }
                        });
                }

                private void ActualizarToolTip()
                {
                        if (m_Resumen != null)
                        {
                                m_ToolTip.SetToolTip(this, m_Resumen.GenerarTextoInformativo() +
                                        "\r\n[Clic izquierdo / Doble clic: Ver detalle completo]\r\n[Clic derecho: Configurar datos visibles]");
                        }
                        else
                        {
                                m_ToolTip.SetToolTip(this, "Control de Monotributo\r\n[Clic derecho para configurar]");
                        }
                }

                protected override void OnMouseEnter(EventArgs e)
                {
                        base.OnMouseEnter(e);
                        m_MouseEncima = true;
                        this.Invalidate();
                }

                protected override void OnMouseLeave(EventArgs e)
                {
                        base.OnMouseLeave(e);
                        m_MouseEncima = false;
                        this.Invalidate();
                }

                protected override void OnMouseClick(MouseEventArgs e)
                {
                        base.OnMouseClick(e);
                        if (e.Button == MouseButtons.Left)
                        {
                                AbrirDetalle();
                        }
                }

                protected override void OnDoubleClick(EventArgs e)
                {
                        base.OnDoubleClick(e);
                        AbrirDetalle();
                }

                private void AbrirDetalle()
                {
                        Lfx.Data.IConnection conn = null;
                        if (Lfx.Workspace.Master != null)
                                conn = Lfx.Workspace.Master.MasterConnection;

                        using (var form = new FormDetalleMonotributo(conn))
                        {
                                form.ShowDialog(this);
                                IniciarCargaDatos();
                        }
                }

                private void AbrirConfigEscalas()
                {
                        using (var form = new FormConfigurarEscalasMonotributo())
                        {
                                if (form.ShowDialog(this) == DialogResult.OK)
                                {
                                        IniciarCargaDatos();
                                }
                        }
                }

                protected override void OnPaint(PaintEventArgs e)
                {
                        base.OnPaint(e);
                        Graphics g = e.Graphics;
                        g.SmoothingMode = SmoothingMode.AntiAlias;

                        Rectangle bounds = new Rectangle(0, 0, this.Width - 1, this.Height - 1);

                        // 1. Fondo de la tarjeta
                        Color fondo = m_MouseEncima ? Color.FromArgb(240, 246, 252) : Color.FromArgb(248, 250, 252);
                        Color borde = m_MouseEncima ? Color.FromArgb(160, 190, 220) : Color.FromArgb(215, 222, 230);

                        using (SolidBrush bFondo = new SolidBrush(fondo))
                        using (Pen pBorde = new Pen(borde, 1f))
                        {
                                g.FillRectangle(bFondo, bounds);
                                g.DrawRectangle(pBorde, bounds);
                        }

                        // 2. Badge "MONOTRIBUTO" a la izquierda con barra de progreso de fondo
                        Rectangle rBadge = new Rectangle(4, 4, 84, this.Height - 8);

                        // Porcentaje del tramo en la categoría proyectada
                        float pct = 0f;
                        if (m_Resumen != null)
                        {
                                if (m_Resumen.ExcluidoProyectado)
                                        pct = 100f;
                                else
                                        pct = (float)m_Resumen.PorcentajeTramoActual;
                        }
                        pct = Math.Max(0f, Math.Min(100f, pct));

                        // Color de fondo del badge (track no completado)
                        Color colorTrack = Color.FromArgb(24, 38, 56);

                        // Color de relleno según categoría (especial para la última categoría K y exclusión)
                        Color colorFill;
                        if (m_Resumen != null && m_Resumen.ExcluidoProyectado)
                        {
                                colorFill = Color.FromArgb(195, 30, 30); // Rojo exclusión total
                        }
                        else if (m_Resumen != null && m_Resumen.EsUltimaCategoria)
                        {
                                // ¡ÚLTIMA CATEGORÍA (K)! Al completar el 100% no se pasa a otra categoría sino que se EXCLUYE.
                                if (pct < 50f)
                                        colorFill = Color.FromArgb(215, 140, 25); // Ámbar preventivo
                                else if (pct < 80f)
                                        colorFill = Color.FromArgb(225, 95, 20); // Naranja alerta
                                else
                                        colorFill = Color.FromArgb(210, 35, 35); // Rojo riesgo inminente de exclusión
                        }
                        else
                        {
                                // Categorías estándar (A a J):
                                if (pct < 85f)
                                        colorFill = Color.FromArgb(35, 115, 195); // Azul ARCA / AFIP
                                else
                                        colorFill = Color.FromArgb(45, 145, 185); // Azul más vivo / cian (próximo a recategorizar)
                        }

                        // Pintar fondo base (track)
                        using (SolidBrush bTrack = new SolidBrush(colorTrack))
                        {
                                g.FillRectangle(bTrack, rBadge);
                        }

                        // Pintar relleno porcentual
                        int fillW = (int)Math.Round(rBadge.Width * (pct / 100.0f));
                        fillW = Math.Max(0, Math.Min(rBadge.Width, fillW));
                        if (fillW > 0)
                        {
                                Rectangle rFill = new Rectangle(rBadge.Left, rBadge.Top, fillW, rBadge.Height);
                                using (SolidBrush bFill = new SolidBrush(colorFill))
                                {
                                        g.FillRectangle(bFill, rFill);
                                }

                                if (fillW < rBadge.Width)
                                {
                                        using (Pen pLine = new Pen(Color.FromArgb(90, 255, 255, 255), 1f))
                                        {
                                                g.DrawLine(pLine, rFill.Right - 1, rFill.Top, rFill.Right - 1, rFill.Bottom);
                                        }
                                }
                        }

                        // Borde del badge
                        using (Pen pBadge = new Pen(Color.FromArgb(60, 85, 115), 1f))
                        {
                                g.DrawRectangle(pBadge, rBadge);
                        }

                        // Textos del badge
                        using (Font fBadgeTit = new Font("Segoe UI", 6.5F, FontStyle.Bold))
                        using (Font fBadgeCat = new Font("Segoe UI", 8.25F, FontStyle.Bold))
                        using (SolidBrush bBlanco = new SolidBrush(Color.White))
                        using (StringFormat sf = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                                Rectangle rBadgeTit = new Rectangle(rBadge.Left, rBadge.Top + 2, rBadge.Width, 14);
                                g.DrawString("MONOTRIBUTO", fBadgeTit, bBlanco, rBadgeTit, sf);

                                Rectangle rBadgeVal = new Rectangle(rBadge.Left, rBadge.Top + 14, rBadge.Width, rBadge.Height - 16);
                                string strCat = "ARCA";
                                if (m_Resumen != null)
                                {
                                        if (m_Resumen.ExcluidoProyectado)
                                        {
                                                strCat = "EXCL.";
                                        }
                                        else if (m_Resumen.CategoriaProyectada != null)
                                        {
                                                strCat = string.Format("Cat. {0} • {1:N0}%", m_Resumen.CategoriaProyectada.Letra, pct);
                                        }
                                        else
                                        {
                                                strCat = "Cat. A • 0%";
                                        }
                                }
                                g.DrawString(strCat, fBadgeCat, bBlanco, rBadgeVal, sf);
                        }

                        // 3. Si está cargando datos iniciales
                        if (m_Cargando && m_Resumen == null)
                        {
                                using (Font f = new Font("Segoe UI", 8.25F, FontStyle.Italic))
                                using (SolidBrush b = new SolidBrush(Color.FromArgb(100, 110, 120)))
                                {
                                        g.DrawString("Calculando facturación...", f, b, 96, 16);
                                }
                                return;
                        }

                        // 4. Armar lista de métricas seleccionadas para mostrar
                        var metricas = new List<MetricaInfo>();

                        if (MostrarMesActual)
                        {
                                decimal v = m_Resumen != null ? m_Resumen.FacturadoMesActual : 0m;
                                metricas.Add(new MetricaInfo("Mes actual:", v, Color.FromArgb(20, 30, 45)));
                        }

                        if (MostrarMesAnterior)
                        {
                                decimal v = m_Resumen != null ? m_Resumen.FacturadoMesAnterior : 0m;
                                metricas.Add(new MetricaInfo("Mes anterior:", v, Color.FromArgb(80, 90, 100)));
                        }

                        if (MostrarPeriodoRecat)
                        {
                                decimal v = m_Resumen != null ? m_Resumen.FacturadoPeriodoRecat : 0m;
                                string etiqueta = (m_Resumen != null && m_Resumen.PeriodoRecat != null && m_Resumen.PeriodoRecat.MesRecategorizacion == 7) ? "Semestre (Jul):" : "Semestre (Ene):";
                                metricas.Add(new MetricaInfo(etiqueta, v, Color.FromArgb(20, 30, 45)));
                        }

                        if (MostrarUltimos12Meses)
                        {
                                decimal v = m_Resumen != null ? m_Resumen.FacturadoUltimos12Meses : 0m;
                                metricas.Add(new MetricaInfo("Últimos 12m:", v, Color.FromArgb(20, 30, 45)));
                        }

                        if (MostrarProyeccion)
                        {
                                decimal v = m_Resumen != null ? m_Resumen.FacturadoProyectado : 0m;
                                Color c = (m_Resumen != null && m_Resumen.ExcluidoProyectado) ? Color.Red : Color.FromArgb(0, 90, 160);
                                metricas.Add(new MetricaInfo("Proy. recat:", v, c, true));
                        }

                        if (MostrarCategoria)
                        {
                                string tit = "Siguiente categoría:";
                                string val = "-";
                                Color c = Color.FromArgb(0, 100, 0);

                                if (m_Resumen != null)
                                {
                                        if (m_Resumen.ExcluidoProyectado)
                                        {
                                                tit = "Situación fiscal:";
                                                val = "EXCLUSIÓN R.G.";
                                                c = Color.Red;
                                        }
                                        else if (m_Resumen.CategoriaProyectada != null)
                                        {
                                                val = "Resta " + FormatoMoneda.Formatear(m_Resumen.MargenProyectado);

                                                if (m_Resumen.EsUltimaCategoria)
                                                {
                                                        tit = "A exclusión:";
                                                        c = Color.FromArgb(200, 50, 0);
                                                }
                                                else
                                                {
                                                        tit = "Siguiente categoría:";
                                                        c = Color.FromArgb(0, 100, 0);
                                                }
                                        }
                                }
                                metricas.Add(new MetricaInfo(tit, val, c, true));
                        }

                        // 5. Dibujar métricas en 2 filas compactas
                        int startX = 96;
                        int colWidth = 126;
                        int row1Y = 5;
                        int row2Y = 25;

                        using (Font fTit = new Font("Segoe UI", 7F, FontStyle.Regular))
                        using (Font fVal = new Font("Segoe UI", 8F, FontStyle.Bold))
                        using (SolidBrush bTit = new SolidBrush(Color.FromArgb(100, 110, 120)))
                        {
                                for (int i = 0; i < metricas.Count; i++)
                                {
                                        int col = i / 2;
                                        int row = i % 2;
                                        int x = startX + (col * colWidth);
                                        int y = (row == 0) ? row1Y : row2Y;

                                        var m = metricas[i];

                                        // Título de la métrica
                                        g.DrawString(m.Titulo, fTit, bTit, x, y);

                                        // Valor de la métrica
                                        using (SolidBrush bVal = new SolidBrush(m.Color))
                                        {
                                                g.DrawString(m.ValorTexto, fVal, bVal, x, y + 9);
                                        }

                                        // Separador vertical sutil entre columnas
                                        if (row == 1 && col < (metricas.Count - 1) / 2)
                                        {
                                                using (Pen pSep = new Pen(Color.FromArgb(225, 230, 235), 1f))
                                                {
                                                        g.DrawLine(pSep, x + colWidth - 6, 6, x + colWidth - 6, this.Height - 6);
                                                }
                                        }
                                }
                        }
                }

                private class MetricaInfo
                {
                        public string Titulo { get; set; }
                        public string ValorTexto { get; set; }
                        public Color Color { get; set; }
                        public bool Destacado { get; set; }

                        public MetricaInfo(string titulo, decimal monto, Color color, bool destacado = false)
                        {
                                this.Titulo = titulo;
                                this.ValorTexto = FormatoMoneda.Formatear(monto);
                                this.Color = color;
                                this.Destacado = destacado;
                        }

                        public MetricaInfo(string titulo, string texto, Color color, bool destacado = false)
                        {
                                this.Titulo = titulo;
                                this.ValorTexto = texto;
                                this.Color = color;
                                this.Destacado = destacado;
                        }
                }
        }
}
