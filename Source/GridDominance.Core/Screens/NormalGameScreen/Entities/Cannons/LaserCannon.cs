﻿using FarseerPhysics;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using GridDominance.Levelfileformat.Blueprint;
using GridDominance.Shared.Resources;
using GridDominance.Shared.Screens.Common;
using GridDominance.Shared.Screens.NormalGameScreen.Entities.EntityOperations;
using GridDominance.Shared.Screens.NormalGameScreen.FractionController;
using GridDominance.Shared.Screens.NormalGameScreen.Fractions;
using GridDominance.Shared.Screens.NormalGameScreen.LaserNetwork;
using GridDominance.Shared.Screens.NormalGameScreen.Physics;
using Microsoft.Xna.Framework;
using MonoSAMFramework.Portable;
using MonoSAMFramework.Portable.BatchRenderer;
using MonoSAMFramework.Portable.DebugTools;
using MonoSAMFramework.Portable.Extensions;
using MonoSAMFramework.Portable.GameMath;
using MonoSAMFramework.Portable.Input;
using MonoSAMFramework.Portable.Screens;
using MonoSAMFramework.Portable.Sound;

namespace GridDominance.Shared.Screens.NormalGameScreen.Entities.Cannons
{
	public class LaserCannon : Cannon, ILaserCannon
	{
		private const float RAY_FORCE = 0.175f;

		public readonly LaserCannonBlueprint Blueprint;
		public readonly LaserSource LaserSource;
		private readonly GDGameScreen _screen;

		public readonly DeltaLimitedFloat CorePulse  = new DeltaLimitedFloat(1, CORE_PULSE * CORE_PULSE_FREQ * 2);
		public float LaserPulseTime = 0f;

		private readonly int coreImage;
		private readonly float coreRotation;
		public float ChargeTime = 0f;

		private readonly SAMEffectWrapper _soundeffect;
		private readonly bool _muted;

		float ILaserCannon.LaserPulseTime => LaserPulseTime;

		public override bool IsLaser => true;

		public LaserCannon(GDGameScreen scrn, LaserCannonBlueprint bp, Fraction[] fractions) : 
			base(scrn, fractions, bp.Player, bp.X, bp.Y, bp.Diameter, bp.CannonID, bp.Rotation, bp.PrecalculatedPaths)
		{
			Blueprint = bp;
			_screen = scrn;
			_muted = scrn.IsPreview;

			LaserSource = scrn.LaserNetwork.AddSource(this);

			coreImage = FloatMath.GetRangedIntRandom(0, Textures.CANNONCORE_COUNT);
			coreRotation = FloatMath.GetRangedRandom(FloatMath.RAD_POS_000, FloatMath.RAD_POS_360);

			_soundeffect = MainGame.Inst.GDSound.GetEffectLaser(this);
			_soundeffect.IsLooped = true;
		}

		protected override void CreatePhysics()
		{
			PhysicsBody = BodyFactory.CreateBody(this.GDManager().PhysicsWorld, ConvertUnits2.ToSimUnits(Position), 0, BodyType.Static);

			PhysicsFixtureBase = FixtureFactory.AttachCircle(
				ConvertUnits.ToSimUnits(Scale * CANNON_DIAMETER / 2), 1,
				PhysicsBody,
				Vector2.Zero, this);

			FixtureFactory.AttachRectangle(
				ConvertUnits.ToSimUnits(Scale * BARREL_WIDTH), ConvertUnits.ToSimUnits(Scale * LASER_BARREL_HEIGHT), 1,
				new Vector2(ConvertUnits.ToSimUnits(Scale * CANNON_DIAMETER / 2), 0),
				PhysicsBody, this);
		}

		protected override void OnDraw(IBatchRenderer sbatch)
		{
			CommonCannonRenderer.DrawLaserCannon_BG(sbatch, Position, Scale, Rotation.ActualValue);
		}

		protected override void OnDrawOrderedForegroundLayer(IBatchRenderer sbatch)
		{
			DrawCrosshair(sbatch);

			CommonCannonRenderer.DrawLaserCannon_FG(sbatch, Position, Scale, Rotation.ActualValue, Fraction.IsNeutral, CannonHealth.ActualValue, coreRotation, CorePulse.ActualValue, coreImage, Fraction.Color);

			DrawShield(sbatch);
		}

