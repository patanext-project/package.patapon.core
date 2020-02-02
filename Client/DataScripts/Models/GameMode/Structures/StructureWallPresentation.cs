using System.Collections.Generic;
using DefaultNamespace;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon.Mixed.Units;
using Patapon4TLB.GameModes;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Modules;
using StormiumTeam.GameBase.Systems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace DataScripts.Models.GameMode.Structures
{
	public class StructureWallPresentation : RuntimeAssetPresentation<StructureWallPresentation>
	{
		public MaterialPropertyBlock mpb;
		public List<Renderer>        rendererArray;
		public List<Renderer>        rendererWithTeamColorArray;
		public float                 reintegrationProgress;

		public Animator animator;

		private static readonly int TintPropertyId     = Shader.PropertyToID("_Color");
		private static readonly int ProgressPropertyId = Shader.PropertyToID("_Progress");
		private static readonly int DeadPropertyId     = Animator.StringToHash("Dead");

		private void OnEnable()
		{
			mpb                   = new MaterialPropertyBlock();
			reintegrationProgress = float.NegativeInfinity;
		}

		private void OnDisable()
		{
			mpb.Clear();
			mpb = null;
		}

		private float m_Reintegration;

		internal void OnUpdate(StructureWallBackend backend, bool teamUpdate, bool hasTeam, bool healthUpdate, bool isDead)
		{
			if (teamUpdate)
			{
				if (hasTeam)
				{
					OnCreate();
					m_Reintegration = 0.0f;
				}
			}

			if (healthUpdate && isDead)
			{
				m_Reintegration = 0.0f;
				reintegrationProgress = 0;
			}

			animator.SetBool(DeadPropertyId, isDead);

			m_Reintegration += Time.deltaTime;

			if (!hasTeam)
			{
				SetReintegrationProgress(0, true);
			}
			else
			{
				if (isDead)
				{
					SetReintegrationProgress(math.clamp(1 - m_Reintegration * 0.1f, 0, 1), true);
				}
				else
				{
					SetReintegrationProgress(m_Reintegration * 3f, true);
				}
			}
		}

		public void SetTeamColor(Color color)
		{
			for (var i = 0; i != rendererWithTeamColorArray.Count; i++)
			{
				rendererWithTeamColorArray[i].GetPropertyBlock(mpb);
				{
					mpb.SetColor(TintPropertyId, color);
				}
				rendererWithTeamColorArray[i].SetPropertyBlock(mpb);
			}
		}

		public void SetReintegrationProgress(float progress, bool force = false)
		{
			if (progress.Equals(reintegrationProgress) && !force)
				return;

			reintegrationProgress = progress;
			for (var i = 0; i != rendererArray.Count; i++)
			{
				rendererArray[i].GetPropertyBlock(mpb);
				{
					mpb.SetFloat(ProgressPropertyId, progress);
				}
				rendererArray[i].SetPropertyBlock(mpb);
			}
		}

		public void OnCreate()
		{
			animator.SetTrigger("Create");
		}
	}

	public class StructureWallBackend : RuntimeAssetBackend<StructureWallPresentation>
	{
		public bool IsDead;
		public bool HasTeam;
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class StructureWallRenderSystem : BaseRenderSystem<StructureWallPresentation>
	{
		private ModuleGetAssetFromGuid m_ModuleGetAssetFromGuid;

		protected override void OnCreate()
		{
			base.OnCreate();

			GetModule(out m_ModuleGetAssetFromGuid);
		}

		protected override void PrepareValues()
		{

		}

		protected override void Render(StructureWallPresentation definition)
		{
			var backend   = (StructureWallBackend) definition.Backend;
			var hadTeam   = backend.HasTeam;
			var wasDead   = backend.IsDead;
			var direction = 1;

			var presentation = backend.Presentation;
			var chunk        = EntityManager.GetChunk(backend.DstEntity);
			var comps        = chunk.Archetype.GetComponentTypes();

			if (EntityManager.TryGetComponentData(backend.DstEntity, out Relative<TeamDescription> teamDesc))
			{
				if (teamDesc.Target == default || !EntityManager.TryGetComponentData<Relative<ClubDescription>>(teamDesc.Target, out var relativeClub))
				{
					backend.HasTeam = false;
				}
				else
				{
					var clubInfo = EntityManager.GetComponentData<ClubInformation>(relativeClub.Target);
					presentation.SetTeamColor(clubInfo.PrimaryColor);

					if (EntityManager.TryGetComponentData<UnitDirection>(teamDesc.Target, out var teamDirection))
						direction = teamDirection.Value;

					backend.HasTeam = true;
				}
			}

			LivableHealth health;
			EntityManager.TryGetComponentData(backend.DstEntity, out health);
			presentation.OnUpdate(backend, hadTeam != backend.HasTeam, backend.HasTeam, wasDead != health.ShouldBeDead(), health.ShouldBeDead());

			backend.IsDead = health.ShouldBeDead();

			var pos = EntityManager.GetComponentData<Translation>(backend.DstEntity).Value;
			pos.z += 300;

			backend.transform.position   = pos;
			backend.transform.localScale = new Vector3(direction, 1, 1);
		}

		protected override void ClearValues()
		{

		}
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class StructureWallPoolSystem : PoolingSystem<StructureWallBackend, StructureWallPresentation>
	{
		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Folder("Models")
			              .Folder("GameModes")
			              .Folder("Structures")
			              .Folder("WoodBarricade")
			              .GetFile("WoodenWall.prefab");

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