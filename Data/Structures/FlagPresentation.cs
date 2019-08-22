using MonoComponents;
using Patapon4TLB.GameModes;
using Patapon4TLB.UI.InGame;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Patapon4TLB.Default.Test.Structures
{
	public class FlagPresentation : RuntimeAssetPresentation<FlagPresentation>
	{
		
	}

	public class FlagBackend : RuntimeAssetBackend<FlagPresentation>
	{

		[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
		public class GenerateSystem : GameBaseSystem
		{
			public AssetPool<GameObject> BackendPool;

			private GetAllBackendModule<FlagBackend> m_GetAllBackendModule;

			private EntityQuery m_FlagQuery;

			protected override void OnCreate()
			{
				base.OnCreate();

				BackendPool = new AssetPool<GameObject>((pool) =>
				{
					var gameObject = new GameObject("Unused FlagBackend", typeof(FlagBackend), typeof(GameObjectEntity));
					gameObject.SetActive(false);

					var backend = gameObject.GetComponent<FlagBackend>();
					backend.SetRootPool(pool);

					return gameObject;
				}, World);

				GetModule(out m_GetAllBackendModule);

				m_FlagQuery = GetEntityQuery(typeof(HeadOnFlag));
			}

			protected override void OnUpdate()
			{
				using (var targetEntities = m_FlagQuery.ToEntityArray(Allocator.TempJob))
				{
					m_GetAllBackendModule.TargetEntities = targetEntities;
					m_GetAllBackendModule.Update(default).Complete();

					foreach (var unattachedEntity in m_GetAllBackendModule.BackendWithoutModel)
					{
						EntityManager
							.GetComponentObject<FlagBackend>(unattachedEntity)
							.Return(true, true);
					}

					foreach (var missingEntity in m_GetAllBackendModule.MissingTargets)
					{
						var gameObject = BackendPool.Dequeue();
						var backend    = gameObject.GetComponent<FlagBackend>();

						gameObject.SetActive(true);
						backend.SetTarget(EntityManager, missingEntity);
					}
				}
			}
		}

		[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
		public class UpdatePresentationSystem : GameBaseSystem
		{
			protected override void OnUpdate()
			{
				Entities.ForEach((FlagBackend backend) =>
				{
					if (backend.Presentation != null)
						return;

					var targetPresentation = StaticSceneResourceHolder.GetPool("versus:flag");
					if (targetPresentation == null)
						return; // todo: get from addressable

					backend.SetPresentationFromPool(targetPresentation);
				});
			}
		}

		[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
		public class UpdateSystem : GameBaseSystem
		{
			protected override void OnUpdate()
			{
				Entities.ForEach((FlagBackend backend) =>
				{
					if (backend.DstEntity == default)
						return;
					
					backend.transform.position = EntityManager.GetComponentData<Translation>(backend.DstEntity).Value;
				});
			}
		}
	}
}