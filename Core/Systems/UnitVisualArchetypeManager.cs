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

			m_PoolByArchetype[addr.GetFile("uberhero_std_unit")]                     = new AsyncAssetPool<GameObject>(uberHeroFolder.GetAsset("Character"));
			m_PoolByArchetype[addr.Folder("uberhero_std_unit").GetFile("yarida")]    = new AsyncAssetPool<GameObject>(uberHeroFolder.GetAsset("CharacterYarida"));
			m_PoolByArchetype[addr.Folder("uberhero_std_unit").GetFile("taterazay")] = new AsyncAssetPool<GameObject>(uberHeroFolder.GetAsset("CharacterTaterazay"));
			m_PoolByArchetype[addr.Folder("uberhero_std_unit").GetFile("guardira")]  = new AsyncAssetPool<GameObject>(uberHeroFolder.GetAsset("CharacterGuardira"));
			m_PoolByArchetype[addr.Folder("uberhero_std_unit").GetFile("wooyari")]   = new AsyncAssetPool<GameObject>(uberHeroFolder.GetAsset("CharacterWooyari"));

			m_PoolByArchetype[addr.GetFile("patapon_std_unit")] = new AsyncAssetPool<GameObject>(pataponFolder.GetAsset("Patapon"));
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
			var inspect = ResPath.Inspect(archetype);
			
			if (archetype.StartsWith("ms://"))
				archetype = archetype.Replace("ms://", string.Empty);
			
			return m_PoolByArchetype.TryGetValue(inspect.ResourcePath, out pool);
		}
	}
}