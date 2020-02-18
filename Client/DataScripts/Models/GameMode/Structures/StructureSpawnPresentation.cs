using System.Collections.Generic;
using DefaultNamespace;
using Misc.Extensions;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon.Mixed.Units;
using Patapon4TLB.GameModes;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Systems;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using Entity = Unity.Entities.Entity;

namespace DataScripts.Models.GameMode.Structures
{
	public class StructureSpawnPresentation : RuntimeAssetPresentation<StructureSpawnPresentation>
	{
		private static readonly int TintPropertyId = Shader.PropertyToID("_Color");

		public Animator animator;

		public MaterialPropertyBlock mpb;
		public List<Renderer>        rendererPrimaryColor;
		public List<Renderer>        rendererSecondaryColor;
		public List<ParticleSystem>  particleSystems;

		private void OnEnable()
		{
			mpb = new MaterialPropertyBlock();
		}

		private void OnDisable()
		{
			mpb.Clear();
			mpb = null;
		}

		public void SetColors(Color primary, Color secondary)
		{
			for (var i = 0; i != rendererPrimaryColor.Count; i++)
			{
				rendererPrimaryColor[i].GetPropertyBlock(mpb);
				{
					mpb.SetColor(TintPropertyId, primary);
				}
				rendererPrimaryColor[i].SetPropertyBlock(mpb);
			}

			for (var i = 0; i != rendererSecondaryColor.Count; i++)
			{
				rendererSecondaryColor[i].GetPropertyBlock(mpb);
				{
					mpb.SetColor(TintPropertyId, secondary);
				}
				rendererSecondaryColor[i].SetPropertyBlock(mpb);
			}

			for (var i = 0; i != particleSystems.Count; i++)
			{
				var module = particleSystems[i].main;
				module.startColor = secondary;
			}
		}
	}

	public class StructureSpawnBackend : RuntimeAssetBackend<StructureSpawnPresentation>
	{
		public bool HasTeam;
	}

	[UpdateInGroup(typeof(OrderGroup.Presentation.InterfaceRendering))]
	public class StructureSpawnRenderSystem : BaseRenderSystem<StructureSpawnPresentation>
	{
		private static readonly int AnimatorKeyIsSet    = Animator.StringToHash("IsSet");
		private static readonly int AnimatorKeyOnCreate = Animator.StringToHash("OnCreate");

		private Entity m_SpectatedEntity;

		protected override void PrepareValues()
		{
			var cameraState = this.GetComputedCameraState().StateData;
			if (cameraState.Target != default)
			{
				m_SpectatedEntity = cameraState.Target;
			}
		}

		protected override void Render(StructureSpawnPresentation definition)
		{
			var backend = (StructureSpawnBackend) definition.Backend;
			var pos     = EntityManager.GetComponentData<Translation>(backend.DstEntity).Value;
			pos.z -= 10;
			pos.y += 2f;

			backend.transform.position = pos;

			var direction      = 1;
			var primaryColor   = Color.white;
			var secondaryColor = Color.gray;
			if (m_SpectatedEntity != default
			    && EntityManager.TryGetComponentData(m_SpectatedEntity, out Relative<TeamDescription> spectatedTeamRelative))
			{
				if (EntityManager.TryGetComponentData(spectatedTeamRelative.Target, out Relative<ClubDescription> clubRelative))
				{
					var clubInfo = EntityManager.GetComponentData<ClubInformation>(clubRelative.Target);
					primaryColor   = clubInfo.PrimaryColor;
					secondaryColor = clubInfo.SecondaryColor;
				}

				if (EntityManager.HasComponent<UnitDirection>(spectatedTeamRelative.Target))
				{
					direction = EntityManager.GetComponentData<UnitDirection>(spectatedTeamRelative.Target).Value;
				}
			}

			var chunk = EntityManager.GetChunk(backend.DstEntity);
			var comps = chunk.Archetype.GetComponentTypes();
			for (var i = 0; i != comps.Length; i++)
			{
				if (comps[i].GetManagedType() == typeof(Relative<TeamDescription>))
				{
					var teamDesc = EntityManager.GetComponentData<Relative<TeamDescription>>(backend.DstEntity);
					if (teamDesc.Target == default)
					{
						backend.HasTeam = false;
						continue;
					}

					if (!backend.HasTeam)
						definition.animator.SetTrigger(AnimatorKeyOnCreate);

					backend.HasTeam = true;
				}
			}

			backend.transform.localScale = new Vector3(direction, 1, 1);

			definition.SetColors(primaryColor, secondaryColor);
			definition.animator.SetBool(AnimatorKeyIsSet, backend.HasTeam);
		}

		protected override void ClearValues()
		{
			m_SpectatedEntity = default;
		}
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class StructureSpawnPoolingSystem : PoolingSystem<StructureSpawnBackend, StructureSpawnPresentation>
	{
		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Folder("Models")
			              .Folder("GameModes")
			              .Folder("Structures")
			              .Folder("StructureSpawn")
			              .GetFile("SpawnFakeUI.prefab");
		                                                            
		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(HeadOnStructure));
		}

		protected override void SpawnBackend(Entity target)
		{
			if (EntityManager.GetComponentData<HeadOnStructure>(target).ScoreType == HeadOnStructure.EScoreType.Wall)
				base.SpawnBackend(target);
		}
	}
}