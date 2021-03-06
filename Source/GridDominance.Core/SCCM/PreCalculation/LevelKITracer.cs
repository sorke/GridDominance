﻿using System;
using System.Collections.Generic;
using System.Linq;
using FarseerPhysics;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using GridDominance.Levelfileformat.Blueprint;
using GridDominance.Shared.Resources;
using GridDominance.Shared.Screens.NormalGameScreen.Entities.Obstacles;
using GridDominance.Shared.Screens.NormalGameScreen.LaserNetwork;
using GridDominance.Shared.Screens.NormalGameScreen.Physics;
using Microsoft.Xna.Framework;
using MonoSAMFramework.Portable.Extensions;
using MonoSAMFramework.Portable.GameMath;
using MonoSAMFramework.Portable.GameMath.Geometry;
using MonoSAMFramework.Portable.GameMath.Geometry.Alignment;

namespace GridDominance.Shared.SCCM.PreCalculation
{
	internal class LevelKITracer
	{
		private int MAX_COUNT_RECAST  = 8;   // Bullet
		private int MAX_COUNT_REFLECT = 8;   // Laser

		private bool NO_LASER_CORNER_HITS = false;
		private float REFRAC_CORNER_ENLARGE_FAC = 1;

		private int RESOLUTION = 3600;

		private float HITBOX_ENLARGE = 32;

		private MarkerCollisionBorder marker_dn = new MarkerCollisionBorder { Side = FlatAlign4.NN };
		private MarkerCollisionBorder marker_de = new MarkerCollisionBorder { Side = FlatAlign4.EE };
		private MarkerCollisionBorder marker_ds = new MarkerCollisionBorder { Side = FlatAlign4.SS };
		private MarkerCollisionBorder marker_dw = new MarkerCollisionBorder { Side = FlatAlign4.WW };

		private MarkerRefractionEdge marker_rn = new MarkerRefractionEdge { Side = FlatAlign4.NN };
		private MarkerRefractionEdge marker_re = new MarkerRefractionEdge { Side = FlatAlign4.EE };
		private MarkerRefractionEdge marker_rs = new MarkerRefractionEdge { Side = FlatAlign4.SS };
		private MarkerRefractionEdge marker_rw = new MarkerRefractionEdge { Side = FlatAlign4.WW };

		private MarkerRefractionCorner marker_rne = new MarkerRefractionCorner { Side = FlatAlign4C.NE };
		private MarkerRefractionCorner marker_rse = new MarkerRefractionCorner { Side = FlatAlign4C.SE };
		private MarkerRefractionCorner marker_rsw = new MarkerRefractionCorner { Side = FlatAlign4C.SW };
		private MarkerRefractionCorner marker_rnw = new MarkerRefractionCorner { Side = FlatAlign4C.NW };
		
		public void PrecalcLaser(LevelBlueprint lvl)
		{
			lvl.GetConfig(LevelBlueprint.KI_CONFIG_TRACE_HITBOX_ENLARGE, ref HITBOX_ENLARGE);
			lvl.GetConfig(LevelBlueprint.KI_CONFIG_TRACE_MAX_BULLETBOUNCE, ref MAX_COUNT_RECAST);
			lvl.GetConfig(LevelBlueprint.KI_CONFIG_TRACE_MAX_LASERREFLECT, ref MAX_COUNT_REFLECT);
			lvl.GetConfig(LevelBlueprint.KI_CONFIG_TRACE_RESOULUTION, ref RESOLUTION);
			lvl.GetConfig(LevelBlueprint.KI_CONFIG_TRACE_MAX_LASERREFLECT, ref MAX_COUNT_REFLECT);
			lvl.GetConfig(LevelBlueprint.KI_CONFIG_TRACE_NO_LASER_CORNER_REFLECT, ref NO_LASER_CORNER_HITS);
			lvl.GetConfig(LevelBlueprint.KI_CONFIG_TRACE_REFRAC_CORNER_ENLARGE_FAC, ref REFRAC_CORNER_ENLARGE_FAC);

			foreach (var cannon in lvl.BlueprintCannons)
				cannon.PrecalculatedPaths = PrecalcBullet(lvl, cannon);

			foreach (var cannon in lvl.BlueprintMinigun)
				cannon.PrecalculatedPaths = PrecalcBullet(lvl, cannon);

			foreach (var cannon in lvl.BlueprintRelayCannon)
				cannon.PrecalculatedPaths = PrecalcBullet(lvl, cannon);

			foreach (var cannon in lvl.BlueprintTrishotCannon)
				cannon.PrecalculatedPaths = PrecalcBullet(lvl, cannon);

			foreach (var cannon in lvl.BlueprintLaserCannons)
				cannon.PrecalculatedPaths = PrecalcLaser(lvl, cannon);

			foreach (var cannon in lvl.BlueprintShieldProjector)
				cannon.PrecalculatedPaths = PrecalcLaser(lvl, cannon);
		}

