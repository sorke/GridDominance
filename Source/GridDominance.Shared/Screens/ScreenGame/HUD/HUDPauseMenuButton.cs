﻿using System;
using GridDominance.Shared.Resources;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.TextureAtlases;
using MonoSAMFramework.Portable.ColorHelper;
using MonoSAMFramework.Portable.Extensions;
using MonoSAMFramework.Portable.Input;
using MonoSAMFramework.Portable.MathHelper;

namespace GridDominance.Shared.Screens.ScreenGame.HUD
{
	class HUDPauseMenuButton : GDGameHUDElement
	{
		public const int MARKER_WIDTH = 33;
		public const int MARKER_HEIGHT = 17;

		private const int WIDTH = 222;
		private const int HEIGHT = HUDPauseButton.DIAMETER;
		private const int GAP = 10;

		private const float CLOSING_DELAY = 0.2f;

		private static readonly Vector2 RELATIVE_SPAWNPOSITION = new Vector2(WIDTH/2 - HUDPauseButton.DIAMETER/2, 64);

		private readonly HUDPauseButton baseButton;
		private readonly int btnIndex;
		private readonly int btnCount;
		private readonly int btnDepth;
		private readonly string btnText;
		private readonly Action btnAction;

		private float openingProgress = 0f;
		public bool IsOpening = true;
		public bool IsClosing = false;

		public override int Depth => btnDepth;

		public HUDPauseMenuButton(HUDPauseButton owner, string buttonText, int buttonDepth, int buttonIndex, int totalButtonCount, Action buttonAction)
		{
			baseButton = owner;
			btnIndex = buttonIndex;
			btnCount = totalButtonCount;
			btnDepth = buttonDepth;
			btnText = buttonText;
			btnAction = buttonAction;

			RelativePosition = new Point(12, 12);
			Size = new Size(0, 0);
			Alignment = owner.Alignment;
		}

		public override void OnInitialize()
		{

		}

		public override void OnRemove()
		{
			// NOP
		}

		protected override void DoUpdate(GameTime gameTime, InputState istate)
		{
			if (IsOpening && FloatMath.IsNotOne(openingProgress))
			{
				bool hasOpened;
				openingProgress = FloatMath.LimitedInc(openingProgress, gameTime.GetElapsedSeconds() * HUDPauseButton.ANIMATION_SPEED, 1f, out hasOpened);
				if (hasOpened)
				{
					IsOpening = false;
				}

				UpdateOpeningPosition();
			}
			else if (IsClosing)
			{
				bool hasClosed;
				openingProgress = FloatMath.LimitedDec(openingProgress, gameTime.GetElapsedSeconds() * HUDPauseButton.ANIMATION_SPEED, 0f, out hasClosed);
				if (hasClosed)
				{
					Remove();
				}

				UpdateClosingPosition();
			}
		}

		private void UpdateOpeningPosition()
		{
			if (openingProgress < 0.5f)
			{
				var stepProgress = openingProgress / 0.5f;

				RelativeCenter = baseButton.RelativeCenter + RELATIVE_SPAWNPOSITION * stepProgress;
				Size = new Size((int) (WIDTH * stepProgress), (int) (HEIGHT * stepProgress));
			}
			else if (openingProgress < 0.55f)
			{
				RelativeCenter = baseButton.RelativeCenter + RELATIVE_SPAWNPOSITION;
				Size = new Size(WIDTH, HEIGHT);
			}
			else if (openingProgress < 1f)
			{
				var stepProgress = (openingProgress - 0.55f) / 0.45f;

				var posX = baseButton.RelativeCenter.X + RELATIVE_SPAWNPOSITION.X;
				var posY = baseButton.RelativeCenter.Y + RELATIVE_SPAWNPOSITION.Y + FloatMath.Min(stepProgress * (btnCount - 1) * (HEIGHT + GAP), btnIndex * (HEIGHT + GAP));

				RelativeCenter = new Vector2(posX, posY);

				Size = new Size(WIDTH, HEIGHT);
			}
			else
			{
				RelativeCenter = baseButton.RelativeCenter + RELATIVE_SPAWNPOSITION + new Vector2(0, btnIndex * (HEIGHT + GAP));
				Size = new Size(WIDTH, HEIGHT);
			}
		}

		private void UpdateClosingPosition()
		{
			var prog = 1 - openingProgress;
			prog -= (btnCount-btnIndex-1) * CLOSING_DELAY;
			if (prog <= 0) prog = 0;
			prog /= 1 - ((btnCount - 1) * CLOSING_DELAY);

			RelativeCenter = baseButton.RelativeCenter + RELATIVE_SPAWNPOSITION + new Vector2(-prog * WIDTH*2, btnIndex * (HEIGHT + GAP));
		}

		protected override void DoDraw(SpriteBatch sbatch, Rectangle bounds)
		{
			var scale = Size.Width * 1f / WIDTH;

			sbatch.Draw(
				Textures.TexHUDButtonPauseMenuBackground.Texture,
				Center,
				Textures.TexHUDButtonPauseMenuBackground.Bounds,
				Color.White,
				0f,
				Textures.TexHUDButtonPauseMenuBackground.Center(),
				scale * Textures.DEFAULT_TEXTURE_SCALE,
				SpriteEffects.None,
				0);

			if (btnIndex == 0)
			{
				sbatch.Draw(
					Textures.TexHUDButtonPauseMenuMarkerBackground.Texture,
					Center + new Vector2(WIDTH/2f - HUDPauseButton.DIAMETER/2f, -(HEIGHT/2f + MARKER_HEIGHT/2f)) * scale,
					Textures.TexHUDButtonPauseMenuMarkerBackground.Bounds,
					Color.White,
					0f,
					Textures.TexHUDButtonPauseMenuMarkerBackground.Center(),
					scale * Textures.DEFAULT_TEXTURE_SCALE,
					SpriteEffects.None,
					0);

				sbatch.Draw(
					Textures.TexHUDButtonPauseMenuMarker.Texture,
					Center + new Vector2(WIDTH / 2f - HUDPauseButton.DIAMETER / 2f, -(HEIGHT / 2f + MARKER_HEIGHT / 2f)) * scale,
					Textures.TexHUDButtonPauseMenuMarker.Bounds,
					IsPressed ? FlatColors.Concrete : FlatColors.Silver,
					0f,
					Textures.TexHUDButtonPauseMenuMarker.Center(),
					scale * Textures.DEFAULT_TEXTURE_SCALE,
					SpriteEffects.None,
					0);
			}

			sbatch.Draw(Textures.TexPixel, bounds, IsPressed ? FlatColors.Concrete : FlatColors.Silver);

			var fontBounds = Textures.HUDFont.MeasureString(btnText);
			sbatch.DrawString(
				Textures.HUDFont, 
				btnText, 
				Center + new Vector2(-WIDTH/2f + 12 + fontBounds.X/2f, 0f)*scale, 
				FlatColors.Foreground, 
				0f,
				fontBounds/2f,
				scale, 
				SpriteEffects.None, 0f);
		}

		protected override void OnPointerClick(Point relPositionPoint, InputState istate)
		{
			if (IsOpening) return;

			btnAction();
		}
	}
}
