using System;
using package.stormiumteam.shared.ecs;
using PataNext.Module.Simulation.Components.GamePlay.Units;
using StormiumTeam.GameBase.Utility.AssetBackend;
using Unity.Mathematics;
using UnityEngine;

namespace PataNext.Client.Graphics.Models
{
	public class PataponBodyManager : MonoBehaviour, IBackendReceiver
	{
		[Serializable]
		public struct AnimationState
		{
			public bool customizeEyelid;

			public bool customizeBodyDirection;
			public bool customizeEyeDirection;
		}

		[SerializeField]
		private AnimationState animationState;

		public AnimationState Animation
		{
			get => animationState;
			set => animationState = value;
		}

		[Serializable]
		public struct BodyProfile
		{
			public Texture2D eyelidTexture;
			[Range(0, 1)]
			public float     eyelidStrength;

			public bool    customEyeDirection;
			public Vector2 eyeDirection;

			public bool    customBodyDirection;
			public Vector2 bodyDirection;
		}

		[SerializeField]
		private BodyProfile idleProfile;

		[SerializeField]
		private BodyProfile enemyOnSightProfile;

		public BodyProfile IdleProfile
		{
			get => idleProfile;
			set => idleProfile = value;
		}

		public BodyProfile EnemyOnSightProfile
		{
			get => enemyOnSightProfile;
			set => enemyOnSightProfile = value;
		}

		public BodyProfile CurrentProfile { get; set; }

		public RuntimeAssetBackendBase Backend { get; set; }

		[SerializeField]
		private PataponBodyController[] controllers;

		[SerializeField]
		private bool editorPreview;

		[SerializeField]
		private bool editorIsEnemyOnSightPreview;

		private void OnDrawGizmos()
		{
			if (editorPreview)
			{
				CurrentProfile = editorIsEnemyOnSightPreview ? EnemyOnSightProfile : IdleProfile;
				
				foreach (var controller in controllers)
				{
					if (false == animationState.customizeEyelid)
					{
						controller.eyelidTexture     = CurrentProfile.eyelidTexture;
						controller.eyelidTextureStep = CurrentProfile.eyelidStrength;
					}

					if (false == animationState.customizeEyeDirection && CurrentProfile.customEyeDirection)
					{
						controller.eyeDirection.localRotation = Quaternion.Euler(CurrentProfile.eyeDirection.y, -CurrentProfile.eyeDirection.x, 0);
					}

					if (false == animationState.customizeBodyDirection && CurrentProfile.customBodyDirection)
					{
						controller.bodyDirection.localRotation = Quaternion.Euler(CurrentProfile.bodyDirection.y, -CurrentProfile.bodyDirection.x, 0);
					}
				}				
			}
		}

		private bool initialFrame;

		private void OnEnable()
		{
			initialFrame = true;
		}

		public void OnBackendSet()
		{
			previousEyeDirection  = new Vector3[controllers.Length];
			previousBodyDirection = new Vector3[controllers.Length];
		}

		private Vector3[] previousEyeDirection;
		private Vector3[] previousBodyDirection;

		public void OnPresentationSystemUpdate()
		{
			var entMgr = Backend.DstEntityManager;
			var ent    = Backend.DstEntity;

			if (entMgr.TryGetComponentData(ent, out UnitEnemySeekingState seekingState))
			{
				CurrentProfile = seekingState.Enemy != default ? enemyOnSightProfile : idleProfile;
			}
		}

		private void Update()
		{
			// TODO: Make sure that in the future those "previous" states are still done before the animation phase
			for (var i = 0; i < controllers.Length; i++)
			{
				var controller = controllers[i];
				previousEyeDirection[i]  = controller.eyeDirection.forward;
				previousBodyDirection[i] = controller.bodyDirection.forward;

				controller.eyeDirection.localRotation  = quaternion.Euler(0, 0, 1);
				controller.bodyDirection.localRotation = quaternion.Euler(0, 0, 1);
			}
		}

		private void LateUpdate()
		{
			// We need to not set data at the initial frame, or else when no profile is set, the default values will be set to the first set profile.
			if (initialFrame)
			{
				initialFrame = false;
				return;
			}

			var euler = quaternion.Euler(0, 0, 1);

			for (var i = 0; i < controllers.Length; i++)
			{
				var controller = controllers[i];
				if (false == animationState.customizeEyelid)
				{
					controller.eyelidTexture     = CurrentProfile.eyelidTexture;
					controller.eyelidTextureStep = CurrentProfile.eyelidStrength;
				}

				var eyeHasBeenAnimated  = controller.eyeDirection.localRotation != euler;
				var bodyHasBeenAnimated = controller.bodyDirection.localRotation != euler;

				// TODO: Lerp
				// Perhaps once the animation stop customize, we register the current direction, and then we interpolate it to the next one?
				// ^ We can't interpolate between every previous state of the frame since the animation will try to move back to the original. ^ 

				if (false == animationState.customizeEyeDirection && (CurrentProfile.customEyeDirection || false == eyeHasBeenAnimated))
				{
					var next = Quaternion.Euler(CurrentProfile.eyeDirection.y, -CurrentProfile.eyeDirection.x, 0);
					if (eyeHasBeenAnimated)
						controller.eyeDirection.localRotation = next;
					else
						controller.eyeDirection.localRotation = Quaternion.Lerp(controller.eyeDirection.localRotation, next, Time.deltaTime * 5);
				}

				if (false == animationState.customizeBodyDirection && (CurrentProfile.customBodyDirection || false == bodyHasBeenAnimated))
				{
					var next = Quaternion.Euler(CurrentProfile.bodyDirection.y, -CurrentProfile.bodyDirection.x, 0);
					if (bodyHasBeenAnimated)
						controller.bodyDirection.localRotation = next;
					else
						controller.bodyDirection.localRotation = Quaternion.Lerp(controller.bodyDirection.localRotation, next, Time.deltaTime * 1);
				}
			}
		}
	}
}