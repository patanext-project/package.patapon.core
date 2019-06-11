using System.Collections.Generic;
using package.patapon.core;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Patapon4TLB.Default.Test
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateAfter(typeof(RhythmEngineClientInputSystem))]
	public class PlayBeatSound : ComponentSystem
	{
		private int  m_LastBeat;
		private bool m_Play;

		private void ForEachEngine(ref RhythmEngineProcess process)
		{
			if (m_LastBeat == process.Beat)
				return;

			m_LastBeat = process.Beat;
			m_Play     = true;
		}

		private void ForEachPressureEvent(Entity entity, ref PressureEvent pressureEvent)
		{
			if (!EntityManager.HasComponent(pressureEvent.Engine, typeof(RhythmEngineSimulateTag)))
				return;
			
			m_AudioSourceOnNewPressure.PlayOneShot(m_AudioOnPressure[pressureEvent.Key][0]);
		}

		private EntityQueryBuilder.F_D<RhythmEngineProcess> m_EngineDelegate;
		private EntityQueryBuilder.F_ED<PressureEvent> m_PressureEventDelegate;

		private AudioSource m_AudioSourceOnNewBeat;
		private AudioSource m_AudioSourceOnNewPressure;

		private AudioClip                                   m_AudioOnNewBeat;
		private Dictionary<int, Dictionary<int, AudioClip>> m_AudioOnPressure;

		protected override void OnCreate()
		{
			base.OnCreate();

			if (!Application.isPlaying)
				return;

			Addressables.InitializationOperation.Completed += op => { OnLoadAssets(); };

			m_EngineDelegate = ForEachEngine;
			m_PressureEventDelegate = ForEachPressureEvent;

			m_AudioSourceOnNewBeat               = new GameObject("(Clip) On New Beat", typeof(AudioSource)).GetComponent<AudioSource>();
			m_AudioSourceOnNewBeat.reverbZoneMix = 0f;
			m_AudioSourceOnNewBeat.spatialBlend  = 0f;
			m_AudioSourceOnNewBeat.volume        = 0.25f;
			
			m_AudioSourceOnNewPressure = new GameObject("(Clip) On New Pressure", typeof(AudioSource)).GetComponent<AudioSource>();
			m_AudioSourceOnNewPressure.reverbZoneMix = 0f;
			m_AudioSourceOnNewPressure.spatialBlend  = 0f;
			m_AudioSourceOnNewPressure.volume        = 0.33f;
		}

		protected void OnLoadAssets()
		{
			Addressables.LoadAsset<AudioClip>("int:RhythmEngine/Sounds/on_new_beat.ogg")
			            .Completed += op => m_AudioOnNewBeat = op.Result;

			m_AudioOnPressure = new Dictionary<int, Dictionary<int, AudioClip>>(12);

			for (int i = 0; i != 4; i++)
			{
				var key = i + 1;

				m_AudioOnPressure[key] = new Dictionary<int, AudioClip>(3);

				for (int r = 0; r != 3; r++)
				{
					var rank = r;

					m_AudioOnPressure[key][rank] = null;

					Addressables.LoadAsset<AudioClip>($"int:RhythmEngine/Sounds/drum_{key}_{rank}.ogg").Completed += op =>
					{
						Debug.Assert(op.IsValid, "op.IsValid");

						m_AudioOnPressure[key][rank] = op.Result;
					};
				}
			}
		}

		protected override void OnUpdate()
		{
			if (!Application.isPlaying)
				return;
			
			m_Play = false;

			Entities.WithAll<RhythmEngineSimulateTag>().ForEach(m_EngineDelegate);

			if (m_Play)
			{
				m_AudioSourceOnNewBeat.PlayOneShot(m_AudioOnNewBeat);
			}

			Entities.ForEach(m_PressureEventDelegate);
		}
	}
}