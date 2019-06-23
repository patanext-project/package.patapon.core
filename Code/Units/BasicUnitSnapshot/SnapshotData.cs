using StormiumTeam.Networking.Utilities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Patapon4TLB.Core.BasicUnitSnapshot
{
	public struct BasicUnitSnapshotData : ISnapshotData<BasicUnitSnapshotData>
	{
		public const int Quantization = 1000;
		public const float DeQuantization = 0.001f;
		
		public uint Tick { get; set; }

		public uint OwnerGhostId;
		public uint TeamGhostId;
		public uint RhythmEngineGhostId;

		public int             Direction;
		public QuantizedFloat3 Position;
		public QuantizedFloat3 Velocity;

		public void PredictDelta(uint tick, ref BasicUnitSnapshotData baseline1, ref BasicUnitSnapshotData baseline2)
		{
			var predict   = new GhostDeltaPredictor(tick, tick, baseline1.Tick, baseline2.Tick);
			var dimension = Direction == 0 ? 3 : 2;
			for (var i = 0; i < dimension; i++)
			{
				Position[i] = predict.PredictInt(Position[i], baseline1.Position[i], baseline2.Position[i]);
			}
		}

		public void Serialize(ref BasicUnitSnapshotData baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
		{
			// 4
			writer.WritePackedUIntDelta(OwnerGhostId, baseline.OwnerGhostId, compressionModel);
			writer.WritePackedUIntDelta(TeamGhostId, baseline.TeamGhostId, compressionModel);
			writer.WritePackedUIntDelta(RhythmEngineGhostId, baseline.RhythmEngineGhostId, compressionModel);
			writer.WritePackedInt(Direction, compressionModel);

			// Position and velocity
			var dimension = Direction == 0 ? 3 : 2; // if the position is invalid, that mean we are in a 3D dimension...
			for (var i = 0; i < dimension; i++)
			{
				writer.WritePackedIntDelta(Position[i], baseline.Position[i], compressionModel);
				writer.WritePackedIntDelta(Velocity[i], baseline.Velocity[i], compressionModel);
			}
		}

		public void Deserialize(uint tick, ref BasicUnitSnapshotData baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
		{
			Tick = tick;
			
			// 4
			OwnerGhostId        = reader.ReadPackedUIntDelta(ref ctx, baseline.OwnerGhostId, compressionModel);
			TeamGhostId         = reader.ReadPackedUIntDelta(ref ctx, baseline.TeamGhostId, compressionModel);
			RhythmEngineGhostId = reader.ReadPackedUIntDelta(ref ctx, baseline.RhythmEngineGhostId, compressionModel);
			Direction           = reader.ReadPackedInt(ref ctx, compressionModel);

			// Position and velocity
			var dimension = Direction == 0 ? 3 : 2;
			for (var i = 0; i < dimension; i++)
			{
				Position[i] = reader.ReadPackedIntDelta(ref ctx, baseline.Position[i], compressionModel);
				Velocity[i] = reader.ReadPackedIntDelta(ref ctx, baseline.Velocity[i], compressionModel);
			}
		}

		public void Interpolate(ref BasicUnitSnapshotData target, float factor)
		{
			OwnerGhostId = target.OwnerGhostId;
			Direction    = target.Direction;

			Position.Result = (int3) math.lerp(Position.Result, target.Position.Result, factor);
			Velocity.Result = (int3) math.lerp(Velocity.Result, target.Velocity.Result, factor);
		}
	}
}