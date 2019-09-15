using Revolution;
using Revolution.NetCode;
using Revolution.Utils;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;
using Unity.Transforms;
using UnityEngine;

namespace Patapon4TLBCore.Snapshots
{
	public struct TranslationSnapshot : IReadWriteSnapshot<TranslationSnapshot>, ISnapshotDelta<TranslationSnapshot>, ISynchronizeImpl<Translation>
	{
		public QuantizedFloat3 Vector;

		public void WriteTo(DataStreamWriter writer, ref TranslationSnapshot baseline, NetworkCompressionModel compressionModel)
		{
			for (var i = 0; i < 2; i++)
				writer.WritePackedIntDelta(Vector[i], baseline.Vector[i], compressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref TranslationSnapshot baseline, NetworkCompressionModel compressionModel)
		{
			for (var i = 0; i < 2; i++)
				Vector[i] = reader.ReadPackedIntDelta(ref ctx, baseline.Vector[i], compressionModel);
		}

		public uint Tick { get; set; }

		public bool DidChange(TranslationSnapshot baseline)
		{
			return math.distance(Vector.Result, baseline.Vector.Result) > 10;
		}

		public void SynchronizeFrom(in Translation component, in DefaultSetup setup, in SerializeClientData serializeData)
		{
			Vector.Set(1000, component.Value);
		}

		public void SynchronizeTo(ref Translation component, in DeserializeClientData deserializeData)
		{
			component.Value = Vector.Get(0.001f);
		}
	}

	public class TranslationSnapshotSystem : ComponentSnapshotSystem_Delta<Translation, TranslationSnapshot>
	{
		public struct Exclude : IComponentData
		{
		}

		public override ComponentType ExcludeComponent => typeof(Exclude);
	}

	public class TransformSnapshotUpdateSystem : ComponentUpdateSystem<Translation, TranslationSnapshot>
	{
	}
}