		private BulletPathBlueprint[] PrecalcBullet(LevelBlueprint lvl, ICannonBlueprint cannon)
		{
			var worldNormal = CreateRayWorld(lvl, 0, 1);
			var worldExtend = CreateRayWorld(lvl, HITBOX_ENLARGE, 1.5f);

			var rayClock = new List<Tuple<BulletPathBlueprint, float>>[RESOLUTION];

			for (int ideg = 0; ideg < RESOLUTION; ideg++)
			{
				float deg = ideg * (360f / RESOLUTION);
				var rays = FindBulletPaths(lvl, worldNormal, worldExtend, cannon, deg);

				rayClock[ideg] = rays;
			}

			List<BulletPathBlueprint> resultRays = new List<BulletPathBlueprint>();
			for (;;)
			{
				for (int ideg = 0; ideg < RESOLUTION; ideg++)
				{
					if (rayClock[ideg].Any()) resultRays.Add(ExtractBestRay(rayClock, ideg, rayClock[ideg].First().Item1.TargetCannonID));
				}
				break;
			}

			for (int i = 0; i < resultRays.Count; i++) resultRays[i] = resultRays[i].AsCleaned();

			return resultRays.ToArray();
		}

		public BulletPathBlueprint[] PrecalcLaser(LevelBlueprint lvl, ICannonBlueprint cannon)
		{
			var worldNormal = CreateRayWorld(lvl, 0, 1);
			var worldExtend = CreateRayWorld(lvl, HITBOX_ENLARGE, 1.5f);

			var rayClock = new List<Tuple<BulletPathBlueprint, float>>[RESOLUTION];

			for (int ideg = 0; ideg < RESOLUTION; ideg++)
			{
				float deg = ideg * (360f / RESOLUTION);
				var rays = FindLaserPaths(lvl, worldNormal, worldExtend, cannon, deg);

				rayClock[ideg] = rays;
			}

			List<BulletPathBlueprint> resultRays = new List<BulletPathBlueprint>();
			for (;;)
			{
				for (int ideg = 0; ideg < RESOLUTION; ideg++)
				{
					if (rayClock[ideg].Any()) resultRays.Add(ExtractBestRay(rayClock, ideg, rayClock[ideg].First().Item1.TargetCannonID));
				}
				break;
			}

			for (int i = 0; i < resultRays.Count; i++) resultRays[i] = resultRays[i].AsCleaned();

			return resultRays.ToArray();
		}

		private BulletPathBlueprint ExtractBestRay(List<Tuple<BulletPathBlueprint, float>>[] rayClock, int iStart, int cid)
		{
			float bestQuality = rayClock[iStart].First(p => p.Item1.TargetCannonID == cid).Item2;
			BulletPathBlueprint bestRay = rayClock[iStart].First(p => p.Item1.TargetCannonID == cid).Item1;
			
			for (int delta = 0; delta < RESOLUTION; delta++)
			{
				var ideg = (iStart + delta + RESOLUTION) % RESOLUTION;

				var clockrays = rayClock[ideg].Where(p => p.Item1.TargetCannonID == cid).ToList();
				if (!clockrays.Any()) break;

				foreach (var ray in clockrays)
				{
					if (ray.Item2 < bestQuality) { bestQuality = ray.Item2; bestRay = ray.Item1; }
					rayClock[ideg].Remove(ray);
				}
			}

			for (int delta = 1; delta < RESOLUTION; delta++)
			{
				var ideg = (iStart - delta + RESOLUTION) % RESOLUTION;

				var clockrays = rayClock[ideg].Where(p => p.Item1.TargetCannonID == cid).ToList();
				if (!clockrays.Any()) break;

				foreach (var ray in clockrays)
				{
					if (ray.Item2 < bestQuality) { bestQuality = ray.Item2; bestRay = ray.Item1; }
					rayClock[ideg].Remove(ray);
				}
			}

			return bestRay;
		}

		private List<Tuple<BulletPathBlueprint, float>> FindBulletPaths(LevelBlueprint lvl, World wBase, World wCollision, ICannonBlueprint cannon, float deg)
		{
			return FindBulletPaths(lvl, wBase, wCollision, cannon.CannonID, new FPoint(cannon.X, cannon.Y), new List<Tuple<Vector2, Vector2>>(), deg * FloatMath.DegRad, deg * FloatMath.DegRad, MAX_COUNT_RECAST);
		}

