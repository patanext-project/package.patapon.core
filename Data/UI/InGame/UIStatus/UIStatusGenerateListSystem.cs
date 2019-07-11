using System.Collections.Generic;
using Patapon4TLB.Core;
using Patapon4TLB.UI.InGame;
using Runtime.Misc;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;

namespace Patapon4TLB.UI
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class UIStatusGenerateListSystem : UIGameSystemBase
	{
		private GameObject m_UIRoot;

		private AssetPool<GameObject>      m_BackendPool;
		private AsyncAssetPool<GameObject> m_PresentationPool;

		private EntityQuery m_UnitQuery;
		
		private GetAllBackendModule<UIStatusBackend> m_GetAllBackendModule; 

		private const string KeyBase = "int:UI/InGame/UnitStatus/";

		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			m_UnitQuery    = GetEntityQuery(typeof(UnitDescription), typeof(Relative<TeamDescription>));

			m_UIRoot = new GameObject("UIStatus_RootContainer", typeof(RectTransform));
			var rectTransform = m_UIRoot.GetComponent<RectTransform>();
			rectTransform.parent     = World.GetOrCreateSystem<UIClientCanvasSystem>().Current.transform;
			rectTransform.localScale = Vector3.one;

			rectTransform.anchoredPosition = new Vector2(30, -30);
			rectTransform.anchorMin        = new Vector2(0, 1);
			rectTransform.anchorMax        = new Vector2(1, 1);
			rectTransform.pivot            = new Vector2(0, 1);
			rectTransform.sizeDelta        = new Vector2(-990, 100);

			m_PresentationPool = new AsyncAssetPool<GameObject>(KeyBase + "UnitStatusObject.prefab");
			m_BackendPool = new AssetPool<GameObject>(pool =>
			{
				// it's important to set GameObjectEntity as last! (or else other components will not get referenced????)
				var gameObject = new GameObject("UIStatus Backend", typeof(RectTransform), typeof(UIStatusBackend), typeof(GameObjectEntity));
				gameObject.SetActive(false);
				
				var backend    = gameObject.GetComponent<UIStatusBackend>();
				backend.SetRootPool(pool);

				return gameObject;
			}, World);
			
			GetModule(out m_GetAllBackendModule);
		}

		protected override void OnUpdate()
		{
			var selfGamePlayer = GetFirstSelfGamePlayer();
			var cameraState    = GetCurrentCameraState(selfGamePlayer);
			if (cameraState.Target == default)
			{
				ManageForUnits(default);
				return;
			}

			if (!TryGetRelative<TeamDescription>(cameraState.Target, out var teamEntity))
			{
				var units = new NativeArray<Entity>(1, Allocator.Temp);
				if (EntityManager.HasComponent<UnitDescription>(cameraState.Target))
				{
					units[0] = cameraState.Target;
				}
				else if (TryGetRelative<UnitDescription>(cameraState.Target, out var relativeUnit))
				{
					if (relativeUnit == cameraState.Target)
						units[0] = cameraState.Target;
					else
					{
						units[0] = relativeUnit;
					}
				}

				ManageForUnits(units);
				units.Dispose();
			}
			else
			{
				var units = new NativeList<Entity>(16, Allocator.Temp);

				var entityType       = GetArchetypeChunkEntityType();
				var relativeTeamType = GetArchetypeChunkComponentType<Relative<TeamDescription>>(true);

				using (var chunks = m_UnitQuery.CreateArchetypeChunkArray(Allocator.TempJob))
				{
					foreach (var chunk in chunks)
					{
						var entityArray       = chunk.GetNativeArray(entityType);
						var relativeTeamArray = chunk.GetNativeArray(relativeTeamType);

						for (int i = 0, count = chunk.Count; i < count; ++i)
						{
							if (relativeTeamArray[i].Target != teamEntity)
								continue;

							units.Add(entityArray[i]);
						}
					}
				}

				ManageForUnits(units);
				units.Dispose();
			}
		}

		private void ManageForUnits(NativeArray<Entity> entities)
		{
			if (entities == default || !entities.IsCreated)
			{
				Entities.ForEach((UIStatusBackend backend) =>
				{
					backend.ReturnDelayed(PostUpdateCommands, true, true);
				});

				return;
			}
			
			m_GetAllBackendModule.TargetEntities = entities;
			m_GetAllBackendModule.Update(default).Complete();

			var unattachedBackend = m_GetAllBackendModule.BackendWithoutModel;
			var unattachedCount   = unattachedBackend.Length;
			for (var i = 0; i != unattachedCount; i++)
			{
				var backend = EntityManager.GetComponentObject<UIStatusBackend>(unattachedBackend[i]);
				backend.SetDestroyFlags(0);
			}

			var missingEntities = m_GetAllBackendModule.MissingTargets;
			var missingCount    = missingEntities.Length;
			for (var i = 0; i != missingCount; i++)
			{
				using (new SetTemporaryActiveWorld(World))
				{
					var backend = m_BackendPool.Dequeue().GetComponent<UIStatusBackend>();
					backend.gameObject.SetActive(true);

					backend.transform.SetParent(m_UIRoot.transform, false);
					backend.SetFromPool(m_PresentationPool, EntityManager, missingEntities[i]);
					
					Debug.Log("Generate for " + missingEntities[i]);
				}
			}
		}
	}
}