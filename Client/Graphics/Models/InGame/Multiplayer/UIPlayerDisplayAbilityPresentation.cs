using GameBase.Roles.Components;
using GameBase.Roles.Descriptions;
using package.patapon.core.Animation.Units;
using StormiumTeam.GameBase.Modules;
using StormiumTeam.GameBase.Utility.AssetBackend;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;

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
		public bool             wasSelectingAbility;
		public AbilitySelection lastAbility;
	}

	[UpdateInGroup(typeof(OrderGroup.Presentation.InterfaceRendering))]
	public class UIPlayerDisplayAbilityRenderSystem : BaseRenderSystem<UIPlayerDisplayAbilityPresentation>
	{
		public Entity    LocalPlayer;
		public AudioClip SwitchAbilityAudio;

		private AudioSource m_AudioSource;

		private struct HandleOpData
		{
		}

		private AsyncOperationModule m_AsyncOp;

		protected override void OnCreate()
		{
			base.OnCreate();

			AudioSource CreateAudioSource(string name, float volume)
			{
				var audioSource = new GameObject("(Clip) " + name, typeof(AudioSource)).GetComponent<AudioSource>();
				audioSource.reverbZoneMix = 0f;
				audioSource.spatialBlend  = 0f;
				audioSource.volume        = volume;

				return audioSource;
			}

			m_AudioSource = CreateAudioSource("AbilitySwitch", 0.25f);
			GetModule(out m_AsyncOp);

			var path = AddressBuilder.Client()
			                         .Folder("Sounds")
			                         .Folder("InGame");
			m_AsyncOp.Add(Addressables.LoadAssetAsync<AudioClip>(path.GetFile("ability_switch.wav")), new HandleOpData { });
		}

		protected override void PrepareValues()
		{
			for (var i = 0; i != m_AsyncOp.Handles.Count; i++)
			{
				var (handle, data) = DefaultAsyncOperation.InvokeExecute<AudioClip, HandleOpData>(m_AsyncOp, ref i);
				if (handle.Result == null)
					continue;
				SwitchAbilityAudio = handle.Result;
			}

			LocalPlayer = this.GetFirstSelfGamePlayer();
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

				if (relativePlayer.Target == LocalPlayer)
					m_AudioSource.PlayOneShot(SwitchAbilityAudio);
			}

			backend.lastAbility         = command.Ability;
			backend.wasSelectingAbility = command.IsSelectingAbility;
		}

		protected override void ClearValues()
		{

		}
	}
}