﻿using GridDominance.Shared.Resources;
using GridDominance.Shared.Screens.OverworldScreen.Entities;
using MonoSAMFramework.Portable.GameMath;
using MonoSAMFramework.Portable.GameMath.Geometry;
using MonoSAMFramework.Portable.Input;
using MonoSAMFramework.Portable.Screens;
using MonoSAMFramework.Portable.Screens.ViewportAdapters;
using MonoSAMFramework.Portable.UpdateAgents.Impl;

namespace GridDominance.Shared.Screens.OverworldScreen.Operations
{
	public class TransitionZoomInToTutorialOperation : FixTimeOperation<GDOverworldScreen>
	{
		private const float DURATION = 0.5f; // sec

		private SAMViewportAdapter vp;

		private FRectangle rectStart;
		private FRectangle rectFinal;

		private readonly OverworldNode_Tutorial _gdNode;

		public override string Name => "TransitionZoomInToTutorial";

		public TransitionZoomInToTutorialOperation(OverworldNode_Tutorial node) : base(DURATION)
		{
			_gdNode = node;
		}

		protected override void OnInit(GDOverworldScreen screen)
		{
			vp = screen.VAdapterGame;

			rectStart = screen.GuaranteedMapViewport;

			rectFinal = FRectangle.CreateByCenter(_gdNode.Position, new FSize(1.8f * GDConstants.TILE_WIDTH, 1.8f * GDConstants.TILE_WIDTH))
				.SetRatioUnderfitKeepCenter(GDConstants.VIEW_WIDTH * 1f / GDConstants.VIEW_HEIGHT);
		}

		protected override void OnStart(GDOverworldScreen screen)
		{
			_gdNode.AlphaOverride = 1;
		}

		protected override void OnProgress(GDOverworldScreen screen, float perc, SAMTime gameTime, InputState istate)
		{
			var bounds = FRectangle.Lerp(rectStart, rectFinal, FloatMath.FunctionEaseInSine(perc));

			vp.ChangeVirtualSize(bounds.Width, bounds.Height);
			screen.MapViewportCenterX = bounds.CenterX;
			screen.MapViewportCenterY = bounds.CenterY;

			_gdNode.AlphaOverride = 1 - FloatMath.FunctionEaseOutExpo(perc, 10);
		}

		protected override void OnEnd(GDOverworldScreen screen)
		{
			_gdNode.AlphaOverride = 0;

			MainGame.Inst.SetTutorialLevelScreen();
		}

		protected override void OnAbort(GDOverworldScreen owner)
		{
			OnEnd(owner);
		}
	}
}
