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
                public bool MostrarTopeAfip { get; set; }
                public bool MostrarProximaEscala { get; set; }
                public bool MostrarCategoria
                {
                        get { return MostrarTopeAfip || MostrarProximaEscala; }
                        set { MostrarTopeAfip = value; MostrarProximaEscala = value; }
                }

                private ContextMenuStrip MenuContextual;
                private ToolStripMenuItem ItemMesActual;
                private ToolStripMenuItem ItemMesAnterior;
                private ToolStripMenuItem ItemPeriodoRecat;
                private ToolStripMenuItem ItemUltimos12Meses;
                private ToolStripMenuItem ItemProyeccion;
                private ToolStripMenuItem ItemTopeAfip;
                private ToolStripMenuItem ItemProximaEscala;
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
                                int mostrarCatPrev = Lfx.Workspace.Master.CurrentConfig.ReadLocalSettingInt("Sistema", "WidgetMonotributo.MostrarCategoria", 1);
                                MostrarTopeAfip = Lfx.Workspace.Master.CurrentConfig.ReadLocalSettingInt("Sistema", "WidgetMonotributo.MostrarTopeAfip", mostrarCatPrev) != 0;
                                MostrarProximaEscala = Lfx.Workspace.Master.CurrentConfig.ReadLocalSettingInt("Sistema", "WidgetMonotributo.MostrarProximaEscala", mostrarCatPrev) != 0;
                        }
                        else
                        {
                                MostrarMesActual = true;
                                MostrarMesAnterior = true;
                                MostrarPeriodoRecat = true;
                                MostrarUltimos12Meses = false;
                                MostrarProyeccion = true;
                                MostrarTopeAfip = true;
                                MostrarProximaEscala = true;
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
                                Lfx.Workspace.Master.CurrentConfig.WriteLocalSetting("Sistema", "WidgetMonotributo.MostrarTopeAfip", MostrarTopeAfip ? 1 : 0);
                                Lfx.Workspace.Master.CurrentConfig.WriteLocalSetting("Sistema", "WidgetMonotributo.MostrarProximaEscala", MostrarProximaEscala ? 1 : 0);
                                Lfx.Workspace.Master.CurrentConfig.WriteLocalSetting("Sistema", "WidgetMonotributo.MostrarCategoria", (MostrarTopeAfip || MostrarProximaEscala) ? 1 : 0);
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

                        ItemPeriodoRecat = new ToolStripMenuItem("Facturado acumulado año / período (AFIP)", null, (s, e) => ToggleOpcion(3));
                        ItemPeriodoRecat.CheckOnClick = true;
                        ItemPeriodoRecat.Checked = MostrarPeriodoRecat;
                        MenuContextual.Items.Add(ItemPeriodoRecat);

                        ItemUltimos12Meses = new ToolStripMenuItem("Últimos 12 meses móviles", null, (s, e) => ToggleOpcion(4));
                        ItemUltimos12Meses.CheckOnClick = true;
                        ItemUltimos12Meses.Checked = MostrarUltimos12Meses;
                        MenuContextual.Items.Add(ItemUltimos12Meses);

                        ItemProyeccion = new ToolStripMenuItem("Proyección anual estimada", null, (s, e) => ToggleOpcion(5));
                        ItemProyeccion.CheckOnClick = true;
                        ItemProyeccion.Checked = MostrarProyeccion;
                        MenuContextual.Items.Add(ItemProyeccion);

                        ItemTopeAfip = new ToolStripMenuItem("Situación en AFIP (Cat. actual y % de tope)", null, (s, e) => ToggleOpcion(6));
                        ItemTopeAfip.CheckOnClick = true;
                        ItemTopeAfip.Checked = MostrarTopeAfip;
                        MenuContextual.Items.Add(ItemTopeAfip);

                        ItemProximaEscala = new ToolStripMenuItem("Próxima escala / Proyección de recategorización", null, (s, e) => ToggleOpcion(7));
                        ItemProximaEscala.CheckOnClick = true;
                        ItemProximaEscala.Checked = MostrarProximaEscala;
                        MenuContextual.Items.Add(ItemProximaEscala);

                        MenuContextual.Items.Add(new ToolStripSeparator());

                        // Submenú para seleccionar categoría registrada en AFIP
                        ToolStripMenuItem itemCatSub = new ToolStripMenuItem("Categoría registrada en ARCA / AFIP");
                        MenuContextual.Items.Add(itemCatSub);

                        ToolStripMenuItem itAuto = new ToolStripMenuItem("Automática (según facturación)", null, (s, e) => CambiarCategoriaInscripta("auto"));
                        itemCatSub.DropDownItems.Add(itAuto);
                        itemCatSub.DropDownItems.Add(new ToolStripSeparator());

                        string[] letrasCat = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K" };
                        foreach (string let in letrasCat)
                        {
                                string l = let;
                                ToolStripMenuItem itL = new ToolStripMenuItem("Categoría " + l, null, (s, e) => CambiarCategoriaInscripta(l));
                                itemCatSub.DropDownItems.Add(itL);
                        }

                        ToolStripMenuItem itConsAfip = new ToolStripMenuItem("Consultar categoría actual en ARCA / AFIP...", IconosMonotributo.Afip, (s, e) => EjecutarConsultaAfipManual());
                        MenuContextual.Items.Add(itConsAfip);

                        MenuContextual.Opening += (s, e) =>
                        {
                                string catAct = Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null ?
                                        Lfx.Workspace.Master.CurrentConfig.ReadGlobalSetting<string>("Sistema.Monotributo.CategoriaInscripta", "auto") : "auto";
                                if (string.IsNullOrEmpty(catAct) || catAct == "*") catAct = "auto";

                                itAuto.Checked = catAct.Equals("auto", StringComparison.OrdinalIgnoreCase);
                                foreach (ToolStripItem it in itemCatSub.DropDownItems)
                                {
                                        if (it is ToolStripMenuItem item && item != itAuto)
                                        {
                                                item.Checked = item.Text.EndsWith(catAct, StringComparison.OrdinalIgnoreCase);
                                        }
                                }
                        };

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

                private void CambiarCategoriaInscripta(string letra)
                {
                        if (Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null)
                        {
                                Lfx.Workspace.Master.CurrentConfig.WriteGlobalSetting("Sistema.Monotributo.CategoriaInscripta", letra);
                        }
                        IniciarCargaDatos();
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
                                        IniciarCargaDatos();

                                        string titular = !string.IsNullOrEmpty(res.RazonSocial) ? (" de " + res.RazonSocial) : "";
                                        MessageBox.Show(this,
                                                string.Format("Se consultó exitosamente el Web Service de ARCA / AFIP.\r\n\r\nCategoría actual detectada: Categoría {0}{1}\r\nCUIT: {2}\r\n\r\nEl widget ha sido actualizado con los nuevos topes.",
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
                                                        res.Mensaje + "\r\n\r\nPuede seleccionar la categoría manualmente desde este menú.",
                                                        "Consulta Web Service AFIP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        }
                                }
                        }
                        finally
                        {
                                Cursor = Cursors.Default;
                        }
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
                                case 6: MostrarTopeAfip = ItemTopeAfip.Checked; break;
                                case 7: MostrarProximaEscala = ItemProximaEscala.Checked; break;
                        }

                        GuardarPreferencias();
                        CalcularAnchoRequerido();
                        if (SolicitudReajusteLayout != null)
                                SolicitudReajusteLayout(this, EventArgs.Empty);

                        this.Invalidate();
                }

                private void CalcularAnchoRequerido()
                {
                        int itemsTexto = 0;
                        if (MostrarMesActual) itemsTexto++;
                        if (MostrarMesAnterior) itemsTexto++;
                        if (MostrarPeriodoRecat) itemsTexto++;
                        if (MostrarUltimos12Meses) itemsTexto++;
                        if (MostrarProyeccion) itemsTexto++;
                        if (MostrarTopeAfip) itemsTexto++;

                        int anchoBadgeIzq = 104;
                        int anchoBadgeDer = MostrarProximaEscala ? 108 : 0;
                        int anchoColumna = 142;

                        // Si no hay métricas de texto activas, dejamos el ancho de los badges
                        if (itemsTexto == 0)
                        {
                                this.Width = anchoBadgeIzq + anchoBadgeDer + (anchoBadgeDer > 0 ? 12 : 8);
                                return;
                        }

                        // Colocamos en pares (2 filas de métricas)
                        int columnas = (itemsTexto + 1) / 2;
                        this.Width = anchoBadgeIzq + (columnas * anchoColumna) + anchoBadgeDer + 14;
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

                                        // Si no tiene categoría inscripta configurada, intentar detectarla silenciosamente con AFIP en background
                                        if (res != null && res.CategoriaInscripta == null)
                                        {
                                                try
                                                {
                                                        var afipRes = Lbl.Impuestos.Monotributo.ConsultaConstanciaAfip.Consultar();
                                                        if (afipRes != null && afipRes.Exito && !string.IsNullOrEmpty(afipRes.Categoria))
                                                        {
                                                                string catLimpia = EscalasMonotributo.Instancia.NormalizarLetra(afipRes.Categoria);
                                                                if (string.IsNullOrEmpty(catLimpia)) catLimpia = afipRes.Categoria;

                                                                if (Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null)
                                                                {
                                                                        Lfx.Workspace.Master.CurrentConfig.WriteGlobalSetting("Sistema.Monotributo.CategoriaInscripta", catLimpia);
                                                                }
                                                                res = CalculadorMonotributo.Calcular(conn);
                                                        }
                                                }
                                                catch { }
                                        }

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

                        // 2. Badge "AFIP" a la izquierda (con porcentaje adentro de la barra y resta abajo)
                        Rectangle rBadgeIzq = new Rectangle(4, 4, 104, this.Height - 8);

                        string diasTxt = (m_Resumen != null && m_Resumen.DiasRestantes > 0) ? string.Format(" ({0}d)", m_Resumen.DiasRestantes) : "";
                        string titIzq = "AFIP";
                        if (m_Resumen != null && m_Resumen.CategoriaInscripta != null)
                                titIzq = "AFIP " + m_Resumen.CategoriaInscripta.Letra + diasTxt;
                        else if (m_Resumen != null && m_Resumen.CategoriaActual != null)
                                titIzq = "CAT. " + m_Resumen.CategoriaActual.Letra + diasTxt;
                        else if (m_Resumen != null)
                                titIzq = "AFIP" + diasTxt;

                        float pctIzq = 0f;
                        Color colorBarraIzq = Color.FromArgb(28, 140, 70);
                        string txtRestaIzq = "-";
                        Color colorRestaIzq = Color.FromArgb(200, 240, 210);

                        if (m_Resumen != null)
                        {
                                if (m_Resumen.CategoriaInscripta != null)
                                {
                                        pctIzq = (float)m_Resumen.PorcentajeCategoriaInscripta;
                                        if (m_Resumen.SuperoCategoriaInscripta)
                                        {
                                                colorBarraIzq = Color.FromArgb(220, 30, 30);
                                                txtRestaIzq = "Exceso " + FormatoMoneda.Formatear(Math.Abs(m_Resumen.MargenCategoriaInscripta));
                                                colorRestaIzq = Color.FromArgb(255, 120, 120);
                                        }
                                        else
                                        {
                                                colorBarraIzq = (pctIzq > 85f) ? Color.FromArgb(230, 110, 0) : Color.FromArgb(28, 140, 70);
                                                decimal monto = m_Resumen.MargenCategoriaInscripta;
                                                txtRestaIzq = "Resta " + FormatoMoneda.Formatear(monto);
                                                if (monto >= 10000000m)
                                                        txtRestaIzq = string.Format("Resta ${0:N1}M", monto / 1000000m);
                                                colorRestaIzq = (pctIzq > 85f) ? Color.FromArgb(255, 200, 120) : Color.FromArgb(200, 240, 210);
                                        }
                                }
                                else if (m_Resumen.ExcluidoActual)
                                {
                                        pctIzq = 100f;
                                        colorBarraIzq = Color.FromArgb(220, 30, 30);
                                        txtRestaIzq = "EXCLUSIÓN";
                                        colorRestaIzq = Color.FromArgb(255, 120, 120);
                                }
                                else if (m_Resumen.CategoriaActual != null)
                                {
                                        pctIzq = (float)m_Resumen.PorcentajeTramoActual;
                                        colorBarraIzq = Color.FromArgb(35, 115, 195);
                                        decimal monto = m_Resumen.MargenProyectado;
                                        txtRestaIzq = "Resta " + FormatoMoneda.Formatear(monto);
                                        if (monto >= 10000000m)
                                                txtRestaIzq = string.Format("Resta ${0:N1}M", monto / 1000000m);
                                        colorRestaIzq = Color.FromArgb(200, 230, 255);
                                }
                        }

                        DibujarBadgeGrafico(g, rBadgeIzq, IconosMonotributo.Afip, titIzq, pctIzq, colorBarraIzq, txtRestaIzq, colorRestaIzq);

                        // 2b. Badge "BAJA / PRÓXIMA ESCALA" a la derecha (si está activo)
                        if (MostrarProximaEscala)
                        {
                                Rectangle rBadgeDer = new Rectangle(this.Width - 108, 4, 104, this.Height - 8);

                                Image iconoDer = IconosMonotributo.Proyeccion;
                                string titDer = "PROYECCIÓN";
                                float pctDer = 0f;
                                Color colorBarraDer = Color.FromArgb(35, 115, 195);
                                string txtRestaDer = "-";
                                Color colorRestaDer = Color.FromArgb(200, 230, 255);

                                if (m_Resumen != null)
                                {
                                        if (m_Resumen.HayOportunidadBaja && m_Resumen.CategoriaBaja != null)
                                        {
                                                iconoDer = IconosMonotributo.Baja;
                                                titDer = "BAJA " + m_Resumen.CategoriaBaja.Letra + diasTxt;
                                                pctDer = (float)m_Resumen.PorcentajeConsumoBajaPeriodo;
                                                colorBarraDer = (pctDer > 85f) ? Color.FromArgb(230, 110, 0) : Color.FromArgb(16, 185, 129);
                                                decimal montoBaja = m_Resumen.MargenParaPasarseDeBajaPeriodo;
                                                txtRestaDer = "Resta " + FormatoMoneda.Formatear(montoBaja);
                                                if (montoBaja >= 10000000m)
                                                        txtRestaDer = string.Format("Resta ${0:N1}M", montoBaja / 1000000m);
                                                colorRestaDer = Color.FromArgb(180, 245, 210);
                                        }
                                        else if (m_Resumen.ExcluidoProyectado)
                                        {
                                                iconoDer = IconosMonotributo.Alerta;
                                                titDer = "EXCLUSIÓN";
                                                pctDer = 100f;
                                                colorBarraDer = Color.FromArgb(220, 30, 30);
                                                txtRestaDer = "Rég. General";
                                                colorRestaDer = Color.FromArgb(255, 150, 150);
                                        }
                                        else if (m_Resumen.CategoriaProyectada != null)
                                        {
                                                int comp = (m_Resumen.CategoriaInscripta != null) ?
                                                        string.Compare(m_Resumen.CategoriaProyectada.Letra, m_Resumen.CategoriaInscripta.Letra, StringComparison.OrdinalIgnoreCase) : 0;
                                                if (comp > 0)
                                                {
                                                        iconoDer = IconosMonotributo.Sube;
                                                        titDer = "SUBE " + m_Resumen.CategoriaProyectada.Letra + diasTxt;
                                                        pctDer = (float)m_Resumen.PorcentajeTopeProyectado;
                                                        colorBarraDer = Color.FromArgb(230, 100, 10);
                                                        decimal monto = m_Resumen.MargenProyectado;
                                                        txtRestaDer = "Resta " + FormatoMoneda.Formatear(monto);
                                                        if (monto >= 10000000m)
                                                                txtRestaDer = string.Format("Resta ${0:N1}M", monto / 1000000m);
                                                        colorRestaDer = Color.FromArgb(255, 210, 170);
                                                }
                                                else
                                                {
                                                        iconoDer = IconosMonotributo.Proyeccion;
                                                        titDer = "PROY. " + m_Resumen.CategoriaProyectada.Letra + diasTxt;
                                                        pctDer = (float)m_Resumen.PorcentajeTopeProyectado;
                                                        colorBarraDer = Color.FromArgb(35, 115, 195);
                                                        decimal monto = m_Resumen.MargenProyectado;
                                                        txtRestaDer = "Resta " + FormatoMoneda.Formatear(monto);
                                                        if (monto >= 10000000m)
                                                                txtRestaDer = string.Format("Resta ${0:N1}M", monto / 1000000m);
                                                        colorRestaDer = Color.FromArgb(200, 230, 255);
                                                }
                                        }
                                }

                                DibujarBadgeGrafico(g, rBadgeDer, iconoDer, titDer, pctDer, colorBarraDer, txtRestaDer, colorRestaDer);
                        }

                        // 3. Si está cargando datos iniciales
                        if (m_Cargando && m_Resumen == null)
                        {
                                using (Font f = new Font("Segoe UI", 8.25F, FontStyle.Italic))
                                using (SolidBrush b = new SolidBrush(Color.FromArgb(100, 110, 120)))
                                {
                                        g.DrawString("Calculando facturación...", f, b, 116, 16);
                                }
                                return;
                        }

                        // 4. Armar lista de métricas seleccionadas para mostrar en el centro
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
                                string etiqueta = "Facturado período:";
                                if (m_Resumen != null && m_Resumen.PeriodoRecat != null)
                                {
                                        if (m_Resumen.PeriodoRecat.FechaInicioPeriodo.Month == 1)
                                                etiqueta = "Facturado " + m_Resumen.PeriodoRecat.FechaInicioPeriodo.Year + ":";
                                        else
                                                etiqueta = "Facturado Jul-Jun:";
                                }
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
                                metricas.Add(new MetricaInfo("Proy. anual:", v, c, true));
                        }

                        if (MostrarTopeAfip)
                        {
                                string tit = "Tope AFIP:";
                                string val = "Sin verificar";
                                Color c = Color.FromArgb(80, 90, 100);

                                if (m_Resumen != null)
                                {
                                        if (m_Resumen.CategoriaInscripta != null)
                                        {
                                                tit = "Tope AFIP (Cat. " + m_Resumen.CategoriaInscripta.Letra + "):";
                                                if (m_Resumen.SuperoCategoriaInscripta)
                                                {
                                                        val = "Superado por " + FormatoMoneda.Formatear(Math.Abs(m_Resumen.MargenCategoriaInscripta));
                                                        c = Color.Red;
                                                }
                                                else
                                                {
                                                        val = "Resta " + FormatoMoneda.Formatear(m_Resumen.MargenCategoriaInscripta) + string.Format(" ({0:N0}%)", m_Resumen.PorcentajeCategoriaInscripta);
                                                        c = (m_Resumen.PorcentajeCategoriaInscripta > 85m) ? Color.FromArgb(200, 80, 0) : Color.FromArgb(0, 100, 0);
                                                }
                                        }
                                        else
                                        {
                                                tit = "Cat. AFIP:";
                                                val = "Sin verificar";
                                                c = Color.FromArgb(100, 110, 120);
                                        }
                                }
                                metricas.Add(new MetricaInfo(tit, val, c, true));
                        }

                        // 5. Dibujar métricas en 2 filas compactas
                        int startX = 114;
                        int colWidth = 142;
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

                private void DibujarBadgeGrafico(Graphics g, Rectangle rBadge, Image icono, string titulo, float pct, Color colorBarra, string textoResta, Color colorResta)
                {
                        // 1. Fondo base del badge y borde
                        using (SolidBrush bFondo = new SolidBrush(Color.FromArgb(22, 34, 48)))
                        using (Pen pBorde = new Pen(Color.FromArgb(50, 75, 105), 1f))
                        {
                                g.FillRectangle(bFondo, rBadge);
                                g.DrawRectangle(pBorde, rBadge);
                        }

                        using (Font fTit = new Font("Segoe UI", 6.5F, FontStyle.Bold))
                        using (Font fPct = new Font("Segoe UI", 6.5F, FontStyle.Bold))
                        using (Font fResta = new Font("Segoe UI", 6.5F, FontStyle.Bold))
                        using (SolidBrush bTit = new SolidBrush(Color.FromArgb(240, 245, 250)))
                        using (SolidBrush bResta = new SolidBrush(colorResta))
                        using (StringFormat sf = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                                // 2. Ícono y Título (Renglón 1)
                                if (icono != null)
                                {
                                        g.DrawImage(icono, new Rectangle(rBadge.Left + 5, rBadge.Top + 1, 12, 12));
                                        Rectangle rTit = new Rectangle(rBadge.Left + 19, rBadge.Top + 1, rBadge.Width - 21, 12);
                                        using (StringFormat sfTit = new StringFormat() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
                                        {
                                                g.DrawString(titulo, fTit, bTit, rTit, sfTit);
                                        }
                                }
                                else
                                {
                                        Rectangle rTit = new Rectangle(rBadge.Left + 2, rBadge.Top + 1, rBadge.Width - 4, 12);
                                        g.DrawString(titulo, fTit, bTit, rTit, sf);
                                }

                                // 3. Barra de progreso (Renglón 2) con porcentaje centrado adentro
                                Rectangle rBarra = new Rectangle(rBadge.Left + 5, rBadge.Top + 14, rBadge.Width - 10, 11);
                                using (SolidBrush bTrack = new SolidBrush(Color.FromArgb(12, 20, 30)))
                                using (Pen pBarra = new Pen(Color.FromArgb(40, 60, 85), 1f))
                                {
                                        g.FillRectangle(bTrack, rBarra);

                                        float pctClamped = Math.Max(0f, Math.Min(100f, pct));
                                        int fillW = (int)Math.Round(rBarra.Width * (pctClamped / 100.0f));
                                        if (fillW > 0)
                                        {
                                                Rectangle rFill = new Rectangle(rBarra.Left, rBarra.Top, fillW, rBarra.Height);
                                                using (SolidBrush bFill = new SolidBrush(colorBarra))
                                                {
                                                        g.FillRectangle(bFill, rFill);
                                                }
                                        }
                                        g.DrawRectangle(pBarra, rBarra);
                                }

                                // Porcentaje escrito centrado adentro de la barra
                                string strPct = string.Format("{0:N1}%", pct);
                                using (SolidBrush bBlanco = new SolidBrush(Color.White))
                                {
                                        g.DrawString(strPct, fPct, bBlanco, rBarra, sf);
                                }

                                // 4. Monto restante (Renglón 3)
                                Rectangle rResta = new Rectangle(rBadge.Left + 2, rBadge.Top + 26, rBadge.Width - 4, 12);
                                if (g.MeasureString(textoResta, fResta).Width > rResta.Width)
                                {
                                        using (Font fMini = new Font("Segoe UI", 6F, FontStyle.Bold))
                                        {
                                                g.DrawString(textoResta, fMini, bResta, rResta, sf);
                                        }
                                }
                                else
                                {
                                        g.DrawString(textoResta, fResta, bResta, rResta, sf);
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
