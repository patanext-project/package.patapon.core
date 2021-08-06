using System;
using System.Collections.Generic;
using PataNext.Client.Core.Addressables;
using StormiumTeam.GameBase.Utility.Misc;
using Unity.Entities;
using UnityEngine;

namespace PataNext.Client.Graphics.Animation.Units.Base
{
	public class StatusEffectManager : SystemBase
	{
		private Dictionary<Type, (Func<Entity, UnitReadStatusEffectState> ac, string translationId)>    stateMap;
		private Dictionary<Type, (Func<Entity, UnitReadStatusEffectSettings> ac, string translationId)> settingsMap;

		public StatusEffectManager()
		{
			stateMap    = new Dictionary<Type, (Func<Entity, UnitReadStatusEffectState> ac, string translationId)>();
			settingsMap = new Dictionary<Type, (Func<Entity, UnitReadStatusEffectSettings> ac, string translationId)>();

			AssetManager.LoadAsset<StatusEffectAsset>(AddressBuilder.Client().GetAsset("Data/StatusEffectAsset.asset"));

			RegisterSettings<Game.Abilities.Effects.CriticalSettings>("critical");
			RegisterSettings<Game.Abilities.Effects.PiercingSettings>("piercing");
			RegisterSettings<Game.Abilities.Effects.KnockBackSettings>("knockback");
			RegisterSettings<Game.Abilities.Effects.StaggerSettings>("stagger");
			RegisterSettings<Game.Abilities.Effects.BurnSettings>("burn");
			RegisterSettings<Game.Abilities.Effects.SleepSettings>("sleep");
			RegisterSettings<Game.Abilities.Effects.FreezeSettings>("freeze");
			RegisterSettings<Game.Abilities.Effects.PoisonSettings>("poison");
			RegisterSettings<Game.Abilities.Effects.TumbleSettings>("tumble");
			RegisterSettings<Game.Abilities.Effects.WindSettings>("wind");
		}

		protected override void OnUpdate()
		{
		}

		public void RegisterState<T>(string translationId)
			where T : struct, IStatusEffectState
		{
			Register<T>(translationId, e =>
			{
				var curr = GetComponent<T>(e);

				UnitReadStatusEffectState state;
				state.Id             = translationId;
				state.Resistance     = curr.Resistance;
				state.Power          = curr.Power;
				state.Immunity       = curr.Immunity;
				state.RegenPerSecond = curr.RegenPerSecond;
				state.ReceivePower   = curr.ReceivePower;

				return state;
			});
		}

		public void RegisterSettings<T>(string translationId)
			where T : struct, IStatusEffectSettings
		{
			Register<T>(translationId, e =>
			{
				var curr = GetComponent<T>(e);

				UnitReadStatusEffectSettings state;
				state.Id             = translationId;
				state.Resistance     = curr.Resistance;
				state.Power          = curr.Power;
				state.Immunity       = curr.Immunity;
				state.RegenPerSecond = curr.RegenPerSecond;

				return state;
			});
		}

		public void Register<T>(string translationId, Func<Entity, UnitReadStatusEffectState> readState)
		{
			stateMap.Add(typeof(T), (readState, translationId));
		}

		public void Register<T>(string translationId, Func<Entity, UnitReadStatusEffectSettings> readSettings)
		{
			settingsMap.Add(typeof(T), (readSettings, translationId));
		}

		public void ReadState<TList>(Entity entity, TList list)
			where TList : IList<UnitReadStatusEffectState>
		{
			foreach (var kvp in stateMap)
			{
				var type = kvp.Key;
				if (false == EntityManager.HasComponent(entity, type))
					continue;

				var (ac, translationId) = kvp.Value;
				list.Add(ac(entity));
			}
		}

		public void ReadSettings<TList>(Entity entity, TList list, string[] constraint = null)
			where TList : IList<UnitReadStatusEffectSettings>
		{
			foreach (var kvp in settingsMap)
			{
				var type = kvp.Key;
				if (false == EntityManager.HasComponent(entity, type))
					continue;

				var (ac, translationId) = kvp.Value;
				var result = ac(entity);
				if (constraint != null && Array.IndexOf(constraint, result.Id) < 0)
					continue;

				list.Add(result);
			}
		}

		public Color GetColor(string type)
		{
			Color color = default;
			foreach (var asset in StatusEffectAsset.AllAssets)
				if (asset.TryGetColor(type, out color))
					return color;

			return color;
		}

		public Sprite GetSprite(string type)
		{
			Sprite sprite = default;
			foreach (var asset in StatusEffectAsset.AllAssets)
				if (asset.TryGetIcon(type, out sprite))
					return sprite;

			return sprite;
		}
	}

	public interface IStatusEffectState : IComponentData
	{
		float Resistance     { get; set; }
		float RegenPerSecond { get; set; }
		float Power          { get; set; }
		float Immunity       { get; set; }
		float ReceivePower   { get; set; }
	}

	public interface IStatusEffectSettings : IComponentData
	{
		float Resistance     { get; set; }
		float RegenPerSecond { get; set; }
		float Power          { get; set; }
		float Immunity       { get; set; }
	}

	public struct UnitReadStatusEffectState
	{
		public string Id;
		public float  Resistance;
		public float  RegenPerSecond;
		public float  Power;
		public float  Immunity;
		public float  ReceivePower;
	}

	public struct UnitReadStatusEffectSettings
	{
		public string Id;
		public float  Resistance;
		public float  RegenPerSecond;
		public float  Power;
		public float  Immunity;
	}
}