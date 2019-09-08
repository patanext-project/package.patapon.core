using Patapon4TLB.Core;
using Patapon4TLB.UI.InGame;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Misc;
using Unity.Collections;
using Unity.Entities;
using Revolution.NetCode;
using UnityEngine;

namespace Patapon4TLB.UI.GameMode.VSHeadOn
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[AlwaysUpdateSystem]
	public class UiHeadOnStatusGenerateList : UIGameSystemBase
	{
		private RectTransform m_Root;

		private AssetPool<GameObject>      m_BackendPool;
		private AsyncAssetPool<GameObject> m_PresentationPool;

		private EntityQuery m_InterfaceQuery;
		private EntityQuery m_UnitQuery;

		private GetAllBackendModule<UiHeadOnUnitStatusBackend> m_GetAllBackendModule;

		private const string KeyBase = "int:UI/GameModes/VSHeadOn/VSHeadOn_UnitStatus.prefab";

		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			m_UnitQuery = GetEntityQuery(typeof(UnitDescription), typeof(Relative<TeamDescription>));
			m_InterfaceQuery = GetEntityQuery(typeof(UiHeadOnPresentation));

			m_PresentationPool = new AsyncAssetPool<GameObject>(KeyBase);
			m_BackendPool = new AssetPool<GameObject>(pool =>
			{
				// it's important to set GameObjectEntity as last! (or else other components will not get referenced????)
				var gameObject = new GameObject("UIUnitStatus Backend", typeof(RectTransform), typeof(UiHeadOnUnitStatusBackend), typeof(GameObjectEntity));
				gameObject.SetActive(false);

				var backend = gameObject.GetComponent<UiHeadOnUnitStatusBackend>();
				backend.SetRootPool(pool);

				return gameObject;
			}, World);

			m_PresentationPool.AddElements(0);
			m_BackendPool.AddElements(0);

			GetModule(out m_GetAllBackendModule);
		}

		protected override void OnUpdate()
		{
			if (m_InterfaceQuery.CalculateEntityCount() == 0)
			{
				ManageForUnits(default, null);
				m_Root = null;
				return;
			}

			var rootHasChanged = false;
			var hud            = EntityManager.GetComponentObject<UiHeadOnPresentation>(m_InterfaceQuery.GetSingletonEntity());
			if (m_Root != hud.DrawerFrame.UnitStatusFrame)
			{
				rootHasChanged = true;
				m_Root         = hud.DrawerFrame.UnitStatusFrame;
			}

			using (var entities = m_UnitQuery.ToEntityArray(Allocator.TempJob))
			{
				ManageForUnits(entities, hud);
			}

			if (rootHasChanged)
			{
				Entities.ForEach((UiHeadOnUnitStatusBackend backend) => { backend.transform.SetParent(m_Root, false); });
			}
		}

		private void ManageForUnits(NativeArray<Entity> entities, UiHeadOnPresentation hud)
		{
			if (entities == default || !entities.IsCreated)
			{
				Entities.ForEach((UiHeadOnUnitStatusBackend backend) => { backend.Return(true, true); });

				return;
			}

			m_GetAllBackendModule.TargetEntities = entities;
			m_GetAllBackendModule.Update(default).Complete();

			var unattachedBackend = m_GetAllBackendModule.BackendWithoutModel;
			var unattachedCount   = unattachedBackend.Length;
			for (var i = 0; i != unattachedCount; i++)
			{
				var backend = EntityManager.GetComponentObject<UiHeadOnUnitStatusBackend>(unattachedBackend[i]);
				backend.SetDestroyFlags(0);
			}

			var missingEntities = m_GetAllBackendModule.MissingTargets;
			var missingCount    = missingEntities.Length;
			for (var i = 0; i != missingCount; i++)
			{
				using (new SetTemporaryActiveWorld(World))
				{
					var backend = m_BackendPool.Dequeue().GetComponent<UiHeadOnUnitStatusBackend>();
					backend.gameObject.SetActive(true);

					if (m_Root != null)
						backend.transform.SetParent(m_Root.transform, false);

					backend.Hud = hud;
					backend.SetTarget(EntityManager, missingEntities[i]);
					backend.SetPresentationFromPool(m_PresentationPool);
				}
			}
		}
	}
}