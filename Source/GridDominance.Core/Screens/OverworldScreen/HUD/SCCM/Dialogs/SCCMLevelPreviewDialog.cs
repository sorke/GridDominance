﻿using System;
using System.IO;
using System.Threading.Tasks;
using GridDominance.Levelfileformat.Blueprint;
using GridDominance.Shared.Resources;
using GridDominance.Shared.Screens.NormalGameScreen.Fractions;
using GridDominance.Shared.SCCM;
using Microsoft.Xna.Framework;
using MonoSAMFramework.Portable;
using MonoSAMFramework.Portable.ColorHelper;
using MonoSAMFramework.Portable.Extensions;
using MonoSAMFramework.Portable.GameMath.Geometry;
using MonoSAMFramework.Portable.Localization;
using MonoSAMFramework.Portable.LogProtocol;
using MonoSAMFramework.Portable.RenderHelper;
using MonoSAMFramework.Portable.Screens.HUD.Elements.Button;
using MonoSAMFramework.Portable.Screens.HUD.Elements.Container;
using MonoSAMFramework.Portable.Screens.HUD.Elements.Primitives;
using MonoSAMFramework.Portable.Screens.HUD.Enums;
using MonoSAMFramework.Portable.UpdateAgents.Impl;

namespace GridDominance.Shared.Screens.OverworldScreen.HUD.SCCM.Dialogs
{
	public class SCCMLevelPreviewDialog : HUDRoundedPanel
	{
		private const float TW = GDConstants.TILE_WIDTH;

		public const float WIDTH = 14 * TW;
		public const float HEIGHT = 8 * TW;

		private enum DownloadState { Initial, Downloading, Error, Finished, CacheUpdate }

		public override int Depth => 0;

		private volatile LevelBlueprint _blueprint = null;

		private volatile SCCMLevelMeta _meta = null;

		private DownloadState _downloadState = DownloadState.Initial;

		private HUDEllipseImageButton _btnStar;
		private HUDEllipseImageButton _btnPlay0;
		private HUDEllipseImageButton _btnPlay1;
		private HUDEllipseImageButton _btnPlay2;
		private HUDEllipseImageButton _btnPlay3;
		private HUDLabel              _lblStar;

		public SCCMLevelPreviewDialog(SCCMLevelMeta meta, LevelBlueprint lvl = null)
		{
			_meta  = meta;
			_blueprint = lvl;
			
			RelativePosition = FPoint.Zero;
			Size = new FSize(WIDTH, HEIGHT);
			Alignment = HUDAlignment.CENTER;
			Background = FlatColors.BackgroundHUD;
		}

