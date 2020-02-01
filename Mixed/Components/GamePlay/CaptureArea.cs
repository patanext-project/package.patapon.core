using Revolution;
using Unity.Entities;
using Unity.Networking.Transport;
using Unity.Physics;

namespace Patapon.Mixed.GamePlay
{
	public enum CaptureAreaType
	{
		/// <summary>
		///     When a team pass on the area, it's captured instantly
		/// </summary>
		Instant,

		/// <summary>
		///     When a team pass on the area, it's capturing progressively
		/// </summary>
		Progressive
	}


	public struct CaptureAreaComponent : IReadWriteComponentSnapshot<CaptureAreaComponent>
	{
		public CaptureAreaType CaptureType;
		public Aabb            Aabb;

		public void WriteTo(DataStreamWriter writer, ref CaptureAreaComponent baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedUInt((uint) CaptureType, jobData.NetworkCompressionModel);
			for (var i = 0; i != 3; i++)
			{
				writer.WritePackedFloat(Aabb.Min[i], jobData.NetworkCompressionModel);
				writer.WritePackedFloat(Aabb.Max[i], jobData.NetworkCompressionModel);
			}
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref CaptureAreaComponent baseline, DeserializeClientData jobData)
		{
			CaptureType = (CaptureAreaType) reader.ReadPackedUInt(ref ctx, jobData.NetworkCompressionModel);
			for (var i = 0; i != 3; i++)
			{
				Aabb.Min[i] = reader.ReadPackedFloat(ref ctx, jobData.NetworkCompressionModel);
				Aabb.Max[i] = reader.ReadPackedFloat(ref ctx, jobData.NetworkCompressionModel);
			}
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : MixedComponentSnapshotSystem<CaptureAreaComponent, DefaultSetup>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}
}