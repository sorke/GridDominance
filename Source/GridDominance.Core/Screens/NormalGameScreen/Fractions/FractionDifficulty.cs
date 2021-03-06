﻿using System;
using GridDominance.Shared.Resources;
using Microsoft.Xna.Framework;
using MonoSAMFramework.Portable.BatchRenderer.TextureAtlases;
using MonoSAMFramework.Portable.ColorHelper;
using MonoSAMFramework.Portable.LogProtocol;
using MonoSAMFramework.Portable.Localization;

namespace GridDominance.Shared.Screens.NormalGameScreen.Fractions
{
	public enum FractionDifficulty
	{
		KI_EASY       = 0x00, // Can be used as Array indizies
		KI_NORMAL     = 0x01,
		KI_HARD       = 0x02,
		KI_IMPOSSIBLE = 0x03,

		NEUTRAL       = 0x10,
		PLAYER        = 0x11,


		DIFF_0        = KI_EASY,
		DIFF_1        = KI_NORMAL,
		DIFF_2        = KI_HARD,
		DIFF_3        = KI_IMPOSSIBLE,
	}

	public static class FractionDifficultyHelper
	{
		public static Color COLOR_DIFFICULTY_0 => MainGame.Inst.Profile.ColorblindMode ? ColorblindColors.SkyBlue     : FlatColors.PeterRiver;
		public static Color COLOR_DIFFICULTY_1 => MainGame.Inst.Profile.ColorblindMode ? ColorblindColors.BluishGreen : FlatColors.Nephritis;
		public static Color COLOR_DIFFICULTY_2 => MainGame.Inst.Profile.ColorblindMode ? ColorblindColors.Yellow      : FlatColors.SunFlower;
		public static Color COLOR_DIFFICULTY_3 => MainGame.Inst.Profile.ColorblindMode ? ColorblindColors.Vermillon   : FlatColors.Pomegranate;

		public const float MULTIPLICATOR_B_PLAYER     = GDConstants.MULTIPLICATOR_BULLET_PLAYER;
		public const float MULTIPLICATOR_B_NEUTRAL    = GDConstants.MULTIPLICATOR_BULLET_NEUTRAL;
		public const float MULTIPLICATOR_B_COMPUTER_S = GDConstants.MULTIPLICATOR_BULLET_SUPERSLOW;
		public const float MULTIPLICATOR_B_COMPUTER_0 = GDConstants.MULTIPLICATOR_BULLET_COMPUTER_0;
		public const float MULTIPLICATOR_B_COMPUTER_1 = GDConstants.MULTIPLICATOR_BULLET_COMPUTER_1;
		public const float MULTIPLICATOR_B_COMPUTER_2 = GDConstants.MULTIPLICATOR_BULLET_COMPUTER_2;
		public const float MULTIPLICATOR_B_COMPUTER_3 = GDConstants.MULTIPLICATOR_BULLET_COMPUTER_3;

		public const float MULTIPLICATOR_L_PLAYER     = GDConstants.MULTIPLICATOR_LASER_PLAYER;
		public const float MULTIPLICATOR_L_NEUTRAL    = GDConstants.MULTIPLICATOR_LASER_NEUTRAL;
		public const float MULTIPLICATOR_L_COMPUTER_0 = GDConstants.MULTIPLICATOR_LASER_COMPUTER_0;
		public const float MULTIPLICATOR_L_COMPUTER_1 = GDConstants.MULTIPLICATOR_LASER_COMPUTER_1;
		public const float MULTIPLICATOR_L_COMPUTER_2 = GDConstants.MULTIPLICATOR_LASER_COMPUTER_2;
		public const float MULTIPLICATOR_L_COMPUTER_3 = GDConstants.MULTIPLICATOR_LASER_COMPUTER_3;

		private const int SCORE_DIFF_0 = GDConstants.SCORE_DIFF_0;
		private const int SCORE_DIFF_1 = GDConstants.SCORE_DIFF_1;
		private const int SCORE_DIFF_2 = GDConstants.SCORE_DIFF_2;
		private const int SCORE_DIFF_3 = GDConstants.SCORE_DIFF_3;

		public static float GetBulletMultiplicator(FractionDifficulty d, bool slow)
		{
			switch (d)
			{
				case FractionDifficulty.PLAYER:
					return MULTIPLICATOR_B_PLAYER;

				case FractionDifficulty.NEUTRAL:
					return MULTIPLICATOR_B_NEUTRAL;

				case FractionDifficulty.KI_EASY:
					return slow ? MULTIPLICATOR_B_COMPUTER_S : MULTIPLICATOR_B_COMPUTER_0;

				case FractionDifficulty.KI_NORMAL:
					return MULTIPLICATOR_B_COMPUTER_1;

				case FractionDifficulty.KI_HARD:
					return MULTIPLICATOR_B_COMPUTER_2;

				case FractionDifficulty.KI_IMPOSSIBLE:
					return MULTIPLICATOR_B_COMPUTER_3;

				default:
					SAMLog.Error("EnumSwitch_GBM", "GetBulletMultiplicator()", "FractionDifficultyHelper.GetMultiplicator -> " + d);
					throw new ArgumentOutOfRangeException(nameof(d), d, null);
			}
		}

