using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Patapon.Client.RhythmEngine
{
	public class SongDescription : IDisposable
	{
		public const string CmdKeyNormal   = "normal";
		public const string CmdKeyPreFever = "prefever";
		public const string CmdKeyFever    = "fever";

		public const string BgmKeyNormalEntrance = "normal_entrance";
		public const string BgmKeyNormal         = "normal";
		public const string BgmKeyFeverEntrance  = "fever_entrance";
		public const string BgmKeyFever          = "fever";

		public readonly DescriptionFileJsonData File;
		public          List<BgmComboPart>      BgmComboParts;

		public List<AudioClip> BgmEntranceClips;
		public List<AudioClip> BgmFeverEntranceClips;
		public List<AudioClip> BgmFeverLoopClips;

		// example of the possibilities: normal, pre-fever, fever
		public Dictionary<string, Dictionary<string, List<AudioClip>>> CommandsAudio;

		private readonly List<AsyncOperationHandle> m_AddrOperations;
		private readonly List<OperationData>        m_OperationData;

		public SongDescription(DescriptionFileJsonData file)
		{
			File = file;

			m_AddrOperations = new List<AsyncOperationHandle>();
			m_OperationData  = new List<OperationData>();

			BgmComboParts = new List<BgmComboPart>();

			CommandsAudio = new Dictionary<string, Dictionary<string, List<AudioClip>>>();
			foreach (var fileCmdAudio in file.commandsAudio)
			{
				CommandsAudio[fileCmdAudio.Key] = new Dictionary<string, List<AudioClip>>();
				foreach (var commands in fileCmdAudio.Value)
				{
					CommandsAudio[fileCmdAudio.Key][commands.Key] = new List<AudioClip>();
					for (var i = 0; i != commands.Value.Length; i++)
					{
						var addrPath = commands.Value[i].Replace("{p}", $"{file.path}/{file.identifier}/commands/{fileCmdAudio.Key}/");

						m_AddrOperations.Add(Addressables.LoadAssetAsync<AudioClip>(addrPath));
						m_OperationData.Add(new OperationData
						{
							Type = OpType.Command,

							CmdType       = commands.Key,
							CmdIdentifier = fileCmdAudio.Key,
							CmdIndex      = i
						});
					}
				}
			}

			var useFullAudio = file.bgmAudioFull != null;
			if (useFullAudio) throw new NotImplementedException();

			if (file.bgmAudioSliced == null)
				throw new Exception("sliced and full values are not set!");

			var  bgmAudioClipCount = 0;
			bool hasEntrancePart   = false, hasNormalPart = false, hasFeverEntrancePart = false, hasFeverPart = false;
			foreach (var bgm in file.bgmAudioSliced)
			{
				bgmAudioClipCount += bgm.Value.Length;
				if (bgm.Key == BgmKeyNormalEntrance)
					hasEntrancePart = true;
				if (bgm.Key.StartsWith(BgmKeyNormal))
					hasNormalPart = true;
				if (bgm.Key == BgmKeyFeverEntrance)
					hasFeverEntrancePart = true;
				if (bgm.Key == BgmKeyFever)
					hasFeverPart = true;
			}

			if (!hasNormalPart && !hasFeverPart)
				throw new Exception($"e: {hasEntrancePart}, n: {hasNormalPart}, fe: {hasFeverEntrancePart}, f: {hasFeverPart}");

			var order = 0;
			foreach (var bgm in file.bgmAudioSliced)
			foreach (var bgmAudioFile in bgm.Value)
			{
				var data = new OperationData {Type = OpType.BgmSlice, BgmSliceOrder = order};
				if (bgm.Key == BgmKeyNormalEntrance)
				{
					data.BgmSliceType = OpBgmSliceType.NormalEntrance;
				}
				else if (bgm.Key.StartsWith(BgmKeyNormal))
				{
					data.BgmSliceType = OpBgmSliceType.Normal;
					if (bgm.Key == BgmKeyNormal)
					{
						data.BgmSliceNormalCmdRank = 0;
					}
					else if (bgm.Key.StartsWith(BgmKeyNormal + "_"))
					{
						var strRank                                                         = bgm.Key.Replace(BgmKeyNormal + "_", string.Empty);
						if (int.TryParse(strRank, out var rank)) data.BgmSliceNormalCmdRank = rank;
					}
				}
				else if (bgm.Key == BgmKeyFeverEntrance)
				{
					data.BgmSliceType = OpBgmSliceType.FeverEntrance;
				}
				else if (bgm.Key == BgmKeyFever)
				{
					data.BgmSliceType = OpBgmSliceType.Fever;
				}

				var op = Addressables.LoadAssetAsync<AudioClip>(bgmAudioFile.Replace("{p}", $"{file.path}/{file.identifier}/bgm/"));

				m_OperationData.Add(data);
				m_AddrOperations.Add(op);

				order++;
			}
		}

		public bool AreAddressableCompleted
		{
			get
			{
				var done = 0;
				for (var i = 0; i != m_AddrOperations.Count; i++)
					if (m_AddrOperations[i].IsDone)
						done++;
				return done == m_AddrOperations.Count;
			}
		}

		public bool IsFinalized { get; private set; }

		public void Dispose()
		{
			for (var i = 0; i != m_AddrOperations.Count; i++)
			{
				Addressables.Release(m_AddrOperations[i]);
				if (m_AddrOperations[i].Result != null)
					Addressables.Release(m_AddrOperations[i].Result);
			}
		}

		public void FinalizeOperation()
		{
			if (IsFinalized) throw new InvalidOperationException("Already finalized song.");

			if (!AreAddressableCompleted)
				throw new InvalidOperationException("We haven't loaded all addressable asset yet!");

			var bgmPriorities = new NativeList<PriorityBgmSlice>(Allocator.TempJob);

			var bgmEntranceClips      = new List<AudioClip>();
			var bgmFeverEntranceClips = new List<AudioClip>();
			var bgmFeverLoopClips     = new List<AudioClip>();
			var bgmComboPartClips     = new Dictionary<int, List<AudioClip>>();

			var count = m_AddrOperations.Count;
			for (var op = 0; op != count; op++)
			{
				var opAddr = m_AddrOperations[op];
				var opData = m_OperationData[op];

				if (!opAddr.IsValid())
				{
					Debug.LogError($"An operation is not valid. (status={opAddr.Status})");
					continue;
				}

				Debug.Log(opAddr.Result);
				switch (opData.Type)
				{
					case OpType.Command:
					{
						CommandsAudio[opData.CmdIdentifier][opData.CmdType].Add((AudioClip) opAddr.Result);

						break;
					}

					case OpType.BgmSlice:
					{
						if (opData.BgmSliceType == OpBgmSliceType.Normal)
							bgmComboPartClips[opData.BgmSliceNormalCmdRank] = new List<AudioClip>();

						bgmPriorities.Add(new PriorityBgmSlice
						{
							OpIndex = op,

							Type    = opData.BgmSliceType,
							CmdRank = opData.BgmSliceNormalCmdRank,
							Order   = opData.BgmSliceOrder
						});
						break;
					}
				}
			}

			((NativeArray<PriorityBgmSlice>) bgmPriorities).Sort();

			for (var i = 0; i != bgmPriorities.Length; i++)
			{
				var bgmPriority = bgmPriorities[i];
				var audioClip   = (AudioClip) m_AddrOperations[bgmPriority.OpIndex].Result;

				switch (bgmPriority.Type)
				{
					case OpBgmSliceType.FeverEntrance:
						bgmFeverEntranceClips.Add(audioClip);
						break;
					case OpBgmSliceType.NormalEntrance:
						bgmEntranceClips.Add(audioClip);
						break;
					case OpBgmSliceType.Normal:
					{
						var clips = bgmComboPartClips[bgmPriority.CmdRank];
						clips.Add(audioClip);
						break;
					}

					case OpBgmSliceType.Fever:
						bgmFeverLoopClips.Add(audioClip);
						break;
				}
			}

			bgmPriorities.Dispose();

			BgmEntranceClips = bgmEntranceClips;
			foreach (var combo in bgmComboPartClips)
				BgmComboParts.Add(new BgmComboPart
				{
					ScoreNeeded = combo.Key,
					ClipCount   = combo.Value.Count,
					Clips       = combo.Value
				});

			BgmFeverEntranceClips = bgmFeverEntranceClips;
			BgmFeverLoopClips     = bgmFeverLoopClips;

			IsFinalized = true;
		}

		private enum OpType
		{
			BgmFull,
			BgmSlice,
			Command
		}

		private enum OpBgmSliceType
		{
			NormalEntrance,
			Normal,
			FeverEntrance,
			Fever
		}

		private struct OperationData
		{
			public OpType Type;

			public OpBgmSliceType BgmSliceType;
			public int            BgmSliceNormalCmdRank;
			public int            BgmSliceOrder;

			public string CmdType;
			public string CmdIdentifier;
			public int    CmdIndex;
		}

		public struct BgmComboPart
		{
			public int             ScoreNeeded;
			public int             ClipCount;
			public List<AudioClip> Clips;
		}

		private struct PriorityBgmSlice : IComparable<PriorityBgmSlice>
		{
			public OpBgmSliceType Type;
			public int            CmdRank;
			public int            Order;

			public int OpIndex;

			public int CompareTo(PriorityBgmSlice other)
			{
				if (Type > other.Type)
					return 1;

				if (CmdRank > other.CmdRank)
					return 1;

				return Order > other.Order ? 1 : -1;
			}
		}
	}
}