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
            "GamePlayerGhostSerializer",

        };
        return arr;
    }

    public int Length => 2;
#endif
    public void Initialize(World world)
    {
        var curDefaultRhythmEngineGhostSpawnSystem = world.GetOrCreateSystem<DefaultRhythmEngineGhostSpawnSystem>();
        m_DefaultRhythmEngineSnapshotDataNewGhostIds = curDefaultRhythmEngineGhostSpawnSystem.NewGhostIds;
        m_DefaultRhythmEngineSnapshotDataNewGhosts = curDefaultRhythmEngineGhostSpawnSystem.NewGhosts;
        curDefaultRhythmEngineGhostSpawnSystem.GhostType = 0;
        var curGamePlayerGhostSpawnSystem = world.GetOrCreateSystem<GamePlayerGhostSpawnSystem>();
        m_GamePlayerSnapshotNewGhostIds = curGamePlayerGhostSpawnSystem.NewGhostIds;
        m_GamePlayerSnapshotNewGhosts = curGamePlayerGhostSpawnSystem.NewGhosts;
        curGamePlayerGhostSpawnSystem.GhostType = 1;

    }

    public void BeginDeserialize(JobComponentSystem system)
    {
        m_DefaultRhythmEngineSnapshotDataFromEntity = system.GetBufferFromEntity<DefaultRhythmEngineSnapshotData>();
        m_GamePlayerSnapshotFromEntity = system.GetBufferFromEntity<GamePlayerSnapshot>();

    }
    public void Deserialize(int serializer, Entity entity, uint snapshot, uint baseline, uint baseline2, uint baseline3,
        DataStreamReader reader,
        ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
    {
        switch (serializer)
        {
        case 0:
            GhostReceiveSystem<GhostDeserializerCollection>.InvokeDeserialize(m_DefaultRhythmEngineSnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            break;
        case 1:
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
                m_DefaultRhythmEngineSnapshotDataNewGhostIds.Add(ghostId);
                m_DefaultRhythmEngineSnapshotDataNewGhosts.Add(GhostReceiveSystem<GhostDeserializerCollection>.InvokeSpawn<DefaultRhythmEngineSnapshotData>(snapshot, reader, ref ctx, compressionModel));
                break;
            case 1:
                m_GamePlayerSnapshotNewGhostIds.Add(ghostId);
                m_GamePlayerSnapshotNewGhosts.Add(GhostReceiveSystem<GhostDeserializerCollection>.InvokeSpawn<GamePlayerSnapshot>(snapshot, reader, ref ctx, compressionModel));
                break;

            default:
                throw new ArgumentException("Invalid serializer type");
        }
    }

    private BufferFromEntity<DefaultRhythmEngineSnapshotData> m_DefaultRhythmEngineSnapshotDataFromEntity;
    private NativeList<int> m_DefaultRhythmEngineSnapshotDataNewGhostIds;
    private NativeList<DefaultRhythmEngineSnapshotData> m_DefaultRhythmEngineSnapshotDataNewGhosts;
    private BufferFromEntity<GamePlayerSnapshot> m_GamePlayerSnapshotFromEntity;
    private NativeList<int> m_GamePlayerSnapshotNewGhostIds;
    private NativeList<GamePlayerSnapshot> m_GamePlayerSnapshotNewGhosts;

}
public class P4ExperimentGhostReceiveSystem : GhostReceiveSystem<GhostDeserializerCollection>
{
}
