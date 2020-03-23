using System.Collections.Generic;
using StormiumTeam.GameBase;
using Unity.Entities;
using UnityEngine;

namespace Patapon.Client.Systems
{
	public class UnitVisualArchetypeManager : ComponentSystem
	{
		private Dictionary<string, AsyncAssetPool<GameObject>> m_PoolByArchetype;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_PoolByArchetype = new Dictionary<string, AsyncAssetPool<GameObject>>();
			// TODO: Need to be dynamic in the future (search based in StreamingAssets)
			m_PoolByArchetype["UH.basic"]     = new AsyncAssetPool<GameObject>("core://Client/Models/UberHero/Character.prefab");
			m_PoolByArchetype["UH.yarida"]    = new AsyncAssetPool<GameObject>("core://Client/Models/UberHero/CharacterYarida.prefab");
			m_PoolByArchetype["UH.taterazay"] = new AsyncAssetPool<GameObject>("core://Client/Models/UberHero/CharacterTaterazay.prefab");
			m_PoolByArchetype["UH.pingrek"] = new AsyncAssetPool<GameObject>("core://Client/Models/UberHero/Character.prefab");
			m_PoolByArchetype["UH.kibadda"] = new AsyncAssetPool<GameObject>("core://Client/Models/UberHero/Character.prefab");
		}

		protected override void OnUpdate()
		{
		}

		public bool TryGetArchetypePool(string archetype, out AsyncAssetPool<GameObject> pool)
		{
			return m_PoolByArchetype.TryGetValue(archetype, out pool);
		}
	}
}