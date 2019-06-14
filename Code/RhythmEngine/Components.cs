using DefaultNamespace;
using package.patapon.core;
using Patapon4TLB.Default.Snapshot;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Patapon4TLB.Default
{
	public struct RhythmEngineState : IComponentData
	{
		public bool IsPaused;
		public bool IsNewBeat;
		public bool IsNewPressure;

		/// <summary>
		/// If a user do a f**k-up (doing pressure in an active command, waited a beat too much,...), he will need to wait a beat before starting to do pressures.
		/// </summary>
		public int NextBeatRecovery;

		public bool ApplyCommandNextBeat;
	}

	public struct RhythmEngineSettings : IComponentData
	{
		public int MaxBeats;
		public int BeatInterval; // in ms

		/// <summary>
		/// Let the players simulate the rhythm engine or not.
		/// If enabled, 'RhythmEngineClientPredictedCommand' and 'RhythmEngineClientRequestedCommand' will be
		/// used instead of 'RhythmEngineCurrentCommand'.
		/// </summary>
		public bool UseClientSimulation;
	}

	public struct PressureEvent : IComponentData
	{
		public Entity Engine;
		public int    Key;
		public int    OriginalBeat;
		public int    CorrectedBeat;
		public float  Score;
	}

	/// <summary>
	/// This component should only be used on simulated rhythm engines (eg: client owned rhythm engines)
	/// </summary>
	[InternalBufferCapacity(8)]
	public struct RhythmEngineCurrentCommand : IBufferElementData
	{
		public RhythmPressureData Data;
	}

	/// <summary>
	/// This component should only be used on server
	/// </summary>
	[InternalBufferCapacity(8)]
	public struct RhythmEngineClientPredictedCommand : IBufferElementData
	{
		public RhythmPressureData Data;
	}

	/// <summary>
	/// This component should only be used on server
	/// </summary>
	[InternalBufferCapacity(8)]
	public struct RhythmEngineClientRequestedCommand : IBufferElementData
	{
		public RhythmPressureData Data;
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

	public struct SkipCommand : IComponentData
	{
	}
}