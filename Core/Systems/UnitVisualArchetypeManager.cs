using System.Collections.Generic;
using PataNext.Client.Core.Addressables;
using StormiumTeam.GameBase.Utility.Pooling;
using Unity.Entities;
using UnityEngine;

namespace PataNext.Client.Systems
{
	public class UnitVisualArchetypeManager : ComponentSystem
	{
		private Dictionary<string, AsyncAssetPool<GameObject>> m_PoolByArchetype;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_PoolByArchetype = new Dictionary<string, AsyncAssetPool<GameObject>>();
			// TODO: Need to be dynamic in the future (search based in StreamingAssets)
			var modelFolder    = AddressBuilder.Client().Folder("Models");
			var uberHeroFolder = modelFolder.Folder("UberHero");
			var pataponFolder  = modelFolder.Folder("Patapon");

			var addr = AddressBuilder.Client()
			                         .Folder("archetype");

			m_PoolByArchetype[addr.GetFile("uberhero_std_unit")]                     = new AsyncAssetPool<GameObject>(uberHeroFolder.GetFile("Character.prefab"));
			m_PoolByArchetype[addr.Folder("uberhero_std_unit").GetFile("yarida")]    = new AsyncAssetPool<GameObject>(uberHeroFolder.GetFile("CharacterYarida.prefab"));
			m_PoolByArchetype[addr.Folder("uberhero_std_unit").GetFile("taterazay")] = new AsyncAssetPool<GameObject>(uberHeroFolder.GetFile("CharacterTaterazay.prefab"));

			m_PoolByArchetype[addr.GetFile("patapon_std_unit")] = new AsyncAssetPool<GameObject>(pataponFolder.GetFile("Patapon.prefab"));
		}

		protected override void OnUpdate()
		{
		}

		public bool TryGetArchetypePool(string archetype, string kit, out AsyncAssetPool<GameObject> pool)
		{
			return TryGetArchetypePool($"{archetype}/{kit}", out pool) || TryGetArchetypePool($"{archetype}", out pool);
		}

		public bool TryGetArchetypePool(string archetype, out AsyncAssetPool<GameObject> pool)
		{
			if (archetype.StartsWith("ms://"))
				archetype = archetype.Replace("ms://", "cr://");
			
			return m_PoolByArchetype.TryGetValue(archetype, out pool);
		}
	}
}