		private List<Tuple<BulletPathBlueprint, float>> FindBulletPaths(LevelBlueprint lvl, World wBase, World wCollision, int sourceID, FPoint rcStart, List<Tuple<Vector2, Vector2>> sourcerays, float startRadians, float cannonRadians, int remainingRecasts)
		{
			var none = new List<Tuple<BulletPathBlueprint, float>>();
			if (remainingRecasts <= 0) return none;

			var rays = sourcerays.ToList();
			
			var rcEnd = rcStart + new Vector2(2048, 0).Rotate(startRadians);

			var traceResult  = RayCastBullet(wBase, rcStart, rcEnd);
			var traceResult2 = RayCastBullet(wCollision, rcStart, rcEnd);

			if (traceResult2 != null && traceResult != null && traceResult2.Item1.UserData != traceResult.Item1.UserData)
			{
				// Dirty hit
				return none;
			}

			if (traceResult == null)
			{
				return none;
			}

			var fCannon = traceResult.Item1.UserData as ICannonBlueprint;
			if (fCannon != null)
			{
				if (fCannon.CannonID == sourceID) return none;

				var quality = Math2D.LinePointDistance(rcStart, traceResult.Item2, new FPoint(fCannon.X, fCannon.Y));
				rays.Add(Tuple.Create(rcStart.ToVec2D(), traceResult.Item2.ToVec2D()));
				var path = new BulletPathBlueprint(fCannon.CannonID, cannonRadians, rays.ToArray());
				return new List<Tuple<BulletPathBlueprint, float>> { Tuple.Create(path, quality) };
			}

			var fGlassBlock = traceResult.Item1.UserData as GlassBlockBlueprint;
			if (fGlassBlock != null)
			{
				rays.Add(Tuple.Create(rcStart.ToVec2D(), traceResult.Item2.ToVec2D()));

				var pNewStart = traceResult.Item2;
				var pVec = Vector2.Reflect(rcEnd - rcStart, traceResult.Item3);

				return FindBulletPaths(lvl, wBase, wCollision, sourceID, pNewStart, rays, pVec.ToAngle(), cannonRadians, remainingRecasts - 1);
			}

			var fMirrorBlock = traceResult.Item1.UserData as MirrorBlockBlueprint;
			if (fMirrorBlock != null)
			{
				rays.Add(Tuple.Create(rcStart.ToVec2D(), traceResult.Item2.ToVec2D()));

				var pNewStart = traceResult.Item2;
				var pVec = Vector2.Reflect(rcEnd - rcStart, traceResult.Item3);

				return FindBulletPaths(lvl, wBase, wCollision, sourceID, pNewStart, rays, pVec.ToAngle(), cannonRadians, remainingRecasts - 1);
			}

			var fMirrorCircle = traceResult.Item1.UserData as MirrorCircleBlueprint;
			if (fMirrorCircle != null)
			{
				rays.Add(Tuple.Create(rcStart.ToVec2D(), traceResult.Item2.ToVec2D()));

				var pNewStart = traceResult.Item2;
				var pVec = Vector2.Reflect(rcEnd - rcStart, traceResult.Item3);

				return FindBulletPaths(lvl, wBase, wCollision, sourceID, pNewStart, rays, pVec.ToAngle(), cannonRadians, remainingRecasts - 1);
			}

			var fVoidWall = traceResult.Item1.UserData as VoidWallBlueprint;
			if (fVoidWall != null)
			{
				return none;
			}

			var fVoidCircle = traceResult.Item1.UserData as VoidCircleBlueprint;
			if (fVoidCircle != null)
			{
				return none;
			}

			var fBlackhole = traceResult.Item1.UserData as BlackHoleBlueprint;
			if (fBlackhole != null)
			{
				return none; // Black holes are _not_ correctly calculated in this preprocessor
			}

			var fPortal = traceResult.Item1.UserData as PortalBlueprint;
			if (fPortal != null)
			{
				bool hit = FloatMath.DiffRadiansAbs(traceResult.Item3.ToAngle(), FloatMath.ToRadians(fPortal.Normal)) < FloatMath.RAD_POS_005;

				if (!hit) return none;

				rays.Add(Tuple.Create(rcStart.ToVec2D(), traceResult.Item2.ToVec2D()));

				var dat = new List<Tuple<BulletPathBlueprint, float>>();
				foreach (var outportal in lvl.BlueprintPortals.Where(p => p.Side != fPortal.Side && p.Group == fPortal.Group))
				{
					var cIn  = new FPoint(fPortal.X, fPortal.Y);
					var cOut = new FPoint(outportal.X, outportal.Y);

					var rot = FloatMath.ToRadians(outportal.Normal - fPortal.Normal + 180);
					var stretch = outportal.Length / fPortal.Length;

					var newAngle = FloatMath.NormalizeAngle(startRadians + rot);
					var newStart = cOut + (traceResult.Item2 - cIn).Rotate(rot) * stretch;

					newStart = newStart.MirrorAtNormal(cOut, Vector2.UnitX.Rotate(FloatMath.ToRadians(outportal.Normal)));

					var sub = FindBulletPaths(lvl, wBase, wCollision, sourceID, newStart, rays, newAngle, cannonRadians, remainingRecasts - 1);
					dat.AddRange(sub);
				}
				return dat;
			}

			var fBorder = traceResult.Item1.UserData as MarkerCollisionBorder;
			if (fBorder != null)
			{
				if (lvl.WrapMode == LevelBlueprint.WRAPMODE_DONUT)
				{
					rays.Add(Tuple.Create(rcStart.ToVec2D(), traceResult.Item2.ToVec2D()));

					var pNewStartX = traceResult.Item2.X;
					var pNewStartY = traceResult.Item2.Y;
					switch (fBorder.Side)
					{
						case FlatAlign4.NN: pNewStartY += lvl.LevelHeight; break;
						case FlatAlign4.EE: pNewStartX -= lvl.LevelWidth; break;
						case FlatAlign4.SS: pNewStartY -= lvl.LevelHeight; break;
						case FlatAlign4.WW: pNewStartX += lvl.LevelWidth; break;
						default:
							throw new ArgumentOutOfRangeException();
					}
					var pVec = rcEnd - rcStart;
					var pNewStart = new FPoint(pNewStartX, pNewStartY);

					return FindBulletPaths(lvl, wBase, wCollision, sourceID, pNewStart, rays, pVec.ToAngle(), cannonRadians, remainingRecasts - 1);
				}
				else if (lvl.WrapMode == LevelBlueprint.WRAPMODE_SOLID)
				{
					rays.Add(Tuple.Create(rcStart.ToVec2D(), traceResult.Item2.ToVec2D()));

					var pNewStart = traceResult.Item2;
					var pVec = Vector2.Reflect(rcEnd - rcStart, traceResult.Item3);

					return FindBulletPaths(lvl, wBase, wCollision, sourceID, pNewStart, rays, pVec.ToAngle(), cannonRadians, remainingRecasts - 1);
				}
				throw new Exception("Unsupported WrapMode: " + lvl.WrapMode);
			}

			throw new Exception("Unknown rayTrace resturn ficture: " + traceResult.Item1.UserData);
		}

