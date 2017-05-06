﻿using MonoSAMFramework.Portable.GameMath;
using MonoSAMFramework.Portable.GameMath.Geometry;
using MonoSAMFramework.Portable.Screens.Agents;
using MonoSAMFramework.Portable.Screens.ViewportAdapters;

namespace GridDominance.Shared.Screens.WorldMapScreen.Agents
{
	public class LeaveTransitionAgent : DecayGameScreenAgent
	{
		private const float DURATION = 1.0f; // sec

		private readonly TolerantBoxingViewportAdapter vp;

		private readonly FRectangle rectStart;
		private readonly FRectangle rectFinal;

		private readonly GDWorldMapScreen _gdScreen;

		public LeaveTransitionAgent(GDWorldMapScreen scrn) : base(scrn, DURATION)
		{
			_gdScreen = scrn;
			vp = (TolerantBoxingViewportAdapter) scrn.VAdapterGame;

			rectStart = scrn.GuaranteedMapViewport;

			rectFinal = scrn.Graph.BoundingViewport;
		}

		protected override void Run(float perc)
		{
			var bounds = FRectangle.Lerp(rectStart, rectFinal, FloatMath.FunctionEaseOutSine(perc));

			vp.ChangeVirtualSize(bounds.Width, bounds.Height);
			Screen.MapViewportCenterX = bounds.CenterX;
			Screen.MapViewportCenterY = bounds.CenterY;

			_gdScreen.ColorOverdraw = FloatMath.FunctionEaseOutExpo(perc, 10);
		}

		protected override void Start()
		{
			var bounds = rectStart;

			vp.ChangeVirtualSize(bounds.Width, bounds.Height);
			Screen.MapViewportCenterX = bounds.CenterX;
			Screen.MapViewportCenterY = bounds.CenterY;

			_gdScreen.ZoomState = BistateProgress.Expanding;

			_gdScreen.ColorOverdraw = 0f;
		}

		protected override void End()
		{
			_gdScreen.ZoomState = BistateProgress.Expanded;

			_gdScreen.ColorOverdraw = 1f;

			MainGame.Inst.SetOverworldScreenWithTransition(_gdScreen.GraphBlueprint);
		}
	}
}