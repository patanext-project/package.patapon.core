using Revolution;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GamePlay.Units
{
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
}