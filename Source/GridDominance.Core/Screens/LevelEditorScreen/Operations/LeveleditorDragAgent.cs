﻿using System.Linq;
using GridDominance.Shared.Resources;
using GridDominance.Shared.Screens.LevelEditorScreen.Entities;
using Microsoft.Xna.Framework;
using MonoSAMFramework.Portable.GameMath;
using MonoSAMFramework.Portable.GameMath.Geometry;
using MonoSAMFramework.Portable.Input;
using MonoSAMFramework.Portable.Screens;
using MonoSAMFramework.Portable.UpdateAgents;

namespace GridDominance.Shared.Screens.LevelEditorScreen.Operations
{
	public class LeveleditorDragAgent : SAMUpdateOp<LevelEditorScreen>
	{
		private const float RETURN_SPEED = 48 * GDConstants.TILE_WIDTH;

		private enum DMode { Nothing, MapDrag, CannonMove, ObstacleMove, WallMove, WallDragP1, WallDragP2, PortalMove }

		private DMode _dragMode = DMode.Nothing;

		private FPoint _mouseStartPos;
		private FPoint _startOffset;
		private Vector2 _dragOffCenter;
		private Vector2 _wallRelativeP2;

		private LevelEditorScreen _gdScreen;

		private FRectangle _boundsMap;
		private FRectangle _boundsWorkingAreaInner;
		private FRectangle _boundsWorkingAreaOuter;
		private DVector _oobForce;

		public override string Name => "LeveleditorDragAgent";

		public LeveleditorDragAgent()
		{
			//
		}

		protected override void OnInit(LevelEditorScreen screen)
		{
			_gdScreen = screen;
		}

