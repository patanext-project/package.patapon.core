using System;
using Patapon4TLB.Core.BasicUnitSnapshot;
using Patapon4TLB.Default.Snapshot;
using Patapon4TLB.GameModes.Basic;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;
using Unity.NetCode;

public struct GhostSerializerCollection : IGhostSerializerCollection
{
    public int FindSerializer(EntityArchetype arch)
    {
        if (m_BasicGameModeSerializer.CanSerialize(arch))
            return 0;
        if (m_BasicUnitGhostSerializer.CanSerialize(arch))
            return 1;
        if (m_DefaultRhythmEngineGhostSerializer.CanSerialize(arch))
            return 2;
        if (m_SynchronizedSimulationTimeGhostSerializer.CanSerialize(arch))
            return 3;
        if (m_GamePlayerGhostSerializer.CanSerialize(arch))
            return 4;
        if (m_TeamEmptyGhostSerializer.CanSerialize(arch))
            return 5;

        throw new ArgumentException("Invalid serializer type");
    }

    public void BeginSerialize(ComponentSystemBase system)
    {
        m_BasicGameModeSerializer.BeginSerialize(system);
        m_BasicUnitGhostSerializer.BeginSerialize(system);
        m_DefaultRhythmEngineGhostSerializer.BeginSerialize(system);
        m_SynchronizedSimulationTimeGhostSerializer.BeginSerialize(system);
        m_GamePlayerGhostSerializer.BeginSerialize(system);
        m_TeamEmptyGhostSerializer.BeginSerialize(system);

    }

    public int CalculateImportance(int serializer, ArchetypeChunk chunk)
    {
        switch (serializer)
        {
            case 0:
                return m_BasicGameModeSerializer.CalculateImportance(chunk);
            case 1:
                return m_BasicUnitGhostSerializer.CalculateImportance(chunk);
            case 2:
                return m_DefaultRhythmEngineGhostSerializer.CalculateImportance(chunk);
            case 3:
                return m_SynchronizedSimulationTimeGhostSerializer.CalculateImportance(chunk);
            case 4:
                return m_GamePlayerGhostSerializer.CalculateImportance(chunk);
            case 5:
                return m_TeamEmptyGhostSerializer.CalculateImportance(chunk);

        }

        throw new ArgumentException("Invalid serializer type");
    }

    public bool WantsPredictionDelta(int serializer)
    {
        switch (serializer)
        {
            case 0:
                return m_BasicGameModeSerializer.WantsPredictionDelta;
            case 1:
                return m_BasicUnitGhostSerializer.WantsPredictionDelta;
            case 2:
                return m_DefaultRhythmEngineGhostSerializer.WantsPredictionDelta;
            case 3:
                return m_SynchronizedSimulationTimeGhostSerializer.WantsPredictionDelta;
            case 4:
                return m_GamePlayerGhostSerializer.WantsPredictionDelta;
            case 5:
                return m_TeamEmptyGhostSerializer.WantsPredictionDelta;

        }

        throw new ArgumentException("Invalid serializer type");
    }

    public int GetSnapshotSize(int serializer)
    {
        switch (serializer)
        {
            case 0:
                return m_BasicGameModeSerializer.SnapshotSize;
            case 1:
                return m_BasicUnitGhostSerializer.SnapshotSize;
            case 2:
                return m_DefaultRhythmEngineGhostSerializer.SnapshotSize;
            case 3:
                return m_SynchronizedSimulationTimeGhostSerializer.SnapshotSize;
            case 4:
                return m_GamePlayerGhostSerializer.SnapshotSize;
            case 5:
                return m_TeamEmptyGhostSerializer.SnapshotSize;

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
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_BasicGameModeSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (BasicGameModeSnapshot*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 1:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_BasicUnitGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (BasicUnitSnapshotData*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 2:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_DefaultRhythmEngineGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (RhythmEngineSnapshotData*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 3:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_SynchronizedSimulationTimeGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (SynchronizedSimulationTimeSnapshot*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 4:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_GamePlayerGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (GamePlayerSnapshot*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 5:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_TeamEmptyGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (TeamEmptySnapshotData*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }

            default:
                throw new ArgumentException("Invalid serializer type");
        }
    }
    private BasicGameModeSerializer m_BasicGameModeSerializer;
    private BasicUnitGhostSerializer m_BasicUnitGhostSerializer;
    private DefaultRhythmEngineGhostSerializer m_DefaultRhythmEngineGhostSerializer;
    private SynchronizedSimulationTimeGhostSerializer m_SynchronizedSimulationTimeGhostSerializer;
    private GamePlayerGhostSerializer m_GamePlayerGhostSerializer;
    private TeamEmptyGhostSerializer m_TeamEmptyGhostSerializer;

}

public class P4ExperimentGhostSendSystem : GhostSendSystem<GhostSerializerCollection>
{
}
