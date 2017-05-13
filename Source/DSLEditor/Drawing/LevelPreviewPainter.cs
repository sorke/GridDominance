using GridDominance.Levelfileformat.Blueprint;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace GridDominance.DSLEditor.Drawing
{
	public class LevelPreviewPainter
	{
		private static readonly Color[] CANNON_COLORS = { Color.LightGray, Color.Green, Color.Red, Color.Blue, Color.Yellow, Color.Cyan, Color.Orange, Color.Pink };
		private static readonly Brush[] COLORS_KITYPE = { Brushes.BlueViolet, Brushes.Brown, Brushes.Olive };

		public readonly Bitmap GraphicsBuffer = new Bitmap(1024, 640);

		public Bitmap DrawOverview(LevelBlueprint level)
		{
			Bitmap img;

			try
			{
				img = Draw(level, -1);
			}
			catch (Exception)
			{
				img = Draw(null, -1);
			}

			Bitmap r = new Bitmap(img.Width + 16, img.Height + 48 + 48 + 16);
			using (Graphics g = Graphics.FromImage(r))
			{
				g.Clear(Color.White);
				g.DrawImageUnscaled(img, 0, 16 + 48);
				g.DrawString(level.Name + ": " + level.FullName, new Font("Calibri", 28, FontStyle.Bold), Brushes.DarkRed, 24, 16 + 8);
				g.DrawString(level.UniqueID.ToString("B"), new Font("Courier New", 28, FontStyle.Bold), Brushes.DarkRed, 24, 16 + 8 + img.Height + 48);


				var kitRect = new RectangleF(img.Width - 16, 48, 32, 32);
				g.FillEllipse(COLORS_KITYPE[level.KIType - 10], kitRect);
				g.DrawString((level.KIType - 10).ToString(), new Font("Courier New", 24, FontStyle.Bold), Brushes.Black, kitRect, new StringFormat {Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center});
			}
			return r;
		}

		public Bitmap DrawOverviewError(string name)
		{
			Bitmap img = Draw(null, -1);

			Bitmap r = new Bitmap(img.Width, img.Height + 48 + 48);
			using (Graphics g = Graphics.FromImage(r))
			{
				g.Clear(Color.White);
				g.DrawImageUnscaled(img, 0, 48);
				g.DrawString(name, new Font("Calibri", 28, FontStyle.Bold), Brushes.DarkRed, 24, 8);
				g.DrawString("{????????-????-????-????-????????????}", new Font("Courier New", 28, FontStyle.Bold), Brushes.DarkRed, 24, 8 + img.Height + 48);
			}
			return r;
		}

		public Bitmap Draw(LevelBlueprint level, int highlightCannon)
		{
			using (Graphics g = Graphics.FromImage(GraphicsBuffer))
			{
				if (level == null)
				{
					g.SmoothingMode = SmoothingMode.AntiAlias;
					g.Clear(Color.OrangeRed);

					g.DrawLine(new Pen(Color.DarkRed, 32), 0, 0, 1024, 640);
					g.DrawLine(new Pen(Color.DarkRed, 32), 1024, 0, 0, 640);
				}
				else
				{
					g.SmoothingMode = SmoothingMode.AntiAlias;
					g.Clear(Color.Black);

					for (int x = 0; x < 16; x++)
					{
						g.DrawLine((x % 2 == 0) ? Pens.DarkGray : Pens.DimGray, x * 64, 0, x * 64, 640);
					}
					for (int y = 0; y < 10; y++)
					{
						g.DrawLine((y % 2 == 0) ? Pens.DarkGray : new Pen(Color.FromArgb(88, 88, 88)), 0, y * 64, 1024, y * 64);
					}


					foreach (var c in level.BlueprintCannons)
					{
						var rectBaseCircle  = new RectangleF(-0.500f, -0.500f, 1.000f, 1.000f);
						var rectOuterCircle = new RectangleF(-0.833f, -0.833f, 1.666f, 1.666f);
						var rectMidArea     = new RectangleF(-0.666f, -0.666f, 1.333f, 1.333f);
						var rectBarrel      = new RectangleF(+0.166f, -0.166f, 0.666f, 0.333f);

						var save = g.Save();
						{
							g.TranslateTransform(c.X, c.Y);
							g.ScaleTransform(c.Diameter, c.Diameter);

							// Mid Area Alpha
							g.FillRectangle(new SolidBrush(Color.FromArgb(64, CANNON_COLORS[c.Player])), rectMidArea);
							
							// Barrel
							g.RotateTransform(c.Rotation);
							g.FillRectangle(new SolidBrush(CANNON_COLORS[c.Player]), rectBarrel);

							// Base
							g.FillEllipse(new SolidBrush(CANNON_COLORS[c.Player]), rectBaseCircle);
							g.DrawEllipse(new Pen(Color.Black, 0.008f), rectBaseCircle);

							// Radius
							g.DrawEllipse(new Pen(CANNON_COLORS[c.Player], 0.032f), rectOuterCircle);
							
						}
						g.Restore(save);
					}
					
					var rayPenBG = new Pen(Color.FromArgb(100, Color.Red), 4f);
					var rayPen   = new Pen(Color.FromArgb(200, Color.Red), 4f) {DashStyle = DashStyle.Dash};
					var rayBrush = new SolidBrush(Color.FromArgb(200, Color.Goldenrod));
					foreach (var c in level.BlueprintCannons.Where(p => p.CannonID == highlightCannon))
					{
						foreach (var path in c.PrecalculatedPaths)
						{
							if (path.PreviewBulletPath != null)
							{
								float cx = c.X;
								float cy = c.Y;
								foreach (var pos in path.PreviewBulletPath)
								{
									g.DrawLine(rayPenBG, cx, cy, pos.X, pos.Y);
									cx = pos.X;
									cy = pos.Y;
								}
							}

							{
								float cx = c.X;
								float cy = c.Y;
								if (path.Rays.Length > 1) g.FillEllipse(rayBrush, cx - 4, cy - 4, 8, 8);
							
								foreach (var ray in path.Rays)
								{
									g.DrawLine(rayPen, cx, cy, ray.Item1, ray.Item2);
							
									if (path.Rays.Length > 1) g.FillEllipse(rayBrush, ray.Item1 - 4, ray.Item2 - 4, 8, 8);
							
									cx = ray.Item1;
									cy = ray.Item2;
								}
							}
						}
					}

					var voidpen = new Pen(Color.FloralWhite, 8);
					foreach (var vwall in level.BlueprintVoidWalls)
					{
						var save = g.Save();
						g.TranslateTransform(vwall.X, vwall.Y);
						g.RotateTransform(vwall.Rotation);
						g.DrawLine(voidpen, -vwall.Length / 2, 0, +vwall.Length / 2, 0);
						g.Restore(save);
					}

					foreach (var vcirc in level.BlueprintVoidCircles)
					{
						var save = g.Save();
						{
							g.TranslateTransform(vcirc.X, vcirc.Y);
							g.DrawEllipse(voidpen, new RectangleF(-vcirc.Diameter / 2f, -vcirc.Diameter / 2f, vcirc.Diameter, vcirc.Diameter));
						}
						g.Restore(save);
					}

					var glassbrush = new SolidBrush(Color.FromArgb(128, Color.Aqua));
					var glasspen = new Pen(Color.White, 4);
					foreach (var vblock in level.BlueprintGlassBlocks)
					{
						var save = g.Save();
						{
							g.TranslateTransform(vblock.X, vblock.Y);
							g.FillRectangle(glassbrush, -vblock.Width / 2, -vblock.Height / 2, vblock.Width, vblock.Height);
							g.DrawRectangle(glasspen, -vblock.Width / 2, -vblock.Height / 2, vblock.Width, vblock.Height);
						}
						g.Restore(save);
					}

					var penBH1 = new Pen(Color.White, 2);
					var brushBH1 = Brushes.Black;
					var penBH2 = new Pen(Color.Black, 2);
					var brushBH2 = Brushes.White;
					foreach (var vhole in level.BlueprintBlackHoles)
					{
						var save = g.Save();
						{
							g.TranslateTransform(vhole.X, vhole.Y);
							if (vhole.Power < 0)
							{
								g.FillEllipse(brushBH1, new RectangleF(-vhole.Diameter / 2f, -vhole.Diameter / 2f, vhole.Diameter, vhole.Diameter));
								for (int i = 1; i <= 6; i++)
								{
									var sz = i / 6f;
									g.DrawEllipse(penBH1, new RectangleF(sz * -vhole.Diameter / 2f, sz * -vhole.Diameter / 2f, sz * vhole.Diameter, sz * vhole.Diameter));
								}
							}
							else
							{
								g.FillEllipse(brushBH2, new RectangleF(-vhole.Diameter / 2f, -vhole.Diameter / 2f, vhole.Diameter, vhole.Diameter));
								for (int i = 1; i <= 6; i++)
								{
									var sz = i / 6f;
									g.DrawEllipse(penBH2, new RectangleF(sz * -vhole.Diameter / 2f, sz * -vhole.Diameter / 2f, sz * vhole.Diameter, sz * vhole.Diameter));
								}
							}
						}
						g.Restore(save);
					}
				}
			}

			return GraphicsBuffer;
		}
	}
}
