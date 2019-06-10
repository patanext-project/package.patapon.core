using System.Collections.Generic;
using package.patapon.core;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Patapon4TLB.Default.Test
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class PlayBeatSound : ComponentSystem
	{
		private int  m_LastBeat;
		private bool m_Play;

		private void ForEach(ref FlowRhythmEngineProcess process)
		{
			if (m_LastBeat == process.Beat)
				return;

			m_LastBeat = process.Beat;
			m_Play     = true;
		}

		private EntityQueryBuilder.F_D<FlowRhythmEngineProcess> m_ProcessDelegate;

		private AudioSource m_AudioSourceOnNewBeat;
		private AudioSource m_AudioSourceOnNewPressure;

		private AudioClip                                   m_AudioOnNewBeat;
		private Dictionary<int, Dictionary<int, AudioClip>> m_AudioOnPressure;

		protected override void OnCreate()
		{
			base.OnCreate();

			Addressables.InitializationOperation.Completed += op => { OnLoadAssets(); };

			m_ProcessDelegate = ForEach;

			m_AudioSourceOnNewBeat               = new GameObject("(Clip) On New Beat", typeof(AudioSource)).GetComponent<AudioSource>();
			m_AudioSourceOnNewBeat.reverbZoneMix = 0f;
			m_AudioSourceOnNewBeat.spatialBlend  = 0f;
			m_AudioSourceOnNewBeat.volume        = 0.25f;
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
			m_Play = false;

			Entities.WithAll<FlowRhythmEngineSimulateTag>().ForEach(m_ProcessDelegate);

			if (m_Play)
			{
				m_AudioSourceOnNewBeat.PlayOneShot(m_AudioOnNewBeat);
			}
		}
	}
}