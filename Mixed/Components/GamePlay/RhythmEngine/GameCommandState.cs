using Patapon4TLB.Default.Player;
using Revolution;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GamePlay.RhythmEngine
{
	public struct GamePredictedCommandState : IComponentData
	{
		public GameCommandState State;
	}

	public struct GameCommandState : IComponentData
	{
		public int StartTime;
		public int EndTime;
		public int ChainEndTime;
		public AbilitySelection Selection;

		public bool IsGamePlayActive(int milliseconds)
		{
			return milliseconds >= StartTime && milliseconds <= EndTime;
		}

		public bool IsInputActive(int milliseconds, int beatInterval)
		{
			return milliseconds >= EndTime - beatInterval && milliseconds <= EndTime + beatInterval;
		}

		public bool HasActivity(int milliseconds, int beatInterval)
		{
			return IsGamePlayActive(milliseconds)
			       || IsInputActive(milliseconds, beatInterval);
		}

		public struct Exclude : IComponentData
		{
		}

		public struct Snapshot : IReadWriteSnapshot<Snapshot>, ISynchronizeImpl<GameCommandState>, ISnapshotDelta<Snapshot>
		{
			public int StartTime;
			public int EndTime;
			public int ChainEndTime;

			public void WriteTo(DataStreamWriter writer, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				writer.WritePackedIntDelta(StartTime, baseline.StartTime, compressionModel);
				writer.WritePackedIntDelta(EndTime, baseline.EndTime, compressionModel);
				writer.WritePackedIntDelta(ChainEndTime, baseline.ChainEndTime, compressionModel);
			}

			public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				StartTime    = reader.ReadPackedIntDelta(ref ctx, baseline.StartTime, compressionModel);
				EndTime      = reader.ReadPackedIntDelta(ref ctx, baseline.EndTime, compressionModel);
				ChainEndTime = reader.ReadPackedIntDelta(ref ctx, baseline.ChainEndTime, compressionModel);
			}

			public uint Tick { get; set; }

			public void SynchronizeFrom(in GameCommandState component, in DefaultSetup setup, in SerializeClientData serializeData)
			{
				StartTime    = component.StartTime;
				EndTime      = component.EndTime;
				ChainEndTime = component.ChainEndTime;
			}

			public void SynchronizeTo(ref GameCommandState component, in DeserializeClientData deserializeData)
			{
				component.StartTime    = StartTime;
				component.EndTime      = EndTime;
				component.ChainEndTime = ChainEndTime;
			}

			public bool DidChange(Snapshot baseline)
			{
				return StartTime != baseline.StartTime
				       || EndTime != baseline.EndTime
				       || ChainEndTime != baseline.ChainEndTime;
			}
		}

		public class NetSynchronize : ComponentSnapshotSystemDelta<GameCommandState, Snapshot>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public class ComponentSnapshotUpdate : ComponentUpdateSystemDirect<GameCommandState, Snapshot>
		{
		}
	}
}