using DefaultNamespace;
using package.patapon.core;
using Patapon4TLB.Default.Snapshot;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Patapon4TLB.Default
{
	public struct DefaultRhythmEngineState : IComponentFromSnapshot<DefaultRhythmEngineSnapshotData>
	{
		public bool IsPaused;
		public int Beat;

		public void Set(DefaultRhythmEngineSnapshotData snapshot)
		{
			Beat = snapshot.Beat;
		}

		public class UpdateFromSnapshot : BaseUpdateFromSnapshotSystem<DefaultRhythmEngineSnapshotData, DefaultRhythmEngineState>
		{
		}
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