using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using Lbl.Impuestos.Monotributo;

namespace Lazaro.WinMain.Principal
{
	/// <summary>
	/// Coordina la verificación automática en segundo plano de las escalas de Monotributo
	/// publicadas por ARCA / AFIP mediante el parser nativo ParserWebAfip.
	/// Si se detectan discrepancias con los valores almacenados en la base de datos,
	/// presenta un cuadro comparativo "antes y después" y solicita confirmación al usuario para actualizar.
	/// </summary>
	public static class VerificadorActualizacionesMonotributo
	{
		private static bool m_Verificando = false;
		private static DateTime m_UltimaVerificacion = DateTime.MinValue;
		private static bool m_UsuarioRechazoEnEstaSesion = false;

		/// <summary>
		/// Comprueba de manera asíncrona en segundo plano si existen actualizaciones de escalas en la web de AFIP.
		/// No bloquea la interfaz de usuario ni retrasa el inicio de la aplicación.
		/// </summary>
		/// <param name="controlContexto">Control o Formulario propietario para invocar el mensaje en el hilo de UI.</param>
		/// <param name="forzar">Si es true, ignora el tiempo mínimo entre chequeos y si el usuario ya había rechazado.</param>
		/// <param name="onActualizado">Callback opcional invocado en el hilo de UI si el usuario acepta la actualización.</param>
		public static void ComprobarEnSegundoPlano(Control controlContexto, bool forzar, Action onActualizado = null)
		{
			if (m_Verificando)
				return;

			if (!forzar)
			{
				if (m_UsuarioRechazoEnEstaSesion)
					return;

				// No saturar con peticiones web si ya verificó exitosamente en los últimos 15 minutos
				if (m_UltimaVerificacion != DateTime.MinValue && (DateTime.Now - m_UltimaVerificacion).TotalMinutes < 15)
					return;
			}

			m_Verificando = true;

			ThreadPool.QueueUserWorkItem(state =>
			{
				try
				{
					string tipoActividad = "servicios";
					if (Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null)
					{
						tipoActividad = Lfx.Workspace.Master.CurrentConfig.ReadGlobalSetting<string>("Sistema.Monotributo.TipoActividad", "servicios");
					}

					List<CategoriaMonotributo> nuevas;
					string infoMeta;
					string error;
					bool ok = EscalasMonotributo.Instancia.ConsultarEscalasDesdeWeb(tipoActividad, out nuevas, out infoMeta, out error);

					if (ok && nuevas != null && nuevas.Count >= 5)
					{
						m_UltimaVerificacion = DateTime.Now;

						// Obtenemos las categorías actuales de la base de datos
						var actuales = EscalasMonotributo.Instancia.Categorias;

						string reporteComparacion;
						int cantidadDiferencias;
						bool hayDiscrepancia = EscalasMonotributo.DetectarDiscrepancias(actuales, nuevas, out reporteComparacion, out cantidadDiferencias);

						if (hayDiscrepancia && cantidadDiferencias > 0)
						{
							if (controlContexto != null && !controlContexto.IsDisposed && controlContexto.IsHandleCreated)
							{
								controlContexto.BeginInvoke((MethodInvoker)delegate
								{
									if (controlContexto.IsDisposed)
										return;

									string cabeceraMeta = !string.IsNullOrEmpty(infoMeta) ? string.Format("\r\n({0})\r\n", infoMeta) : "\r\n";
									string mensaje = string.Format(
										"Se detectaron actualizaciones en las escalas oficiales de Monotributo publicadas por ARCA / AFIP.{0}\r\n" +
										"Comparación de topes de facturación anual (Antes ➔ Nuevo oficial AFIP):\r\n" +
										"─────────────────────────────────────────────────────────────\r\n" +
										"{1}" +
										"─────────────────────────────────────────────────────────────\r\n\r\n" +
										"¿Desea actualizar las categorías del sistema a estos nuevos valores oficiales?",
										cabeceraMeta,
										reporteComparacion);

									DialogResult res = MessageBox.Show(controlContexto, mensaje,
										"Actualización de Escalas de Monotributo (ARCA / AFIP)",
										MessageBoxButtons.YesNo,
										MessageBoxIcon.Question);

									if (res == DialogResult.Yes)
									{
										m_UsuarioRechazoEnEstaSesion = false;
										EscalasMonotributo.Instancia.Guardar(nuevas);
										EscalasMonotributo.UltimaFuenteMetadata = "ARCA Oficial (Web)";
										EscalasMonotributo.UltimaVigenciaMetadata = infoMeta;

										if (Lfx.Workspace.Master != null && Lfx.Workspace.Master.CurrentConfig != null && !string.IsNullOrEmpty(infoMeta))
										{
											Lfx.Workspace.Master.CurrentConfig.WriteGlobalSetting("Sistema.Monotributo.UltimaMetadata", infoMeta);
										}

										if (onActualizado != null)
										{
											try { onActualizado(); } catch { }
										}

										MessageBox.Show(controlContexto,
											"Las escalas de categorías de Monotributo se actualizaron exitosamente a los valores oficiales vigentes.",
											"Escalas Actualizadas",
											MessageBoxButtons.OK,
											MessageBoxIcon.Information);
									}
									else
									{
										m_UsuarioRechazoEnEstaSesion = true;
									}
								});
							}
						}
					}
				}
				catch
				{
					// Se capturan excepciones de red para no interrumpir la experiencia de uso
				}
				finally
				{
					m_Verificando = false;
				}
			});
		}
	}
}
