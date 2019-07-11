using Patapon4TLB.Core;
using Runtime.Misc;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.Rendering;

namespace Patapon4TLB.UI.InGame
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class GenerateUIPlayerTargetCursorSystem : UIGameSystemBase
	{
		private AssetPool<GameObject>      m_BackendPool;
		private AsyncAssetPool<GameObject> m_PresentationPool;

		private EntityQuery m_ControlledUnitQuery;

		private const string KeyBase = "int:UI/InGame/Multiplayer/";

		private GetAllBackendModule<UIPlayerTargetCursorBackend> m_GetAllBackendModule;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_PresentationPool = new AsyncAssetPool<GameObject>(KeyBase + "MpTargetCursor.prefab");
			m_BackendPool = new AssetPool<GameObject>((pool) =>
			{
				var gameObject = new GameObject("TargetCursor Backend", typeof(SortingGroup), typeof(UIPlayerTargetCursorBackend), typeof(GameObjectEntity));
				gameObject.SetActive(false);

				var backend = gameObject.GetComponent<UIPlayerTargetCursorBackend>();
				backend.SetRootPool(pool);

				return gameObject;
			}, World);

			GetModule(out m_GetAllBackendModule);

			m_ControlledUnitQuery = GetEntityQuery(typeof(UnitTargetPosition));
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
					backend.ReturnDelayed(PostUpdateCommands, true, true);
				});
			}

			m_GetAllBackendModule.TargetEntities = entities;
			m_GetAllBackendModule.Update(default).Complete();

			var unattachedBackend = m_GetAllBackendModule.BackendWithoutModel;
			var unattachedCount   = unattachedBackend.Length;
			for (var i = 0; i != unattachedCount; i++)
			{
				var backend = EntityManager.GetComponentObject<UIPlayerTargetCursorBackend>(unattachedBackend[i]);
				backend.SetDestroyFlags(0);
			}

			var missingEntities = m_GetAllBackendModule.MissingTargets;
			var missingCount    = missingEntities.Length;
			for (var i = 0; i != missingCount; i++)
			{
				using (new SetTemporaryActiveWorld(World))
				{
					var backend = m_BackendPool.Dequeue().GetComponent<UIPlayerTargetCursorBackend>();
					backend.gameObject.SetActive(true);

					var sortingGroup = backend.GetComponent<SortingGroup>();
					sortingGroup.sortingLayerName = "UI";
					sortingGroup.sortingOrder     = (int) UICanvasOrder.UnitCursor;

					backend.SetFromPool(m_PresentationPool, EntityManager, missingEntities[i]);
				}
			}
		}
	}
}