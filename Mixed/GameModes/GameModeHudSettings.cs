using Revolution;
using Scripts.Utilities;
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

		public void WriteTo(DataStreamWriter writer, ref GameModeHudSettings baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WriteBitBool(EnableUnitSounds);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref GameModeHudSettings baseline, DeserializeClientData jobData)
		{
			EnableUnitSounds = reader.ReadBitBool(ref ctx);
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
}