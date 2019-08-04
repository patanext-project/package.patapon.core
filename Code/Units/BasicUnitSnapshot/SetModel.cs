using Runtime.Misc;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Patapon4TLB.Core.BasicUnitSnapshot
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class BasicUnitSetModel : ComponentSystem
	{
		public AssetPool<GameObject> BackendPool;

		public struct ToModel : IComponentData
		{
			public Entity Target;
		}

		private EntityQuery m_UnitQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			BackendPool = new AssetPool<GameObject>((pool) =>
			{
				var gameObject = new GameObject("UnitVisualBackend Pooled");
				gameObject.SetActive(false);

				var backend = gameObject.AddComponent<UnitVisualBackend>();
				backend.SetRootPool(pool);

				gameObject.AddComponent<UnitVisualAnimation>();
				gameObject.AddComponent<GameObjectEntity>();

				return gameObject;
			}, World);

			BackendPool.AddElements(16);

			m_UnitQuery = GetEntityQuery(new EntityQueryDesc
			{
				All  = new ComponentType[] {typeof(BasicUnitSnapshotData), typeof(ReplicatedEntityComponent), typeof(Translation)},
				None = new ComponentType[] {typeof(ToModel)}
			});
		}

		protected override void OnUpdate()
		{
			var entities = m_UnitQuery.ToEntityArray(Allocator.TempJob);
			for (var ent = 0; ent != entities.Length; ent++)
			{
				var gameObject = BackendPool.Dequeue();
				var backend    = gameObject.GetComponent<UnitVisualBackend>();

				using (new SetTemporaryActiveWorld(World))
				{
					gameObject.SetActive(true);
				}

				backend.OnReset();
				backend.gameObject.name = "UnitBackend:" + entities[ent];
				backend.SetTarget(EntityManager, entities[ent]);

				EntityManager.AddComponentData(entities[ent], new ToModel {Target = gameObject.GetComponent<GameObjectEntity>().Entity});
			}

			entities.Dispose();
		}
	}
}