		protected override void OnUpdate(LevelEditorScreen screen, SAMTime gameTime, InputState istate)
		{
			const float raster = (GDConstants.TILE_WIDTH / 2f);
			var rx = raster * FloatMath.Round((istate.GamePointerPositionOnMap.X - _dragOffCenter.X) / raster);
			var ry = raster * FloatMath.Round((istate.GamePointerPositionOnMap.Y - _dragOffCenter.Y) / raster);

			_boundsWorkingAreaOuter = _gdScreen.VAdapterGame.VirtualTotalBoundingBox.AsDeflated(
				0, 
				4 * GDConstants.TILE_WIDTH, 
				_gdScreen.GDHUD.AttrPanel.IsVisible ? 4 * GDConstants.TILE_WIDTH : 0, 
				0);
			_boundsWorkingAreaInner = _boundsWorkingAreaOuter.AsDeflated(GDConstants.TILE_WIDTH, GDConstants.TILE_WIDTH, GDConstants.TILE_WIDTH, GDConstants.TILE_WIDTH);

			_boundsMap = FRectangle.CreateByTopLeft(
				_gdScreen.MapOffsetX,
				_gdScreen.MapOffsetY,
				_gdScreen.LevelData.Width * GDConstants.TILE_WIDTH,
				_gdScreen.LevelData.Height * GDConstants.TILE_WIDTH);

			if (_dragMode == DMode.MapDrag)
			{
				if (_gdScreen.Mode==LevelEditorMode.Mouse && istate.IsRealDown)
				{
					var delta = istate.GamePointerPosition - _mouseStartPos;
					_gdScreen.MapOffsetX = _startOffset.X + delta.X;
					_gdScreen.MapOffsetY = _startOffset.Y + delta.Y;
				}
				else
				{
					_dragMode = DMode.Nothing;
				}
			}
			else if (_dragMode == DMode.CannonMove)
			{
				if (_gdScreen.Mode == LevelEditorMode.Mouse && istate.IsRealDown && _gdScreen.Selection is CannonStub cs)
				{
					var ins = _gdScreen.CanInsertCannonStub(new FPoint(rx, ry), cs.Scale, cs);
					if (ins != null)
					{
						cs.Center = ins.Position;
					}
				}
				else
				{
					_dragMode = DMode.Nothing;
				}
			}
			else if (_dragMode == DMode.ObstacleMove)
			{
				if (_gdScreen.Mode == LevelEditorMode.Mouse && istate.IsRealDown && _gdScreen.Selection is ObstacleStub os)
				{
					var ins = _gdScreen.CanInsertObstacleStub(new FPoint(rx, ry), os.ObstacleType, os.Width, os.Height, os.Rotation, os);
					if (ins != null)
					{
						os.Center = ins.Position;
					}
				}
				else
				{
					_dragMode = DMode.Nothing;
				}
			}
			else if (_dragMode == DMode.WallMove)
			{
				if (_gdScreen.Mode == LevelEditorMode.Mouse && istate.IsRealDown && _gdScreen.Selection is WallStub ws)
				{
					var ins = _gdScreen.CanInsertWallStub(new FPoint(rx, ry), new FPoint(rx, ry) + _wallRelativeP2, ws);
					if (ins != null)
					{
						ws.Point1 = ins.Point1;
						ws.Point2 = ins.Point2;
					}
				}
				else
				{
					_dragMode = DMode.Nothing;
				}
			}
			else if (_dragMode == DMode.WallDragP1)
			{
				if (_gdScreen.Mode == LevelEditorMode.Mouse && istate.IsRealDown && _gdScreen.Selection is WallStub ws)
				{
					var ins = _gdScreen.CanInsertWallStub(new FPoint(rx, ry), ws.Point2, ws);
					if (ins != null)
					{
						ws.Point1 = ins.Point1;
					}
				}
				else
				{
					_dragMode = DMode.Nothing;
				}
			}
			else if (_dragMode == DMode.WallDragP2)
			{
				if (_gdScreen.Mode == LevelEditorMode.Mouse && istate.IsRealDown && _gdScreen.Selection is WallStub ws)
				{
					var ins = _gdScreen.CanInsertWallStub(ws.Point1, new FPoint(rx, ry), ws);
					if (ins != null)
					{
						ws.Point2 = ins.Point2;
					}
				}
				else
				{
					_dragMode = DMode.Nothing;
				}
			}
			else if (_dragMode == DMode.PortalMove)
			{
				if (_gdScreen.Mode == LevelEditorMode.Mouse && istate.IsRealDown && _gdScreen.Selection is PortalStub ps)
				{
					var ins = _gdScreen.CanInsertPortalStub(new FPoint(rx, ry), ps.Length, ps.Normal, ps);
					if (ins != null)
					{
						ps.Center = ins.Center;
					}
				}
				else
				{
					_dragMode = DMode.Nothing;
				}
			}
			else if (_dragMode == DMode.Nothing)
			{
				if (_gdScreen.Mode == LevelEditorMode.Mouse && istate.IsExclusiveJustDown)
				{
					var clickedCannon   = _gdScreen.GetEntities<CannonStub>().FirstOrDefault(s => s.GetClickArea().Contains(istate.GamePointerPositionOnMap));
					var clickedObstacle = _gdScreen.GetEntities<ObstacleStub>().FirstOrDefault(s => s.GetClickArea().Contains(istate.GamePointerPositionOnMap));
					var clickedWallBase = _gdScreen.GetEntities<WallStub>().FirstOrDefault(s => s.GetClickArea().Contains(istate.GamePointerPositionOnMap));
					var clickedWallP1   = _gdScreen.GetEntities<WallStub>().FirstOrDefault(s => s.IsClickP1(istate.GamePointerPositionOnMap));
					var clickedWallP2   = _gdScreen.GetEntities<WallStub>().FirstOrDefault(s => s.IsClickP2(istate.GamePointerPositionOnMap));
					var clickedPortal   = _gdScreen.GetEntities<PortalStub>().FirstOrDefault(s => s.GetClickArea().Contains(istate.GamePointerPositionOnMap));
					if (clickedCannon != null)
					{
						istate.Swallow(InputConsumer.GameBackground);
						_gdScreen.SelectStub(clickedCannon);
						_mouseStartPos = istate.GamePointerPosition;
						_startOffset = _gdScreen.MapOffset;
						_dragOffCenter = istate.GamePointerPositionOnMap - clickedCannon.Center;
						_wallRelativeP2 = Vector2.Zero;
						_dragMode = DMode.CannonMove;
					}
					else if (clickedPortal != null)
					{
						istate.Swallow(InputConsumer.GameBackground);
						_gdScreen.SelectStub(clickedPortal);
						_mouseStartPos = istate.GamePointerPosition;
						_startOffset = _gdScreen.MapOffset;
						_dragOffCenter = istate.GamePointerPositionOnMap - clickedPortal.Center;
						_wallRelativeP2 = Vector2.Zero;
						_dragMode = DMode.PortalMove;
					}
					else if (clickedObstacle != null)
					{
						istate.Swallow(InputConsumer.GameBackground);
						_gdScreen.SelectStub(clickedObstacle);
						_mouseStartPos = istate.GamePointerPosition;
						_startOffset = _gdScreen.MapOffset;
						_dragOffCenter = istate.GamePointerPositionOnMap - clickedObstacle.Center;
						_wallRelativeP2 = Vector2.Zero;
						_dragMode = DMode.ObstacleMove;
					}
					else if (clickedWallP1 != null)
					{
						istate.Swallow(InputConsumer.GameBackground);
						_gdScreen.SelectStub(clickedWallP1);
						_mouseStartPos = istate.GamePointerPosition;
						_startOffset = _gdScreen.MapOffset;
						_dragOffCenter = Vector2.Zero;
						_wallRelativeP2 = Vector2.Zero;
						_dragMode = DMode.WallDragP1;
					}
					else if (clickedWallP2 != null)
					{
						istate.Swallow(InputConsumer.GameBackground);
						_gdScreen.SelectStub(clickedWallP2);
						_mouseStartPos = istate.GamePointerPosition;
						_startOffset = _gdScreen.MapOffset;
						_dragOffCenter = Vector2.Zero;
						_wallRelativeP2 = Vector2.Zero;
						_dragMode = DMode.WallDragP2;
					}
					else if (clickedWallBase != null)
					{
						istate.Swallow(InputConsumer.GameBackground);
						_gdScreen.SelectStub(clickedWallBase);
						_mouseStartPos = istate.GamePointerPosition;
						_startOffset = _gdScreen.MapOffset;
						_dragOffCenter = istate.GamePointerPositionOnMap - clickedWallBase.Point1;
						_wallRelativeP2 = clickedWallBase.Point2 - clickedWallBase.Point1;
						_dragMode = DMode.WallMove;
					}
					else
					{
						istate.Swallow(InputConsumer.GameBackground);
						_gdScreen.SelectStub(null);
						_mouseStartPos = istate.GamePointerPosition;
						_startOffset = _gdScreen.MapOffset;
						_dragOffCenter = Vector2.Zero;
						_wallRelativeP2 = Vector2.Zero;
						_dragMode = DMode.MapDrag;
					}

				}
			}

			_oobForce = CalculateOOB();
			if (!_oobForce.IsZero() && _dragMode != DMode.MapDrag)
			{
				UpdateMapRestDrag(gameTime);
			}
		}
		
