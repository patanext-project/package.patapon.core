using System;
using BundleSystem;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Core.Addressables;
using PataNext.Module.Simulation.Components.Roles;
using PataNext.Module.Simulation.Components.Units;
using PataNext.Module.Simulation.Resources;
using StormiumTeam.GameBase.Utility.AssetBackend;
using StormiumTeam.GameBase.Utility.AssetBackend.Components;
using StormiumTeam.GameBase.Utility.Misc;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;

namespace PataNext.Client.Graphics.Animation.Units.Base
{
	public abstract class UnitVisualPresentation : RuntimeAssetPresentation
	{
		public Animator Animator;

		/// <summary>
		/// Return the folders for animation.
		/// </summary>
		/// <remarks>
		///	The order is the OverrideObject then folders.
		/// If the first folder does not contains the animation, it will search it on the next.
		/// </remarks>
		public AssetPathSerializable[] animationAssetFolders;

		/// <summary>
		/// Get the cache for animation. If empty, animations will not be re-used
		/// </summary>
		public string animationCacheId;

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
			Presentation                  = presentation;
			Presentation.Animator.enabled = true;

			// reset graph ofc when getting a new presentation
			DestroyPlayableGraph();
			CreatePlayableGraph($"{Backend.DstEntity}");
			CreatePlayable(presentation.Animator);

			SetAnimatorOutput("standard output", presentation.Animator);
			m_PlayableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

			CurrAnimation = new TargetAnimation(null);

			m_PlayableGraph.Stop();
			m_PlayableGraph.Play();
		}

		public void SetTargetAnimationWithTypeKeepTransition(Type type)
		{
			CurrAnimation = new TargetAnimation(type,
				transitionStart: CurrAnimation.TransitionStart, transitionEnd: CurrAnimation.TransitionEnd,
				previousType: CurrAnimation.Type);
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
		public UnitKitResource       CurrentKitResource;
		public UnitArchetypeResource CurrentArchetypeResource;

		public  AssetPath              CurrentArchetype;
		private UnitVisualAnimation m_Animation;

		public UnitVisualAnimation Animation => m_Animation;

		public override void OnTargetUpdate()
		{
			CurrentKitResource       = default;
			CurrentArchetypeResource = default;
			
			if (!TryGetComponent(out m_Animation))
			{
				m_Animation = gameObject.AddComponent<UnitVisualAnimation>();
				if (!DstEntityManager.HasComponent(BackendEntity, typeof(UnitVisualAnimation)))
				{
					DstEntityManager.AddComponentObject(BackendEntity, m_Animation);
				}
			}

			m_Animation.OnBackendSet(this);
			DstEntityManager.AddComponentData(BackendEntity, RuntimeAssetDisable.All);
		}

		public override void OnPresentationSet()
		{
			// set layer recursive...
			foreach (var tr in gameObject.GetComponentsInChildren<Transform>())
				tr.gameObject.layer = gameObject.layer;
			
			Presentation.Animator.WriteDefaultValues();
			Presentation.Animator.Rebind();
			Animation.OnPresentationSet(Presentation);
			Presentation.Animator.runtimeAnimatorController = null;
		}

		public override void ReturnPresentation(bool unsetChildren = true)
		{
			if (Presentation != null && Presentation.Animator != null)
			{
				Presentation.Animator.WriteDefaultValues();
				Presentation.Animator.Rebind();
			}
			base.ReturnPresentation(unsetChildren);
		}

		public override void OnReset()
		{
			if (m_Animation != null)
			{
				m_Animation.DestroyPlayableGraph();
				m_Animation = null;
			}

			CurrentArchetype = AssetPath.Empty;
		}
	}

	public struct UnitVisualSourceBackend : IComponentData
	{
		public Entity Backend;
	}
	
	public class UnitVisualBackendSpawnSystem : PoolingSystem<UnitVisualBackend, UnitVisualPresentation>
	{
		protected override AssetPath AddressableAsset => AddressBuilder.Client()
		                                                            .Folder("Models")
		                                                            .Folder("UberHero")
		                                                            .GetAsset("EmptyPresentation");

		protected override Type[] AdditionalBackendComponents => new Type[] {typeof(SortingGroup)};

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(UnitDescription));
		}

		protected override void SpawnBackend(Entity target)
		{
			base.SpawnBackend(target);
			EntityManager.SetOrAddComponentData(target, new UnitVisualSourceBackend {Backend = LastBackend.BackendEntity});

			var sortingGroup = LastBackend.GetComponent<SortingGroup>();
			sortingGroup.sortingLayerName = "Entities";
			sortingGroup.sortingOrder     = 0;

			LastBackend.gameObject.layer     = LayerMask.NameToLayer("Entities");
			LastBackend.transform.localScale = new Vector3(1, 1, 0.1f);
		}
	}
}