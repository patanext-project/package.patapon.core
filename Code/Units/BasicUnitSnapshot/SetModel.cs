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
		public AsyncAssetPool<GameObject> PresentationPool;
		public AssetPool<GameObject>      BackendPool;

		private struct ToModel : IComponentData
		{
			public Entity Target;
		}

		private EntityQuery m_UnitQuery;
		
		protected override void OnCreate()
		{
			base.OnCreate();

			PresentationPool = new AsyncAssetPool<GameObject>("character");
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

			m_UnitQuery = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[]{typeof(BasicUnitSnapshotData), typeof(ReplicatedEntityComponent), typeof(Translation)},
				None = new ComponentType[]{typeof(ToModel)}
			});
		}

		protected override void OnUpdate()
		{
			var entities = Entities.WithAll<BasicUnitSnapshotData, ReplicatedEntityComponent>().WithNone<ToModel>().ToEntityQuery().ToEntityArray(Allocator.TempJob);
			for (var ent = 0; ent != entities.Length; ent++)
			{
				var gameObject = BackendPool.Dequeue();
				var backend    = gameObject.GetComponent<UnitVisualBackend>();

				backend.OnReset();
				backend.SetFromPool(PresentationPool, EntityManager, entities[ent]);
				
				Debug.Log("Model set for " + entities[ent]);

				using (new SetTemporaryActiveWorld(World))
				{
					gameObject.SetActive(true);
				}

				EntityManager.AddComponentData(entities[ent], new ToModel {Target = gameObject.GetComponent<GameObjectEntity>().Entity});
			}

			entities.Dispose();
		}
	}
}