		private List<Tuple<BulletPathBlueprint, float>> FindLaserPaths(LevelBlueprint lvl, World wBase, World wCollision, ICannonBlueprint cannon, float deg)
		{
			return FindLaserPaths(lvl, wBase, wCollision, cannon.CannonID, new FPoint(cannon.X, cannon.Y), new List<Tuple<Vector2, Vector2>>(), deg * FloatMath.DegRad, deg * FloatMath.DegRad, MAX_COUNT_REFLECT, false, null);
		}

		private List<Tuple<BulletPathBlueprint, float>> FindLaserPaths(LevelBlueprint lvl, World wBase, World wCollision, int sourceID, FPoint rcStart, List<Tuple<Vector2, Vector2>> sourcerays, float startRadians, float cannonRadians, int remainingRecasts, bool inGlassBlock, object objIgnore)
		{
			var none = new List<Tuple<BulletPathBlueprint, float>>();
			if (remainingRecasts <= 0) return none;

			var rays = sourcerays.ToList();

			var rcEnd = rcStart + new Vector2(2048, 0).Rotate(startRadians);

			var traceResult  = RayCastLaser(wBase, rcStart, rcEnd, objIgnore);
			var traceResult2 = RayCastLaser(wCollision, rcStart, rcEnd, objIgnore);

			if (traceResult2 != null && traceResult != null && traceResult2.Item1.UserData != traceResult.Item1.UserData)
			{
				// Dirty hit
				return none;
			}

			if (traceResult == null)
			{
				return none;
			}

			foreach (var r in rays)
			{
				if (Math2D.LineIntersectionExt(rcStart, traceResult.Item2, r.Item1.ToFPoint(), r.Item2.ToFPoint(), -0.1f, out _, out _, out _))
				{
					return none;
				}
			}
			
			
			var fCannon = traceResult.Item1.UserData as ICannonBlueprint;
			if (fCannon != null)
			{
				if (fCannon.CannonID == sourceID) return none;

				var quality = Math2D.LinePointDistance(rcStart, traceResult.Item2, new FPoint(fCannon.X, fCannon.Y));
				rays.Add(Tuple.Create(rcStart.ToVec2D(), traceResult.Item2.ToVec2D()));
				var path = new BulletPathBlueprint(fCannon.CannonID, cannonRadians, rays.ToArray());
				return new List<Tuple<BulletPathBlueprint, float>> { Tuple.Create(path, quality) };
			}

			var fGlassBlock = traceResult.Item1.UserData as MarkerRefractionEdge;
			if (fGlassBlock != null)
			{
				rays.Add(Tuple.Create(rcStart.ToVec2D(), traceResult.Item2.ToVec2D()));

				var pNewStart = traceResult.Item2;

				var normal = traceResult.Item3;
				var aIn = (rcStart - rcEnd).ToAngle() - normal.ToAngle();

				// sin(aIn) / sin(aOut) = currRefractIdx / Glass.RefractIdx

				var n = inGlassBlock ? (GlassBlockBlueprint.REFRACTION_INDEX / 1f) : (1f / GlassBlockBlueprint.REFRACTION_INDEX);

				var sinaOut = FloatMath.Sin(aIn) * n;

				var dat = new List<Tuple<BulletPathBlueprint, float>>();

				var isRefracting = sinaOut < 1 && sinaOut > -1;
				var isReflecting = FloatMath.Abs(aIn) > LaserNetwork.MIN_REFRACT_ANGLE && (!inGlassBlock || (inGlassBlock && !isRefracting));
				
				if (isRefracting) // refraction
				{
					// refraction

					var aOut = FloatMath.Asin(sinaOut);
					var pRefractAngle = (-normal).ToAngle() + aOut;

					var sub = FindLaserPaths(lvl, wBase, wCollision, sourceID, pNewStart, rays, pRefractAngle, cannonRadians, remainingRecasts - 1, !inGlassBlock, fGlassBlock);
					dat.AddRange(sub);
				}

				if (isReflecting)
				{
					// reflection

					var pReflectVec = Vector2.Reflect(rcEnd - rcStart, traceResult.Item3);

					var sub = FindLaserPaths(lvl, wBase, wCollision, sourceID, pNewStart, rays, pReflectVec.ToAngle(), cannonRadians, remainingRecasts - 1, !inGlassBlock, fGlassBlock);
					dat.AddRange(sub);
				}

				return dat;
			}

			var fGlassBlockCorner = traceResult.Item1.UserData as MarkerRefractionCorner;
			if (fGlassBlockCorner != null)
			{
				if (NO_LASER_CORNER_HITS) return none;
				
				rays.Add(Tuple.Create(rcStart.ToVec2D(), traceResult.Item2.ToVec2D()));

				var pNewStart = traceResult.Item2;

				var dat = new List<Tuple<BulletPathBlueprint, float>>();

				if (!inGlassBlock)
				{
					// reflection

					var pReflectVec = Vector2.Reflect(rcEnd - rcStart, traceResult.Item3);

					var sub = FindLaserPaths(lvl, wBase, wCollision, sourceID, pNewStart, rays, pReflectVec.ToAngle(), cannonRadians, remainingRecasts - 1, !inGlassBlock, fGlassBlockCorner);
					dat.AddRange(sub);
				}

				return dat;
			}

			var fMirrorBlock = traceResult.Item1.UserData as MirrorBlockBlueprint;
			if (fMirrorBlock != null)
			{
				rays.Add(Tuple.Create(rcStart.ToVec2D(), traceResult.Item2.ToVec2D()));

				var pNewStart = traceResult.Item2;
				var pVec = Vector2.Reflect(rcEnd - rcStart, traceResult.Item3);

				return FindLaserPaths(lvl, wBase, wCollision, sourceID, pNewStart, rays, pVec.ToAngle(), cannonRadians, remainingRecasts - 1, inGlassBlock, fMirrorBlock);
			}

			var fMirrorCircle = traceResult.Item1.UserData as MirrorCircleBlueprint;
			if (fMirrorCircle != null)
			{
				rays.Add(Tuple.Create(rcStart.ToVec2D(), traceResult.Item2.ToVec2D()));

				var pNewStart = traceResult.Item2;
				var pVec = Vector2.Reflect(rcEnd - rcStart, traceResult.Item3);

				return FindLaserPaths(lvl, wBase, wCollision, sourceID, pNewStart, rays, pVec.ToAngle(), cannonRadians, remainingRecasts - 1, inGlassBlock, fMirrorCircle);
			}

			var fVoidWall = traceResult.Item1.UserData as VoidWallBlueprint;
			if (fVoidWall != null)
			{
				return none;
			}

			var fVoidCircle = traceResult.Item1.UserData as VoidCircleBlueprint;
			if (fVoidCircle != null)
			{
				return none;
			}

			var fPortal = traceResult.Item1.UserData as PortalBlueprint;
			if (fPortal != null)
			{
				bool hit = FloatMath.DiffRadiansAbs(traceResult.Item3.ToAngle(), FloatMath.ToRadians(fPortal.Normal)) < FloatMath.RAD_POS_005;

				if (!hit) return none;

				rays.Add(Tuple.Create(rcStart.ToVec2D(), traceResult.Item2.ToVec2D()));

				var dat = new List<Tuple<BulletPathBlueprint, float>>();
				foreach (var outportal in lvl.BlueprintPortals.Where(p => p.Side != fPortal.Side && p.Group == fPortal.Group))
				{
					var cIn = new FPoint(fPortal.X, fPortal.Y);
					var cOut = new FPoint(outportal.X, outportal.Y);

					var rot = FloatMath.ToRadians(outportal.Normal - fPortal.Normal + 180);
					var stretch = outportal.Length / fPortal.Length;

					var newAngle = FloatMath.NormalizeAngle(startRadians + rot);
					var newStart = cOut + stretch * (traceResult.Item2 - cIn).Rotate(rot);

					newStart = newStart.MirrorAtNormal(cOut, Vector2.UnitX.Rotate(FloatMath.ToRadians(outportal.Normal)));

					var sub = FindLaserPaths(lvl, wBase, wCollision, sourceID, newStart, rays, newAngle, cannonRadians, remainingRecasts - 1, inGlassBlock, outportal);
					dat.AddRange(sub);
				}
				return dat;
			}

			var fBorder = traceResult.Item1.UserData as MarkerCollisionBorder;
			if (fBorder != null)
			{
				if (lvl.WrapMode == LevelBlueprint.WRAPMODE_DONUT)
				{
					rays.Add(Tuple.Create(rcStart.ToVec2D(), traceResult.Item2.ToVec2D()));

					var pNewStartX = traceResult.Item2.X;
					var pNewStartY = traceResult.Item2.Y;
					switch (fBorder.Side)
					{
						case FlatAlign4.NN: pNewStartY += lvl.LevelHeight; break;
						case FlatAlign4.EE: pNewStartX -= lvl.LevelWidth; break;
						case FlatAlign4.SS: pNewStartY -= lvl.LevelHeight; break;
						case FlatAlign4.WW: pNewStartX += lvl.LevelWidth; break;
						default:
							throw new ArgumentOutOfRangeException();
					}
					var pVec = rcEnd - rcStart;
					var pNewStart = new FPoint(pNewStartX, pNewStartY);

					return FindLaserPaths(lvl, wBase, wCollision, sourceID, pNewStart, rays, pVec.ToAngle(), cannonRadians, remainingRecasts - 1, inGlassBlock, fBorder);
				}
				else if (lvl.WrapMode == LevelBlueprint.WRAPMODE_SOLID)
				{
					rays.Add(Tuple.Create(rcStart.ToVec2D(), traceResult.Item2.ToVec2D()));

					var pNewStart = traceResult.Item2;
					var pVec = Vector2.Reflect(rcEnd - rcStart, traceResult.Item3);

					return FindLaserPaths(lvl, wBase, wCollision, sourceID, pNewStart, rays, pVec.ToAngle(), cannonRadians, remainingRecasts - 1, inGlassBlock, fBorder);
				}
				throw new Exception("Unsupported WrapMode: " + lvl.WrapMode);
			}

			throw new Exception("Unknown rayTrace resturn ficture: " + traceResult.Item1.UserData);
		}

