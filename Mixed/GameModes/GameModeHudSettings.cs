using Revolution;
using Scripts.Utilities;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GameModes
{
	public struct GameModeHudSettings : IReadWriteComponentSnapshot<GameModeHudSettings>, ISnapshotDelta<GameModeHudSettings>
	{
		/// <summary>
		/// Death, reborn, ... sounds
		/// </summary>
		public bool EnableUnitSounds;

		public bool EnablePreMatchInterface;
		public bool EnableGameModeInterface;

		public uint                 StatusTick;
		public NativeString512      StatusMessage;
		public NativeString64       StatusMessageArg0;
		public NativeString64       StatusMessageArg1;
		public NativeString64       StatusMessageArg2;
		public NativeString64       StatusMessageArg3;
		public EGameModeStatusSound StatusSound;

		public void PushStatus(UTick tick, NativeString512 msg, EGameModeStatusSound sound = EGameModeStatusSound.None)
		{
			StatusTick    = tick.AsUInt;
			StatusMessage = msg;
			StatusSound   = sound;

			StatusMessageArg0 = default;
			StatusMessageArg1 = default;
			StatusMessageArg2 = default;
			StatusMessageArg3 = default;
		}

		public void PushStatus(UTick tick, NativeString512 msg, NativeString64 arg0, EGameModeStatusSound sound = EGameModeStatusSound.None)
		{
			StatusTick    = tick.AsUInt;
			StatusMessage = msg;
			StatusSound   = sound;

			StatusMessageArg0 = arg0;
			StatusMessageArg1 = default;
			StatusMessageArg2 = default;
			StatusMessageArg3 = default;
		}

		public void PushStatus(UTick tick, NativeString512 msg, NativeString64 arg0, NativeString64 arg1, EGameModeStatusSound sound = EGameModeStatusSound.None)
		{
			StatusTick    = tick.AsUInt;
			StatusMessage = msg;
			StatusSound   = sound;

			StatusMessageArg0 = arg0;
			StatusMessageArg1 = arg1;
			StatusMessageArg2 = default;
			StatusMessageArg3 = default;
		}

		public void WriteTo(DataStreamWriter writer, ref GameModeHudSettings baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WriteBitBool(EnableUnitSounds);
			writer.WriteBitBool(EnableGameModeInterface);
			writer.WriteBitBool(EnablePreMatchInterface);
			writer.WritePackedUInt(StatusTick, jobData.NetworkCompressionModel);
			writer.WritePackedStringDelta(StatusMessage, default, jobData.NetworkCompressionModel);
			writer.WritePackedStringDelta(StatusMessageArg0, default, jobData.NetworkCompressionModel);
			writer.WritePackedStringDelta(StatusMessageArg1, default, jobData.NetworkCompressionModel);
			writer.WritePackedStringDelta(StatusMessageArg2, default, jobData.NetworkCompressionModel);
			writer.WritePackedStringDelta(StatusMessageArg3, default, jobData.NetworkCompressionModel);
			writer.WritePackedUInt((uint) StatusSound, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref GameModeHudSettings baseline, DeserializeClientData jobData)
		{
			EnableUnitSounds        = reader.ReadBitBool(ref ctx);
			EnableGameModeInterface = reader.ReadBitBool(ref ctx);
			EnablePreMatchInterface = reader.ReadBitBool(ref ctx);
			StatusTick              = reader.ReadPackedUInt(ref ctx, jobData.NetworkCompressionModel);
			StatusMessage           = reader.ReadPackedStringDelta(ref ctx, default(NativeString512), jobData.NetworkCompressionModel);
			StatusMessageArg0       = reader.ReadPackedStringDelta(ref ctx, default(NativeString64), jobData.NetworkCompressionModel);
			StatusMessageArg1       = reader.ReadPackedStringDelta(ref ctx, default(NativeString64), jobData.NetworkCompressionModel);
			StatusMessageArg2       = reader.ReadPackedStringDelta(ref ctx, default(NativeString64), jobData.NetworkCompressionModel);
			StatusMessageArg3       = reader.ReadPackedStringDelta(ref ctx, default(NativeString64), jobData.NetworkCompressionModel);
			StatusSound             = (EGameModeStatusSound) reader.ReadPackedUInt(ref ctx, jobData.NetworkCompressionModel);
		}

		public bool DidChange(GameModeHudSettings baseline)
		{
			return UnsafeUtilityOp.AreNotEquals(ref this, ref baseline);
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : MixedComponentSnapshotSystemDelta<GameModeHudSettings>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}

	public enum EGameModeStatusSound : uint
	{
		None                 = 0,
		TowerControlCaptured = 10,
		FlagCaptured         = 11,
		NewLeader            = 12,
		TeamKill             = 13,
		WinningSequence      = 14,
	}
}