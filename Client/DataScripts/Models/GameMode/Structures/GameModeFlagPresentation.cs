using System;
using MonoComponents;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes.VSHeadOn;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Systems;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace DataScripts.Models.GameMode.Structures
{
	public class GameModeFlagPresentation : RuntimeAssetPresentation<GameModeFlagPresentation>
	{
		private static readonly int Tint2PropertyId = Shader.PropertyToID("_OverlayColor");
		
		public Renderer[] renderersForTeamColor;
		public MaterialPropertyBlock mpb;
		
		private void OnEnable()
		{
			mpb = new MaterialPropertyBlock();
		}

		private void OnDisable()
		{
			mpb.Clear();
			mpb = null;
		}

		private Color m_LastTeamColor;
		public void SetTeamColor(Color color)
		{
			if (m_LastTeamColor == color)
				return;
			m_LastTeamColor = color;
			
			foreach (var r in renderersForTeamColor)
			{
				r.GetPropertyBlock(mpb);
				mpb.SetColor(Tint2PropertyId, color);
				r.SetPropertyBlock(mpb);
			}
		}
		
	}

	public class GameModeFlagBackend : RuntimeAssetBackend<GameModeFlagPresentation>
	{
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class GameModeFlagPoolingSystem : PoolingSystem<GameModeFlagBackend, GameModeFlagPresentation>
	{
		protected override string AddressableAsset => string.Empty;

		protected override Type[] AdditionalBackendComponents => new Type[] {typeof(SortingGroup)};

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(HeadOnFlag));
		}

		protected override void SpawnBackend(Entity target)
		{
			base.SpawnBackend(target);

			var sortingGroup = LastBackend.GetComponent<SortingGroup>();
			sortingGroup.sortingLayerName = "MovableStructures";
			sortingGroup.sortingOrder     = 0;
		}
	}

	[AlwaysSynchronizeSystem]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class GameModeFlagSetPresentation : JobGameBaseSystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			Entities.ForEach((GameModeFlagBackend backend) =>
			{
				if (backend.HasIncomingPresentation)
					return;
				
				var pool = StaticSceneResourceHolder.GetPool("versus:flag");
				if (pool == null)
					return;
				
				backend.SetPresentationFromPool(pool);
			}).WithStructuralChanges().Run();
			
			return default;
		}
	}

	[UpdateInGroup(typeof(OrderGroup.Presentation.AfterSimulation))]
	public class GameModeFlagRenderSystem : BaseRenderSystem<GameModeFlagPresentation>
	{
		protected override void PrepareValues()
		{
			
		}

		protected override void Render(GameModeFlagPresentation definition)
		{
			var backend = definition.Backend;
			EntityManager.TryGetComponentData(backend.DstEntity, out Translation translation);
			backend.transform.position = new Vector3 {x = translation.Value.x, z = 100};

			if (EntityManager.TryGetComponentData(backend.DstEntity, out Relative<TeamDescription> relativeTeam)
			    && EntityManager.TryGetComponentData(relativeTeam.Target, out Relative<ClubDescription> relativeClub))
			{
				var clubInfo = EntityManager.GetComponentData<ClubInformation>(relativeClub.Target);
				definition.SetTeamColor(clubInfo.PrimaryColor);
			}
		}

		protected override void ClearValues()
		{
			
		}
	}
}