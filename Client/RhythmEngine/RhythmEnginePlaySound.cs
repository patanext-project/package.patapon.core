using Misc.Extensions;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Systems;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RhythmEngine
{
	[AlwaysUpdateSystem]
	[UpdateInGroup(typeof(PresentationSystemGroup))]
	public class RhythmEnginePlaySound : GameBaseSystem
	{
		private enum DataType
		{
			OnNewBeat
		}

		private struct Data
		{
			public DataType Type;
		}

		public bool IsNewBeat;

		private EntityQuery m_EngineQuery;

		private AudioSource m_AudioSourceOnNewBeat;

		private AudioClip m_AudioOnNewBeat;

		private AsyncOperationModule m_AsyncOpModule;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_EngineQuery = GetEntityQuery(typeof(RhythmEngineDescription), typeof(Relative<PlayerDescription>));

			AudioSource CreateAudioSource(string name, float volume)
			{
				var audioSource = new GameObject("(Clip) " + name, typeof(AudioSource)).GetComponent<AudioSource>();
				audioSource.reverbZoneMix = 0f;
				audioSource.spatialBlend  = 0f;
				audioSource.volume        = volume;

				return audioSource;
			}

			m_AudioSourceOnNewBeat = CreateAudioSource("On New Beat", 1);

			GetModule(out m_AsyncOpModule);

			m_AsyncOpModule.Add(Addressables.LoadAssetAsync<AudioClip>("core://Client/Sounds/Rhythm/Effects/on_new_beat.ogg"), new Data {Type = DataType.OnNewBeat});
		}

		protected override void OnUpdate()
		{
			for (var i = 0; i < m_AsyncOpModule.Handles.Count; i++)
			{
				var (handle, data) = m_AsyncOpModule.Get<AudioClip, Data>(i);
				if (!handle.IsDone)
					continue;

				if (handle.Result != null)
				{
					switch (data.Type)
					{
						case DataType.OnNewBeat:
							m_AudioOnNewBeat = handle.Result;
							break;
					}
				}

				m_AsyncOpModule.Handles.RemoveAtSwapBack(i);
				i--;
			}

			InitializeValues();

			if (IsNewBeat && m_AudioOnNewBeat != null)
			{
				Debug.Log("yes");
				m_AudioSourceOnNewBeat.PlayOneShot(m_AudioOnNewBeat);
			}

			ClearValues();
		}

		private void InitializeValues()
		{
			var player = this.GetFirstSelfGamePlayer();
			if (player == default)
				return;

			Entity engine;
			if (this.TryGetCurrentCameraState(player, out var camState))
				engine = GetEngineFromTarget(camState.Target, player);
			else
				engine = FindPlayerEngine(player);
			
			if (engine == default)
				return;

			IsNewBeat = EntityManager.GetComponentData<RhythmEngineState>(engine).IsNewBeat;
		}

		private Entity FindPlayerEngine(Entity player)
		{
			var engineEntities  = m_EngineQuery.ToEntityArray(Allocator.TempJob);
			var relativePlayers = m_EngineQuery.ToComponentDataArray<Relative<PlayerDescription>>(Allocator.TempJob);
			for (var ent = 0; ent < engineEntities.Length; ent++)
			{
				if (relativePlayers[ent].Target == player)
					return engineEntities[ent];
			}

			engineEntities.Dispose();
			relativePlayers.Dispose();

			return default;
		}

		private Entity GetEngineFromTarget(Entity target, Entity fallback = default)
		{
			if (EntityManager.TryGetComponentData(target, out Relative<RhythmEngineDescription> relativeRhythmEngine)
			    && relativeRhythmEngine.Target != default)
				return relativeRhythmEngine.Target;

			if (EntityManager.TryGetComponentData(target, out Relative<PlayerDescription> relativePlayer)
			    && relativePlayer.Target != default)
				return FindPlayerEngine(relativePlayer.Target);

			return FindPlayerEngine(fallback);
		}

		private void ClearValues()
		{
			IsNewBeat = false;
		}
	}
}