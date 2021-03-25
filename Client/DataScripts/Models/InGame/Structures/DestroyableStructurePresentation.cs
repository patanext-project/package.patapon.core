using System.Collections.Generic;
using System.Linq;
using package.stormiumteam.shared.ecs;
using PataNext.Client.DataScripts.Models.Projectiles;
using PataNext.Client.Systems;
using PataNext.Module.Simulation.Components.Units;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems.Ext;
using StormiumTeam.GameBase.GamePlay.Health;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Roles.Descriptions;
using StormiumTeam.GameBase.Utility.AssetBackend;
using StormiumTeam.GameBase.Utility.Rendering.BaseSystems;
using StormiumTeam.GameBase.Utility.Rendering.MaterialProperty;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace PataNext.Client.DataScripts.Models.InGame.Structures
{
	public class DestroyableStructurePresentation : EntityVisualPresentation
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

		public AudioClip onCaptureSound;

		public ECSoundEmitterComponent onCaptureSoundEmitter = new ECSoundEmitterComponent
		{
			volume       = 1,
			spatialBlend = 0,
			position     = 0,
			minDistance  = 20,
			maxDistance  = 40,
			rollOf       = AudioRolloffMode.Logarithmic
		};

		public AudioClip onDestroySound;

		public ECSoundEmitterComponent onDestroySoundEmitter = new ECSoundEmitterComponent
		{
			volume       = 1,
			spatialBlend = 0,
			position     = 0,
			minDistance  = 15,
			maxDistance  = 30,
			rollOf       = AudioRolloffMode.Logarithmic
		};

		public  bool                       GetPropertiesFromChildren = false;
		private List<MaterialPropertyBase> m_MaterialProperties;

		private Renderer[] m_computedRenderersTeamArray;
		private Renderer[] m_computedRenderersNormalArray;

		public override void OnBackendSet()
		{
			base.OnBackendSet();
			
			Backend.GetComponent<SortingGroup>()
			       .sortingLayerName = "MovableStructures";
		}

		protected virtual void OnEnable()
		{
			mpb                  = new MaterialPropertyBlock();
			m_MaterialProperties = new List<MaterialPropertyBase>();
			foreach (var comp in GetPropertiesFromChildren
				? GetComponentsInChildren<MaterialPropertyBase>()
				: GetComponents<MaterialPropertyBase>())
				m_MaterialProperties.Add(comp);

			var computedRenderersTeamArray = new List<Renderer>(16);
			computedRenderersTeamArray.AddRange(rendererWithTeamColorArray);
			m_computedRenderersTeamArray = computedRenderersTeamArray.ToArray();

			var computedRenderersNormalArray = new List<Renderer>(16);
			computedRenderersNormalArray.AddRange(rendererArray);
			m_computedRenderersNormalArray = computedRenderersNormalArray.Except(m_computedRenderersTeamArray).ToArray();

			// copy mat
			void createmat(Renderer[] renders)
			{
				foreach (var r in renders)
				{
					if (r.sharedMaterial != null)
						r.material = new Material(r.sharedMaterial);
				}
			}

			createmat(m_computedRenderersNormalArray);
			createmat(m_computedRenderersTeamArray);
		}

		protected virtual void OnDisable()
		{
			mpb.Clear();
			mpb = null;
		}

		private void OnDestroy()
		{
			foreach (var r in rendererArray)
			{
				r.SetPropertyBlock(null);
				Destroy(r.material);
			}

			foreach (var r in rendererWithTeamColorArray)
			{
				r.SetPropertyBlock(null);
				Destroy(r.material);
			}
		}

		private Color m_TeamColor;

		public virtual void SetTeamColor(Color color)
		{
			m_TeamColor = color;
		}

		public virtual void Render()
		{
			mpb.Clear();

			foreach (var materialProperty in m_MaterialProperties)
			{
				materialProperty.RenderOn(mpb);
			}

			foreach (var r in m_computedRenderersNormalArray)
			{
				r.GetPropertyBlock(mpb);
				foreach (var materialProperty in m_MaterialProperties)
				{
					materialProperty.RenderOn(mpb);
				}

				r.SetPropertyBlock(mpb);
			}

			foreach (var r in m_computedRenderersTeamArray)
			{
				r.GetPropertyBlock(mpb);
				mpb.SetColor(teamTintPropertyId, m_TeamColor);
				foreach (var materialProperty in m_MaterialProperties)
				{
					materialProperty.RenderOn(mpb);
				}

				r.SetPropertyBlock(mpb);
			}
		}

		private EPhase m_PreviousPhase;

		public virtual void SetPhase(EPhase phase, bool sameTeam)
		{
			foreach (var a in animators) a.SetInteger(phaseAnimInt, (int) phase);

			if (m_PreviousPhase != phase)
			{
				AudioClip               clipToPlay = null;
				ECSoundEmitterComponent emitter    = default;

				var trigger = string.Empty;
				if (phase == EPhase.Normal)
					trigger = onIdleAnimTrigger;
				if (phase == EPhase.Captured)
				{
					trigger = onCapturedAnimTrigger;
					if (sameTeam)
					{
						clipToPlay = onCaptureSound;
						emitter    = onCaptureSoundEmitter;
					}
				}

				if (phase == EPhase.Destroyed)
				{
					trigger    = onDestroyedAnimTrigger;
					clipToPlay = onDestroySound;
					emitter    = onDestroySoundEmitter;
				}

				if (trigger != string.Empty)
					foreach (var a in animators)
						a.SetTrigger(trigger);

				m_PreviousPhase = phase;

				if (clipToPlay != null)
				{
					var entityManager = Backend.DstEntityManager;
					var world         = entityManager.World;

					var soundDef = world.GetExistingSystem<ECSoundSystem>().ConvertClip(clipToPlay);
					if (soundDef.IsValid)
					{
						var soundEntity = entityManager.CreateEntity(typeof(ECSoundEmitterComponent), typeof(ECSoundDefinition), typeof(ECSoundOneShotTag));
						emitter.position = transform.position;

						Debug.LogError("play: " + clipToPlay);

						entityManager.SetComponentData(soundEntity, emitter);
						entityManager.SetComponentData(soundEntity, soundDef);
					}
				}
			}
		}

		private bool state_hasTeam;

		[Unity.Entities.UpdateInGroup(typeof(OrderGroup.Presentation.AfterSimulation))]
		public class RenderSystem : BaseRenderSystem<DestroyableStructurePresentation>
		{
			public Entity PlayerTeam;

			protected override void PrepareValues()
			{
				var camState = this.GetComputedCameraState().StateData;
				if (EntityManager.TryGetComponentData(camState.Target, out Relative<TeamDescription> relativeTeam))
				{
					PlayerTeam = relativeTeam.Target;
				}
			}

			protected override void Render(DestroyableStructurePresentation definition)
			{
				if (!(definition.Backend is EntityVisualBackend backend))
					return;

				backend.letPresentationUpdateTransform = true;

				var dstEntity = backend.DstEntity;

				var direction = 1;
				var sameTeam  = false;
				if (EntityManager.TryGetComponentData(dstEntity, out Relative<TeamDescription> teamDesc))
				{
					sameTeam = teamDesc.Target == PlayerTeam;

					if (teamDesc.Target == default || !EntityManager.TryGetComponentData<Relative<ClubDescription>>(teamDesc.Target, out var relativeClub))
					{
						definition.state_hasTeam = false;
						definition.SetTeamColor(Color.white);
					}
					else
					{
						var clubInfo = EntityManager.GetComponentData<ClubInformation>(relativeClub.Target);
						definition.SetTeamColor(clubInfo.PrimaryColor);

						if (EntityManager.TryGetComponentData<UnitDirection>(teamDesc.Target, out var teamDirection))
							direction = teamDirection.Value;

						definition.state_hasTeam = true;
					}
				}

				var phase = EPhase.Normal;
				if (definition.state_hasTeam)
					phase = EPhase.Captured;
				if (HasComponent<LivableIsDead>(dstEntity))
					phase = EPhase.Destroyed;

				definition.SetPhase(phase, sameTeam);

				var pos = EntityManager.GetComponentData<Translation>(dstEntity).Value;
				pos.z += 300;

				backend.transform.position   = pos;
				backend.transform.localScale = new Vector3(direction, 1, 1);

				definition.Render();
			}

			protected override void ClearValues()
			{

			}
		}
	}
}