		private Tuple<Fixture, FPoint, Vector2> RayCastBullet(World w, FPoint start, FPoint end)
		{
			Tuple<Fixture, FPoint, Vector2> result = null;

			//     return -1:       ignore this fixture and continue 
			//     return  0:       terminate the ray cast
			//     return fraction: clip the ray to this point
			//     return 1:        don't clip the ray and continue
			Func<Fixture, Vector2, Vector2, float, float> callback = (f, pos, normal, frac) =>
			{
				if (f.UserData is MarkerRefractionEdge) return -1; // ignore
				if (f.UserData is MarkerRefractionCorner) return -1; // ignore

				result = Tuple.Create(f, pos.ToFPoint(), normal);

				return frac; // limit
			};

			w.RayCast(callback, start.ToVec2D(), end.ToVec2D());

			return result;
		}

		private Tuple<Fixture, FPoint, Vector2> RayCastLaser(World w, FPoint start, FPoint end, object objIgnore)
		{
			Tuple<Fixture, FPoint, Vector2> result = null;

			//     return -1:       ignore this fixture and continue 
			//     return  0:       terminate the ray cast
			//     return fraction: clip the ray to this point
			//     return 1:        don't clip the ray and continue
			float Callback(Fixture f, Vector2 pos, Vector2 normal, float frac)
			{
				if (f.UserData is GlassBlockBlueprint) return -1; // ignore
				if (f.UserData is BlackHoleBlueprint) return -1; // ignore
				if (objIgnore != null && f.UserData == objIgnore) return -1; // ignore

				result = Tuple.Create(f, pos.ToFPoint(), normal);

				return frac; // limit
			}

			w.RayCast(Callback, start.ToVec2D(), end.ToVec2D());

			return result;
		}

