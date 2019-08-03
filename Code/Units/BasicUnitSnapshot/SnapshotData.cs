using StormiumTeam.Networking.Utilities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Patapon4TLB.Core.BasicUnitSnapshot
{
	public struct BasicUnitSnapshotData : ISnapshotData<BasicUnitSnapshotData>
	{
		public const int   Quantization   = 10000;
		public const float DeQuantization = 0.0001f;

		public uint Tick { get; set; }

		public byte GroundFlags;

		public int             Direction;
		public QuantizedFloat3 TargetPosition;
		public QuantizedFloat3 Position;
		public QuantizedFloat3 Velocity;

		public void PredictDelta(uint tick, ref BasicUnitSnapshotData baseline1, ref BasicUnitSnapshotData baseline2)
		{
			var predict   = new GhostDeltaPredictor(tick, tick, baseline1.Tick, baseline2.Tick);
			var dimension = Direction == 0 ? 3 : 2;
			for (var i = 0; i < dimension; i++)
			{
				Position[i]       = predict.PredictInt(Position[i], baseline1.Position[i], baseline2.Position[i]);
				TargetPosition[i] = predict.PredictInt(TargetPosition[i], baseline1.TargetPosition[i], baseline2.TargetPosition[i]);
			}
		}

		public void Serialize(ref BasicUnitSnapshotData baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
		{
			// 2
			writer.WritePackedUIntDelta(GroundFlags, baseline.GroundFlags, compressionModel);
			writer.WritePackedInt(Direction, compressionModel);

			// Position and velocity
			var dimension = Direction == 0 ? 3 : 2; // if the position is invalid, that mean we are in a 3D dimension...
			for (var i = 0; i < dimension; i++)
			{
				writer.WritePackedIntDelta(Position[i], baseline.Position[i], compressionModel);
				writer.WritePackedIntDelta(Velocity[i], baseline.Velocity[i], compressionModel);
				writer.WritePackedIntDelta(TargetPosition[i], baseline.TargetPosition[i], compressionModel);
			}
		}

		public void Deserialize(uint tick, ref BasicUnitSnapshotData baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
		{
			Tick = tick;

			// 2
			GroundFlags = (byte) reader.ReadPackedUIntDelta(ref ctx, baseline.GroundFlags, compressionModel);
			Direction   = reader.ReadPackedInt(ref ctx, compressionModel);

			// Position and velocity
			var dimension = Direction == 0 ? 3 : 2;
			for (var i = 0; i < dimension; i++)
			{
				Position[i]       = reader.ReadPackedIntDelta(ref ctx, baseline.Position[i], compressionModel);
				Velocity[i]       = reader.ReadPackedIntDelta(ref ctx, baseline.Velocity[i], compressionModel);
				TargetPosition[i] = reader.ReadPackedIntDelta(ref ctx, baseline.TargetPosition[i], compressionModel);
			}
		}

		public void Interpolate(ref BasicUnitSnapshotData target, float factor)
		{
			Direction = target.Direction;

			Position.Result = (int3) math.lerp(Position.Result, target.Position.Result, factor);
			Velocity.Result = (int3) math.lerp(Velocity.Result, target.Velocity.Result, factor);
		}
	}
}