		protected override void OnUpdate(SAMTime gameTime, InputState istate)
		{
			controller.Update(gameTime, istate);

			bool change = Rotation.CUpdate(gameTime);
			if (change) {_screen.LaserNetwork.SemiDirty = true; ChargeTime = 0; }

			CrosshairSize.Update(gameTime);

			UpdatePhysicBodies();
			UpdateHealth(gameTime);
			UpdateBoost(gameTime);
			UpdateNetwork(gameTime);
			UpdateCore(gameTime);
			UpdateDamage(gameTime);
			UpdateShield(gameTime);

#if DEBUG
			if (IsMouseDownOnThis(istate) && DebugSettings.Get("AssimilateCannon"))
			{
				var bckp = DebugSettings.Get("ImmortalCannons");
				DebugSettings.SetManual("ImmortalCannons", false);

				while (Fraction.Type != FractionType.PlayerFraction)
					TakeDamage(this.GDOwner().GetPlayerFraction(), 1);

				DebugSettings.SetManual("ImmortalCannons", bckp);

				CannonHealth.SetForce(1f);
			}
			if (IsMouseDownOnThis(istate) && DebugSettings.Get("LooseCannon"))
			{
				var bckp = DebugSettings.Get("ImmortalCannons");
				DebugSettings.SetManual("ImmortalCannons", false);

				while (Fraction.Type != FractionType.ComputerFraction)
					TakeDamage(this.GDOwner().GetComputerFraction(), 1);

				DebugSettings.SetManual("ImmortalCannons", bckp);

				CannonHealth.SetForce(1f);
			}
			if (IsMouseDownOnThis(istate) && DebugSettings.Get("AbandonCannon"))
			{
				CannonHealth.SetForce(0f);
				SetFraction(Fraction.GetNeutral());
			}
#endif
		}

		private void UpdateCore(SAMTime gameTime)
		{
			if (CannonHealth.ActualValue < FULL_LASER_HEALTH || Fraction.IsNeutral)
			{
				CorePulse.Set(1);
			}
			else
			{
				CorePulse.Set(1 + FloatMath.Sin(gameTime.TotalElapsedSeconds * CORE_PULSE_FREQ) * CORE_PULSE);
			}
			CorePulse.Update(gameTime);

			if (CannonHealth.ActualValue < FULL_LASER_HEALTH || Fraction.IsNeutral || !LaserSource.LaserPowered)
			{
				if (LaserPulseTime > 0f) LaserPulseTime = FloatMath.LimitedDec(LaserPulseTime, gameTime.ElapsedSeconds, 0f);
			}
			else
			{
				LaserPulseTime += gameTime.ElapsedSeconds;
			}
			
			if (controller.DoBarrelRecharge()) ChargeTime += gameTime.ElapsedSeconds;

			if (CannonHealth.ActualValue < 1) ChargeTime = 0;
		}

		private void UpdateNetwork(SAMTime gameTime)
		{
			bool active = CannonHealth.TargetValue >= FULL_LASER_HEALTH;

			LaserSource.SetState(active, Fraction, Rotation.ActualValue, ChargeTime > LASER_CHARGE_COOLDOWN);

			if (!_muted && MainGame.Inst.Profile.EffectsEnabled)
			{
				if ( LaserSource.LaserPowered && !_soundeffect.IsPlaying) _soundeffect.Play();
				if (!LaserSource.LaserPowered &&  _soundeffect.IsPlaying) _soundeffect.Stop();
			}
		}

