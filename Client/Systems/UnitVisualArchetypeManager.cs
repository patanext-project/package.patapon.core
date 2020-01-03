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
			m_PoolByArchetype["UH.basic"] = new AsyncAssetPool<GameObject>("core://Client/Models/UberHero/Character.prefab");
		}

		protected override void OnUpdate()
		{

		}

		public AsyncAssetPool<GameObject> GetArchetypePool(string archetype)
		{
			return m_PoolByArchetype[archetype];
		}
	}
}