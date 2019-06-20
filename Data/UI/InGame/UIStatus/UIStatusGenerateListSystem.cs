using System.Collections.Generic;
using Patapon4TLB.Core;
using Patapon4TLB.UI.InGame;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Patapon4TLB.UI
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class UIStatusGenerateListSystem : UIGameSystemBase
	{
		private GameObject m_UIRoot;

		private AssetPool<GameObject>      m_BackendPool;
		private AsyncAssetPool<GameObject> m_PresentationPool;

		private EntityQuery m_UnitQuery;
		private EntityQuery m_BackendQuery;

		private const string KeyBase = "int:UI/InGame/UnitStatus/";

		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			m_UnitQuery    = GetEntityQuery(typeof(UnitDescription), typeof(Relative<TeamDescription>));
			m_BackendQuery = GetEntityQuery(typeof(UIStatusBackend));

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
				var backend    = gameObject.GetComponent<UIStatusBackend>();
				backend.SetRootPool(pool);

				return gameObject;
			}, World);
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
					Debug.Log("Units[0] " + cameraState.Target);
				}
				else if (TryGetRelative<UnitDescription>(cameraState.Target, out var relativeUnit))
				{
					Debug.Log("hello");

					if (relativeUnit == cameraState.Target)
						units[0] = cameraState.Target;
					else
					{
						units[0] = relativeUnit;
						Debug.Log("Relative? " + relativeUnit);
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
					backend.DisableNextUpdate                 = true;
					backend.ReturnToPoolOnDisable             = true;
					backend.ReturnPresentationToPoolNextFrame = true;
				});

				return;
			}

			var backendEntities = m_BackendQuery.ToEntityArray(Allocator.TempJob);
			// first, we check the missing backends
			for (var ent = 0; ent != entities.Length; ent++)
			{
				var backend = default(UIStatusBackend);
				for (var back = 0; back != backendEntities.Length; back++)
				{
					var tmp = EntityManager.GetComponentObject<UIStatusBackend>(backendEntities[back]);
					if (tmp.entity != entities[ent])
						continue;

					backend = tmp;
					break;
				}

				if (backend != null)
					continue;

				Debug.Log("Create for " + entities[ent]);

				backend = m_BackendPool.Dequeue().GetComponent<UIStatusBackend>();
				backend.transform.SetParent(m_UIRoot.transform, false);
				backend.entity = entities[ent];

				backend.SetFromPool(m_PresentationPool, EntityManager, entities[ent]);
			}

			// now, check for useless backends
			for (var back = 0; back != backendEntities.Length; back++)
			{
				var backend = EntityManager.GetComponentObject<UIStatusBackend>(backendEntities[back]);
				var result  = default(Entity);

				Debug.Log("Target backend entity: " + backend.entity);
				for (var ent = 0; ent != entities.Length; ent++)
				{
					if (backend.entity != entities[ent])
						continue;

					result = entities[ent];
					break;
				}

				if (result == default)
				{
					Debug.Log("Return");
					backend.Return(true, true);
				}
			}

			backendEntities.Dispose();
		}
	}
}