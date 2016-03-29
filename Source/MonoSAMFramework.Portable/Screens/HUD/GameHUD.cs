﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoSAMFramework.Portable.External;
using MonoSAMFramework.Portable.Input;

namespace MonoSAMFramework.Portable.Screens.HUD
{
	public abstract class GameHUD : ISAMDrawable, ISAMUpdateable
	{
		protected readonly Screen Owner;

		public GameHUD(Screen scrn)
		{
			Owner = scrn;
		}
		
		public abstract void Update(GameTime gameTime, InputState istate);
		public abstract void Draw(SpriteBatch sbatch);
	}
}