using package.patapon.core;
using Revolution;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon4TLB.Default
{
	public struct RhythmEngineState : IReadWriteComponentSnapshot<RhythmEngineState>
	{
		public bool IsPaused;
		public bool IsNewBeat;
		public bool IsNewPressure;

		/// <summary>
		/// If a user do a f**k-up (doing pressure in an active command, waited a beat too much,...), he will need to wait a beat before starting to do pressures.
		/// </summary>
		public int NextBeatRecovery;

		public bool ApplyCommandNextBeat;
		public bool VerifyCommand;
		public int LastPressureBeat;

		public bool IsRecovery(int processBeat)
		{
			return NextBeatRecovery > processBeat;
		}

		public void WriteTo(DataStreamWriter writer, ref RhythmEngineState baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedUIntDelta(IsPaused ? 1u : 0u, baseline.IsPaused ? 1u : 0u, jobData.NetworkCompressionModel);
			writer.WritePackedIntDelta(NextBeatRecovery, baseline.NextBeatRecovery, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref RhythmEngineState baseline, DeserializeClientData jobData)
		{
			IsPaused = reader.ReadPackedUIntDelta(ref ctx, baseline.IsPaused ? 1u : 0u, jobData.NetworkCompressionModel) == 1;
			NextBeatRecovery = reader.ReadPackedIntDelta(ref ctx, baseline.NextBeatRecovery, jobData.NetworkCompressionModel);
		}
	}

	public struct RhythmEngineSettings : IReadWriteComponentSnapshot<RhythmEngineSettings>
	{
		public int MaxBeats;
		public int BeatInterval; // in ms

		/// <summary>
		/// Let the players simulate the rhythm engine or not.
		/// If enabled, 'RhythmEngineClientPredictedCommand' and 'RhythmEngineClientRequestedCommand' will be
		/// used instead of 'RhythmEngineCurrentCommand'.
		/// </summary>
		public bool UseClientSimulation;

		public void WriteTo(DataStreamWriter writer, ref RhythmEngineSettings baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedIntDelta(MaxBeats, baseline.MaxBeats, jobData.NetworkCompressionModel);
			writer.WritePackedIntDelta(BeatInterval, baseline.BeatInterval, jobData.NetworkCompressionModel);
			writer.WritePackedUIntDelta(UseClientSimulation ? 1u : 0u, baseline.UseClientSimulation ? 1u : 0u, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref RhythmEngineSettings baseline, DeserializeClientData jobData)
		{
			MaxBeats     = reader.ReadPackedIntDelta(ref ctx, baseline.MaxBeats, jobData.NetworkCompressionModel);
			BeatInterval = reader.ReadPackedIntDelta(ref ctx, baseline.BeatInterval, jobData.NetworkCompressionModel);

			UseClientSimulation = reader.ReadPackedUIntDelta(ref ctx, baseline.UseClientSimulation ? 1u : 0u, jobData.NetworkCompressionModel) == 1;
		}
	}

	public struct PressureEvent : IComponentData
	{
		public Entity Engine;
		public int    Key;
		public long   TimeMs;
		public int    RenderBeat;
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