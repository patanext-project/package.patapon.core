using System;
using System.Collections.Generic;
using PataNext.Client.Core.Addressables;
using StormiumTeam.GameBase.Utility.Misc;
using StormiumTeam.GameBase.Utility.Pooling;
using Unity.Entities;
using UnityEngine;

namespace PataNext.Client.Systems
{
	public class UnitVisualArchetypeManager : ComponentSystem
	{
		private Dictionary<string, Dictionary<string, AsyncAssetPool<GameObject>>> m_PoolByArchetype;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_PoolByArchetype = new Dictionary<string, Dictionary<string, AsyncAssetPool<GameObject>>>();
			// TODO: Need to be dynamic in the future (search based in StreamingAssets)
			var modelFolder    = AddressBuilder.Client().Folder("Models");
			var uberHeroFolder = modelFolder.Folder("UberHero");
			var pataponFolder  = modelFolder.Folder("Patapon");

			var archAddr = AddressBuilder.Client().Folder("archetype");
			var kitAddr  = AddressBuilder.Client().Folder("kit");

			var uberHeroMap = new Dictionary<string, AsyncAssetPool<GameObject>>
			{
				[string.Empty]                    = new AsyncAssetPool<GameObject>(uberHeroFolder.GetAsset("Character")),
				[kitAddr.GetFile("yarida")]       = new AsyncAssetPool<GameObject>(uberHeroFolder.GetAsset("CharacterYarida")),
				[kitAddr.GetFile("taterazay")]    = new AsyncAssetPool<GameObject>(uberHeroFolder.GetAsset("CharacterTaterazay")),
				[kitAddr.GetFile("guardira")]     = new AsyncAssetPool<GameObject>(uberHeroFolder.GetAsset("CharacterGuardira")),
				[kitAddr.GetFile("wooyari")]      = new AsyncAssetPool<GameObject>(uberHeroFolder.GetAsset("CharacterYarida")),
				[kitAddr.GetFile("wondabarappa")] = new AsyncAssetPool<GameObject>(uberHeroFolder.GetAsset("CharacterWooyari"))
			};

			var ponMap = new Dictionary<string, AsyncAssetPool<GameObject>>
			{
				[string.Empty]              = new AsyncAssetPool<GameObject>(pataponFolder.GetAsset("Patapon")),
				[kitAddr.GetFile("yarida")] = new AsyncAssetPool<GameObject>(pataponFolder.GetAsset("PataponYarida"))
			};

			m_PoolByArchetype[archAddr.GetFile("uberhero_std_unit")] = uberHeroMap;
			m_PoolByArchetype[archAddr.GetFile("patapon_std_unit")]  = ponMap;
		}

		protected override void OnUpdate()
		{
		}

		public bool TryGetArchetypePool(string archetype, out AsyncAssetPool<GameObject> pool)
		{
			return tryGet(archetype, string.Empty, out pool);
		}

		public bool TryGetArchetypePool(string archetype, string kit, out AsyncAssetPool<GameObject> pool)
		{
			return tryGet(archetype, kit, out pool) || tryGet(archetype, string.Empty, out pool);
		}

		private bool tryGet(string archetype, string kit, out AsyncAssetPool<GameObject> pool)
		{
			var archInspect = ResPath.Inspect(archetype);
			var kitInspect  = ResPath.Inspect(kit);

			Debug.LogError($"tryGet {archetype}, {kit}");
			
			pool = null;
			if (!m_PoolByArchetype.TryGetValue(archInspect.ResourcePath, out var kitMap))
				return false;

			return kitMap.TryGetValue(kitInspect.ResourcePath, out pool);
		}
	}
}