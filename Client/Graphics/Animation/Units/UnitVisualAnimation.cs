using package.patapon.core.Animation;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Systems;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Playables;
using GameObjectEntity = Unity.Entities.GameObjectEntity;

namespace Patapon.Client.Graphics.Animation.Units
{
	public abstract class UnitVisualPresentation : RuntimeAssetPresentation<UnitVisualPresentation>
	{
		public Animator Animator;

		public abstract void UpdateData();
	}

	public class UnitVisualPlayableBehaviourData : PlayableBehaviorData
	{
		public UnitVisualAnimation VisualAnimation;
		public TargetAnimation     CurrAnimation => VisualAnimation.CurrAnimation;
		public double              RootTime      => VisualAnimation.RootTime;
	}

	public class UnitVisualAnimation : VisualAnimation
	{
		public double                 RootTime     => rootMixer.GetTime();
		public UnitVisualBackend      Backend      { get; private set; }
		public UnitVisualPresentation Presentation { get; private set; }

		public TargetAnimation CurrAnimation { get; private set; } = new TargetAnimation(null);

		public void OnDisable()
		{
			DestroyPlayableGraph();
		}

		public void OnBackendSet(UnitVisualBackend backend)
		{
			Backend = backend;

			DestroyPlayableGraph();
			CreatePlayableGraph($"{backend.DstEntity}");
			CreatePlayable();
			
			m_PlayableGraph.Stop();
		}

		public void OnPresentationSet(UnitVisualPresentation presentation)
		{
			Presentation = presentation;
			SetAnimatorOutput("standard output", presentation.Animator);
			
			m_PlayableGraph.Stop();
			m_PlayableGraph.Play();
		}

		public void SetTargetAnimation(TargetAnimation target)
		{
			CurrAnimation = target;
		}

		public UnitVisualPlayableBehaviourData GetBehaviorData()
		{
			return new UnitVisualPlayableBehaviourData
			{
				DstEntity        = Backend.DstEntity,
				DstEntityManager = Backend.DstEntityManager,
				VisualAnimation  = this
			};
		}
	}

	public class UnitVisualBackend : RuntimeAssetBackend<UnitVisualPresentation>
	{
		private UnitVisualAnimation m_Animation;

		public UnitVisualAnimation Animation => m_Animation;

		public string CurrentArchetype;

		public override void OnTargetUpdate()
		{
			if (!TryGetComponent(out m_Animation))
			{
				m_Animation = gameObject.AddComponent<UnitVisualAnimation>();
			}

			m_Animation.OnBackendSet(this);
			DstEntityManager.AddComponentData(BackendEntity, RuntimeAssetDisable.All);
		}

		public override void OnPresentationSet()
		{
			Animation.OnPresentationSet(Presentation);
			Presentation.Animator.runtimeAnimatorController = null;
		}

		public override void OnReset()
		{
			GetComponent<UnitVisualAnimation>().DestroyPlayableGraph();
			m_Animation      = null;
			CurrentArchetype = string.Empty;
		}
	}

	public class UnitVisualBackendSpawnSystem : PoolingSystem<UnitVisualBackend, UnitVisualPresentation>
	{
		protected override string AddressableAsset => "core://Client/Models/UberHero/EmptyPresentation.prefab";
		
		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(UnitDescription));
		}
	}
}