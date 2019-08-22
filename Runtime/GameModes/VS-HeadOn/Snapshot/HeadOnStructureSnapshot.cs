using System;
using Patapon4TLB.Default;
using Patapon4TLB.GameModes.Authoring;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.Networking.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Transforms;

namespace Patapon4TLB.GameModes.Snapshot
{
	public struct HeadOnStructureSnapshot : ISnapshotData<HeadOnStructureSnapshot>
	{
		public const int Quantization    = 1000;
		public const float InvQuantization = 1f / 1000f;

		public uint Tick { get; set; }

		public QuantizedFloat3 Position;

		public HeadOnStructure.EType Type;
		public CaptureAreaType       CaptureType;

		public int TimeToCapture;
		public int Progress0;
		public int Progress1;

		// 0: wood, 1: steel, 2: iron
		public uint Level;

		public void PredictDelta(uint tick, ref HeadOnStructureSnapshot baseline1, ref HeadOnStructureSnapshot baseline2)
		{
			throw new NotImplementedException();
		}

		public void Serialize(ref HeadOnStructureSnapshot baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
		{
			for (var i = 0; i < 2; i++)
				writer.WritePackedIntDelta(Position[i], baseline.Position[i], compressionModel);

			writer.WritePackedUIntDelta((uint) Type, (uint) baseline.Type, compressionModel);
			writer.WritePackedUIntDelta((uint) CaptureType, (uint) baseline.CaptureType, compressionModel);
			writer.WritePackedUIntDelta(Level, baseline.Level, compressionModel);

			writer.WritePackedIntDelta(TimeToCapture, baseline.TimeToCapture, compressionModel);

			writer.WritePackedIntDelta(Progress0, baseline.Progress0, compressionModel);
			writer.WritePackedIntDelta(Progress1, baseline.Progress1, compressionModel);
		}

		public void Deserialize(uint tick, ref HeadOnStructureSnapshot baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
		{
			Tick = tick;
			
			for (var i = 0; i < 2; i++)
				Position[i] = reader.ReadPackedIntDelta(ref ctx, baseline.Position[i], compressionModel);

			Type        = (HeadOnStructure.EType) reader.ReadPackedUIntDelta(ref ctx, (uint) baseline.Type, compressionModel);
			CaptureType = (CaptureAreaType) reader.ReadPackedUIntDelta(ref ctx, (uint) baseline.CaptureType, compressionModel);
			Level       = reader.ReadPackedUIntDelta(ref ctx, baseline.Level, compressionModel);

			TimeToCapture = reader.ReadPackedIntDelta(ref ctx, baseline.TimeToCapture, compressionModel);
			Progress0     = reader.ReadPackedIntDelta(ref ctx, baseline.Progress0, compressionModel);
			Progress1     = reader.ReadPackedIntDelta(ref ctx, baseline.Progress1, compressionModel);
		}

		public void Interpolate(ref HeadOnStructureSnapshot target, float factor)
		{
			this = target;
		}
	}

	public struct HeadOnStructureGhostSerializer : IGhostSerializer<HeadOnStructureSnapshot>
	{
		public int SnapshotSize => UnsafeUtility.SizeOf<HeadOnStructureSnapshot>();

		public int CalculateImportance(ArchetypeChunk chunk)
		{
			return 10;
		}

		public bool WantsPredictionDelta => false;

		public void BeginSerialize(ComponentSystemBase system)
		{
			system.GetGhostComponentType(out LocalToWorldGhostType);
			system.GetGhostComponentType(out StructureGhostType);
			system.GetGhostComponentType(out CaptureAreaGhostType);
		}

		public GhostComponentType<LocalToWorld>         LocalToWorldGhostType;
		public GhostComponentType<HeadOnStructure>      StructureGhostType;
		public GhostComponentType<CaptureAreaComponent> CaptureAreaGhostType;

		public bool CanSerialize(EntityArchetype arch)
		{
			var comps = arch.GetComponentTypes();
			var count = 0;
			for (var i = 0; i != comps.Length; i++)
			{
				if (comps[i] == StructureGhostType) count++;
				if (comps[i] == CaptureAreaGhostType) count++;
			}

			return count == 2;
		}

		public unsafe void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref HeadOnStructureSnapshot snapshot)
		{
			var transform = chunk.GetNativeArray(LocalToWorldGhostType.Archetype)[ent];
			snapshot.Position.Set(HeadOnStructureSnapshot.Quantization, transform.Position);

			var structureData = chunk.GetNativeArray(StructureGhostType.Archetype)[ent];
			snapshot.Type          = structureData.Type;
			snapshot.TimeToCapture = structureData.TimeToCapture;
			snapshot.Progress0     = structureData.CaptureProgress[0];
			snapshot.Progress1     = structureData.CaptureProgress[1];

			var captureArea = chunk.GetNativeArray(CaptureAreaGhostType.Archetype)[ent];
			snapshot.CaptureType = captureArea.CaptureType;
		}
	}

	public class HeadOnStructureGhostSpawnSystem : DefaultGhostSpawnSystem<HeadOnStructureSnapshot>
	{
		protected override EntityArchetype GetGhostArchetype()
		{
			return EntityManager.CreateArchetype
			(
				typeof(HeadOnStructureSnapshot),
				typeof(Translation),
				typeof(HeadOnStructure),
				typeof(LivableHealth),
				typeof(CaptureAreaComponent),
				typeof(ReplicatedEntityComponent)
			);
		}

		protected override EntityArchetype GetPredictedGhostArchetype()
		{
			return GetGhostArchetype();
		}
	}

	[UpdateInGroup(typeof(UpdateGhostSystemGroup))]
	public class HeadOnStructureUpdateSystem : JobComponentSystem
	{
		private struct JobProcess : IJobForEach_BCCC<HeadOnStructureSnapshot, Translation, HeadOnStructure, CaptureAreaComponent>
		{
			public UTick ServerTick;

			public unsafe void Execute(DynamicBuffer<HeadOnStructureSnapshot> snapshotArray, ref Translation translation, ref HeadOnStructure structure, ref CaptureAreaComponent captureArea)
			{
				snapshotArray.GetDataAtTick(ServerTick.AsUInt, out var snapshotData);

				translation.Value            = snapshotData.Position.Get(HeadOnStructureSnapshot.InvQuantization);
				structure.Type               = snapshotData.Type;
				structure.CaptureProgress[0] = snapshotData.Progress0;
				structure.CaptureProgress[1] = snapshotData.Progress1;
				structure.TimeToCapture      = snapshotData.TimeToCapture;
				captureArea.CaptureType      = snapshotData.CaptureType;
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new JobProcess
			{
				ServerTick = World.GetExistingSystem<NetworkTimeSystem>().GetTickInterpolated()
			}.Schedule(this, inputDeps);
		}
	}
}