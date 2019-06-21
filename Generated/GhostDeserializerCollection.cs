using System;
using Patapon4TLB.Default.Snapshot;
using Patapon4TLB.GameModes.Basic;
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
            "BasicGameModeSerializer",
            "DefaultRhythmEngineGhostSerializer",
            "SynchronizedSimulationTimeGhostSerializer",
            "GamePlayerGhostSerializer",
            "TeamEmptyGhostSerializer",

        };
        return arr;
    }

    public int Length => 5;
#endif
    public void Initialize(World world)
    {
        var curBasicGameModeSnapshotGhostSpawnSystem = world.GetOrCreateSystem<BasicGameModeSnapshotGhostSpawnSystem>();
        m_BasicGameModeSnapshotNewGhostIds = curBasicGameModeSnapshotGhostSpawnSystem.NewGhostIds;
        m_BasicGameModeSnapshotNewGhosts = curBasicGameModeSnapshotGhostSpawnSystem.NewGhosts;
        curBasicGameModeSnapshotGhostSpawnSystem.GhostType = 0;
        var curDefaultRhythmEngineGhostSpawnSystem = world.GetOrCreateSystem<DefaultRhythmEngineGhostSpawnSystem>();
        m_RhythmEngineSnapshotDataNewGhostIds = curDefaultRhythmEngineGhostSpawnSystem.NewGhostIds;
        m_RhythmEngineSnapshotDataNewGhosts = curDefaultRhythmEngineGhostSpawnSystem.NewGhosts;
        curDefaultRhythmEngineGhostSpawnSystem.GhostType = 1;
        var curSynchronizedSimulationTimeGhostSpawnSystem = world.GetOrCreateSystem<SynchronizedSimulationTimeGhostSpawnSystem>();
        m_SynchronizedSimulationTimeSnapshotNewGhostIds = curSynchronizedSimulationTimeGhostSpawnSystem.NewGhostIds;
        m_SynchronizedSimulationTimeSnapshotNewGhosts = curSynchronizedSimulationTimeGhostSpawnSystem.NewGhosts;
        curSynchronizedSimulationTimeGhostSpawnSystem.GhostType = 2;
        var curGamePlayerGhostSpawnSystem = world.GetOrCreateSystem<GamePlayerGhostSpawnSystem>();
        m_GamePlayerSnapshotNewGhostIds = curGamePlayerGhostSpawnSystem.NewGhostIds;
        m_GamePlayerSnapshotNewGhosts = curGamePlayerGhostSpawnSystem.NewGhosts;
        curGamePlayerGhostSpawnSystem.GhostType = 3;
        var curTeamEmptyGhostSpawnSystem = world.GetOrCreateSystem<TeamEmptyGhostSpawnSystem>();
        m_TeamEmptySnapshotDataNewGhostIds = curTeamEmptyGhostSpawnSystem.NewGhostIds;
        m_TeamEmptySnapshotDataNewGhosts = curTeamEmptyGhostSpawnSystem.NewGhosts;
        curTeamEmptyGhostSpawnSystem.GhostType = 4;

    }

    public void BeginDeserialize(JobComponentSystem system)
    {
        m_BasicGameModeSnapshotFromEntity = system.GetBufferFromEntity<BasicGameModeSnapshot>();
        m_RhythmEngineSnapshotDataFromEntity = system.GetBufferFromEntity<RhythmEngineSnapshotData>();
        m_SynchronizedSimulationTimeSnapshotFromEntity = system.GetBufferFromEntity<SynchronizedSimulationTimeSnapshot>();
        m_GamePlayerSnapshotFromEntity = system.GetBufferFromEntity<GamePlayerSnapshot>();
        m_TeamEmptySnapshotDataFromEntity = system.GetBufferFromEntity<TeamEmptySnapshotData>();

    }
    public void Deserialize(int serializer, Entity entity, uint snapshot, uint baseline, uint baseline2, uint baseline3,
        DataStreamReader reader,
        ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
    {
        switch (serializer)
        {
        case 0:
            GhostReceiveSystem<GhostDeserializerCollection>.InvokeDeserialize(m_BasicGameModeSnapshotFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            break;
        case 1:
            GhostReceiveSystem<GhostDeserializerCollection>.InvokeDeserialize(m_RhythmEngineSnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            break;
        case 2:
            GhostReceiveSystem<GhostDeserializerCollection>.InvokeDeserialize(m_SynchronizedSimulationTimeSnapshotFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            break;
        case 3:
            GhostReceiveSystem<GhostDeserializerCollection>.InvokeDeserialize(m_GamePlayerSnapshotFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            break;
        case 4:
            GhostReceiveSystem<GhostDeserializerCollection>.InvokeDeserialize(m_TeamEmptySnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
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
                m_BasicGameModeSnapshotNewGhostIds.Add(ghostId);
                m_BasicGameModeSnapshotNewGhosts.Add(GhostReceiveSystem<GhostDeserializerCollection>.InvokeSpawn<BasicGameModeSnapshot>(snapshot, reader, ref ctx, compressionModel));
                break;
            case 1:
                m_RhythmEngineSnapshotDataNewGhostIds.Add(ghostId);
                m_RhythmEngineSnapshotDataNewGhosts.Add(GhostReceiveSystem<GhostDeserializerCollection>.InvokeSpawn<RhythmEngineSnapshotData>(snapshot, reader, ref ctx, compressionModel));
                break;
            case 2:
                m_SynchronizedSimulationTimeSnapshotNewGhostIds.Add(ghostId);
                m_SynchronizedSimulationTimeSnapshotNewGhosts.Add(GhostReceiveSystem<GhostDeserializerCollection>.InvokeSpawn<SynchronizedSimulationTimeSnapshot>(snapshot, reader, ref ctx, compressionModel));
                break;
            case 3:
                m_GamePlayerSnapshotNewGhostIds.Add(ghostId);
                m_GamePlayerSnapshotNewGhosts.Add(GhostReceiveSystem<GhostDeserializerCollection>.InvokeSpawn<GamePlayerSnapshot>(snapshot, reader, ref ctx, compressionModel));
                break;
            case 4:
                m_TeamEmptySnapshotDataNewGhostIds.Add(ghostId);
                m_TeamEmptySnapshotDataNewGhosts.Add(GhostReceiveSystem<GhostDeserializerCollection>.InvokeSpawn<TeamEmptySnapshotData>(snapshot, reader, ref ctx, compressionModel));
                break;

            default:
                throw new ArgumentException("Invalid serializer type");
        }
    }

    private BufferFromEntity<BasicGameModeSnapshot> m_BasicGameModeSnapshotFromEntity;
    private NativeList<int> m_BasicGameModeSnapshotNewGhostIds;
    private NativeList<BasicGameModeSnapshot> m_BasicGameModeSnapshotNewGhosts;
    private BufferFromEntity<RhythmEngineSnapshotData> m_RhythmEngineSnapshotDataFromEntity;
    private NativeList<int> m_RhythmEngineSnapshotDataNewGhostIds;
    private NativeList<RhythmEngineSnapshotData> m_RhythmEngineSnapshotDataNewGhosts;
    private BufferFromEntity<SynchronizedSimulationTimeSnapshot> m_SynchronizedSimulationTimeSnapshotFromEntity;
    private NativeList<int> m_SynchronizedSimulationTimeSnapshotNewGhostIds;
    private NativeList<SynchronizedSimulationTimeSnapshot> m_SynchronizedSimulationTimeSnapshotNewGhosts;
    private BufferFromEntity<GamePlayerSnapshot> m_GamePlayerSnapshotFromEntity;
    private NativeList<int> m_GamePlayerSnapshotNewGhostIds;
    private NativeList<GamePlayerSnapshot> m_GamePlayerSnapshotNewGhosts;
    private BufferFromEntity<TeamEmptySnapshotData> m_TeamEmptySnapshotDataFromEntity;
    private NativeList<int> m_TeamEmptySnapshotDataNewGhostIds;
    private NativeList<TeamEmptySnapshotData> m_TeamEmptySnapshotDataNewGhosts;

}
public class P4ExperimentGhostReceiveSystem : GhostReceiveSystem<GhostDeserializerCollection>
{
}
