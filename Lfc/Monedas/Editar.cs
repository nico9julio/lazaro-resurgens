using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Lfc.Monedas
{
        public partial class Editar : Lcc.Edicion.ControlEdicion
        {
                public Editar()
                {
                        ElementoTipo = typeof(Lbl.Entidades.Moneda);

                        InitializeComponent();
                }

                public override void ActualizarControl()
                {
                        Lbl.Entidades.Moneda Res = this.Elemento as Lbl.Entidades.Moneda;
                        EntradaNombre.Text = Res.Nombre;
                        EntradaCotizacion.ValueDecimal = Res.Cotizacion;
                        EntradaCodigoIso.Text = Res.NomenclaturaIso;
                        EntradaSigno.Text = Res.Simbolo;
                        if (Res.Fecha != null)
                                LabelFechaValor.Text = "La �ltima fecha de modificaci�n registrada es del: " + Res.Fecha;
                        else
                                LabelFechaValor.Text = "";

                        base.ActualizarControl();
                }

                public override Lfx.Types.OperationResult ValidarControl()
                {
                      

                        if (EntradaCotizacion.ValueDecimal<0)
                              return new Lfx.Types.FailureOperationResult("La catizaci�n debe ser mayor a 0");
                        if ((EntradaCodigoIso.Text.Length > 3) || (EntradaCodigoIso.Text.Length < 1))
                                return new Lfx.Types.FailureOperationResult("El C�digo ISO debe ser de tres letras m�ximo");
                        if ((EntradaSigno.Text.Length > 3) || (EntradaSigno.Text.Length < 1))
                                return new Lfx.Types.FailureOperationResult("El s�mbolo de la moneda debe ser de tres caracteres m�ximo");
                        return base.ValidarControl();
                }

                public override void ActualizarElemento()
                {
                        Lbl.Entidades.Moneda Res = this.Elemento as Lbl.Entidades.Moneda;


                        Res.Cotizacion = EntradaCotizacion.ValueDecimal;
                        Res.Nombre = EntradaNombre.Text;
                        Res.NomenclaturaIso = EntradaCodigoIso.Text;
                        Res.Simbolo = EntradaSigno.Text;
                        base.ActualizarElemento();
                }
        }
}
