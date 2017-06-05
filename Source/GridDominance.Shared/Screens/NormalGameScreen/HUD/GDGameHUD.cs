﻿using GridDominance.Levelfileformat.Blueprint;
using GridDominance.Shared.Resources;
using GridDominance.Shared.SaveData;
using GridDominance.Shared.Screens.NormalGameScreen.Fractions;
using GridDominance.Shared.Screens.ScreenGame;
using MonoSAMFramework.Portable.DebugTools;
using MonoSAMFramework.Portable.Input;
using MonoSAMFramework.Portable.Screens;
using MonoSAMFramework.Portable.Screens.HUD;

namespace GridDominance.Shared.Screens.NormalGameScreen.HUD
{
	public class GDGameHUD : GameHUD
	{
		public GDGameScreen GDOwner => (GDGameScreen)Screen;
		
		public readonly HUDPauseButton BtnPause;
		public readonly HUDSpeedBaseButton BtnSpeed;

		public GDGameHUD(GDGameScreen scrn) : base(scrn, Textures.HUDFontRegular)
		{
			AddElement(BtnPause = new HUDPauseButton());
			AddElement(BtnSpeed = new HUDSpeedBaseButton());
		}
		
#if DEBUG
		protected override void OnUpdate(SAMTime gameTime, InputState istate)
		{
			root.IsVisible = !DebugSettings.Get("HideHUD");
		}
#endif
	}
}
