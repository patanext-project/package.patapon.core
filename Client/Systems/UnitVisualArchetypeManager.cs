using System.Collections.Generic;
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
			m_PoolByArchetype["st:pn/archetype/uberhero_std_unit"]     = new AsyncAssetPool<GameObject>("core://Client/Models/UberHero/Character.prefab");
			m_PoolByArchetype["st:pn/archetype/uberhero_std_unit/yarida"]    = new AsyncAssetPool<GameObject>("core://Client/Models/UberHero/CharacterYarida.prefab");
			m_PoolByArchetype["st:pn/archetype/uberhero_std_unit/taterazay"] = new AsyncAssetPool<GameObject>("core://Client/Models/UberHero/CharacterTaterazay.prefab");
			m_PoolByArchetype["st:pn/archetype/uberhero_std_unit/pingrek"]   = new AsyncAssetPool<GameObject>("core://Client/Models/UberHero/Character.prefab");
			m_PoolByArchetype["st:pn/archetype/uberhero_std_unit/kibadda"]   = new AsyncAssetPool<GameObject>("core://Client/Models/UberHero/Character.prefab");

			m_PoolByArchetype["st:pn/archetype/patapon_std_unit"] = new AsyncAssetPool<GameObject>("core://Client/Models/Patapon/Patapon.prefab");
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
			return m_PoolByArchetype.TryGetValue(archetype, out pool);
		}
	}
}