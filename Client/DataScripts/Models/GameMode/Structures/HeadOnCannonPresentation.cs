using System;
using GameBase.Roles.Components;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Utility.AssetBackend;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using StormiumTeam.GameBase.Utility.Rendering.BaseSystems;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace PataNext.Client.DataScripts.Models.GameMode.Structures
{
	public class HeadOnCannonPresentation : RuntimeAssetPresentation<HeadOnCannonPresentation>
	{
		public enum EPhase {
			NotInit,
			Idle,
			None
		}
		
		public string fireTrigger = "Fire";
		public string idleTrigger = "Idle";
		public string upTrigger   = "Up";
		public string noneTrigger = "None";
		
		private static readonly int TintPropertyId = Shader.PropertyToID("_Color");

		public Animator[] animators;

		public override void OnReset()
		{
			base.OnReset();

			m_PreviousPhase = EPhase.NotInit;
		}
		
		public Renderer[]            renderersForTeamColor;
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
				mpb.SetColor(TintPropertyId, color);
				r.SetPropertyBlock(mpb);
			}
		}

		private EPhase m_PreviousPhase;
		public void SetPhase(EPhase phase)
		{
			if (m_PreviousPhase == phase)
				return;
			
			string trigger;

			m_PreviousPhase = phase;
			switch (phase)
			{
				case EPhase.Idle:
					trigger = upTrigger;
					break;
				case EPhase.None:
					trigger = noneTrigger;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(phase), phase, null);
			}

			if (trigger != string.Empty)
			{
				foreach (var a in animators)
					a.SetTrigger(trigger);
			}
		}

		private UTick m_PreviousFireTick;
		public void SetFireTick(UTick tick)
		{
			if (m_PreviousFireTick == tick)
				return;
			m_PreviousFireTick = tick;
			
			foreach (var a in animators)
				a.SetTrigger(fireTrigger);
		}
	}

	public class HeadOnCannonBackend : RuntimeAssetBackend<HeadOnCannonPresentation>
	{
		
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	[UpdateInGroup(typeof(OrderGroup.Presentation.AfterSimulation))]
	public class HeadOnCannonPoolingSystem : PoolingSystem<HeadOnCannonBackend, HeadOnCannonPresentation>
	{
		protected override Type[] AdditionalBackendComponents => new[] {typeof(SortingGroup)};

		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Folder("Models")
			              .Folder("GameModes")
			              .Folder("Cannon")
			              .GetFile("Cannon.prefab");
		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(HeadOnCannon), typeof(Translation), typeof(Relative<TeamDescription>));
		}

		protected override void SpawnBackend(Entity target)
		{
			base.SpawnBackend(target);

			var sortingGroup = LastBackend.GetComponent<SortingGroup>();
			sortingGroup.sortingLayerName = "MovableStructures";
			sortingGroup.sortingOrder = -1;
		}
		
	}

	public class HeadOnCannonRenderSystem : BaseRenderSystem<HeadOnCannonPresentation>
	{
		protected override void PrepareValues()
		{
			
		}

		protected override void Render(HeadOnCannonPresentation definition)
		{
			var backend = definition.Backend;
			var pos     = EntityManager.GetComponentData<Translation>(backend.DstEntity).Value;
			pos.z = 100;

			backend.transform.position = pos;

			var team = EntityManager.GetComponentData<Relative<TeamDescription>>(backend.DstEntity);
			if (EntityManager.TryGetComponentData(team.Target, out UnitDirection direction))
			{
				backend.transform.localScale = new Vector3
				{
					x = direction.Value,
					y = 1, z = 1
				};
			}

			var cannonData = EntityManager.GetComponentData<HeadOnCannon>(backend.DstEntity);
			definition.SetPhase(cannonData.Active ? HeadOnCannonPresentation.EPhase.Idle : HeadOnCannonPresentation.EPhase.None);
			if (cannonData.NextShootTick.Value > 0 && cannonData.NextShootTick <= ServerTick && cannonData.Active)
				definition.SetFireTick(cannonData.NextShootTick);
			definition.OnSystemUpdate();
			
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