﻿using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoSAMFramework.Portable.Input;
using MonoSAMFramework.Portable.MathHelper;
using MonoSAMFramework.Portable.Screens.HUD;

namespace GridDominance.Shared.Screens.ScreenGame.HUD
{
	public class HUDIncrementIndicatorLabel : HUDLabel
	{
		private readonly HUDRawText incrementLabel;

		private const float WIGGLE_SPEED = 2f;
		private const float WIGGLE_OFFSET = 2.5f;

		private float currentWiggleProgress = 0;

		public HUDIncrementIndicatorLabel(string value, string increment, int depth = 0)
			: base(depth)
		{
			Text = value;

			incrementLabel = new HUDRawText
			{
				Text = increment,
			};
		}
		
		public override void OnInitialize()
		{
			base.OnInitialize();

			incrementLabel.Alignment = HUDAlignment.ABSOLUTE;
			incrementLabel.FontSize = FontSize / 2;
			incrementLabel.Font = Font;
			incrementLabel.TextColor = TextColor;

			AddElement(incrementLabel);
		}

		protected override void DoUpdate(GameTime gameTime, InputState istate)
		{
			currentWiggleProgress += gameTime.GetElapsedSeconds() * WIGGLE_SPEED;

			var offset = WIGGLE_OFFSET * FloatMath.Sin(currentWiggleProgress * FloatMath.PI);
			var innerRel = internalText.Position + new Point(InnerLabelSize.Width + 5, (int)(offset - incrementLabel.Height / 4));

			incrementLabel.RelativePosition = innerRel;
		}
	}
}
