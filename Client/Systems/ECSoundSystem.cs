using System.Collections.Generic;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Patapon.Client.Systems
{
	[AlwaysSynchronizeSystem]
	[UpdateInGroup(typeof(OrderGroup.Presentation.AfterSimulation))]
	public class ECSoundSystem : JobComponentSystem
	{
		private const int SourceCount = 48;

		private List<AudioClip> m_AudioClips;

		private int           m_SourceRoulette;
		private AudioSource[] m_AudioSources;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_AudioSources = new AudioSource[SourceCount];
			for (var i = 0; i != SourceCount; i++)
			{
				var go = new GameObject($"({World}) AudioSource#{i}", typeof(AudioSource));
				m_AudioSources[i] = go.GetComponent<AudioSource>();
			}

			m_AudioClips = new List<AudioClip> {null};
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

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			Entities.WithAll<ECSoundOneShotTag>().ForEach((Entity ent, in ECSoundEmitterComponent emitter, in ECSoundDefinition definition) =>
			{
				var source = FindSource();
				source.volume      = emitter.volume;
				source.rolloffMode = emitter.rollOf;
				source.minDistance = emitter.minDistance;
				source.maxDistance = emitter.maxDistance;
				source.clip        = m_AudioClips[definition.Index];

				if (emitter.spatialBlend >= 1 && emitter.spatialBlend <= 2)
				{
					source.spatialize         = true;
					source.spatialBlend       = emitter.spatialBlend - 2;
					source.transform.position = emitter.position;
				}
				else
				{
					source.spatialize = false;
				}

				source.Play();
				
				Debug.LogError("Play sound");

				EntityManager.DestroyEntity(ent);
			}).WithStructuralChanges().Run();

			return inputDeps;
		}

		public ECSoundDefinition ConvertClip(AudioClip clip)
		{
			m_AudioClips.Add(clip);
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

	public struct ECSoundEmitterComponent : IComponentData
	{
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