		private World CreateRayWorld(LevelBlueprint lvl, float extend, float cannonEnlarge)
		{
			var world = new World(Vector2.Zero);

			ConvertUnits.SetDisplayUnitToSimUnitRatio(1);

			foreach (var elem in lvl.AllCannons)
			{
				if (elem.Diameter < 0.01f) throw new Exception("Invalid Physics");

				var body = BodyFactory.CreateBody(world, new Vector2(elem.X, elem.Y), 0, BodyType.Static, elem);
				FixtureFactory.AttachCircle(cannonEnlarge * elem.Diameter / 2f + extend, 1, body, Vector2.Zero, elem);
			}

			foreach (var elem in lvl.BlueprintGlassBlocks)
			{
				if (elem.Width < 0.01f) throw new Exception("Invalid Physics");
				if (elem.Height < 0.01f) throw new Exception("Invalid Physics");

				var body = BodyFactory.CreateBody(world, new Vector2(elem.X, elem.Y), FloatMath.ToRadians(elem.Rotation), BodyType.Static, elem);
				FixtureFactory.AttachRectangle(elem.Width + extend, elem.Height + extend, 1, Vector2.Zero, body, elem);

				var pw = world;
				var w = ConvertUnits.ToSimUnits(elem.Width + extend);
				var h = ConvertUnits.ToSimUnits(elem.Height + extend);
				var p = ConvertUnits2.ToSimUnits(new FPoint(elem.X, elem.Y));
				var rot = FloatMath.ToRadians(elem.Rotation);
				var mw = GlassBlock.MARKER_WIDTH;
				var cw = 10*GlassBlock.CORNER_WIDTH* REFRAC_CORNER_ENLARGE_FAC;

				var bodyN = BodyFactory.CreateBody(pw, p, rot, BodyType.Static, marker_rn);
				FixtureFactory.AttachRectangle(w, mw, 1, new Vector2(0, -(h - mw) / 2f), bodyN, marker_rn);

				var bodyE = BodyFactory.CreateBody(pw, p, rot, BodyType.Static, marker_re);
				FixtureFactory.AttachRectangle(mw, h, 1, new Vector2(+(w - mw) / 2f, 0), bodyE, marker_re);

				var bodyS = BodyFactory.CreateBody(pw, p, rot, BodyType.Static, marker_rs);
				FixtureFactory.AttachRectangle(w, mw, 1, new Vector2(0, +(h - mw) / 2f), bodyS, marker_rs);

				var bodyW = BodyFactory.CreateBody(pw, p, rot, BodyType.Static, marker_rw);
				FixtureFactory.AttachRectangle(mw, h, 1, new Vector2(-(w - mw) / 2f, 0), bodyW, marker_rw);


				var bodyNE = BodyFactory.CreateBody(pw, p, rot, BodyType.Static, marker_rne);
				FixtureFactory.AttachRectangle(cw, cw, 1, new Vector2(+w / 2, -h / 2), bodyNE, marker_rne);

				var bodySE = BodyFactory.CreateBody(pw, p, rot, BodyType.Static, marker_rse);
				FixtureFactory.AttachRectangle(cw, cw, 1, new Vector2(+w / 2, +h / 2), bodySE, marker_rse);

				var bodySW = BodyFactory.CreateBody(pw, p, rot, BodyType.Static, marker_rsw);
				FixtureFactory.AttachRectangle(cw, cw, 1, new Vector2(-w / 2, +h / 2), bodySW, marker_rsw);

				var bodyNW = BodyFactory.CreateBody(pw, p, rot, BodyType.Static, marker_rnw);
				FixtureFactory.AttachRectangle(cw, cw, 1, new Vector2(-w / 2, -h / 2), bodyNW, marker_rnw);
			}

			foreach (var elem in lvl.BlueprintVoidCircles)
			{
				if (elem.Diameter < 0.01f) throw new Exception("Invalid Physics");

				var body = BodyFactory.CreateBody(world, new Vector2(elem.X, elem.Y), 0, BodyType.Static, elem);
				FixtureFactory.AttachCircle(elem.Diameter / 2f + extend, 1, body, Vector2.Zero, elem);
			}

			foreach (var elem in lvl.BlueprintVoidWalls)
			{
				if (elem.Length < 0.01f) throw new Exception("Invalid Physics");

				var body = BodyFactory.CreateBody(world, new Vector2(elem.X, elem.Y), 0, BodyType.Static, elem);
				FixtureFactory.AttachRectangle(elem.Length + extend, VoidWallBlueprint.DEFAULT_WIDTH + extend, 1, Vector2.Zero, body, elem);
				body.Rotation = FloatMath.DegRad * elem.Rotation;
			}

			foreach (var elem in lvl.BlueprintBlackHoles)
			{
				if (elem.Diameter < 0.01f) throw new Exception("Invalid Physics");

				var body = BodyFactory.CreateBody(world, new Vector2(elem.X, elem.Y), 0, BodyType.Static, elem);
				FixtureFactory.AttachCircle(elem.Diameter * 0.5f * 0.3f, 1, body, Vector2.Zero, elem);
			}

			foreach (var elem in lvl.BlueprintPortals)
			{
				if (elem.Length < 0.01f) throw new Exception("Invalid Physics");

				var body = BodyFactory.CreateBody(world, new Vector2(elem.X, elem.Y), 0, BodyType.Static, elem);
				FixtureFactory.AttachRectangle(elem.Length + extend, PortalBlueprint.DEFAULT_WIDTH + extend, 1, Vector2.Zero, body, elem);
				body.Rotation = FloatMath.DegRad * (elem.Normal + 90);
			}

			foreach (var elem in lvl.BlueprintMirrorBlocks)
			{
				if (elem.Width < 0.01f) throw new Exception("Invalid Physics");
				if (elem.Height < 0.01f) throw new Exception("Invalid Physics");

				var body = BodyFactory.CreateBody(world, new Vector2(elem.X, elem.Y), FloatMath.ToRadians(elem.Rotation), BodyType.Static, elem);
				FixtureFactory.AttachRectangle(elem.Width + extend, elem.Height + extend, 1, Vector2.Zero, body, elem);
			}

			foreach (var elem in lvl.BlueprintMirrorCircles)
			{
				if (elem.Diameter < 0.01f) throw new Exception("Invalid Physics");

				var body = BodyFactory.CreateBody(world, new Vector2(elem.X, elem.Y), 0, BodyType.Static, elem);
				FixtureFactory.AttachCircle(elem.Diameter / 2f + extend, 1, body, Vector2.Zero, elem);
			}

			foreach (var elem in lvl.BlueprintLaserCannons)
			{
				if (elem.Diameter < 0.01f) throw new Exception("Invalid Physics");

				var body = BodyFactory.CreateBody(world, new Vector2(elem.X, elem.Y), 0, BodyType.Static, elem);
				FixtureFactory.AttachCircle(cannonEnlarge * elem.Diameter / 2f + extend, 1, body, Vector2.Zero, elem);
			}

			if (lvl.WrapMode == LevelBlueprint.WRAPMODE_SOLID || lvl.WrapMode == LevelBlueprint.WRAPMODE_DONUT)
			{
				var mw = lvl.LevelWidth;
				var mh = lvl.LevelHeight;
				var ex = 2 * GDConstants.TILE_WIDTH;

				var rn = new FRectangle(-ex, -ex, mw + 2 * ex, ex);
				var re = new FRectangle(+mw, -ex, ex, mh + 2 * ex);
				var rs = new FRectangle(-ex, +mh, mw + 2 * ex, ex);
				var rw = new FRectangle(-ex, -ex, ex, mh + 2 * ex);

				var bn = BodyFactory.CreateBody(world, ConvertUnits.ToSimUnits(rn.VecCenter), 0, BodyType.Static, marker_dn);
				var be = BodyFactory.CreateBody(world, ConvertUnits.ToSimUnits(re.VecCenter), 0, BodyType.Static, marker_de);
				var bs = BodyFactory.CreateBody(world, ConvertUnits.ToSimUnits(rs.VecCenter), 0, BodyType.Static, marker_ds);
				var bw = BodyFactory.CreateBody(world, ConvertUnits.ToSimUnits(rw.VecCenter), 0, BodyType.Static, marker_dw);

				var fn = FixtureFactory.AttachRectangle(ConvertUnits.ToSimUnits(rn.Width), ConvertUnits.ToSimUnits(rn.Height), 1, Vector2.Zero, bn, marker_dn);
				var fe = FixtureFactory.AttachRectangle(ConvertUnits.ToSimUnits(re.Width), ConvertUnits.ToSimUnits(re.Height), 1, Vector2.Zero, be, marker_de);
				var fs = FixtureFactory.AttachRectangle(ConvertUnits.ToSimUnits(rs.Width), ConvertUnits.ToSimUnits(rs.Height), 1, Vector2.Zero, bs, marker_ds);
				var fw = FixtureFactory.AttachRectangle(ConvertUnits.ToSimUnits(rw.Width), ConvertUnits.ToSimUnits(rw.Height), 1, Vector2.Zero, bw, marker_dw);
			}

			return world;
		}
	}
}