		private void UpdateDamage(SAMTime gameTime)
		{
			if (!LaserSource.LaserActive) return;

			foreach (var ray in LaserSource.Lasers)
			{
				if (ray.Terminator != LaserRayTerminator.Target &&
				    ray.Terminator != LaserRayTerminator.LaserSelfTerm &&
				    ray.Terminator != LaserRayTerminator.LaserFaultTerm &&
				    ray.Terminator != LaserRayTerminator.LaserMultiTerm &&
				    ray.Terminator != LaserRayTerminator.BulletTerm) continue;

				if (ray.Terminator == LaserRayTerminator.BulletTerm && LaserSource.LaserPowered)
				{
					ray.TerminatorBullet.PhysicsBody.ApplyForce((ray.End - ray.Start).WithLength(RAY_FORCE));
				}
				
				if (ray.TargetCannon == null) continue;

				if (ray.TargetCannon.Fraction == Fraction)
				{
					if (ray.TargetCannon == this) continue; // stop touching yourself
					
					if (!LaserSource.LaserPowered) continue;

					ray.TargetCannon.ApplyLaserBoost(this, Fraction.LaserMultiplicator * gameTime.ElapsedSeconds * LASER_BOOST_PER_SECOND);
				}
				else
				{
					var dmg = Fraction.LaserMultiplicator * gameTime.ElapsedSeconds * LASER_DAMAGE_PER_SECOND;

					if (!LaserSource.LaserPowered || ray.Terminator != LaserRayTerminator.Target) dmg = 0;
					
					ray.TargetCannon.TakeLaserDamage(Fraction, ray, dmg);
				}
				
			}
		}
		
		public override void ApplyBoost()
		{
			if (Fraction.IsNeutral) return;

			CannonHealth.Inc(HEALTH_HIT_GEN);
			if (CannonHealth.Limit(0f, 1f) == 1)
			{
				AddOperation(new CannonBooster(1 / (BOOSTER_LIFETIME_MULTIPLIER * Fraction.LaserMultiplicator)));
			}
		}

		public override void ResetChargeAndBooster()
		{
			AbortAllOperations(o => o is CannonBooster);
		}

		public override void ForceResetBarrelCharge()
		{
			ChargeTime = 0f;
		}

		public override KIController CreateKIController(GDGameScreen screen, Fraction fraction)
		{
			return new LaserKIController(screen, this, fraction);
		}

		public void RemoteUpdate(Fraction frac, float hp, byte boost, float chrg, float shield, float sendertime)
		{
			if (frac != Fraction) SetFraction(frac);

			ManualBoost = boost;

			var delta = GDOwner.LevelTime - sendertime;

			ChargeTime = chrg + delta;

			CannonHealth.Set(hp);
			ShieldTime = shield;

			var ups = delta / (1 / 30f);

			if (ups > 1)
			{
				var iups = FloatMath.Min(FloatMath.Round(ups), 10);
				var gt30 = new SAMTime((delta / iups) * GDOwner.GameSpeed, MonoSAMGame.CurrentTime.TotalElapsedSeconds);

				for (int i = 0; i < iups; i++)
				{
					RemoteUpdateSim(gt30);
				}
			}
		}

		private void RemoteUpdateSim(SAMTime gameTime)
		{
			CannonHealth.Update(gameTime);

			if (CannonHealth.TargetValue < 1 && CannonHealth.TargetValue > MIN_REGEN_HEALTH && (LastAttackingLasersEnemy <= LastAttackingLasersFriends))
			{
				var bonus = START_HEALTH_REGEN + (END_HEALTH_REGEN - START_HEALTH_REGEN) * CannonHealth.TargetValue;

				bonus /= Scale;

				CannonHealth.Inc(bonus * gameTime.ElapsedSeconds);
				CannonHealth.Limit(0f, 1f);
			}

			if (LastAttackingShieldLaser > 0)
			{
				ShieldTime += (SHIELD_CHARGE_SPEED) * gameTime.ElapsedSeconds;
				if (ShieldTime > MAX_SHIELD_TIME) ShieldTime = MAX_SHIELD_TIME;
			}
			else
			{
				ShieldTime -= (SHIELD_DISCHARGE_SPEED) * gameTime.ElapsedSeconds;
				if (ShieldTime < 0) ShieldTime = 0;
			}
		}

		public override AbstractFractionController CreateNeutralController(GDGameScreen screen, Fraction fraction)
		{
			return new EmptyController(screen, this, fraction);
		}

		public override void SetFractionAndHealth(Fraction fraction, float hp)
		{
			SetFraction(fraction);
			CannonHealth.Set(hp);
			CannonHealth.Limit(0f, 1f);
		}
	}
}
