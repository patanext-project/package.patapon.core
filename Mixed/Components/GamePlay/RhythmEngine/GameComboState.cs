using Patapon.Mixed.GamePlay.Units;
using Revolution;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GamePlay.RhythmEngine
{
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
				JinnEnergy = 0;
			}

			if (IsFever)
			{
				ChainToFever = 0;
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
			this = baseline;
			Score         = reader.ReadPackedIntDelta(ref ctx, baseline.Score, jobData.NetworkCompressionModel);
			Chain         = reader.ReadPackedIntDelta(ref ctx, baseline.Chain, jobData.NetworkCompressionModel);
			ChainToFever  = reader.ReadPackedIntDelta(ref ctx, baseline.ChainToFever, jobData.NetworkCompressionModel);
			IsFever       = reader.ReadPackedUIntDelta(ref ctx, baseline.IsFever ? 1u : 0u, jobData.NetworkCompressionModel) == 1;
			JinnEnergy    = reader.ReadPackedIntDelta(ref ctx, baseline.JinnEnergy, jobData.NetworkCompressionModel);
			JinnEnergyMax = reader.ReadPackedIntDelta(ref ctx, baseline.JinnEnergyMax, jobData.NetworkCompressionModel);
		}
		
		public struct Exclude : IComponentData
		{}
		
		public class Synchronize : MixedComponentSnapshotSystem<GameComboState, DefaultSetup>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}
}