		public override void OnInitialize()
		{
			#region Header
			
			AddElement(new HUDRectangle(-99)
			{
				Alignment = HUDAlignment.TOPCENTER,
				RelativePosition = FPoint.Zero,
				Size = new FSize(WIDTH, 1.40f*TW),

				Definition = HUDBackgroundDefinition.CreateRounded(FlatColors.BackgroundHUD.Darken(0.9f), 16, true, true, false, false),
			});

			AddElement(new HUDLabel
			{
				TextAlignment = HUDAlignment.CENTER,
				Alignment = HUDAlignment.TOPCENTER,
				RelativePosition = new FPoint(0, 0),
				Size = new FSize(WIDTH, 1.40f*TW),

				Font = Textures.HUDFontBold,
				FontSize = TW,

				Text = _meta?.LevelName ?? _blueprint.FullName,
				WordWrap = HUDWordWrap.Ellipsis,
				TextColor = Color.White,
			});
			
			var starred = MainGame.Inst.Profile.HasCustomLevelStarred(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID);
			var mylevel = ((_meta?.UserID ?? _blueprint.CustomMeta_UserID) == MainGame.Inst.Profile.OnlineUserID);

			AddElement(_btnStar = new HUDEllipseImageButton
			{
				Alignment = HUDAlignment.TOPRIGHT,
				RelativePosition = new FPoint(0, 0),
				Size = new FSize(64, 64),

				Image = Textures.TexIconStar,
				BackgroundNormal   = Color.Transparent,
				BackgroundPressed  = FlatColors.ButtonPressedHUD,
				ImageColor         = (starred || mylevel) ? FlatColors.SunFlower : FlatColors.Silver,
				ImageAlignment = HUDImageAlignmentAlgorithm.CENTER,
				ImageScale     = HUDImageScaleAlgorithm.STRETCH,

				Click = (s, a) => ToggleStar(),

				IsEnabled = ((_meta?.UserID ?? _blueprint.CustomMeta_UserID) != MainGame.Inst.Profile.OnlineUserID) && (MainGame.Inst.Profile.HasCustomLevelBeaten(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID)),
			});
			
			AddElement(_lblStar = new HUDLabel
			{
				Alignment = HUDAlignment.TOPRIGHT,
				RelativePosition = new FPoint(0, 55),
				Size = new FSize(64, 32),

				Font = Textures.HUDFontRegular,
				FontSize = 24,

				Text = (_meta==null) ? "?" : _meta.Stars.ToString(),
				WordWrap = HUDWordWrap.NoWrap,
				TextColor = (starred || mylevel) ? FlatColors.SunFlower : FlatColors.Silver,
				TextAlignment = HUDAlignment.TOPCENTER,
			});

			#endregion

			#region Tab Header

			AddElement(new HUDLabel
			{
				TextAlignment = HUDAlignment.BOTTOMLEFT,
				Alignment = HUDAlignment.TOPLEFT,
				RelativePosition = new FPoint(1*TW, 1*TW),
				Size = new FSize(3*TW, TW),

				Font = Textures.HUDFontBold,
				FontSize = 32,

				L10NText = L10NImpl.STR_INF_YOU,
				WordWrap = HUDWordWrap.Ellipsis,
				TextColor = FlatColors.Clouds,
			});

			AddElement(new HUDLabel
			{
				TextAlignment = HUDAlignment.BOTTOMLEFT,
				Alignment = HUDAlignment.TOPLEFT,
				RelativePosition = new FPoint(4*TW, 1*TW),
				Size = new FSize(6*TW, TW),

				Font = Textures.HUDFontBold,
				FontSize = 32,

				L10NText = L10NImpl.STR_INF_HIGHSCORE,
				WordWrap = HUDWordWrap.Ellipsis,
				TextColor = FlatColors.Clouds,
			});

			AddElement(new HUDLabel
			{
				TextAlignment = HUDAlignment.BOTTOMLEFT,
				Alignment = HUDAlignment.TOPLEFT,
				RelativePosition = new FPoint(10*TW, 1*TW),
				Size = new FSize(3*TW, TW),

				Font = Textures.HUDFontBold,
				FontSize = 32,

				L10NText = L10NImpl.STR_INF_CLEARS,
				WordWrap = HUDWordWrap.Ellipsis,
				TextColor = FlatColors.Clouds,
			});

			#endregion

			#region Tab Col Images

			AddElement(new HUDImage
			{
				Alignment = HUDAlignment.TOPLEFT,
				RelativePosition = new FPoint(8, 2*TW + 16 + (48+16)*0 + 8),
				Size = new FSize(48, 48),

				Image = Textures.TexDifficultyLine0,
				Color = MainGame.Inst.Profile.HasCustomLevelBeaten(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID, FractionDifficulty.DIFF_0) ? FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_0) : FlatColors.Silver,
				ImageAlignment = HUDImageAlignmentAlgorithm.CENTER,
				ImageScale     = HUDImageScaleAlgorithm.STRETCH,
			});

			AddElement(new HUDImage
			{
				Alignment = HUDAlignment.TOPLEFT,
				RelativePosition = new FPoint(8, 2*TW + 16 + (48+16)*1 + 8),
				Size = new FSize(48, 48),

				Image = Textures.TexDifficultyLine1,
				Color = MainGame.Inst.Profile.HasCustomLevelBeaten(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID, FractionDifficulty.DIFF_1) ? FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_1) : FlatColors.Silver,
				ImageAlignment = HUDImageAlignmentAlgorithm.CENTER,
				ImageScale     = HUDImageScaleAlgorithm.STRETCH,
			});

