using Patapon4TLB.Core;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Misc;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.Rendering;

namespace Patapon4TLB.UI.InGame
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class GenerateUIPlayerDisplayNameSystem : UIGameSystemBase
	{
		private AssetPool<GameObject>      m_BackendPool;
		private AsyncAssetPool<GameObject> m_PresentationPool;

		private EntityQuery m_PlayerUnitQuery;

		private GetAllBackendModule<UIPlayerDisplayNameBackend> m_GetAllBackendModule;

		private const string KeyBase = "int:UI/InGame/Multiplayer/";

		protected override void OnCreate()
		{
			base.OnCreate();

			m_PresentationPool = new AsyncAssetPool<GameObject>(KeyBase + "MpDisplayName.prefab");
			m_BackendPool = new AssetPool<GameObject>((pool) =>
			{
				var gameObject = new GameObject("DisplayName Backend", typeof(SortingGroup), typeof(UIPlayerDisplayNameBackend), typeof(GameObjectEntity));
				gameObject.SetActive(false);

				var backend = gameObject.GetComponent<UIPlayerDisplayNameBackend>();
				backend.SetRootPool(pool);

				return gameObject;
			}, World);

			m_PresentationPool.AddElements(16);
			m_BackendPool.AddElements(16);

			GetModule(out m_GetAllBackendModule);

			m_PlayerUnitQuery = GetEntityQuery(typeof(UnitDescription), typeof(Relative<PlayerDescription>));
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
					backend.ReturnDelayed(PostUpdateCommands, true, true);
				});
			}

			m_GetAllBackendModule.TargetEntities = entities;
			m_GetAllBackendModule.Update(default).Complete();

			var unattachedBackend = m_GetAllBackendModule.BackendWithoutModel;
			var unattachedCount   = unattachedBackend.Length;
			for (var i = 0; i != unattachedCount; i++)
			{
				var backend = EntityManager.GetComponentObject<UIPlayerDisplayNameBackend>(unattachedBackend[i]);
				backend.SetDestroyFlags(0);
			}

			var missingEntities = m_GetAllBackendModule.MissingTargets;
			var missingCount    = missingEntities.Length;
			for (var i = 0; i != missingCount; i++)
			{
				using (new SetTemporaryActiveWorld(World))
				{
					var backend = m_BackendPool.Dequeue().GetComponent<UIPlayerDisplayNameBackend>();
					backend.gameObject.SetActive(true);

					var sortingGroup = backend.GetComponent<SortingGroup>();
					sortingGroup.sortingLayerName = "OverlayUI";
					sortingGroup.sortingOrder     = (int) UICanvasOrder.UnitName;

					backend.SetTarget(EntityManager, missingEntities[i]);
					backend.SetPresentationFromPool(m_PresentationPool);
				}
			}
		}
	}
}