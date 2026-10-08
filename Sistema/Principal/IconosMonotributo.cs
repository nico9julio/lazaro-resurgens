using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Lazaro.WinMain.Principal
{
	/// <summary>
	/// Proveedor de mini-íconos gráficos (Bitmaps ARGB de 14x14 px) generados en memoria
	/// para el módulo de Monotributo. Garantiza compatibilidad universal y nitidez
	/// en cualquier versión de Windows y GDI+, eliminando problemas de fuentes o emojis rotos.
	/// </summary>
	public static class IconosMonotributo
	{
		private static Bitmap s_Afip;
		private static Bitmap s_Baja;
		private static Bitmap s_Sube;
		private static Bitmap s_Escudo;
		private static Bitmap s_Moneda;
		private static Bitmap s_Alerta;
		private static Bitmap s_Bombilla;
		private static Bitmap s_Proyeccion;

		public static Bitmap Afip => s_Afip ?? (s_Afip = CrearIconoAfip(14, 14));
		public static Bitmap Baja => s_Baja ?? (s_Baja = CrearIconoBaja(14, 14));
		public static Bitmap Sube => s_Sube ?? (s_Sube = CrearIconoSube(14, 14));
		public static Bitmap Escudo => s_Escudo ?? (s_Escudo = CrearIconoEscudo(14, 14));
		public static Bitmap Moneda => s_Moneda ?? (s_Moneda = CrearIconoMoneda(14, 14));
		public static Bitmap Alerta => s_Alerta ?? (s_Alerta = CrearIconoAlerta(14, 14));
		public static Bitmap Bombilla => s_Bombilla ?? (s_Bombilla = CrearIconoBombilla(14, 14));
		public static Bitmap Proyeccion => s_Proyeccion ?? (s_Proyeccion = CrearIconoProyeccion(14, 14));

		private static Bitmap CrearBitmapBase(int w, int h, out Graphics g)
		{
			Bitmap bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			g = Graphics.FromImage(bmp);
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.PixelOffsetMode = PixelOffsetMode.Half;
			g.InterpolationMode = InterpolationMode.HighQualityBicubic;
			g.Clear(Color.Transparent);
			return bmp;
		}

		private static Bitmap CrearIconoAfip(int w, int h)
		{
			Graphics g;
			Bitmap bmp = CrearBitmapBase(w, h, out g);
			using (g)
			{
				// Pórtico institucional clásico (templo / columnas de AFIP)
				Point[] fronton = new Point[] {
					new Point(1, 4),
					new Point(7, 1),
					new Point(12, 4)
				};
				using (SolidBrush bAzul = new SolidBrush(Color.FromArgb(30, 64, 175)))
				using (Pen pBorde = new Pen(Color.FromArgb(96, 165, 250), 1f))
				{
					g.FillPolygon(bAzul, fronton);
					g.DrawPolygon(pBorde, fronton);

					// Viga de soporte
					g.FillRectangle(bAzul, 1, 4, 12, 2);

					// 3 Columnas
					using (SolidBrush bCol = new SolidBrush(Color.FromArgb(59, 130, 246)))
					{
						g.FillRectangle(bCol, 2, 6, 2, 5);
						g.FillRectangle(bCol, 6, 6, 2, 5);
						g.FillRectangle(bCol, 10, 6, 2, 5);
					}

					// Base de apoyo
					g.FillRectangle(bAzul, 0, 11, 14, 2);
				}
			}
			return bmp;
		}

		private static Bitmap CrearIconoBaja(int w, int h)
		{
			Graphics g;
			Bitmap bmp = CrearBitmapBase(w, h, out g);
			using (g)
			{
				// Círculo verde esmeralda con flecha blanca hacia abajo
				using (SolidBrush bFondo = new SolidBrush(Color.FromArgb(16, 185, 129)))
				using (Pen pBorde = new Pen(Color.FromArgb(5, 150, 105), 1f))
				{
					g.FillEllipse(bFondo, 0, 0, 13, 13);
					g.DrawEllipse(pBorde, 0, 0, 13, 13);
				}

				using (SolidBrush bBlanco = new SolidBrush(Color.White))
				{
					// Vástago de la flecha
					g.FillRectangle(bBlanco, 5, 2, 3, 4);

					// Punta de flecha hacia abajo
					Point[] punta = new Point[] {
						new Point(2, 6),
						new Point(11, 6),
						new Point(6, 11)
					};
					g.FillPolygon(bBlanco, punta);
				}
			}
			return bmp;
		}

		private static Bitmap CrearIconoSube(int w, int h)
		{
			Graphics g;
			Bitmap bmp = CrearBitmapBase(w, h, out g);
			using (g)
			{
				// Círculo naranja ámbar con flecha blanca hacia arriba
				using (SolidBrush bFondo = new SolidBrush(Color.FromArgb(249, 115, 22)))
				using (Pen pBorde = new Pen(Color.FromArgb(217, 85, 8), 1f))
				{
					g.FillEllipse(bFondo, 0, 0, 13, 13);
					g.DrawEllipse(pBorde, 0, 0, 13, 13);
				}

				using (SolidBrush bBlanco = new SolidBrush(Color.White))
				{
					// Punta de flecha hacia arriba
					Point[] punta = new Point[] {
						new Point(6, 2),
						new Point(2, 7),
						new Point(11, 7)
					};
					g.FillPolygon(bBlanco, punta);

					// Vástago de la flecha
					g.FillRectangle(bBlanco, 5, 7, 3, 4);
				}
			}
			return bmp;
		}

		private static Bitmap CrearIconoEscudo(int w, int h)
		{
			Graphics g;
			Bitmap bmp = CrearBitmapBase(w, h, out g);
			using (g)
			{
				// Escudo de control / protección azul
				Point[] escudo = new Point[] {
					new Point(1, 2),
					new Point(12, 2),
					new Point(12, 7),
					new Point(6, 13),
					new Point(1, 7)
				};
				using (SolidBrush bAzul = new SolidBrush(Color.FromArgb(37, 99, 235)))
				using (Pen pBorde = new Pen(Color.FromArgb(30, 58, 138), 1f))
				{
					g.FillPolygon(bAzul, escudo);
					g.DrawPolygon(pBorde, escudo);
				}

				// Brillo / detalle interno
				using (Pen pBrillo = new Pen(Color.FromArgb(147, 197, 253), 1f))
				{
					g.DrawLine(pBrillo, 3, 4, 6, 4);
					g.DrawLine(pBrillo, 3, 4, 3, 7);
				}
			}
			return bmp;
		}

		private static Bitmap CrearIconoMoneda(int w, int h)
		{
			Graphics g;
			Bitmap bmp = CrearBitmapBase(w, h, out g);
			using (g)
			{
				// Moneda dorada con signo $
				using (SolidBrush bOro = new SolidBrush(Color.FromArgb(245, 158, 11)))
				using (Pen pBorde = new Pen(Color.FromArgb(180, 83, 9), 1f))
				{
					g.FillEllipse(bOro, 0, 0, 13, 13);
					g.DrawEllipse(pBorde, 0, 0, 13, 13);
				}

				// Signo $ centrado
				using (Font f = new Font("Segoe UI", 7F, FontStyle.Bold))
				using (SolidBrush bTxt = new SolidBrush(Color.White))
				using (StringFormat sf = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
				{
					g.DrawString("$", f, bTxt, new RectangleF(0, 0, 13, 13), sf);
				}
			}
			return bmp;
		}

		private static Bitmap CrearIconoBombilla(int w, int h)
		{
			Graphics g;
			Bitmap bmp = CrearBitmapBase(w, h, out g);
			using (g)
			{
				// Bombilla dorada
				using (SolidBrush bLuz = new SolidBrush(Color.FromArgb(250, 204, 21)))
				using (Pen pLuz = new Pen(Color.FromArgb(217, 119, 6), 1f))
				{
					g.FillEllipse(bLuz, 2, 0, 9, 9);
					g.DrawEllipse(pLuz, 2, 0, 9, 9);
				}

				// Casquillo metálico gris
				using (SolidBrush bMetal = new SolidBrush(Color.FromArgb(156, 163, 175)))
				{
					g.FillRectangle(bMetal, 4, 8, 5, 3);
				}
				using (SolidBrush bBase = new SolidBrush(Color.FromArgb(75, 85, 99)))
				{
					g.FillRectangle(bBase, 5, 11, 3, 2);
				}
			}
			return bmp;
		}

		private static Bitmap CrearIconoAlerta(int w, int h)
		{
			Graphics g;
			Bitmap bmp = CrearBitmapBase(w, h, out g);
			using (g)
			{
				Point[] tri = new Point[] {
					new Point(6, 0),
					new Point(13, 13),
					new Point(0, 13)
				};
				using (SolidBrush bRojo = new SolidBrush(Color.FromArgb(239, 68, 68)))
				using (Pen pBorde = new Pen(Color.FromArgb(185, 28, 28), 1f))
				{
					g.FillPolygon(bRojo, tri);
					g.DrawPolygon(pBorde, tri);
				}

				using (Font f = new Font("Segoe UI", 7F, FontStyle.Bold))
				using (SolidBrush bBlanco = new SolidBrush(Color.White))
				using (StringFormat sf = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
				{
					g.DrawString("!", f, bBlanco, new RectangleF(0, 2, 13, 11), sf);
				}
			}
			return bmp;
		}

		private static Bitmap CrearIconoProyeccion(int w, int h)
		{
			Graphics g;
			Bitmap bmp = CrearBitmapBase(w, h, out g);
			using (g)
			{
				using (SolidBrush bAzul = new SolidBrush(Color.FromArgb(37, 99, 235)))
				using (Pen pBorde = new Pen(Color.FromArgb(30, 64, 175), 1f))
				{
					g.FillEllipse(bAzul, 0, 0, 13, 13);
					g.DrawEllipse(pBorde, 0, 0, 13, 13);
				}

				using (SolidBrush bPunto = new SolidBrush(Color.White))
				{
					g.FillEllipse(bPunto, 4, 4, 5, 5);
				}
			}
			return bmp;
		}
	}
}