			AddElement(new HUDImage
			{
				Alignment = HUDAlignment.TOPLEFT,
				RelativePosition = new FPoint(8, 2*TW + 16 + (48+16)*2 + 8),
				Size = new FSize(48, 48),

				Image = Textures.TexDifficultyLine2,
				Color = MainGame.Inst.Profile.HasCustomLevelBeaten(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID, FractionDifficulty.DIFF_2) ? FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_2) : FlatColors.Silver,
				ImageAlignment = HUDImageAlignmentAlgorithm.CENTER,
				ImageScale     = HUDImageScaleAlgorithm.STRETCH,
			});

			AddElement(new HUDImage
			{
				Alignment = HUDAlignment.TOPLEFT,
				RelativePosition = new FPoint(8, 2*TW + 16 + (48+16)*3 + 8),
				Size = new FSize(48, 48),

				Image = Textures.TexDifficultyLine3,
				Color = MainGame.Inst.Profile.HasCustomLevelBeaten(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID, FractionDifficulty.DIFF_3) ? FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_3) : FlatColors.Silver,
				ImageAlignment = HUDImageAlignmentAlgorithm.CENTER,
				ImageScale     = HUDImageScaleAlgorithm.STRETCH,
			});

			#endregion

			#region Tab Col 1

			AddElement(new HUDLabel
			{
				TextAlignment = HUDAlignment.CENTERLEFT,
				Alignment = HUDAlignment.TOPLEFT,
				RelativePosition = new FPoint(1*TW, 2*TW + 16 + (48+16)*0),
				Size = new FSize(3*TW, TW),

				Font = Textures.HUDFontRegular,
				FontSize = 32,

				Text = MainGame.Inst.Profile.GetCustomLevelTimeString(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID, FractionDifficulty.DIFF_0),
				WordWrap = HUDWordWrap.Ellipsis,
				TextColor = MainGame.Inst.Profile.HasCustomLevelBeaten(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID, FractionDifficulty.DIFF_0) ? FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_0) : FlatColors.TextHUD,
			});

			AddElement(new HUDLabel
			{
				TextAlignment = HUDAlignment.CENTERLEFT,
				Alignment = HUDAlignment.TOPLEFT,
				RelativePosition = new FPoint(1*TW, 2*TW + 16 + (48+16)*1),
				Size = new FSize(3*TW, TW),

				Font = Textures.HUDFontRegular,
				FontSize = 32,

				Text = MainGame.Inst.Profile.GetCustomLevelTimeString(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID, FractionDifficulty.DIFF_1),
				WordWrap = HUDWordWrap.Ellipsis,
				TextColor = MainGame.Inst.Profile.HasCustomLevelBeaten(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID, FractionDifficulty.DIFF_1) ? FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_1) : FlatColors.TextHUD,
			});

			AddElement(new HUDLabel
			{
				TextAlignment = HUDAlignment.CENTERLEFT,
				Alignment = HUDAlignment.TOPLEFT,
				RelativePosition = new FPoint(1*TW, 2*TW + 16 + (48+16)*2),
				Size = new FSize(3*TW, TW),

				Font = Textures.HUDFontRegular,
				FontSize = 32,

				Text = MainGame.Inst.Profile.GetCustomLevelTimeString(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID, FractionDifficulty.DIFF_2),
				WordWrap = HUDWordWrap.Ellipsis,
				TextColor = MainGame.Inst.Profile.HasCustomLevelBeaten(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID, FractionDifficulty.DIFF_2) ? FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_2) : FlatColors.TextHUD,
			});

			AddElement(new HUDLabel
			{
				TextAlignment = HUDAlignment.CENTERLEFT,
				Alignment = HUDAlignment.TOPLEFT,
				RelativePosition = new FPoint(1*TW, 2*TW + 16 + (48+16)*3),
				Size = new FSize(3*TW, TW),

				Font = Textures.HUDFontRegular,
				FontSize = 32,

				Text = MainGame.Inst.Profile.GetCustomLevelTimeString(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID, FractionDifficulty.DIFF_3),
				WordWrap = HUDWordWrap.Ellipsis,
				TextColor = MainGame.Inst.Profile.HasCustomLevelBeaten(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID, FractionDifficulty.DIFF_3) ? FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_3) : FlatColors.TextHUD,
			});
			
			#endregion

			#region Tab Col 2
			
			AddElement(new HUDLabel
			{
				TextAlignment = HUDAlignment.CENTERLEFT,
				Alignment = HUDAlignment.TOPLEFT,
				RelativePosition = new FPoint(4*TW, 2*TW + 16 + (48+16)*0),
				Size = new FSize(6*TW, TW),

				Font = Textures.HUDFontRegular,
				FontSize = 32,

				Text = _meta?.Highscores[(int)FractionDifficulty.DIFF_0].FormatHighscoreCell() ?? "",
				WordWrap = HUDWordWrap.Ellipsis,
				TextColor = MainGame.Inst.Profile.HasCustomLevelBeaten(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID, FractionDifficulty.DIFF_0) ? FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_0) : FlatColors.TextHUD,
			});
			
			AddElement(new HUDLabel
			{
				TextAlignment = HUDAlignment.CENTERLEFT,
				Alignment = HUDAlignment.TOPLEFT,
				RelativePosition = new FPoint(4*TW, 2*TW + 16 + (48+16)*1),
				Size = new FSize(6*TW, TW),

				Font = Textures.HUDFontRegular,
				FontSize = 32,

				Text = _meta?.Highscores[(int)FractionDifficulty.DIFF_1].FormatHighscoreCell() ?? "",
				WordWrap = HUDWordWrap.Ellipsis,
				TextColor = MainGame.Inst.Profile.HasCustomLevelBeaten(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID, FractionDifficulty.DIFF_1) ? FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_1) : FlatColors.TextHUD,
			});
			
			AddElement(new HUDLabel
			{
				TextAlignment = HUDAlignment.CENTERLEFT,
				Alignment = HUDAlignment.TOPLEFT,
				RelativePosition = new FPoint(4*TW, 2*TW + 16 + (48+16)*2),
				Size = new FSize(6*TW, TW),

				Font = Textures.HUDFontRegular,
				FontSize = 32,

				Text = _meta?.Highscores[(int)FractionDifficulty.DIFF_2].FormatHighscoreCell() ?? "",
				WordWrap = HUDWordWrap.Ellipsis,
				TextColor = MainGame.Inst.Profile.HasCustomLevelBeaten(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID, FractionDifficulty.DIFF_2) ? FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_2) : FlatColors.TextHUD,
			});
			
			AddElement(new HUDLabel
			{
				TextAlignment = HUDAlignment.CENTERLEFT,
				Alignment = HUDAlignment.TOPLEFT,
				RelativePosition = new FPoint(4*TW, 2*TW + 16 + (48+16)*3),
				Size = new FSize(6*TW, TW),

				Font = Textures.HUDFontRegular,
				FontSize = 32,

				Text = _meta?.Highscores[(int)FractionDifficulty.DIFF_3].FormatHighscoreCell() ?? "",
				WordWrap = HUDWordWrap.Ellipsis,
				TextColor = MainGame.Inst.Profile.HasCustomLevelBeaten(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID, FractionDifficulty.DIFF_3) ? FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_3) : FlatColors.TextHUD,
			});
			
			#endregion

			#region Tab Col 3
			
			AddElement(new HUDLabel
			{
				TextAlignment = HUDAlignment.CENTERLEFT,
				Alignment = HUDAlignment.TOPLEFT,
				RelativePosition = new FPoint(10*TW, 2*TW + 16 + (48+16)*0),
				Size = new FSize(3*TW, TW),

				Font = Textures.HUDFontRegular,
				FontSize = 32,

				Text = _meta?.Highscores[(int)FractionDifficulty.DIFF_0].FormatGlobalClearsCell() ?? "",
				WordWrap = HUDWordWrap.NoWrap,
				TextColor = MainGame.Inst.Profile.HasCustomLevelBeaten(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID, FractionDifficulty.DIFF_0) ? FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_0) : FlatColors.TextHUD,
			});
			
			AddElement(new HUDLabel
			{
				TextAlignment = HUDAlignment.CENTERLEFT,
				Alignment = HUDAlignment.TOPLEFT,
				RelativePosition = new FPoint(10*TW, 2*TW + 16 + (48+16)*1),
				Size = new FSize(3*TW, TW),

				Font = Textures.HUDFontRegular,
				FontSize = 32,

				Text = _meta?.Highscores[(int)FractionDifficulty.DIFF_1].FormatGlobalClearsCell() ?? "",
				WordWrap = HUDWordWrap.NoWrap,
				TextColor = MainGame.Inst.Profile.HasCustomLevelBeaten(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID, FractionDifficulty.DIFF_1) ? FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_1) : FlatColors.TextHUD,
			});
			
			AddElement(new HUDLabel
			{
				TextAlignment = HUDAlignment.CENTERLEFT,
				Alignment = HUDAlignment.TOPLEFT,
				RelativePosition = new FPoint(10*TW, 2*TW + 16 + (48+16)*2),
				Size = new FSize(3*TW, TW),

				Font = Textures.HUDFontRegular,
				FontSize = 32,

				Text = _meta?.Highscores[(int)FractionDifficulty.DIFF_2].FormatGlobalClearsCell() ?? "",
				WordWrap = HUDWordWrap.NoWrap,
				TextColor = MainGame.Inst.Profile.HasCustomLevelBeaten(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID, FractionDifficulty.DIFF_2) ? FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_2) : FlatColors.TextHUD,
			});
			
			AddElement(new HUDLabel
			{
				TextAlignment = HUDAlignment.CENTERLEFT,
				Alignment = HUDAlignment.TOPLEFT,
				RelativePosition = new FPoint(10*TW, 2*TW + 16 + (48+16)*3),
				Size = new FSize(3*TW, TW),

				Font = Textures.HUDFontRegular,
				FontSize = 32,

				Text = _meta?.Highscores[(int)FractionDifficulty.DIFF_3].FormatGlobalClearsCell() ?? "",
				WordWrap = HUDWordWrap.NoWrap,
				TextColor = MainGame.Inst.Profile.HasCustomLevelBeaten(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID, FractionDifficulty.DIFF_3) ? FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_3) : FlatColors.TextHUD,
			});
			
			#endregion
			
			#region Footer

			AddElement(new HUDRectangle
			{
				Alignment = HUDAlignment.BOTTOMCENTER,
				RelativePosition = FPoint.Zero,
				Size = new FSize(WIDTH, 1.5f*TW),

				Definition = HUDBackgroundDefinition.CreateRounded(FlatColors.BackgroundHUD2, 16, false, false, true, true),
			});

			AddElement(new HUDSeperator(HUDOrientation.Horizontal, 3)
			{
				Alignment = HUDAlignment.BOTTOMCENTER,
				RelativePosition = new FPoint(0, 1.5f*TW),
				Size = new FSize(WIDTH, HUD.PixelWidth),

				Color = FlatColors.SeperatorHUD,
			});

			AddElement(_btnPlay0 = new HUDEllipseImageButton
			{
				Alignment = HUDAlignment.BOTTOMCENTER,
				RelativePosition = new FPoint(-84/2 - 16 - 84 - 32, 6),
				Size = new FSize(84, 84),

				Image = Textures.TexDifficultyLine0,
				BackgroundNormal   = FlatColors.ButtonHUD,
				BackgroundPressed = FlatColors.ButtonPressedHUD,
				ImageColor = FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_0),
				ImageAlignment = HUDImageAlignmentAlgorithm.CENTER,
				ImageScale     = HUDImageScaleAlgorithm.STRETCH,

				Click = (s, a) => Play(FractionDifficulty.DIFF_0),
			});
			
			AddElement(_btnPlay1 = new HUDEllipseImageButton
			{
				Alignment = HUDAlignment.BOTTOMCENTER,
				RelativePosition = new FPoint(-84/2 - 16, 6),
				Size = new FSize(84, 84),

				Image = Textures.TexDifficultyLine1,
				BackgroundNormal   = FlatColors.ButtonHUD,
				BackgroundPressed = FlatColors.ButtonPressedHUD,
				ImageColor = FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_1),
				ImageAlignment = HUDImageAlignmentAlgorithm.CENTER,
				ImageScale     = HUDImageScaleAlgorithm.STRETCH,

				Click = (s, a) => Play(FractionDifficulty.DIFF_1),
			});
			
			AddElement(_btnPlay2 = new HUDEllipseImageButton
			{
				Alignment = HUDAlignment.BOTTOMCENTER,
				RelativePosition = new FPoint(+84/2 + 16, 6),
				Size = new FSize(84, 84),

				Image = Textures.TexDifficultyLine2,
				BackgroundNormal   = FlatColors.ButtonHUD,
				BackgroundPressed = FlatColors.ButtonPressedHUD,
				ImageColor = FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_2),
				ImageAlignment = HUDImageAlignmentAlgorithm.CENTER,
				ImageScale     = HUDImageScaleAlgorithm.STRETCH,

				Click = (s, a) => Play(FractionDifficulty.DIFF_2),
			});
			
			AddElement(_btnPlay3 = new HUDEllipseImageButton
			{
				Alignment = HUDAlignment.BOTTOMCENTER,
				RelativePosition = new FPoint(+84/2 + 16 + 84 + 32, 6),
				Size = new FSize(84, 84),

				Image = Textures.TexDifficultyLine3,
				BackgroundNormal   = FlatColors.ButtonHUD,
				BackgroundPressed = FlatColors.ButtonPressedHUD,
				ImageColor = FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_3),
				ImageAlignment = HUDImageAlignmentAlgorithm.CENTER,
				ImageScale     = HUDImageScaleAlgorithm.STRETCH,

				Click = (s, a) => Play(FractionDifficulty.DIFF_3),
			});
			
			AddElement(new HUDLabel
			{
				TextAlignment = HUDAlignment.BOTTOMRIGHT,
				Alignment = HUDAlignment.BOTTOMRIGHT,
				RelativePosition = new FPoint(4, 4),
				Size = new FSize(236, 32),

				Font = Textures.HUDFontRegular,
				FontSize = 32,

				Text = _meta?.Username ?? "???",
				WordWrap = HUDWordWrap.Ellipsis,
				TextColor = FlatColors.GreenSea,
			});

			#endregion

			if (_blueprint==null && _meta != null) 
				StartDownload();
			else if (_blueprint != null && _meta == null) 
				StartMetaUpdate();
			else if (_blueprint != null && _meta != null) 
				_downloadState = DownloadState.Finished;
			else
				SAMLog.Error("SCCMLPD::EnumSwitch_OI", $"_blueprint: '{_blueprint}' | _meta = '{_meta}'");
		}

		private void StartDownload()
		{
			_btnPlay0.ImageRotation = 0f;
			_btnPlay0.ImageRotationSpeed = 0.25f;
			_btnPlay0.Image = Textures.CannonCogBig;
			_btnPlay0.ImageColor = FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_0);
			_btnPlay0.ImagePadding = 8;

			_btnPlay1.ImageRotation = 0f;
			_btnPlay1.ImageRotationSpeed = 0.25f;
			_btnPlay1.Image = Textures.CannonCogBig;
			_btnPlay1.ImageColor = FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_1);
			_btnPlay1.ImagePadding = 8;

			_btnPlay2.ImageRotation = 0f;
			_btnPlay2.ImageRotationSpeed = 0.25f;
			_btnPlay2.Image = Textures.CannonCogBig;
			_btnPlay2.ImageColor = FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_2);
			_btnPlay2.ImagePadding = 8;

			_btnPlay3.ImageRotation = 0f;
			_btnPlay3.ImageRotationSpeed = 0.25f;
			_btnPlay3.Image = Textures.CannonCogBig;
			_btnPlay3.ImageColor = FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_3);
			_btnPlay3.ImagePadding = 8;

			StartDownloadLevel().RunAsync();
			_downloadState = DownloadState.Downloading;
		}

		private void StartMetaUpdate()
		{
			StartMetaUpdateLevel().RunAsync();
			_downloadState = DownloadState.CacheUpdate;
		}

		private async Task StartDownloadLevel()
		{
			try
			{
				var binary = await MainGame.Inst.Backend.DownloadUserLevel(MainGame.Inst.Profile, _meta.OnlineID);

				if (binary == null)
				{
					MonoSAMGame.CurrentInst.DispatchBeginInvoke(() =>
					{
						HUD.ShowToast("SCCMLPD::DF(MULTI)", L10N.T(L10NImpl.STR_SCCM_DOWNLOADFAILED), 40, FlatColors.Flamingo, FlatColors.Foreground, 3f);
						MonoSAMGame.CurrentInst.DispatchBeginInvoke(OnDownloadFailed);
					});
					return;
				}

				using (var ms = new MemoryStream(binary))
				{
					using (var br = new BinaryReader(ms))
					{
						var bp = new LevelBlueprint();
						bp.BinaryDeserialize(br);
						
						_blueprint = bp;
						
						MonoSAMGame.CurrentInst.DispatchBeginInvoke(OnDownloadSuccess);
					}
				}
			}
			catch (Exception e)
			{
				MonoSAMGame.CurrentInst.DispatchBeginInvoke(() =>
				{
					HUD.ShowToast("SCCMLPD::DF(MULTI)", L10N.T(L10NImpl.STR_SCCM_DOWNLOADFAILED), 40, FlatColors.Flamingo, FlatColors.Foreground, 3f);
				});

				MonoSAMGame.CurrentInst.DispatchBeginInvoke(OnDownloadFailed);
				
				SAMLog.Error("SCCMLPD:DownloadException", e);
			}
		}

		private async Task StartMetaUpdateLevel()
		{
			try
			{
				var meta = await MainGame.Inst.Backend.QueryUserLevelMeta(MainGame.Inst.Profile, _blueprint.CustomMeta_LevelID);

				if (meta == null)
				{
					MonoSAMGame.CurrentInst.DispatchBeginInvoke(() =>
					{
						HUD.ShowToast("SCCMLPD::CUF(MULTI)", L10N.T(L10NImpl.STR_SCCM_DOWNLOADFAILED), 40, FlatColors.Flamingo, FlatColors.Foreground, 3f);
						MonoSAMGame.CurrentInst.DispatchBeginInvoke(OnDownloadFailed);
					});
					return;
				}

				_meta = meta;
				MonoSAMGame.CurrentInst.DispatchBeginInvoke(OnCacheUpdateSuccess);
			}
			catch (Exception e)
			{
				MonoSAMGame.CurrentInst.DispatchBeginInvoke(() =>
				{
					HUD.ShowToast("SCCMLPD::CUF(MULTI)", L10N.T(L10NImpl.STR_SCCM_DOWNLOADFAILED), 40, FlatColors.Flamingo, FlatColors.Foreground, 3f);
				});

				MonoSAMGame.CurrentInst.DispatchBeginInvoke(OnDownloadFailed);
				
				SAMLog.Error("SCCMLPD:CacheUpdateException", e);
			}
		}

		private void OnDownloadFailed()
		{
			_downloadState = DownloadState.Error;
			
			_btnPlay0.ImageRotation = 0f;
			_btnPlay0.ImageRotationSpeed = 0f;
			_btnPlay0.Image = Textures.TexIconError;
			_btnPlay0.ImageColor = FlatColors.Pomegranate;
			_btnPlay0.ImagePadding = 0;

			_btnPlay1.ImageRotation = 0f;
			_btnPlay1.ImageRotationSpeed = 0f;
			_btnPlay1.Image = Textures.TexIconError;
			_btnPlay1.ImageColor = FlatColors.Pomegranate;
			_btnPlay1.ImagePadding = 0;

			_btnPlay2.ImageRotation = 0f;
			_btnPlay2.ImageRotationSpeed = 0f;
			_btnPlay2.Image = Textures.TexIconError;
			_btnPlay2.ImageColor = FlatColors.Pomegranate;
			_btnPlay2.ImagePadding = 0;

			_btnPlay3.ImageRotation = 0f;
			_btnPlay3.ImageRotationSpeed = 0f;
			_btnPlay3.Image = Textures.TexIconError;
			_btnPlay3.ImageColor = FlatColors.Pomegranate;
			_btnPlay3.ImagePadding = 0;
		}

		private void OnCacheUpdateSuccess()
		{
			Remove();
			HUD.AddModal(new SCCMLevelPreviewDialog(_meta, _blueprint), true, 0.5f, 0f);
		}

		private void OnDownloadSuccess()
		{
			_downloadState = DownloadState.Finished;

			AddOperationDelayed(new SingleLambdaOperation<SCCMLevelPreviewDialog>("FinishButton0", e =>
			{
				_btnPlay0.ImageRotation = 0f;
				_btnPlay0.ImageRotationSpeed = 0f;
				_btnPlay0.Image = Textures.TexDifficultyLine0;
				_btnPlay0.ImageColor = FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_0);
				_btnPlay0.ImagePadding = 8;
			}), 0 * 0.150f);
			
			AddOperationDelayed(new SingleLambdaOperation<SCCMLevelPreviewDialog>("FinishButton1", e =>
			{
				_btnPlay1.ImageRotation = 0f;
				_btnPlay1.ImageRotationSpeed = 0f;
				_btnPlay1.Image = Textures.TexDifficultyLine1;
				_btnPlay1.ImageColor = FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_1);
				_btnPlay1.ImagePadding = 8;
			}), 1 * 0.150f);
			
			AddOperationDelayed(new SingleLambdaOperation<SCCMLevelPreviewDialog>("FinishButton2", e =>
			{
				_btnPlay2.ImageRotation = 0f;
				_btnPlay2.ImageRotationSpeed = 0f;
				_btnPlay2.Image = Textures.TexDifficultyLine2;
				_btnPlay2.ImageColor = FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_2);
				_btnPlay2.ImagePadding = 8;
			}), 2 * 0.150f);
			
			AddOperationDelayed(new SingleLambdaOperation<SCCMLevelPreviewDialog>("FinishButton3", e =>
			{
				_btnPlay3.ImageRotation = 0f;
				_btnPlay3.ImageRotationSpeed = 0f;
				_btnPlay3.Image = Textures.TexDifficultyLine3;
				_btnPlay3.ImageColor = FractionDifficultyHelper.GetColor(FractionDifficulty.DIFF_3);
				_btnPlay3.ImagePadding = 8;
			}), 3 * 0.150f);
		}

		private void ToggleStar()
		{
			if (_btnStar.Image == Textures.CannonCogBig) return;
			if (_meta?.UserID == MainGame.Inst.Profile.OnlineUserID) return;
			if (!MainGame.Inst.Profile.HasCustomLevelBeaten(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID)) return;

			_btnStar.Image = Textures.CannonCogBig;
			_btnStar.ImageColor = FlatColors.Clouds;
			_btnStar.ImageRotation = 0f;
			_btnStar.ImageRotationSpeed = 0.5f;

			DoToggleStar(!MainGame.Inst.Profile.HasCustomLevelStarred(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID)).RunAsync();
		}

		private void Play(FractionDifficulty d)
		{
			if (_downloadState == DownloadState.Initial)
			{
				return;
			}
			else if (_downloadState == DownloadState.Error)
			{
				StartDownload();
				return;
			}
			else if (_downloadState == DownloadState.Downloading)
			{
				MonoSAMGame.CurrentInst.DispatchBeginInvoke(() =>
				{
					HUD.ShowToast(null, L10N.T(L10NImpl.STR_SCCM_DOWNLOADINPROGRESS), 32, FlatColors.Orange, FlatColors.Foreground, 1f);
				});
				return;
			}
			else if (_downloadState == DownloadState.Finished || _downloadState == DownloadState.CacheUpdate)
			{
				if (GDConstants.LevelIntVersion < _blueprint.CustomMeta_MinLevelIntVersion)
				{
					HUD.ShowToast(null, L10N.T(L10NImpl.STR_SCCM_VERSIONTOOOLD), 32, FlatColors.SunFlower, FlatColors.Foreground, 3f);
					return;
				}

				MainGame.Inst.Backend.SetCustomLevelPlayed(MainGame.Inst.Profile, _blueprint.CustomMeta_LevelID, d).RunAsync();

				MainGame.Inst.SetCustomLevelScreen(_blueprint, d);
			}
			else
			{
				SAMLog.Error("SCCMLPD::EnumSwitch_PLAY", "_downloadState: " + _downloadState);
			}
		}

		private async Task DoToggleStar(bool v)
		{
			// [Request] -> [Set icon to spinner] -> [Response] -> [Save Profile] -> [update icon]

			var r = await MainGame.Inst.Backend.SetCustomLevelStarred(MainGame.Inst.Profile, _meta?.OnlineID ?? _blueprint.CustomMeta_LevelID, v);

			MainGame.Inst.DispatchBeginInvoke(() => 
			{
				if (r != null)
				{
					_btnStar.Image = Textures.TexIconStar;
					_btnStar.ImageColor = (r.Item2) ? FlatColors.SunFlower : FlatColors.Silver;
					_btnStar.ImageRotation = 0f;
					_btnStar.ImageRotationSpeed = 0f;

					_lblStar.Text = r.Item1.ToString();
					_lblStar.TextColor = (r.Item2) ? FlatColors.SunFlower : FlatColors.Silver;
				}
				else
				{
					_btnStar.Image = Textures.TexIconStar;
					_btnStar.ImageColor = MainGame.Inst.Profile.HasCustomLevelStarred(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID) ? FlatColors.SunFlower : FlatColors.Silver;
					_btnStar.ImageRotation = 0f;
					_btnStar.ImageRotationSpeed = 0f;

					_lblStar.Text = (_meta==null) ? "???" : _meta.Stars.ToString();
					_lblStar.TextColor = MainGame.Inst.Profile.HasCustomLevelStarred(_meta?.OnlineID ?? _blueprint.CustomMeta_LevelID) ? FlatColors.SunFlower : FlatColors.Silver;
				}

			});
		}
	}
}
