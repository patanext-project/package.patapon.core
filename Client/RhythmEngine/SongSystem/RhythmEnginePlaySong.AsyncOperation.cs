using StormiumTeam.GameBase.Modules;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Patapon.Client.RhythmEngine
{
	public partial class RhythmEnginePlaySong
	{
		private const string Addr1Path = "core://Client/Sounds/Rhythm/Effects/";
		private const string Addr2Path = "core://Client/Sounds/Effects/HeroModeActivation/Combo/";

		private AsyncOperationModule m_AsyncOpModule;

		public void RegisterAsyncOperations()
		{
			GetModule(out m_AsyncOpModule);

			m_AsyncOpModule.Add(Addressables.LoadAssetAsync<AudioClip>(Addr1Path + "fever_lost.wav"), new DataAsyncOp {Type  = DataAsyncOp.EType.FeverLost});
			m_AsyncOpModule.Add(Addressables.LoadAssetAsync<AudioClip>(Addr1Path + "voice_fever.wav"), new DataAsyncOp {Type = DataAsyncOp.EType.FeverVoice});

			m_HeroModeChainClips = new AudioClip[4];
			for (var i = 0; i < m_HeroModeChainClips.Length; i++)
			{
				m_AsyncOpModule.Add(Addressables.LoadAssetAsync<AudioClip>($"{Addr2Path}return0{i}.wav"), new DataAsyncOp
				{
					Type        = DataAsyncOp.EType.HeroVoiceReturn,
					ReturnIndex = i
				});
			}
		}

		public void UpdateAsyncOperations()
		{
			for (var i = 0; i != m_AsyncOpModule.Handles.Count; i++)
			{
				var (handle, data) = m_AsyncOpModule.Get<AudioClip, DataAsyncOp>(i);
				if (!handle.IsDone)
					continue;

				switch (data.Type)
				{
					case DataAsyncOp.EType.FeverLost:
						m_FeverLostClip = handle.Result;
						break;
					case DataAsyncOp.EType.FeverVoice:
						m_FeverClip = handle.Result;
						break;
					case DataAsyncOp.EType.HeroVoiceReturn:
						m_HeroModeChainClips[data.ReturnIndex] = handle.Result;
						break;
				}
			}

			if (CurrentSong == null)
				return;

			if (CurrentSong.AreAddressableCompleted && !CurrentSong.IsFinalized)
			{
				CurrentSong.FinalizeOperation();
				Debug.Log("------------------- Finalizing song data...");
			}
		}

		private struct DataAsyncOp
		{
			public enum EType
			{
				FeverLost,
				FeverVoice,
				HeroVoiceReturn
			}

			public EType Type;
			public int   ReturnIndex;
		}
	}
}