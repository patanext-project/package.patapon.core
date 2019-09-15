using Revolution;
using Revolution.NetCode;
using Revolution.Utils;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;
using Unity.Transforms;

namespace Patapon4TLBCore.Snapshots
{
	public struct LocalToWorldSnapshot : IReadWriteSnapshot<LocalToWorldSnapshot>, ISnapshotDelta<LocalToWorldSnapshot>, ISynchronizeImpl<LocalToWorld>
	{
		public QuantizedFloat3 Position;
		public QuantizedFloat3 Rotation;

		public void WriteTo(DataStreamWriter writer, ref LocalToWorldSnapshot baseline, NetworkCompressionModel compressionModel)
		{
			for (var i = 0; i < 2; i++)
				writer.WritePackedIntDelta(Position[i], baseline.Position[i], compressionModel);
			for (var i = 0; i < 2; i++)
				writer.WritePackedIntDelta(Rotation[i], baseline.Rotation[i], compressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref LocalToWorldSnapshot baseline, NetworkCompressionModel compressionModel)
		{
			for (var i = 0; i < 2; i++)
				Position[i] = reader.ReadPackedIntDelta(ref ctx, baseline.Position[i], compressionModel);
			for (var i = 0; i < 2; i++)
				Rotation[i] = reader.ReadPackedIntDelta(ref ctx, baseline.Rotation[i], compressionModel);
		}

		public uint Tick { get; set; }

		public bool DidChange(LocalToWorldSnapshot baseline)
		{
			return math.distance(Position.Result, baseline.Position.Result) > 10 || !math.all(Rotation.Result == baseline.Rotation.Result);
		}

		public void SynchronizeFrom(in LocalToWorld component, in DefaultSetup setup, in SerializeClientData serializeData)
		{
			var rt = new RigidTransform(component.Value);

			Position.Set(1000, rt.pos);
			Rotation.Set(1000, math.forward(rt.rot));
		}

		public void SynchronizeTo(ref LocalToWorld component, in DeserializeClientData deserializeData)
		{
			var ltw = math.float4x4(quaternion.LookRotation(Rotation.Get(0.001f), math.up()), Position.Get(0.001f));

			component.Value = ltw;
		}
	}

	public class LocalToWorldSnapshotSystem : ComponentSnapshotSystem_Delta<LocalToWorld, LocalToWorldSnapshot>
	{
		public struct Exclude : IComponentData
		{
		}

		public override ComponentType ExcludeComponent => typeof(Exclude);
	}
	
	public class LocalToWorldUpdateSystem : ComponentUpdateSystem<LocalToWorld, LocalToWorldSnapshot>
	{}
}