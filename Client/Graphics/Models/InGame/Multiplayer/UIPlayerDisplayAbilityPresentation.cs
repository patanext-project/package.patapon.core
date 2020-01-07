using System;
using package.stormiumteam.shared.ecs;
using Patapon4TLB.Default.Player;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Systems;
using Unity.Transforms;
using UnityEngine;

namespace package.patapon.core.Models.InGame.Multiplayer
{
	public class UIPlayerDisplayAbilityPresentation : RuntimeAssetPresentation<UIPlayerDisplayAbilityPresentation>
	{
		public SpriteRenderer[] Quads;
		public Animator         Animator;

		[SerializeField]
		private Sprite[] spriteResources;

		private void OnEnable()
		{
			Animator = GetComponent<Animator>();

			Debug.Assert(spriteResources.Length == 3, "spriteResources.Length == 2");
			Debug.Assert(Animator != null, "Animator != null");
		}

		public void Set(int index)
		{
			Debug.Assert(index < 3, "index < 3");
			foreach (var quad in Quads)
				quad.sprite = spriteResources[index];
		}
	}

	public class UIPlayerDisplayAbilityBackend : RuntimeAssetBackend<UIPlayerDisplayAbilityPresentation>
	{
		public bool wasSelectingAbility;
		public AbilitySelection lastAbility;
	}

	public class UIPlayerDisplayAbilityRenderSystem : BaseRenderSystem<UIPlayerDisplayAbilityPresentation>
	{
		protected override void PrepareValues()
		{

		}

		protected override void Render(UIPlayerDisplayAbilityPresentation definition)
		{
			var backend        = (UIPlayerDisplayAbilityBackend) definition.Backend;
			var targetEntity   = backend.DstEntity;
			var targetPosition = EntityManager.GetComponentData<Translation>(targetEntity);
			backend.transform.position = new Vector3
			{
				x = targetPosition.Value.x,
				y = -0.3f
			};

			if (!EntityManager.TryGetComponentData(targetEntity, out Relative<PlayerDescription> relativePlayer))
				return;

			EntityManager.TryGetComponentData(relativePlayer.Target, out GamePlayerCommand playerCommand);

			var command = playerCommand.Base;
			definition.Set((int) command.Ability);

			if (backend.lastAbility != command.Ability || (backend.wasSelectingAbility != command.IsSelectingAbility && command.IsSelectingAbility))
			{
				definition.Animator.SetTrigger("Show");
			}
			
			backend.lastAbility         = command.Ability;
			backend.wasSelectingAbility = command.IsSelectingAbility;
		}

		protected override void ClearValues()
		{

		}
	}
}