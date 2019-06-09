using System;
using Patapon4TLB.Default.Snapshot;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;
using Unity.NetCode;

public struct GhostDeserializerCollection : IGhostDeserializerCollection
{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
    public string[] CreateSerializerNameList()
    {
        var arr = new string[]
        {
            "DefaultRhythmEngineGhostSerializer",
            "SynchronizedSimulationTimeGhostSerializer",
            "GamePlayerGhostSerializer",

        };
        return arr;
    }

    public int Length => 3;
#endif
    public void Initialize(World world)
    {
        var curDefaultRhythmEngineGhostSpawnSystem = world.GetOrCreateSystem<DefaultRhythmEngineGhostSpawnSystem>();
        m_RhythmEngineSnapshotDataNewGhostIds = curDefaultRhythmEngineGhostSpawnSystem.NewGhostIds;
        m_RhythmEngineSnapshotDataNewGhosts = curDefaultRhythmEngineGhostSpawnSystem.NewGhosts;
        curDefaultRhythmEngineGhostSpawnSystem.GhostType = 0;
        var curSynchronizedSimulationTimeGhostSpawnSystem = world.GetOrCreateSystem<SynchronizedSimulationTimeGhostSpawnSystem>();
        m_SynchronizedSimulationTimeSnapshotNewGhostIds = curSynchronizedSimulationTimeGhostSpawnSystem.NewGhostIds;
        m_SynchronizedSimulationTimeSnapshotNewGhosts = curSynchronizedSimulationTimeGhostSpawnSystem.NewGhosts;
        curSynchronizedSimulationTimeGhostSpawnSystem.GhostType = 1;
        var curGamePlayerGhostSpawnSystem = world.GetOrCreateSystem<GamePlayerGhostSpawnSystem>();
        m_GamePlayerSnapshotNewGhostIds = curGamePlayerGhostSpawnSystem.NewGhostIds;
        m_GamePlayerSnapshotNewGhosts = curGamePlayerGhostSpawnSystem.NewGhosts;
        curGamePlayerGhostSpawnSystem.GhostType = 2;

    }

    public void BeginDeserialize(JobComponentSystem system)
    {
        m_RhythmEngineSnapshotDataFromEntity = system.GetBufferFromEntity<RhythmEngineSnapshotData>();
        m_SynchronizedSimulationTimeSnapshotFromEntity = system.GetBufferFromEntity<SynchronizedSimulationTimeSnapshot>();
        m_GamePlayerSnapshotFromEntity = system.GetBufferFromEntity<GamePlayerSnapshot>();

    }
    public void Deserialize(int serializer, Entity entity, uint snapshot, uint baseline, uint baseline2, uint baseline3,
        DataStreamReader reader,
        ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
    {
        switch (serializer)
        {
        case 0:
            GhostReceiveSystem<GhostDeserializerCollection>.InvokeDeserialize(m_RhythmEngineSnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            break;
        case 1:
            GhostReceiveSystem<GhostDeserializerCollection>.InvokeDeserialize(m_SynchronizedSimulationTimeSnapshotFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            break;
        case 2:
            GhostReceiveSystem<GhostDeserializerCollection>.InvokeDeserialize(m_GamePlayerSnapshotFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            break;

        default:
            throw new ArgumentException("Invalid serializer type");
        }
    }
    public void Spawn(int serializer, int ghostId, uint snapshot, DataStreamReader reader,
        ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
    {
        switch (serializer)
        {
            case 0:
                m_RhythmEngineSnapshotDataNewGhostIds.Add(ghostId);
                m_RhythmEngineSnapshotDataNewGhosts.Add(GhostReceiveSystem<GhostDeserializerCollection>.InvokeSpawn<RhythmEngineSnapshotData>(snapshot, reader, ref ctx, compressionModel));
                break;
            case 1:
                m_SynchronizedSimulationTimeSnapshotNewGhostIds.Add(ghostId);
                m_SynchronizedSimulationTimeSnapshotNewGhosts.Add(GhostReceiveSystem<GhostDeserializerCollection>.InvokeSpawn<SynchronizedSimulationTimeSnapshot>(snapshot, reader, ref ctx, compressionModel));
                break;
            case 2:
                m_GamePlayerSnapshotNewGhostIds.Add(ghostId);
                m_GamePlayerSnapshotNewGhosts.Add(GhostReceiveSystem<GhostDeserializerCollection>.InvokeSpawn<GamePlayerSnapshot>(snapshot, reader, ref ctx, compressionModel));
                break;

            default:
                throw new ArgumentException("Invalid serializer type");
        }
    }

    private BufferFromEntity<RhythmEngineSnapshotData> m_RhythmEngineSnapshotDataFromEntity;
    private NativeList<int> m_RhythmEngineSnapshotDataNewGhostIds;
    private NativeList<RhythmEngineSnapshotData> m_RhythmEngineSnapshotDataNewGhosts;
    private BufferFromEntity<SynchronizedSimulationTimeSnapshot> m_SynchronizedSimulationTimeSnapshotFromEntity;
    private NativeList<int> m_SynchronizedSimulationTimeSnapshotNewGhostIds;
    private NativeList<SynchronizedSimulationTimeSnapshot> m_SynchronizedSimulationTimeSnapshotNewGhosts;
    private BufferFromEntity<GamePlayerSnapshot> m_GamePlayerSnapshotFromEntity;
    private NativeList<int> m_GamePlayerSnapshotNewGhostIds;
    private NativeList<GamePlayerSnapshot> m_GamePlayerSnapshotNewGhosts;

}
public class P4ExperimentGhostReceiveSystem : GhostReceiveSystem<GhostDeserializerCollection>
{
}
