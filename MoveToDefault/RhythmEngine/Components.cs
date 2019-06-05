using DefaultNamespace;
using package.patapon.core;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon4TLB.Default
{
	public struct DefaultRhythmEngineSnapshotData : ISnapshotFromComponent<DefaultRhythmEngineSnapshotData, DefaultRhythmEngineSettings, DefaultRhythmEngineState>
	{
		public uint Tick { get; private set; }
		
		public int MaxBeats;
		public int Beat;
		
		public void PredictDelta(uint tick, ref DefaultRhythmEngineSnapshotData baseline1, ref DefaultRhythmEngineSnapshotData baseline2)
		{
		}

		public void Serialize(ref DefaultRhythmEngineSnapshotData baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
		{
			writer.WritePackedUInt((uint) MaxBeats, compressionModel);
			writer.WritePackedUInt((uint) Beat, compressionModel);
		}

		public void Deserialize(uint tick, ref DefaultRhythmEngineSnapshotData baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
		{
			Tick = tick;

			MaxBeats = (int) reader.ReadPackedUInt(ref ctx, compressionModel);
			Beat = (int) reader.ReadPackedUInt(ref ctx, compressionModel);
		}

		public void Interpolate(ref DefaultRhythmEngineSnapshotData target, float factor)
		{
			MaxBeats = target.MaxBeats;
			Beat = target.Beat;
		}

		public void Set(DefaultRhythmEngineSettings settings, DefaultRhythmEngineState state)
		{
			MaxBeats = settings.MaxBeats;
			Beat = state.Beat;
		}
		
		public class RegisterSerializer : AddComponentSerializer<DefaultRhythmEngineSettings, DefaultRhythmEngineState, DefaultRhythmEngineSnapshotData>
		{}
	}

	public struct DefaultRhythmEngineSettings : IComponentFromSnapshot<DefaultRhythmEngineSnapshotData>
	{
		public int MaxBeats;

		public void Set(DefaultRhythmEngineSnapshotData snapshot)
		{
			MaxBeats = snapshot.MaxBeats;
		}

		public class UpdateFromSnapshot : BaseUpdateFromSnapshotSystem<DefaultRhythmEngineSnapshotData, DefaultRhythmEngineSettings>
		{
		}
	}

	public struct DefaultRhythmEngineState : IComponentFromSnapshot<DefaultRhythmEngineSnapshotData>
	{
		public int Beat;

		public void Set(DefaultRhythmEngineSnapshotData snapshot)
		{
			Beat = snapshot.Beat;
		}

		public class UpdateFromSnapshot : BaseUpdateFromSnapshotSystem<DefaultRhythmEngineSnapshotData, DefaultRhythmEngineState>
		{
		}
	}

	public struct PressureEvent : IComponentData
	{
		public Entity Engine;
		public int Key;
	}

	[InternalBufferCapacity(8)]
	public struct DefaultRhythmEngineCurrentCommand : IBufferElementData
	{
		public FlowRhythmPressureData Data;
	}
	
	public struct DefaultRhythmCommand : IComponentData
	{

	}

	public struct MarchCommand : IComponentData
	{
	}

	public struct AttackCommand : IComponentData
	{
	}

	public struct DefendCommand : IComponentData
	{
	}

	public struct ChargeCommand : IComponentData
	{
	}

	public struct RetreatCommand : IComponentData
	{
	}

	public struct JumpCommand : IComponentData
	{
	}

	public struct PartyCommand : IComponentData
	{
	}

	public struct SummonCommand : IComponentData
	{
	}

	public struct BackwardCommand : IComponentData
	{
	}
}