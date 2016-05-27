﻿using GridDominance.Shared.Screens.ScreenGame.hud;
using MonoSAMFramework.Portable.Screens.HUD;

namespace GridDominance.Shared.Screens.ScreenGame.HUD
{
	static class GDHudExtension
	{
		public static GDGameHUD GDHUD(this HUDElement e) => (GDGameHUD) e.HUD;
	}
}
