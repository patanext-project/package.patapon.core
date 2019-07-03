using Patapon4TLB.Core;
using Runtime.Misc;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Patapon4TLB.UI.InGame
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class GenerateUIPlayerDisplayNameSystem : UIGameSystemBase
	{
		private AssetPool<GameObject>      m_BackendPool;
		private AsyncAssetPool<GameObject> m_PresentationPool;

		private EntityQuery m_PlayerUnitQuery;
		private EntityQuery m_BackendQuery;

		private Canvas m_Canvas;

		private const string KeyBase = "int:UI/InGame/Multiplayer/";

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Canvas                      = World.GetOrCreateSystem<UIClientCanvasSystem>().CreateCanvas(out _, "MpDisplayName");
			m_Canvas.renderMode           = RenderMode.WorldSpace;
			m_Canvas.sortingOrder         = (int) UICanvasOrder.UnitName;
			m_Canvas.sortingLayerName     = "UI";
			m_Canvas.transform.localScale = Vector3.one * 0.0125f;

			m_PresentationPool = new AsyncAssetPool<GameObject>(KeyBase + "MpDisplayName.prefab");
			m_BackendPool = new AssetPool<GameObject>((pool) =>
			{
				var gameObject = new GameObject("DisplayName Backend", typeof(RectTransform), typeof(UIPlayerDisplayNameBackend), typeof(GameObjectEntity));
				gameObject.SetActive(false);

				var backend = gameObject.GetComponent<UIPlayerDisplayNameBackend>();
				backend.SetRootPool(pool);

				return gameObject;
			}, World);

			m_PlayerUnitQuery = GetEntityQuery(typeof(UnitDescription), typeof(Relative<PlayerDescription>));
			m_BackendQuery        = GetEntityQuery(typeof(UIPlayerDisplayNameBackend));
		}

		protected override void OnUpdate()
		{
			var controlledUnits = m_PlayerUnitQuery.ToEntityArray(Allocator.TempJob);
			
			Generate(controlledUnits);

			controlledUnits.Dispose();
		}

		private void Generate(NativeArray<Entity> entities)
		{
			if (!entities.IsCreated || entities.Length < 0)
			{
				Entities.ForEach((UIPlayerDisplayNameBackend backend) =>
				{
					backend.DisableNextUpdate                 = true;
					backend.ReturnToPoolOnDisable             = true;
					backend.ReturnPresentationToPoolNextFrame = true;
				});
			}

			var backendEntities = m_BackendQuery.ToEntityArray(Allocator.TempJob);
			// flags previous backend, check for corresponding unit-backend and un-flags, or create one.
			for (var ent = 0; ent != entities.Length; ent++)
			{
				UIPlayerDisplayNameBackend backend = null;
				for (var back = 0; back != backendEntities.Length; back++)
				{
					var tmp = EntityManager.GetComponentObject<UIPlayerDisplayNameBackend>(backendEntities[back]);
					if (tmp.DstEntity != entities[ent])
					{
						if (tmp.DestroyFlags >= 0)
							tmp.SetDestroyFlags(0);

						continue;
					}

					backend = tmp;
				}

				if (backend != null)
				{
					backend.SetDestroyFlags(-1);
					continue;
				}
				
				Debug.Log("Create UIPlayerTargetCursor for " + entities[ent]);

				using (new SetTemporaryActiveWorld(World))
				{
					backend = m_BackendPool.Dequeue().GetComponent<UIPlayerDisplayNameBackend>();
					backend.gameObject.SetActive(true);

					backend.transform.SetParent(m_Canvas.transform, false);
					backend.SetFromPool(m_PresentationPool, EntityManager, entities[ent]);
				}
			}

			backendEntities.Dispose();
		}
	}
}