﻿using GridDominance.Shared.Framework;
using GridDominance.Shared.Screens.GameScreen.Entities;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace GridDominance.Shared.Screens.GameScreen.FractionController
{
	abstract class AbstractFractionController
	{
		public const float COMPUTER_UPDATE_TIME = 1.666f;

		protected readonly GameScreen Owner;

		private readonly float updateInterval;
		private float timeSinceLastUpdate = 0;

		protected readonly Cannon Cannon;
		protected readonly Fraction Fraction;

		protected AbstractFractionController(float interval, GameScreen owner, Cannon cannon, Fraction fraction)
		{
			updateInterval = interval;
			Cannon = cannon;
			Fraction = fraction;
			Owner = owner;
		}

		public void Update(GameTime gameTime, InputState istate)
		{
			timeSinceLastUpdate -= gameTime.GetElapsedSeconds();
			if (timeSinceLastUpdate <= 0)
			{
				timeSinceLastUpdate = updateInterval;

				Calculate(istate);
			}
		}

		protected abstract void Calculate(InputState istate);
	}
}
