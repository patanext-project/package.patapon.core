using Revolution;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;
using UnityEngine;

namespace package.patapon.core
{
	// this is a header
	public struct FlowCommandManagerTypeDefinition : IComponentData
	{

	}

	public struct RhythmCurrentCommand : IReadWriteComponentSnapshot<RhythmCurrentCommand, GhostSetup>
	{
		public Entity Previous;
		public Entity CommandTarget;

		/// <summary>
		/// When will the command be active?
		/// </summary>
		/// <remarks>
		/// >=0 = the active beat (will have the same effect as -1 if CommandTarget don't exist or is null).
		/// -1 = not in effect.
		/// -2 = forever.
		/// </remarks>
		public int ActiveAtTime;

		/// <summary>
		/// If you want to set a custom beat ending.
		/// </summary>
		/// <remarks>
		/// >0 = the ending beat.
		/// 0 = the command will never be executed (but why).
		/// -1 = not in effect.
		/// -2 = forever (you can make a combo with ActiveAtBeat set at -1 to have a forever non ending command).
		/// </remarks>
		public int CustomEndTime;

		/// <summary>
		/// Power is associated with beat score, this is a value between 0 and 100.
		/// </summary>
		/// <remarks>
		/// This is not associated at all with fever state, the command will check if there is fever or not on the engine.
		/// The game will check if it can enable hero mode if power is 100.
		/// </remarks>
		public int Power;

		public bool HasPredictedCommands;

		public void WriteTo(DataStreamWriter writer, ref RhythmCurrentCommand baseline, GhostSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedUInt(setup[CommandTarget], jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref RhythmCurrentCommand baseline, DeserializeClientData jobData)
		{
			jobData.GhostToEntityMap.TryGetValue(reader.ReadPackedUInt(ref ctx, jobData.NetworkCompressionModel), out CommandTarget);
		}
	}

	public struct GamePredictedCommandState : IComponentData
	{
		public GameCommandState State;
	}

	/// <summary>
	/// The predicted client should only be used for presentation stuff (UI, sounds).
	/// To see if you should use the predicted component, check first if the server sided one is active or not.
	/// If false, then use the predicted one.
	/// </summary>
	public struct GameComboPredictedClient : IComponentData
	{
		public GameComboState State;
	}

	public struct GameComboState : IReadWriteComponentSnapshot<GameComboState>
	{
		public bool IsPerfect => Score >= 50;

		/// <summary>
		/// The score of the current combo. A perfect combo do a +5
		/// </summary>
		public int Score;

		/// <summary>
		/// The current chain of the combo
		/// </summary>
		public int Chain;

		/// <summary>
		/// It will be used to know when we should have the fever, it shouldn't be used to know the current chain.
		/// </summary>
		public int ChainToFever;

		/// <summary>
		/// The fever state, enabled if we have a score or 6 or more.
		/// </summary>
		public bool IsFever;

		public int JinnEnergy;
		public int JinnEnergyMax;

		public void Update(RhythmCurrentCommand rhythm, bool predicted)
		{
			var p = rhythm.Power - 50;
			if (p > 0 && Score < 0)
				Score = 0;

			Chain++;
			Score = math.min(Score + p, 200);
			Score = p;

			if (!IsFever)
			{
				ChainToFever++;
			}

			if (IsFever)
			{
				ChainToFever = 0;

				// add jinn energy
				if (Score >= 50) // we have a little bonus when doing a perfect command
				{
					JinnEnergy += 10;
				}
			}

			var needed = 0;
			if (ChainToFever < 2)
				needed += 100;

			if (!IsFever &&
			    (ChainToFever >= 9) ||
			    (ChainToFever >= 3 && Score >= 50) ||
			    (Score > (10 - ChainToFever) * 10 + needed))
			{
				IsFever = true;
			}
		}

		public void WriteTo(DataStreamWriter writer, ref GameComboState baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedIntDelta(Score, baseline.Score, jobData.NetworkCompressionModel);
			writer.WritePackedIntDelta(Chain, baseline.Chain, jobData.NetworkCompressionModel);
			writer.WritePackedIntDelta(ChainToFever, baseline.ChainToFever, jobData.NetworkCompressionModel);
			writer.WritePackedUIntDelta(IsFever ? 1u : 0u, baseline.IsFever ? 1u : 0u, jobData.NetworkCompressionModel);
			writer.WritePackedIntDelta(JinnEnergy, baseline.JinnEnergy, jobData.NetworkCompressionModel);
			writer.WritePackedIntDelta(JinnEnergyMax, baseline.JinnEnergyMax, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref GameComboState baseline, DeserializeClientData jobData)
		{
			Score         = reader.ReadPackedIntDelta(ref ctx, baseline.Score, jobData.NetworkCompressionModel);
			Chain         = reader.ReadPackedIntDelta(ref ctx, baseline.Chain, jobData.NetworkCompressionModel);
			ChainToFever  = reader.ReadPackedIntDelta(ref ctx, baseline.ChainToFever, jobData.NetworkCompressionModel);
			IsFever       = reader.ReadPackedUIntDelta(ref ctx, baseline.IsFever ? 1u : 0u, jobData.NetworkCompressionModel) == 1;
			JinnEnergy    = reader.ReadPackedIntDelta(ref ctx, baseline.JinnEnergy, jobData.NetworkCompressionModel);
			JinnEnergyMax = reader.ReadPackedIntDelta(ref ctx, baseline.JinnEnergyMax, jobData.NetworkCompressionModel);
		}
	}

	public struct GameComboChain : IBufferElementData
	{

	}

	public struct GameCommandState : IReadWriteComponentSnapshot<GameCommandState>
	{
		public int StartTime;
		public int EndTime;
		public int ChainEndTime;

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

		public void WriteTo(DataStreamWriter writer, ref GameCommandState baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedIntDelta(StartTime, baseline.StartTime, jobData.NetworkCompressionModel);
			writer.WritePackedIntDelta(EndTime, baseline.EndTime, jobData.NetworkCompressionModel);
			writer.WritePackedIntDelta(ChainEndTime, baseline.ChainEndTime, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref GameCommandState baseline, DeserializeClientData jobData)
		{
			StartTime    = reader.ReadPackedIntDelta(ref ctx, baseline.StartTime, jobData.NetworkCompressionModel);
			EndTime      = reader.ReadPackedIntDelta(ref ctx, baseline.EndTime, jobData.NetworkCompressionModel);
			ChainEndTime = reader.ReadPackedIntDelta(ref ctx, baseline.ChainEndTime, jobData.NetworkCompressionModel);
		}
	}

	public struct RhythmCommandSequence
	{
		public RangeInt BeatRange;
		public int      Key;

		public RhythmCommandSequence(int beatFract, int key)
		{
			BeatRange = new RangeInt(beatFract, 0);
			Key       = key;
		}

		public RhythmCommandSequence(int beatFract, int beatFractLength, int key)
		{
			BeatRange = new RangeInt(beatFract, beatFractLength);
			Key       = key;
		}

		public int BeatEnd => BeatRange.end;
	}

	public struct RhythmCommandSequenceContainer : IBufferElementData
	{
		public RhythmCommandSequence Value;
	}

	public struct RhythmCommandData : IComponentData
	{
		public NativeString64 Identifier;
		public int            BeatLength;
	}
}