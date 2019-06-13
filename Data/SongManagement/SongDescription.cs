using System;
using System.Collections.Generic;
using Patapon4TLB.Core.json;
using StormiumTeam.GameBase;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Patapon4TLB.Core
{
	public class SongDescription : IDisposable
	{
		private enum OpType
		{
			BgmFull,
			BgmSlice,
			Command,
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
		}

		public struct BgmComboPart
		{
			public int ScoreNeeded;
			public int ClipCount;
			public List<AudioClip> Clips;
		}

		private List<IAsyncOperation> m_AddrOperations;
		private List<OperationData>   m_OperationData;
		private bool                  m_IsFinalized;

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

		public bool IsFinalized => m_IsFinalized;

		public readonly DescriptionFileJsonData File;

		// example of the possibilities: normal, pre-fever, fever
		public Dictionary<string, Dictionary<string, List<AudioClip>>> CommandsAudio;

		public List<AudioClip>          BgmEntranceClips;
		public List<AudioClip> BgmFeverEntranceClips;
		public List<AudioClip>                BgmFeverLoopClips;
		public List<BgmComboPart> BgmComboParts;

		public SongDescription(DescriptionFileJsonData file)
		{
			File = file;

			m_AddrOperations = new List<IAsyncOperation>();
			m_OperationData  = new List<OperationData>();
			
			BgmComboParts = new List<BgmComboPart>();

			CommandsAudio = new Dictionary<string, Dictionary<string, List<AudioClip>>>();
			foreach (var fileCmdAudio in file.commandsAudio)
			{
				CommandsAudio[fileCmdAudio.Key] = new Dictionary<string, List<AudioClip>>();
				foreach (var commands in fileCmdAudio.Value)
				{
					var audioList = CommandsAudio[fileCmdAudio.Key][commands.Key] = new List<AudioClip>();
					for (var i = 0; i != commands.Value.Length; i++)
					{
						var addrPath = commands.Value[i];
						addrPath = addrPath.Replace("{p}", $"songs:{file.identifier}/commands/{fileCmdAudio.Key}/");

						var insertIndex = i;
						Addressables.LoadAsset<AudioClip>(addrPath).Completed += (op) =>
						{
							if (op.Status == AsyncOperationStatus.Failed)
							{
								return;
							}

							audioList.Insert(insertIndex, op.Result);
						};
					}
				}
			}

			var useFullAudio = file.bgmAudioFull != null;
			if (useFullAudio)
			{
				throw new NotImplementedException();
			}

			if (file.bgmAudioSliced == null)
				throw new Exception("sliced and full values are not set!");

			var  bgmAudioClipCount = 0;
			bool hasEntrancePart   = false, hasNormalPart = false, hasFeverEntrancePart = false, hasFeverPart = false;
			foreach (var bgm in file.bgmAudioSliced)
			{
				bgmAudioClipCount += bgm.Value.Length;
				if (bgm.Key == "normal_entrance")
					hasEntrancePart = true;
				if (bgm.Key.StartsWith("normal"))
					hasNormalPart = true;
				if (bgm.Key == "fever_entrance")
					hasFeverEntrancePart = true;
				if (bgm.Key == "fever")
					hasFeverPart = true;
			}

			if (!hasNormalPart && !hasFeverPart)
				throw new Exception($"e: {hasEntrancePart}, n: {hasNormalPart}, fe: {hasFeverEntrancePart}, f: {hasFeverPart}");

			var order = 0;
			foreach (var bgm in file.bgmAudioSliced)
			{
				foreach (var bgmAudioFile in bgm.Value)
				{
					var data = new OperationData {Type = OpType.BgmSlice, BgmSliceOrder = order};
					if (bgm.Key == "normal_entrance")
					{
						data.BgmSliceType = OpBgmSliceType.NormalEntrance;
					}
					else if (bgm.Key.StartsWith("normal"))
					{
						data.BgmSliceType = OpBgmSliceType.Normal;
						if (bgm.Key == "normal") data.BgmSliceNormalCmdRank = 0;
						else if (bgm.Key.StartsWith("normal_"))
						{
							var strRank = bgm.Key.Replace("normal_", string.Empty);
							if (int.TryParse(strRank, out var rank))
							{
								data.BgmSliceNormalCmdRank = rank;
							}
						}
					}
					else if (bgm.Key == "fever_entrance")
					{
						data.BgmSliceType = OpBgmSliceType.FeverEntrance;
					}
					else if (bgm.Key == "fever")
					{
						data.BgmSliceType = OpBgmSliceType.Fever;
					}

					var op = Addressables.LoadAsset<AudioClip>(bgmAudioFile.Replace("{p}", $"songs:{file.identifier}/bgm/"));

					m_OperationData.Add(data);
					m_AddrOperations.Add(op);

					order++;
				}
			}
		}

		public void FinalizeOperation()
		{
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

				if (!opAddr.IsValid)
				{
					Debug.Log($"An operation is not valid. (status={opAddr.Status})");
					continue;
				}

				switch (opData.Type)
				{
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
			{
				var clip = AudioClipUtility.Combine($"{nameof(BgmComboParts)}-{combo.Key}", combo.Value.ToArray());
				BgmComboParts.Add(new BgmComboPart
				{
					ScoreNeeded = combo.Key,
					ClipCount   = combo.Value.Count,
					Clips       = combo.Value
				});
			}

			BgmFeverEntranceClips = bgmFeverEntranceClips;
			BgmFeverLoopClips = bgmFeverLoopClips;

			m_IsFinalized = true;
		}

		public void Dispose()
		{
			for (var i = 0; i != m_AddrOperations.Count; i++)
			{
				m_AddrOperations[i].Release();
				if (m_AddrOperations[i].Result != null)
					Addressables.ReleaseAsset(m_AddrOperations[i].Result);
			}
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