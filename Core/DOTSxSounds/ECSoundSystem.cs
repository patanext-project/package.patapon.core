using System;
using System.Collections.Generic;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Utility.Rendering;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PataNext.Client.Systems
{
	[AlwaysSynchronizeSystem]
	[UpdateInGroup(typeof(OrderGroup.Presentation.AfterSimulation))]
	public class ECSoundSystem : SystemBase
	{
		private const int SourceCount = 16;

		private List<AudioClip>            m_AudioClips;
		private Dictionary<AudioClip, int> m_ClipToDefinition;

		private int           m_SourceRoulette;
		private AudioSource[] m_AudioSources;
		private Dictionary<string, AudioSource> m_TaggedAudioSources;

		private ClientCreateCameraSystem m_CameraSystem;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_AudioSources = new AudioSource[SourceCount];
			for (var i = 0; i != SourceCount; i++)
			{
				var go = new GameObject($"({World}) AudioSource#{i}", typeof(AudioSource));
				m_AudioSources[i] = go.GetComponent<AudioSource>();
			}

			m_TaggedAudioSources = new Dictionary<string, AudioSource>();

			m_AudioClips       = new List<AudioClip> {null};
			m_ClipToDefinition = new Dictionary<AudioClip, int>();

			m_CameraSystem = World.GetExistingSystem<ClientCreateCameraSystem>();
		}

		private AudioSource FindSource(string tag)
		{
			if (m_TaggedAudioSources.TryGetValue(tag, out var audioSource))
				return audioSource;

			var go = new GameObject($"({World}) AudioSource#{tag}", typeof(AudioSource));

			m_TaggedAudioSources[tag] = audioSource = go.GetComponent<AudioSource>();
			return audioSource;
		}

		private AudioSource FindSource()
		{
			foreach (var audioSource in m_AudioSources)
			{
				if (audioSource.isPlaying)
					continue;
				return audioSource;
			}

			var source = m_AudioSources[m_SourceRoulette++];
			if (m_SourceRoulette >= m_AudioSources.Length)
				m_SourceRoulette = 0;
			return source;
		}

		protected override void OnUpdate()
		{
			Entities.WithAll<ECSoundOneShotTag>().ForEach((Entity ent, in ECSoundEmitterComponent emitter, in ECSoundDefinition definition) =>
			{
				var stopPrevious = HasComponent<ECSoundInterruptSource>(ent);
				
				AudioSource source = null;
				if (!EntityManager.HasComponent<ECSoundTags>(ent))
					source = FindSource();
				else
				{
					var soundTags = EntityManager.GetComponentData<ECSoundTags>(ent);
					foreach (var tag in soundTags.Tags)
					{
						source = FindSource(tag);
						if (source != null)
							break;
					}

					if (source == null)
						source = FindSource();
				}
				
				source.volume      = emitter.volume;
				source.rolloffMode = AudioRolloffMode.Linear;
				source.minDistance = emitter.minDistance;
				source.maxDistance = emitter.maxDistance;
				source.clip        = m_AudioClips[definition.Index];

				if (emitter.spatialBlend >= 0 && emitter.spatialBlend <= 2)
				{
					source.spread = 180;

					var pos = emitter.position;
					if (emitter.spatialBlend < 1)
					{
						pos.y = 0;
						pos.z = 0;
					}

					source.spatialize         = true;
					source.transform.position = pos;
					source.spatialBlend       = 0;
				}
				else
				{
					source.spatialize = false;

					var dist = math.clamp(
						math.unlerp(emitter.minDistance, emitter.maxDistance, math.distance(m_CameraSystem.Camera.transform.position.x, emitter.position.x)),
						0, 1);
					source.volume     = math.lerp(source.volume, 0, dist);
				}

				if (stopPrevious || !source.isPlaying)
				{
					source.Stop();
					source.Play();
				}

				EntityManager.DestroyEntity(ent);
			}).WithStructuralChanges().Run();
		}

		public ECSoundDefinition ConvertClip(AudioClip clip)
		{
			if (m_ClipToDefinition.TryGetValue(clip, out var definitionId))
				return new ECSoundDefinition {Index = definitionId};
			
			m_AudioClips.Add(clip);
			m_ClipToDefinition[clip] = m_AudioClips.Count - 1;
			return new ECSoundDefinition
			{
				Index = m_AudioClips.Count - 1
			};

		}
	}

	public struct ECSoundDefinition : IComponentData
	{
		public bool IsValid => Index > 0;

		public int Index;
	}

	public class ECSoundTags : IComponentData
	{
		public string[] Tags;
	}

	public struct ECSoundInterruptSource : IComponentData
	{
		
	}
	
	[Serializable]
	public struct ECSoundEmitterComponent : IComponentData
	{
		public AudioSpeakerMode speakerMode;
		
		public float  volume;
		public float  spatialBlend;
		public float3 position;

		public float            minDistance;
		public float            maxDistance;
		public AudioRolloffMode rollOf;

		public void make_flat()
		{
			spatialBlend = -1;
		}

		public void make_1d()
		{
			spatialBlend = 0;
		}

		public void make_2d()
		{
			spatialBlend = 1;
		}

		public void make_3d()
		{
			spatialBlend = 2;
		}
	}

	public struct ECSoundOneShotTag : IComponentData
	{
	}
}