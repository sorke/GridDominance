﻿using GridDominance.Shared.Resources;
using MonoSAMFramework.Portable.BatchRenderer;
using MonoSAMFramework.Portable.ColorHelper;
using MonoSAMFramework.Portable.GameMath.Geometry;
using MonoSAMFramework.Portable.Input;
using MonoSAMFramework.Portable.Screens;
using MonoSAMFramework.Portable.Screens.Background;

namespace GridDominance.Shared.Screens.LevelEditorScreen
{
	class LevelEditorBackground : GameBackground
	{
		private const int TILE_WIDTH = GDConstants.TILE_WIDTH;

		public LevelEditorScreen GDOwner => (LevelEditorScreen)Owner;

		public LevelEditorBackground(GameScreen scrn) : base(scrn)
		{

		}

		public override void Draw(IBatchRenderer sbatch)
		{
			var gridArea = FRectangle.CreateByTopLeft(
				0, 
				0, 
				GDOwner.LevelData.Width * TILE_WIDTH, 
				GDOwner.LevelData.Height * TILE_WIDTH);

			sbatch.DrawStretched(Textures.TexPixel, GDOwner.CompleteMapViewport, FlatColors.MidnightBlue);

			sbatch.DrawStretched(Textures.TexPixel, gridArea, FlatColors.Background);
			
			for (int x = 0; x < GDOwner.LevelData.Width+1; x++)
			{
				sbatch.DrawCentered(Textures.TexPixel, new FPoint(gridArea.X + x * TILE_WIDTH, gridArea.CenterY), 2 * Owner.PixelWidth, gridArea.Height, FlatColors.BackgroundHighlight);
			}
			for (int y = 0; y < GDOwner.LevelData.Height+1; y++)
			{
				sbatch.DrawCentered(Textures.TexPixel, new FPoint(gridArea.CenterX, gridArea.Y + y * TILE_WIDTH), gridArea.Width, 2 * Owner.PixelWidth, FlatColors.BackgroundHighlight);
			}

		}

		public override void Update(SAMTime gameTime, InputState istate)
		{
			//
		}
	}
}
