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
	public class GenerateUIPlayerTargetCursorSystem : UIGameSystemBase
	{
		private AssetPool<GameObject>      m_BackendPool;
		private AsyncAssetPool<GameObject> m_PresentationPool;

		private EntityQuery m_ControlledUnitQuery;
		private EntityQuery m_BackendQuery;

		private Canvas m_Canvas;

		private const string KeyBase = "int:UI/InGame/Multiplayer/";

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Canvas                      = World.GetOrCreateSystem<UIClientCanvasSystem>().CreateCanvas(out _, "MpTargetCursor");
			m_Canvas.renderMode           = RenderMode.WorldSpace;
			m_Canvas.sortingOrder         = (int) UICanvasOrder.UnitCursor;
			m_Canvas.sortingLayerName     = "UI";
			m_Canvas.transform.localScale = Vector3.one * 0.0075f;

			m_PresentationPool = new AsyncAssetPool<GameObject>(KeyBase + "MpTargetCursor.prefab");
			m_BackendPool = new AssetPool<GameObject>((pool) =>
			{
				var gameObject = new GameObject("TargetCursor Backend", typeof(RectTransform), typeof(UIPlayerTargetCursorBackend), typeof(GameObjectEntity));
				gameObject.SetActive(false);

				var backend = gameObject.GetComponent<UIPlayerTargetCursorBackend>();
				backend.SetRootPool(pool);

				return gameObject;
			}, World);

			m_ControlledUnitQuery = GetEntityQuery(typeof(UnitTargetPosition));
			m_BackendQuery        = GetEntityQuery(typeof(UIPlayerTargetCursorBackend));
		}

		protected override void OnUpdate()
		{
			var controlledUnits = m_ControlledUnitQuery.ToEntityArray(Allocator.TempJob);
			
			Generate(controlledUnits);

			controlledUnits.Dispose();
		}

		private void Generate(NativeArray<Entity> entities)
		{
			if (!entities.IsCreated || entities.Length < 0)
			{
				Entities.ForEach((UIPlayerTargetCursorBackend backend) =>
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
				UIPlayerTargetCursorBackend backend = null;
				for (var back = 0; back != backendEntities.Length; back++)
				{
					var tmp = EntityManager.GetComponentObject<UIPlayerTargetCursorBackend>(backendEntities[back]);
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
					backend = m_BackendPool.Dequeue().GetComponent<UIPlayerTargetCursorBackend>();
					backend.gameObject.SetActive(true);

					backend.transform.SetParent(m_Canvas.transform, false);
					backend.SetFromPool(m_PresentationPool, EntityManager, entities[ent]);
				}
			}

			backendEntities.Dispose();
		}
	}
}