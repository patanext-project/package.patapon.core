using StormiumTeam.GameBase;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Patapon.Client.RhythmEngine
{
	public partial class RhythmEnginePlaySong
	{
		private const string AddrPath = "core://Client/Sounds/Rhythm/Effects/";

		private AsyncOperationModule m_AsyncOpModule;

		public void RegisterAsyncOperations()
		{
			GetModule(out m_AsyncOpModule);

			m_AsyncOpModule.Add(Addressables.LoadAssetAsync<AudioClip>(AddrPath + "fever_lost.wav"), new DataAsyncOp {Type  = DataAsyncOp.EType.FeverLost});
			m_AsyncOpModule.Add(Addressables.LoadAssetAsync<AudioClip>(AddrPath + "voice_fever.wav"), new DataAsyncOp {Type = DataAsyncOp.EType.FeverVoice});
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
				FeverVoice
			}

			public EType Type;
		}
	}
}