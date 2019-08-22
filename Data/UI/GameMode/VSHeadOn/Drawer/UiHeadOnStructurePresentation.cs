using Patapon4TLB.GameModes;
using Patapon4TLB.UI.GameMode.VSHeadOn;
using Patapon4TLB.UI.InGame;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Misc;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

namespace Data.UI.GameMode.VSHeadOn.Drawer
{
	public class UiHeadOnStructurePresentation : RuntimeAssetPresentation<UiHeadOnStructurePresentation>
	{
		public MaskableGraphic[] graphics;

		public void SetColor(Color primary)
		{
			for (var i = 0; i != graphics.Length; i++)
			{
				graphics[i].color = primary;
			}
		}
	}

	public class UiHeadOnStructureBackend : RuntimeAssetBackend<UiHeadOnStructurePresentation>
	{
		public UiHeadOnPresentation Hud;

		public RectTransform rectTransform { get; private set; }

		public override void OnTargetUpdate()
		{
			DstEntityManager.AddComponentData(BackendEntity, RuntimeAssetDisable.All);
		}

		public override void OnComponentEnabled()
		{
			rectTransform = GetComponent<RectTransform>();
		}

		[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
		public class ProcessSystem : UIGameSystemBase
		{
			protected override void OnUpdate()
			{
				Entities.ForEach((UiHeadOnStructureBackend backend) =>
				{
					if (backend.Presentation == null)
						return;
					if (!EntityManager.HasComponent<Translation>(backend.DstEntity))
						return;

					var scale = Vector3.one;
					var color = Color.gray;
					if (TryGetRelative<TeamDescription>(backend.DstEntity, out var teamDesc))
					{
						if (EntityManager.HasComponent<TeamDirection>(teamDesc))
						{
							scale.x *= EntityManager.GetComponentData<TeamDirection>(teamDesc).Value;
						}

						if (TryGetRelative<ClubDescription>(teamDesc, out var clubDesc))
						{
							var clubInfo = EntityManager.GetComponentData<ClubInformation>(clubDesc);
							color = clubInfo.PrimaryColor;
						}
					}
					else
						scale *= 0;

					if (EntityManager.HasComponent<LivableHealth>(backend.DstEntity))
					{
						var health = EntityManager.GetComponentData<LivableHealth>(backend.DstEntity);
						if (health.ShouldBeDead())
							scale *= 0;
					}
					
					var positionOnDrawer = backend.Hud.GetPositionOnDrawer(EntityManager.GetComponentData<Translation>(backend.DstEntity).Value);
					backend.transform.localPosition = new Vector3
					{
						x = positionOnDrawer.x
					};
					backend.transform.localScale = scale;
					backend.Presentation.SetColor(color);
				});
			}
		}

		[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
		public class GenerateSystem : GameBaseSystem
		{
			private EntityQuery m_InterfaceQuery;
			private EntityQuery m_StructureQuery;

			private GetAllBackendModule<UiHeadOnStructureBackend> m_GetAllBackendModule;

			private AssetPool<GameObject>      m_BackendPool;
			private AsyncAssetPool<GameObject> m_PresentationPool;
			private RectTransform              m_Root;

			protected override void OnCreate()
			{
				base.OnCreate();

				GetModule(out m_GetAllBackendModule);

				m_InterfaceQuery = GetEntityQuery(typeof(UiHeadOnPresentation));
				m_StructureQuery = GetEntityQuery(new EntityQueryDesc
				{
					All = new ComponentType[] {typeof(HeadOnStructure)},
				});
				m_BackendPool = new AssetPool<GameObject>((pool) =>
				{
					var gameObject = new GameObject("Wall Backend", typeof(UiHeadOnStructureBackend), typeof(GameObjectEntity));
					gameObject.SetActive(false);

					return gameObject;
				}, World);
				m_PresentationPool = new AsyncAssetPool<GameObject>("int:UI/GameModes/Shared/WallIcon.prefab");
			}

			protected override void OnUpdate()
			{
				if (m_InterfaceQuery.CalculateEntityCount() == 0)
				{
					ManageFor(default, null);
					m_Root = null;
					return;
				}

				var rootHasChanged = false;
				var hud            = EntityManager.GetComponentObject<UiHeadOnPresentation>(m_InterfaceQuery.GetSingletonEntity());
				if (m_Root != hud.DrawerFrame.StructureFrame)
				{
					rootHasChanged = true;
					m_Root         = hud.DrawerFrame.StructureFrame;
				}

				using (var entities = m_StructureQuery.ToEntityArray(Allocator.TempJob))
				{
					ManageFor(entities, hud);
				}

				if (rootHasChanged)
				{
					Entities.ForEach((UiHeadOnStructureBackend backend) => { backend.transform.SetParent(m_Root, false); });
				}
			}

			private void ManageFor(NativeArray<Entity> entities, UiHeadOnPresentation hud)
			{
				if (entities == default || !entities.IsCreated)
				{
					Entities.ForEach((UiHeadOnStructureBackend backend) => { backend.Return(true, true); });

					return;
				}

				m_GetAllBackendModule.TargetEntities = entities;
				m_GetAllBackendModule.Update(default).Complete();

				var unattachedBackend = m_GetAllBackendModule.BackendWithoutModel;
				var unattachedCount   = unattachedBackend.Length;
				for (var i = 0; i != unattachedCount; i++)
				{
					var backend = EntityManager.GetComponentObject<UiHeadOnStructureBackend>(unattachedBackend[i]);
					backend.SetDestroyFlags(0);
				}

				var missingEntities = m_GetAllBackendModule.MissingTargets;
				var missingCount    = missingEntities.Length;
				for (var i = 0; i != missingCount; i++)
				{
					using (new SetTemporaryActiveWorld(World))
					{
						var backend = m_BackendPool.Dequeue().GetComponent<UiHeadOnStructureBackend>();
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
}