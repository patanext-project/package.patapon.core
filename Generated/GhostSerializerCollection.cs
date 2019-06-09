using System;
using Patapon4TLB.Default.Snapshot;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;
using Unity.NetCode;
using UnityEngine;

public struct GhostSerializerCollection : IGhostSerializerCollection
{
    public int FindSerializer(EntityArchetype arch)
    {
        if (m_DefaultRhythmEngineGhostSerializer.CanSerialize(arch))
            return 0;
        if (m_SynchronizedSimulationTimeGhostSerializer.CanSerialize(arch))
            return 1;
        if (m_GamePlayerGhostSerializer.CanSerialize(arch))
            return 2;

        var types = arch.GetComponentTypes();
        for (var i = 0; i != types.Length; i++)
        {
            Debug.Log(types[i].GetManagedType().Name);
        }

        throw new ArgumentException("Invalid serializer type");
    }

    public void BeginSerialize(ComponentSystemBase system)
    {
        m_DefaultRhythmEngineGhostSerializer.BeginSerialize(system);
        m_SynchronizedSimulationTimeGhostSerializer.BeginSerialize(system);
        m_GamePlayerGhostSerializer.BeginSerialize(system);

    }

    public int CalculateImportance(int serializer, ArchetypeChunk chunk)
    {
        switch (serializer)
        {
            case 0:
                return m_DefaultRhythmEngineGhostSerializer.CalculateImportance(chunk);
            case 1:
                return m_SynchronizedSimulationTimeGhostSerializer.CalculateImportance(chunk);
            case 2:
                return m_GamePlayerGhostSerializer.CalculateImportance(chunk);

        }

        throw new ArgumentException("Invalid serializer type");
    }

    public bool WantsPredictionDelta(int serializer)
    {
        switch (serializer)
        {
            case 0:
                return m_DefaultRhythmEngineGhostSerializer.WantsPredictionDelta;
            case 1:
                return m_SynchronizedSimulationTimeGhostSerializer.WantsPredictionDelta;
            case 2:
                return m_GamePlayerGhostSerializer.WantsPredictionDelta;

        }

        throw new ArgumentException("Invalid serializer type");
    }

    public int GetSnapshotSize(int serializer)
    {
        switch (serializer)
        {
            case 0:
                return m_DefaultRhythmEngineGhostSerializer.SnapshotSize;
            case 1:
                return m_SynchronizedSimulationTimeGhostSerializer.SnapshotSize;
            case 2:
                return m_GamePlayerGhostSerializer.SnapshotSize;

        }

        throw new ArgumentException("Invalid serializer type");
    }

    public unsafe int Serialize(int serializer, ArchetypeChunk chunk, int startIndex, uint currentTick,
        Entity* currentSnapshotEntity, void* currentSnapshotData,
        GhostSystemStateComponent* ghosts, NativeArray<Entity> ghostEntities,
        NativeArray<int> baselinePerEntity, NativeList<SnapshotBaseline> availableBaselines,
        DataStreamWriter dataStream, NetworkCompressionModel compressionModel)
    {
        switch (serializer)
        {
            case 0:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_DefaultRhythmEngineGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (RhythmEngineSnapshotData*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 1:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_SynchronizedSimulationTimeGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (SynchronizedSimulationTimeSnapshot*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 2:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_GamePlayerGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (GamePlayerSnapshot*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }

            default:
                throw new ArgumentException("Invalid serializer type");
        }
    }
    private DefaultRhythmEngineGhostSerializer m_DefaultRhythmEngineGhostSerializer;
    private SynchronizedSimulationTimeGhostSerializer m_SynchronizedSimulationTimeGhostSerializer;
    private GamePlayerGhostSerializer m_GamePlayerGhostSerializer;

}

public class P4ExperimentGhostSendSystem : GhostSendSystem<GhostSerializerCollection>
{
}
