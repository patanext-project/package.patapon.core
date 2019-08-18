using System;
using DefaultNamespace;
using Patapon4TLB.Default;
using StormiumTeam.Networking.Utilities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Transforms;
using UnityEngine;

namespace Patapon4TLB.GameModes
{
	public class HeadOnFlagAuthoring : MonoBehaviour, IConvertGameObjectToEntity
	{
		public HeadOnDefineTeamAuthoring TeamDefine;

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			Debug.Assert(TeamDefine != null, "TeamDefine != null");

			dstManager.AddComponentData(entity, new HeadOnFlag());
			dstManager.AddComponentData(entity, TeamDefine.FindOrCreate(dstManager));
			dstManager.AddComponent(entity, typeof(GhostComponent));
		}

		private void OnDrawGizmos()
		{
			Color color;
			if (TeamDefine != null)
			{
				switch (TeamDefine.PredefinedTeam)
				{
					case EHeadOnTeamTarget.Blue:
						color = Color.blue;
						break;
					case EHeadOnTeamTarget.Red:
						color = Color.red;
						break;
					case EHeadOnTeamTarget.Undefined:
						color = Color.green;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			else
			{
				color = Color.black;
			}

			Gizmos.color = Color.Lerp(color, Color.white, 0.33f);
			Gizmos.DrawWireSphere(transform.position, 0.25f);
		}
	}

	public struct HeadOnFlag : IComponentData
	{
	}

	public struct HeadOnFlagSnapshotData : ISnapshotData<HeadOnFlagSnapshotData>
	{
		public const int   Quantization   = 100;
		public const float DeQuantization = 1 / 100f;

		public uint            Style;
		public uint            DefinedTeam;
		public QuantizedFloat3 Position;
		public uint            Tick { get; set; }

		public void PredictDelta(uint tick, ref HeadOnFlagSnapshotData baseline1, ref HeadOnFlagSnapshotData baseline2)
		{
			throw new NotImplementedException();
		}

		public void Serialize(ref HeadOnFlagSnapshotData baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
		{
			writer.WritePackedUIntDelta(Style, baseline.Style, compressionModel);
			writer.WritePackedUIntDelta(DefinedTeam, baseline.DefinedTeam, compressionModel);
			for (var i = 0; i != 2; i++)
			{
				writer.WritePackedIntDelta(Position[i], baseline.Position[i], compressionModel);
			}
		}

		public void Deserialize(uint tick, ref HeadOnFlagSnapshotData baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
		{
			Tick = tick;

			Style       = reader.ReadPackedUIntDelta(ref ctx, baseline.Style, compressionModel);
			DefinedTeam = reader.ReadPackedUIntDelta(ref ctx, baseline.DefinedTeam, compressionModel);
			for (var i = 0; i != 2; i++)
			{
				Position[i] = reader.ReadPackedIntDelta(ref ctx, baseline.Position[i], compressionModel);
			}
		}

		public void Interpolate(ref HeadOnFlagSnapshotData target, float factor)
		{
			this = target;
		}
	}

	public struct HeadOnFlagSerializer : IGhostSerializer<HeadOnFlagSnapshotData>
	{
		public int SnapshotSize => UnsafeUtility.SizeOf<HeadOnFlagSnapshotData>();

		public int CalculateImportance(ArchetypeChunk chunk)
		{
			return 1;
		}

		public bool WantsPredictionDelta => false;

		public void BeginSerialize(ComponentSystemBase system)
		{
			m_FlagTagType               = ComponentType.ReadWrite<HeadOnFlag>();
			m_HeadOnTeamTargetGhostType = system.GetGhostComponentType<HeadOnTeamTarget>();
			m_LocalToWorldGhostType     = system.GetGhostComponentType<LocalToWorld>();
		}

		private ComponentType                        m_FlagTagType;
		private GhostComponentType<HeadOnTeamTarget> m_HeadOnTeamTargetGhostType;
		private GhostComponentType<LocalToWorld>     m_LocalToWorldGhostType;

		public bool CanSerialize(EntityArchetype arch)
		{
			var types = arch.GetComponentTypes();
			var count = 0;
			for (var i = 0; i != types.Length; i++)
			{
				if (types[i].TypeIndex == m_FlagTagType.TypeIndex) count++;
				if (types[i] == m_HeadOnTeamTargetGhostType) count++;
				if (types[i] == m_LocalToWorldGhostType) count++;
			}

			return count == 3;
		}

		public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref HeadOnFlagSnapshotData snapshot)
		{
			snapshot.Tick = tick;

			var ltw = chunk.GetNativeArray(m_LocalToWorldGhostType.Archetype)[ent];
			snapshot.Position.Set(HeadOnFlagSnapshotData.Quantization, ltw.Position);

			var headOnTeamTarget = chunk.GetNativeArray(m_HeadOnTeamTargetGhostType.Archetype)[ent];
			snapshot.DefinedTeam = (uint) headOnTeamTarget.TeamIndex;
		}
	}

	public class HeadOnFlagGhostSpawnSystem : DefaultGhostSpawnSystem<HeadOnFlagSnapshotData>
	{
		protected override EntityArchetype GetGhostArchetype()
		{
			return EntityManager.CreateArchetype
			(
				typeof(HeadOnFlagSnapshotData),
				typeof(HeadOnFlag),
				typeof(HeadOnTeamTarget),
				typeof(LocalToWorld),
				typeof(Translation),
				typeof(ReplicatedEntityComponent)
			);
		}

		protected override EntityArchetype GetPredictedGhostArchetype()
		{
			return GetGhostArchetype();
		}

		[UpdateInGroup(typeof(UpdateGhostSystemGroup))]
		public class Process : JobComponentSystem
		{
			private struct Job : IJobForEach_BCC<HeadOnFlagSnapshotData, HeadOnTeamTarget, Translation>
			{
				public void Execute([ReadOnly] DynamicBuffer<HeadOnFlagSnapshotData> snapshotBuffer, ref HeadOnTeamTarget teamTarget, ref Translation translation)
				{
					if (snapshotBuffer.Length <= 0)
						return;

					var snapshot = snapshotBuffer[snapshotBuffer.Length - 1];
					
					teamTarget.TeamIndex = (int) snapshot.DefinedTeam;
					translation.Value    = snapshot.Position.Get(HeadOnFlagSnapshotData.DeQuantization);
				}
			}

			protected override JobHandle OnUpdate(JobHandle inputDeps)
			{
				return new Job
				{
				}.Schedule(this, inputDeps);
			}
		}
	}
}