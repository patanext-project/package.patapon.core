using Revolution;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Transforms;
using UnityEngine;

namespace Patapon4TLB.Core.Snapshots
{
	public struct TranslationSnapshot : IReadWriteSnapshot<TranslationSnapshot>, ISnapshotDelta<TranslationSnapshot>, ISynchronizeImpl<Translation>, IInterpolatable<TranslationSnapshot>
	{
		public const int    dimension = 2;
		public       float2 Value;

		public void WriteTo(DataStreamWriter writer, ref TranslationSnapshot baseline, NetworkCompressionModel compressionModel)
		{
			for (var i = 0; i < dimension; i++)
				writer.WritePackedFloatDelta(Value[i], default, compressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref TranslationSnapshot baseline, NetworkCompressionModel compressionModel)
		{
			for (var i = 0; i < dimension; i++)
				Value[i] = reader.ReadPackedFloatDelta(ref ctx, default, compressionModel);
		}

		public uint Tick { get; set; }

		public bool DidChange(TranslationSnapshot baseline)
		{
			return math.any(Value != baseline.Value);
		}

		public void SynchronizeFrom(in Translation component, in DefaultSetup setup, in SerializeClientData serializeData)
		{
			Value.x = component.Value.x;
			Value.y = component.Value.y;
		}

		public void SynchronizeTo(ref Translation component, in DeserializeClientData deserializeData)
		{
			component.Value.x = Value.x;
			component.Value.y = Value.y;
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : ComponentSnapshotSystemBasic<Translation, TranslationSnapshot>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public class LocalUpdate : ComponentUpdateSystemInterpolated<Translation, TranslationSnapshot>
		{
		}

		public void Interpolate(TranslationSnapshot target, float factor)
		{
			Value = math.lerp(Value, target.Value, factor);
		}
	}

	public struct TranslationDirectSnapshot : IReadWriteSnapshot<TranslationDirectSnapshot>, ISnapshotDelta<TranslationDirectSnapshot>, ISynchronizeImpl<Translation>
	{
		public const int    dimension = 2;
		public       float2 Value;

		public void WriteTo(DataStreamWriter writer, ref TranslationDirectSnapshot baseline, NetworkCompressionModel compressionModel)
		{
			for (var i = 0; i < dimension; i++)
				writer.WritePackedFloatDelta(Value[i], default, compressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref TranslationDirectSnapshot baseline, NetworkCompressionModel compressionModel)
		{
			for (var i = 0; i < dimension; i++)
				Value[i] = reader.ReadPackedFloatDelta(ref ctx, default, compressionModel);
		}

		public uint Tick { get; set; }

		public bool DidChange(TranslationDirectSnapshot baseline)
		{
			return math.any(Value != baseline.Value);
		}

		public void SynchronizeFrom(in Translation component, in DefaultSetup setup, in SerializeClientData serializeData)
		{
			Value.x = component.Value.x;
			Value.y = component.Value.y;
		}

		public void SynchronizeTo(ref Translation component, in DeserializeClientData deserializeData)
		{
			Debug.Log("use direct");
			component.Value.x = Value.x;
			component.Value.y = Value.y;
		}

		public struct Exclude : IComponentData
		{
		}
		
		public struct Use : IComponentData {}

		public class NetSynchronize : ComponentSnapshotSystemBasic<Translation, TranslationDirectSnapshot>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);

			public override NativeArray<ComponentType> EntityComponents =>
				new NativeArray<ComponentType>(2, Allocator.Temp)
				{
					[0] = typeof(TranslationSnapshot.Exclude),
					[1] = typeof(Use),
				};
		}

		public class LocalUpdate : ComponentUpdateSystemDirect<Translation, TranslationDirectSnapshot>
		{
		}
	}
}