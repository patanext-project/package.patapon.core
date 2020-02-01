using System;
using System.Collections.Generic;
using P4TLB.MasterServer;
using P4TLB.MasterServer.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.GamePlay.Abilities.CTate;
using Patapon.Mixed.GamePlay.Abilities.CYari;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.Units;
using Patapon4TLB.Default.Player;
using Revolution;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Patapon4TLB.Core
{
	public static class MasterServerAbilities
	{
		private const string InternalFormat = "{0}";

		public static Dictionary<string, object> AbilityDataMap = new Dictionary<string, object>();

		private static void _c(ComponentSystemBase system, Entity entity, string typeId, AbilitySelection selection)
		{
			system.World
			      .GetOrCreateSystem<AbilityRegisterSystem>()
			      .SpawnFor(typeId, entity, selection: selection, data: AbilityDataMap);
		}

		public static void Convert(ComponentSystemBase system, Entity entity, DynamicBuffer<UnitDefinedAbilities> abilities)
		{
			if (AbilityDataMap.Count == 0)
			{
				// initialize some values here?
			}

			var array = abilities.ToNativeArray(Allocator.TempJob);
			foreach (var ab in array) _c(system, entity, ab.Type.ToString(), ab.Selection);

			array.Dispose();
		}

		public static string GetInternal(P4OfficialAbilities ability)
		{
			return string.Format(InternalFormat, ability.ToString().Replace(nameof(P4OfficialAbilities), string.Empty));
		}
	}
}