		public static float GetLaserMultiplicator(FractionDifficulty d)
		{
			switch (d)
			{
				case FractionDifficulty.PLAYER:
					return MULTIPLICATOR_L_PLAYER;

				case FractionDifficulty.NEUTRAL:
					return MULTIPLICATOR_L_NEUTRAL;

				case FractionDifficulty.KI_EASY:
					return MULTIPLICATOR_L_COMPUTER_0;

				case FractionDifficulty.KI_NORMAL:
					return MULTIPLICATOR_L_COMPUTER_1;

				case FractionDifficulty.KI_HARD:
					return MULTIPLICATOR_L_COMPUTER_2;

				case FractionDifficulty.KI_IMPOSSIBLE:
					return MULTIPLICATOR_L_COMPUTER_3;

				default:
					SAMLog.Error("EnumSwitch_GLM", "GetBulletMultiplicator()", "FractionDifficultyHelper.GetMultiplicator -> " + d);
					throw new ArgumentOutOfRangeException(nameof(d), d, null);
			}
		}

		public static string GetShortIdentifier(FractionDifficulty d)
		{
			switch (d)
			{
				case FractionDifficulty.PLAYER:
					return "P";
				case FractionDifficulty.NEUTRAL:
					return "N";
				case FractionDifficulty.KI_EASY:
					return "C0";
				case FractionDifficulty.KI_NORMAL:
					return "C1";
				case FractionDifficulty.KI_HARD:
					return "C2";
				case FractionDifficulty.KI_IMPOSSIBLE:
					return "C3";
				default:
					SAMLog.Error("EnumSwitch_GSI", "GetScore()", "FractionDifficultyHelper.GetShortIdentifier -> " + d);
					throw new ArgumentOutOfRangeException(nameof(d), d, null);
			}
		}

		public static TextureRegion2D GetIcon(FractionDifficulty d)
		{
			switch (d)
			{
				case FractionDifficulty.KI_EASY:
					return Textures.TexDifficultyLine0;
				case FractionDifficulty.KI_NORMAL:
					return Textures.TexDifficultyLine1;
				case FractionDifficulty.KI_HARD:
					return Textures.TexDifficultyLine2;
				case FractionDifficulty.KI_IMPOSSIBLE:
					return Textures.TexDifficultyLine3;
				case FractionDifficulty.NEUTRAL:
				case FractionDifficulty.PLAYER:
				default:
					SAMLog.Error("FRACDIFF::EnumSwitch_GI", "value = " + d);
					break;
			}

			SAMLog.Error("EnumSwitch_GI", "GetScore()", "FractionDifficultyHelper.GetIcon -> " + d);
			return null;
		}

		public static Color GetColor(FractionDifficulty d)
		{
			switch (d)
			{
				case FractionDifficulty.KI_EASY:
					return COLOR_DIFFICULTY_0;
				case FractionDifficulty.KI_NORMAL:
					return COLOR_DIFFICULTY_1;
				case FractionDifficulty.KI_HARD:
					return COLOR_DIFFICULTY_2;
				case FractionDifficulty.KI_IMPOSSIBLE:
					return COLOR_DIFFICULTY_3;
				case FractionDifficulty.NEUTRAL:
				case FractionDifficulty.PLAYER:
				default:
					SAMLog.Error("FRACDIFF::EnumSwitch_GC", "value = " + d);
					return Color.Magenta;
			}
		}

		public static string GetDescription(FractionDifficulty d)
		{
			switch (d)
			{
				case FractionDifficulty.DIFF_0:
					return L10N.T(L10NImpl.STR_DIFF_0);
				case FractionDifficulty.DIFF_1:
					return L10N.T(L10NImpl.STR_DIFF_1);
				case FractionDifficulty.DIFF_2:
					return L10N.T(L10NImpl.STR_DIFF_2);
				case FractionDifficulty.DIFF_3:
					return L10N.T(L10NImpl.STR_DIFF_3);
				case FractionDifficulty.NEUTRAL:
				case FractionDifficulty.PLAYER:
				default:
					SAMLog.Error("FRACDIFF::EnumSwitch_GD", "value = " + d);
					return "?";
			}
		}

		public static int GetScore(FractionDifficulty d)
		{
			switch (d)
			{
				case FractionDifficulty.KI_EASY:
					return SCORE_DIFF_0;
				case FractionDifficulty.KI_NORMAL:
					return SCORE_DIFF_1;
				case FractionDifficulty.KI_HARD:
					return SCORE_DIFF_2;
				case FractionDifficulty.KI_IMPOSSIBLE:
					return SCORE_DIFF_3;
				case FractionDifficulty.NEUTRAL:
				case FractionDifficulty.PLAYER:
				default:
					SAMLog.Error("FRACDIFF::EnumSwitch_GS", "value = " + d);
					return 0;
			}
		}
	}
}
