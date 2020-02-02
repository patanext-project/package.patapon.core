using System;
using System.Collections.Generic;
using DefaultNamespace;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Systems;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using EntityQuery = Unity.Entities.EntityQuery;

namespace DataScripts.Models.GameMode.Structures
{
	public class HeadOnStructurePresentation : RuntimeAssetPresentation<HeadOnStructurePresentation>
	{
		public enum EPhase
		{
			Normal    = 0,
			Captured  = 1,
			Destroyed = 2
		}

		public MaterialPropertyBlock mpb;
		public List<Renderer>        rendererArray;
		public List<Renderer>        rendererWithTeamColorArray;

		public List<Animator> animators;

		[Header("Shader Properties")]
		public string teamTintPropertyId = "_Color";

		[Header("Animator Properties")]
		public string onIdleAnimTrigger = "OnIdle";

		public string onCapturedAnimTrigger  = "OnCaptured";
		public string onDestroyedAnimTrigger = "OnDestroyed";
		public string phaseAnimInt           = "Phase";

		private List<MaterialPropertyBase> m_MaterialProperties;

		protected virtual void OnEnable()
		{
			mpb                  = new MaterialPropertyBlock();
			m_MaterialProperties = new List<MaterialPropertyBase>();
			foreach (var comp in GetComponents<MaterialPropertyBase>())
				m_MaterialProperties.Add(comp);
		}

		protected virtual void OnDisable()
		{
			mpb.Clear();
			mpb = null;
		}

		private Color m_TeamColor;

		public virtual void SetTeamColor(Color color)
		{
			m_TeamColor = color;
		}

		public virtual void Render()
		{
			foreach (var r in rendererWithTeamColorArray)
			{
				r.GetPropertyBlock(mpb);

				if (!string.IsNullOrEmpty(teamTintPropertyId))
					mpb.SetColor(teamTintPropertyId, m_TeamColor);
				r.SetPropertyBlock(mpb);
			}

			foreach (var r in rendererArray)
			{
				r.GetPropertyBlock(mpb);

				foreach (var materialProperty in m_MaterialProperties)
				{
					materialProperty.RenderOn(mpb);
				}

				r.SetPropertyBlock(mpb);
			}
		}

		private EPhase m_PreviousPhase;

		public virtual void SetPhase(EPhase phase)
		{
			foreach (var a in animators) a.SetInteger(phaseAnimInt, (int) phase);

			if (m_PreviousPhase != phase)
			{
				var trigger = string.Empty;
				if (phase == EPhase.Normal)
					trigger = onIdleAnimTrigger;
				if (phase == EPhase.Captured)
					trigger = onCapturedAnimTrigger;
				if (phase == EPhase.Destroyed)
					trigger = onDestroyedAnimTrigger;

				Debug.Log($"On trigger: {trigger}");
				if (trigger != string.Empty)
					foreach (var a in animators)
						a.SetTrigger(trigger);

				m_PreviousPhase = phase;
			}
		}
	}

	public class HeadOnStructureBackend : RuntimeAssetBackend<HeadOnStructurePresentation>
	{
		public bool HasTeam;
	}

	public class HeadOnStructureRenderSystem : BaseRenderSystem<HeadOnStructureBackend>
	{
		protected override void PrepareValues()
		{

		}

		protected override void Render(HeadOnStructureBackend definition)
		{
			if (definition.Presentation == null)
				return;

			var presentation = definition.Presentation;
			var dstEntity    = definition.DstEntity;

			var direction = 1;
			if (EntityManager.TryGetComponentData(dstEntity, out Relative<TeamDescription> teamDesc))
			{
				if (teamDesc.Target == default || !EntityManager.TryGetComponentData<Relative<ClubDescription>>(teamDesc.Target, out var relativeClub))
				{
					definition.HasTeam = false;
					presentation.SetTeamColor(Color.white);
				}
				else
				{
					var clubInfo = EntityManager.GetComponentData<ClubInformation>(relativeClub.Target);
					presentation.SetTeamColor(clubInfo.PrimaryColor);

					if (EntityManager.TryGetComponentData<UnitDirection>(teamDesc.Target, out var teamDirection))
						direction = teamDirection.Value;

					definition.HasTeam = true;
				}
			}

			EntityManager.TryGetComponentData(dstEntity, out LivableHealth health);

			var phase = HeadOnStructurePresentation.EPhase.Normal;
			if (definition.HasTeam)
				phase = HeadOnStructurePresentation.EPhase.Captured;
			if (health.IsDead)
				phase = HeadOnStructurePresentation.EPhase.Destroyed;
			presentation.SetPhase(phase);

			var pos = EntityManager.GetComponentData<Translation>(dstEntity).Value;
			pos.z += 300;

			definition.transform.position   = pos;
			definition.transform.localScale = new Vector3(direction, 1, 1);
			
			presentation.Render();
			presentation.OnSystemUpdate();
		}

		protected override void ClearValues()
		{

		}
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class HeadOnStructurePoolSystem : PoolingSystem<HeadOnStructureBackend, HeadOnStructurePresentation>
	{
		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Folder("Models")
			              .Folder("GameModes")
			              .Folder("Structures")
			              .Folder("CobblestoneBarricade")
			              .GetFile("CobblestoneBarricade.prefab");

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(HeadOnStructure));
		}

		protected override void SpawnBackend(Entity target)
		{
			if (EntityManager.GetComponentData<HeadOnStructure>(target).ScoreType == HeadOnStructure.EScoreType.Tower)
				base.SpawnBackend(target);
		}
	}
}