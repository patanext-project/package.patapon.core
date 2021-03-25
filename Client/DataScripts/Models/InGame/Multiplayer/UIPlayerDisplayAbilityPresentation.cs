using GameHost.ShareSimuWorldFeature.Systems;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Core.Addressables;
using PataNext.Module.Simulation.Components;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems.Ext;
using StormiumTeam.GameBase.Modules;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Roles.Descriptions;
using StormiumTeam.GameBase.Utility.AssetBackend;
using StormiumTeam.GameBase.Utility.Misc;
using StormiumTeam.GameBase.Utility.Rendering.BaseSystems;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace PataNext.Client.Graphics.Models.InGame.Multiplayer
{
	public class UIPlayerDisplayAbilityPresentation : RuntimeAssetPresentation
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
		private InterFrame  m_InterFrame;

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
			m_AsyncOp.Add(AssetManager.LoadAssetAsync<AudioClip>(path.GetAsset("ability_switch.wav")), new HandleOpData { });
		}

		protected override void PrepareValues()
		{
			for (var i = 0; i != m_AsyncOp.Handles.Count; i++)
			{
				var (handle, data) = DefaultAsyncOperation.InvokeExecute<AudioClip, HandleOpData>(m_AsyncOp, ref i);
				if (handle?.Result == null)
					continue;
				SwitchAbilityAudio = handle.Result;
			}

			LocalPlayer = this.GetFirstSelfGamePlayer();

			if (HasSingleton<InterFrame>())
				m_InterFrame = GetSingleton<InterFrame>();
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

			EntityManager.TryGetComponentData(relativePlayer.Target, out GameRhythmInputComponent command);
			
			definition.Set((int) command.Ability);
			if (backend.lastAbility != command.Ability || command.AbilityInterFrame.HasBeenPressed(m_InterFrame.Range))
			{
				definition.Animator.SetTrigger("Show");

				if (relativePlayer.Target == LocalPlayer)
				{
					m_AudioSource.Stop();
					m_AudioSource.clip = SwitchAbilityAudio;
					m_AudioSource.Play();
				}
			}

			backend.lastAbility         = command.Ability;
		}

		protected override void ClearValues()
		{

		}
	}
}