		private DVector CalculateOOB()
		{
			var v2 = new DVector();

			if (_boundsMap.Left < _boundsWorkingAreaOuter.Left && _boundsMap.Right  < _boundsWorkingAreaInner.Right )  v2.X = +1;
			if (_boundsMap.Left > _boundsWorkingAreaInner.Left && _boundsMap.Right  > _boundsWorkingAreaOuter.Right )  v2.X = -1;
			if (_boundsMap.Top  < _boundsWorkingAreaOuter.Top  && _boundsMap.Bottom < _boundsWorkingAreaInner.Bottom) v2.Y = +1;
			if (_boundsMap.Top  > _boundsWorkingAreaInner.Top  && _boundsMap.Bottom > _boundsWorkingAreaOuter.Bottom) v2.Y = -1;

			return v2;
		}

		private void UpdateMapRestDrag(SAMTime gameTime)
		{
			_gdScreen.MapOffsetX += _oobForce.X * RETURN_SPEED * gameTime.ElapsedSeconds;
			_gdScreen.MapOffsetY += _oobForce.Y * RETURN_SPEED * gameTime.ElapsedSeconds;

			_boundsMap = FRectangle.CreateByTopLeft(
				_gdScreen.MapOffsetX,
				_gdScreen.MapOffsetY,
				_gdScreen.LevelData.Width * GDConstants.TILE_WIDTH,
				_gdScreen.LevelData.Height * GDConstants.TILE_WIDTH);

			var next = CalculateOOB();

			if (next.X == -1 && _oobForce.X == +1)
			{
				next.X = 0;
				_gdScreen.MapOffsetX = _boundsWorkingAreaInner.Right - _boundsMap.Width;
			}
			if (next.X == +1 && _oobForce.X == -1)
			{
				next.X = 0;
				_gdScreen.MapOffsetX = _boundsWorkingAreaInner.Left;
			}
			if (next.Y == -1 && _oobForce.Y == +1)
			{
				next.Y = 0;
				_gdScreen.MapOffsetY = _boundsWorkingAreaInner.Bottom - _boundsMap.Height;
			}
			if (next.Y == +1 && _oobForce.Y == -1)
			{
				next.Y = 0;
				_gdScreen.MapOffsetY = _boundsWorkingAreaInner.Top;
			}

			_oobForce = next;
		}

		public void ManualStartCannonMove(InputState istate)
		{
			_gdScreen.SetMode(LevelEditorMode.Mouse);
			_mouseStartPos = istate.GamePointerPosition;
			_startOffset = _gdScreen.MapOffset;
			_dragOffCenter = Vector2.Zero;
			_wallRelativeP2 = Vector2.Zero;
			_dragMode = DMode.CannonMove;
		}

		public void ManualStartObstacleMove(InputState istate)
		{
			_gdScreen.SetMode(LevelEditorMode.Mouse);
			_mouseStartPos = istate.GamePointerPosition;
			_startOffset = _gdScreen.MapOffset;
			_dragOffCenter = Vector2.Zero;
			_wallRelativeP2 = Vector2.Zero;
			_dragMode = DMode.ObstacleMove;
		}

		public void ManualStartWallDragP2(InputState istate)
		{
			_gdScreen.SetMode(LevelEditorMode.Mouse);
			_mouseStartPos = istate.GamePointerPosition;
			_startOffset = _gdScreen.MapOffset;
			_dragOffCenter = Vector2.Zero;
			_wallRelativeP2 = Vector2.Zero;
			_dragMode = DMode.WallDragP2;
		}

		public void ManualStartPortalMove(InputState istate)
		{
			_gdScreen.SetMode(LevelEditorMode.Mouse);
			_mouseStartPos = istate.GamePointerPosition;
			_startOffset = _gdScreen.MapOffset;
			_dragOffCenter = Vector2.Zero;
			_wallRelativeP2 = Vector2.Zero;
			_dragMode = DMode.PortalMove;
		}
	}
}
