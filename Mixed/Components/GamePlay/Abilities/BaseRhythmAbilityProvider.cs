using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Systems.GamePlay;
using DefaultNamespace;
using Patapon4TLB.Default.Player;
using Revolution;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Patapon.Mixed.GamePlay.Abilities
{
	public class AbilityRegisterSystem : ComponentSystem
	{
		private Dictionary<string, BaseRhythmAbilityProvider> m_ProviderMap;

		protected override void OnCreate()
		{
			base.OnCreate();
			m_ProviderMap = new Dictionary<string, BaseRhythmAbilityProvider>(8);
		}

		public Entity SpawnFor(string abilityId, Entity owner, AbilitySelection selection = AbilitySelection.Horizontal, Entity overrideCommand = default, Dictionary<string, object> data = null)
		{
			if (!m_ProviderMap.TryGetValue(abilityId, out var provider))
			{
				Debug.LogWarning($"No provider for ability '{abilityId}'");
				return default;
			}

			provider.DataMap = data;
			return provider.SpawnLocalEntityWithArguments(new CreateAbility
			{
				Owner           = owner,
				OverrideCommand = overrideCommand,
				Selection       = selection
			});
		}

		public void Register(BaseRhythmAbilityProvider provider)
		{
			m_ProviderMap[provider.MasterServerId] = provider;
		}

		protected override void OnUpdate()
		{

		}
	}

	public struct CreateAbility
	{
		public Entity           Owner           { get; set; }
		public Entity           OverrideCommand { get; set; }
		public AbilitySelection Selection       { get; set; }
	}

	public abstract class BaseRhythmAbilityProvider : BaseProviderBatch<CreateAbility>
	{
		public abstract string MasterServerId  { get; }
		public abstract Type   ChainingCommand { get; }
		public virtual  Type[] ComboCommands   => null;
		public virtual  Type[] HeroModeAllowedCommands   => null;

		public Dictionary<string, object> DataMap;

		protected string configuration;

		protected override void OnCreate()
		{
			base.OnCreate();
			World.GetOrCreateSystem<AbilityRegisterSystem>()
			     .Register(this);
		}

		protected T GetValue<T>(string path, T def = default)
		{
			if (DataMap == null)
				return def;
			return DataMap.TryGetValue(path, out var obj) && obj is T data ? data : def;
		}

		protected string GetConfigurationData()
		{
			return configuration;
		}
	}

	public abstract class BaseRhythmAbilityProvider<TAbility> : BaseRhythmAbilityProvider
		where TAbility : struct, IComponentData
	{
		public virtual bool UseOldRhythmAbilityState => false;
		public virtual bool UseStatsModification => true;

		protected virtual string file_path_prefix => string.Empty;
		protected virtual string file_path
		{
			get
			{
				var str = $"{Application.streamingAssetsPath}/abilities/{{0}}/{typeof(TAbility).Name.Replace("Ability", string.Empty)}.json";
				if (!string.IsNullOrEmpty(file_path_prefix))
					str = string.Format(str, file_path_prefix + "/{0}/");
				
				if (ComboCommands == null || ComboCommands.Length == 0)
					str = string.Format(str, ChainingCommand.Name);
				else
				{
					str = string.Format(str, string.Join("_", ComboCommands.Append(ChainingCommand).Select(t => t.Name)));
				}

				return str;
			}
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			var fileInfo = new FileInfo(file_path);
			fileInfo.Directory.Create();
			
			if (!fileInfo.Exists)
			{
				var stream = fileInfo.Create();
				var bytes  = Encoding.UTF8.GetBytes("{}");
				stream.Write(bytes, 0, bytes.Length);

				stream.Dispose();
			}

			configuration = File.ReadAllText(file_path);
		}

		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new ComponentType[]
			{
				typeof(EntityDescription),
				typeof(ActionDescription),
				typeof(TAbility),
				typeof(Owner),
				typeof(DestroyChainReaction),
				typeof(PlayEntityTag),
				typeof(GhostEntity),
			};

			if (UseOldRhythmAbilityState)
				entityComponents = entityComponents.Concat(new ComponentType[]
				{
					typeof(AbilityRhythmState)
				}).ToArray();
			else
				entityComponents = entityComponents.Concat(new ComponentType[]
				{
					typeof(AbilityState),
					typeof(AbilityEngineSet),
					typeof(AbilityActivation)
				}).ToArray();

			if (UseStatsModification)
				entityComponents = entityComponents.Concat(new ComponentType[]
				{
					typeof(AbilityModifyStatsOnChaining),
					typeof(AbilityControlVelocity),
				}).ToArray();
		}

		Entity FindCommand(Type type)
		{
			using (var query = EntityManager.CreateEntityQuery(type))
			{
				if (query.CalculateEntityCount() == 0)
				{
					Debug.Log("nay " + type);
					return Entity.Null;
				}

				using (var entities = query.ToEntityArray(Allocator.TempJob))
				{
					return entities[0];
				}
			}
		}

		public override void SetEntityData(Entity entity, CreateAbility data)
		{
			var combos = new FixedList32<Entity>();
			if (ComboCommands != null)
			{
				foreach (var type in ComboCommands)
				{
					combos.Add(FindCommand(type));
				}
			}
			
			var allowedCommands = new FixedList64<Entity>();
			if (HeroModeAllowedCommands != null)
			{
				foreach (var type in HeroModeAllowedCommands)
				{
					allowedCommands.Add(FindCommand(type));
				}
			}

			if (data.OverrideCommand == Entity.Null)
			{
				data.OverrideCommand = FindCommand(ChainingCommand);
			}

			EntityManager.ReplaceOwnerData(entity, data.Owner);
			EntityManager.SetComponentData(entity, EntityDescription.New<ActionDescription>());
			if (UseOldRhythmAbilityState)
			{
				EntityManager.SetComponentData(entity, new AbilityRhythmState {Command = data.OverrideCommand, TargetSelection = data.Selection});
			}
			else
			{
				EntityManager.SetComponentData(entity, new AbilityActivation
				{
					Type                                     = EActivationType.Normal,
					HeroModeMaxCombo                         = -1,
					HeroModeImperfectLimitBeforeDeactivation = 2,

					Selection               = data.Selection,
					Chaining                = data.OverrideCommand,
					Combos                  = combos,
					HeroModeAllowedCommands = allowedCommands
				});
			}

			EntityManager.SetComponentData(entity, new TAbility());
			EntityManager.SetComponentData(entity, new Owner {Target = data.Owner});
			EntityManager.SetComponentData(entity, new DestroyChainReaction(data.Owner));

			if (UseStatsModification)
			{
				var component = new AbilityModifyStatsOnChaining();
				var map       = StatisticModifierJson.FromMap(GetConfigurationData());

				void try_get(string val, out StatisticModifier modifier)
				{
					if (!map.TryGetValue(val, out modifier))
						modifier = StatisticModifier.Default;
				}

				try_get("active", out component.ActiveModifier);
				try_get("fever", out component.FeverModifier);
				try_get("perfect", out component.PerfectModifier);
				try_get("charge", out component.ChargeModifier);

				EntityManager.SetComponentData(entity, component);
			}
		}
	}
}