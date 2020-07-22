using GameBase.Roles.Components;
using PataNext.Client.Core.Addressables;
using PataNext.Client.GameModes.VSHeadOn.Interface;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Utility.AssetBackend;
using StormiumTeam.GameBase.Utility.Pooling;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using StormiumTeam.GameBase.Utility.Rendering.BaseSystems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Color = UnityEngine.Color;

namespace PataNext.Client.DataScripts.Interface.GameMode.VSHeadOn
{
	public abstract class UIHeadOnStructurePresentationBase : RuntimeAssetPresentation<UIHeadOnStructurePresentationBase>
	{
		public abstract void SetTeamColor(Color primary, Color secondary);
	}

	public class UIHeadOnStructureBackend : RuntimeAssetBackend<UIHeadOnStructurePresentationBase>
	{
		public UIHeadOnPresentation PreviousHud;
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class UIHeadOnStructurePoolingSystem : PoolingSystem<UIHeadOnStructureBackend, UIHeadOnStructurePresentationBase>
	{
		protected override string AddressableAsset => string.Empty;

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(HeadOnStructure));
		}

		private AsyncAssetPool<GameObject> m_WallPool;
		private AsyncAssetPool<GameObject> m_TowerPool;
		private AsyncAssetPool<GameObject> m_TowerControlPool;

		protected override void OnCreate()
		{
			base.OnCreate();

			var builder = AddressBuilder.Client()
			                            .Interface()
			                            .GameMode()
			                            .Folder("VSHeadOn")
			                            .Folder("Structures");
			m_WallPool = new AsyncAssetPool<GameObject>(builder.Folder("Wall").GetFile("WallIcon.prefab"));
			m_TowerPool = new AsyncAssetPool<GameObject>(builder.Folder("Tower").GetFile("TowerIcon.prefab"));
			m_TowerControlPool = new AsyncAssetPool<GameObject>(builder.Folder("TowerControl").GetFile("TowerControlIcon.prefab"));
		}

		protected override void SpawnBackend(Entity target)
		{
			base.SpawnBackend(target);

			if (EntityManager.GetComponentData<HeadOnStructure>(target).ScoreType == HeadOnStructure.EScoreType.Wall)
				LastBackend.SetPresentationFromPool(m_WallPool);
			if (EntityManager.GetComponentData<HeadOnStructure>(target).ScoreType == HeadOnStructure.EScoreType.Tower)
				LastBackend.SetPresentationFromPool(m_TowerPool);
			if (EntityManager.GetComponentData<HeadOnStructure>(target).ScoreType == HeadOnStructure.EScoreType.TowerControl)
				LastBackend.SetPresentationFromPool(m_TowerControlPool);
		}
	}

	[UpdateInGroup(typeof(OrderGroup.Presentation.InterfaceRendering))]
	public abstract class UIHeadOnStructureBaseRenderSystem<T> : BaseRenderSystem<T>
		where T : UIHeadOnStructurePresentationBase
	{
		public abstract bool DefaultBehavior { get; }

		public UIHeadOnPresentation Hud;
		
		public Relative<TeamDescription> TeamRelative;
		public UnitDirection TeamDirection;
		public ClubInformation ClubInformation;
		public LivableHealth Health;
		public float3 PositionOnDrawer;

		public MpVersusHeadOn GameMode;
		
		private EntityQuery m_HudQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_HudQuery = GetEntityQuery(typeof(UIHeadOnPresentation));

			RequireForUpdate(m_HudQuery);
		}

		protected override void PrepareValues()
		{
			Hud = EntityManager.GetComponentObject<UIHeadOnPresentation>(m_HudQuery.GetSingletonEntity());
			if (HasSingleton<MpVersusHeadOn>())
				GameMode = GetSingleton<MpVersusHeadOn>();
		}

		protected override void Render(T definition)
		{
			var targetEntity = definition.Backend.DstEntity;
			var backend = (UIHeadOnStructureBackend) definition.Backend;
			
			ClubInformation = new ClubInformation {PrimaryColor = Color.white, SecondaryColor = Color.grey};
			if (EntityManager.TryGetComponentData(targetEntity, out TeamRelative))
			{
				EntityManager.TryGetComponentData(TeamRelative.Target, out TeamDirection);

				if (EntityManager.TryGetComponentData(TeamRelative.Target, out Relative<ClubDescription> clubRelative))
					ClubInformation = EntityManager.GetComponentData<ClubInformation>(clubRelative.Target);
			}
			else
			{
				TeamRelative = default;
				TeamDirection = default;
			}

			EntityManager.TryGetComponentData(targetEntity, out Health);

			if (backend.PreviousHud != Hud)
			{
				backend.PreviousHud = Hud;
				backend.transform.SetParent(Hud.DrawerFrame.GetDrawer(UIHeadOnDrawerType.Structure), false);
			}

			PositionOnDrawer = backend.PreviousHud.GetPositionOnDrawer(EntityManager.GetComponentData<Translation>(targetEntity).Value, DrawerAlignment.Bottom);
			PositionOnDrawer.z = 0;

			if (DefaultBehavior)
			{
				var transform = backend.transform;
				transform.localPosition = PositionOnDrawer;

				var scale = TeamDirection.Value;
				if (Health.IsDead)
					scale = 0;
				transform.localScale = new Vector3(scale, 1, 1);
				
				definition.SetTeamColor(ClubInformation.PrimaryColor, ClubInformation.SecondaryColor);
			}
		}

		protected override void ClearValues()
		{
			
		}
	}
}