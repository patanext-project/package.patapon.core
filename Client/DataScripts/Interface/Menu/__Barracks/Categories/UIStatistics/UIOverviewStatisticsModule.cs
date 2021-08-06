using System;
using System.Collections.Generic;
using System.Globalization;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.Module.Simulation.Components.GamePlay.Units;
using PataNext.UnityCore.Utilities;
using StormiumTeam.GameBase.Utility.Rendering.BaseSystems;
using StormiumTeam.Shared;
using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PataNext.Client.DataScripts.Interface.Menu.__Barracks.Categories
{
	public class UIOverviewStatisticsModule : UIOverviewModuleBase
	{
		[SerializeField]
		private GameObject fullViewPrefab;

		private GameObject fullView;

		[Serializable]
		public struct MiniStats
		{
			public UILabel<int>   healthLabel;
			public UILabel<int>   defenseLabel;
			public UILabel<int>   strengthLabel;
			public UILabel<float> attackSpeedLabel;

			public StatusEffectTableRow[] rows;
		}

		public MiniStats miniStats;

		public override void OnBackendSet()
		{
			base.OnBackendSet();
			
			miniStats.attackSpeedLabel.Format = f => f.ToString("F2", CultureInfo.InvariantCulture);
			
			Debug.Assert(miniStats.rows.Length == 8, "miniStats.rows.Length == 8");
		}

		public override bool Enter()
		{
			fullView = Instantiate(fullViewPrefab, Data.RootTransform, false);
			return true;
		}

		public override void ForceExit()
		{
			exit();
		}

		private void exit()
		{
			Destroy(fullView);
			Data.QuitView();
		}
		
		class RenderSystem : BaseRenderSystem<UIOverviewStatisticsModule>
		{
			private bool   exitRequested;
			private Entity previousStatsEntity;
			
			private StatusEffectManager statusEffectManager;
			private LocalizationSystem  localizationSystem;

			private List<UnitReadStatusEffectSettings> unitStatusEffectList;

			private static string[] constraint = new[]
			{
				"critical",
				"piercing",
				"knockback",
				"stagger",
				"burn",
				"sleep",
				"freeze",
				"poison"
			};

			private Localization statusEffectsLocal;
			
			protected override void OnCreate()
			{
				base.OnCreate();

				statusEffectManager = World.GetExistingSystem<StatusEffectManager>();
				localizationSystem  = World.GetExistingSystem<LocalizationSystem>();

				statusEffectsLocal = localizationSystem.LoadLocal("status_effects");
				
				unitStatusEffectList = new List<UnitReadStatusEffectSettings>();
			}

			protected override void PrepareValues()
			{
				var inputSystem = EventSystem.current.GetComponent<StandaloneInputModule>();
				exitRequested = Input.GetButton(inputSystem.cancelButton);
			}

			protected override void Render(UIOverviewStatisticsModule definition)
			{
				if (!EntityManager.TryGetComponentData(definition.Data.Entity, out UnitStatistics stats))
					return;

				definition.miniStats.healthLabel.Value      = stats.Health;
				definition.miniStats.defenseLabel.Value     = stats.Defense;
				definition.miniStats.strengthLabel.Value    = stats.Attack;
				definition.miniStats.attackSpeedLabel.Value = stats.AttackSpeed;

				unitStatusEffectList.Clear();
				statusEffectManager.ReadSettings(definition.Data.Entity, unitStatusEffectList, constraint);

				for (var i = 0; i < definition.miniStats.rows.Length; i++)
				{
					var row = definition.miniStats.rows[i];
					if (i >= unitStatusEffectList.Count)
					{
						row.gameObject.SetActive(false);
						continue;
					}

					var settings = unitStatusEffectList[i];
					row.gameObject.SetActive(true);

					row.background.color = statusEffectManager.GetColor(settings.Id);
					row.icon.sprite      = statusEffectManager.GetSprite(settings.Id);
					row.category.text    = statusEffectsLocal[settings.Id, "Name"];
					row.power.Value      = (int)settings.Power;
					row.resistance.Value = (int)settings.Resistance;
					row.gain.Value       = settings.RegenPerSecond;
					row.immunity.Value   = settings.Immunity;
				}

				if (!definition.IsActive)
					return;

				if (exitRequested)
					definition.exit();
			}

			protected override void ClearValues()
			{
				exitRequested = false;
			}
		}
	}
}