using Revolution;
using Scripts.Utilities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Networking.Transport;
using Utilities;

namespace Patapon4TLB.Default
{
	public struct TargetSceneAsset : IReadWriteComponentSnapshot<TargetSceneAsset>, ISnapshotDelta<TargetSceneAsset>
	{
		public NativeString512 Str;

		public void WriteTo(DataStreamWriter writer, ref TargetSceneAsset baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedStringDelta(Str, default, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref TargetSceneAsset baseline, DeserializeClientData jobData)
		{
			Str = reader.ReadPackedStringDelta(ref ctx, default(NativeString512), jobData.NetworkCompressionModel);
		}

		public bool DidChange(TargetSceneAsset baseline)
		{
			return UnsafeUtilityOp.AreNotEquals(ref this, ref baseline);
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : MixedComponentSnapshotSystemDelta<TargetSceneAsset>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}
}