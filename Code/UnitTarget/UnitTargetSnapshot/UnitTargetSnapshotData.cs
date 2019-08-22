using StormiumTeam.Networking.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Transforms;

namespace Patapon4TLB.Core
{
	public struct UnitTargetSnapshotData : ISnapshotData<UnitTargetSnapshotData>
	{
		public const int   Quantization   = 1000;
		public const float DeQuantization = 0.001f;

		public uint Tick { get; set; }

		public QuantizedFloat3 Position;

		public void PredictDelta(uint tick, ref UnitTargetSnapshotData baseline1, ref UnitTargetSnapshotData baseline2)
		{
			var predict = new GhostDeltaPredictor(tick, tick, baseline1.Tick, baseline2.Tick);
			for (var i = 0; i < 1; i++)
			{
				Position[i] = predict.PredictInt(Position[i], baseline1.Position[i], baseline2.Position[i]);
			}
		}

		public void Serialize(ref UnitTargetSnapshotData baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
		{
			// Position
			for (var i = 0; i < 1; i++)
			{
				writer.WritePackedIntDelta(Position[i], baseline.Position[i], compressionModel);
			}
		}

		public void Deserialize(uint tick, ref UnitTargetSnapshotData baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
		{
			Tick = tick;

			for (var i = 0; i < 1; i++)
			{
				Position[i] = reader.ReadPackedIntDelta(ref ctx, baseline.Position[i], compressionModel);
			}
		}

		public void Interpolate(ref UnitTargetSnapshotData target, float factor)
		{
			Position.Result = (int3) math.lerp(Position.Result, target.Position.Result, factor);
		}
	}

	public struct UnitTargetGhostSerializer : IGhostSerializer<UnitTargetSnapshotData>
	{
		public int SnapshotSize => UnsafeUtility.SizeOf<UnitTargetSnapshotData>();

		public int CalculateImportance(ArchetypeChunk chunk)
		{
			return 100;
		}

		public bool WantsPredictionDelta => true;

		public void BeginSerialize(ComponentSystemBase system)
		{
			UnitTargetDescriptionType = ComponentType.ReadWrite<UnitTargetDescription>();
			system.GetGhostComponentType(out TranslationGhostType);
		}

		public ComponentType                   UnitTargetDescriptionType;
		public GhostComponentType<Translation> TranslationGhostType;

		public bool CanSerialize(EntityArchetype arch)
		{
			var matches = 0;
			var comps   = arch.GetComponentTypes();
			for (var i = 0; i != comps.Length; i++)
			{
				if (comps[i].TypeIndex == UnitTargetDescriptionType.TypeIndex) matches++;
				if (comps[i] == TranslationGhostType) matches++;
			}

			return matches == 2;
		}

		public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref UnitTargetSnapshotData snapshot)
		{
			snapshot.Tick = tick;

			var translation = chunk.GetNativeArray(TranslationGhostType.Archetype)[ent];
			snapshot.Position.Set(UnitTargetSnapshotData.Quantization, translation.Value);
		}
	}
}