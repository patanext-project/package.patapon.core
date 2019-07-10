using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;
using Unity.NetCode;
using StormiumTeam.GameBase;
using Patapon4TLB.Core.BasicUnitSnapshot;
using Patapon4TLB.Default;
using Patapon4TLB.Default.Snapshot;
using Patapon4TLB.GameModes.Basic;

public struct GhostSerializerCollection : IGhostSerializerCollection
{
    public int FindSerializer(EntityArchetype arch)
    {
        if (m_BasicGameModeSerializer.CanSerialize(arch))
            return 0;
        if (m_JumpAbilityGhostSerializer.CanSerialize(arch))
            return 1;
        if (m_MarchAbilityGhostSerializer.CanSerialize(arch))
            return 2;
        if (m_RetreatAbilityGhostSerializer.CanSerialize(arch))
            return 3;
        if (m_DefaultRhythmEngineGhostSerializer.CanSerialize(arch))
            return 4;
        if (m_BasicUnitGhostSerializer.CanSerialize(arch))
            return 5;
        if (m_SynchronizedSimulationTimeGhostSerializer.CanSerialize(arch))
            return 6;
        if (m_GamePlayerGhostSerializer.CanSerialize(arch))
            return 7;
        if (m_TeamEmptyGhostSerializer.CanSerialize(arch))
            return 8;

        throw new ArgumentException("Invalid serializer type");
    }

    public void BeginSerialize(ComponentSystemBase system)
    {
        m_BasicGameModeSerializer.BeginSerialize(system);
        m_JumpAbilityGhostSerializer.BeginSerialize(system);
        m_MarchAbilityGhostSerializer.BeginSerialize(system);
        m_RetreatAbilityGhostSerializer.BeginSerialize(system);
        m_DefaultRhythmEngineGhostSerializer.BeginSerialize(system);
        m_BasicUnitGhostSerializer.BeginSerialize(system);
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
                return m_JumpAbilityGhostSerializer.CalculateImportance(chunk);
            case 2:
                return m_MarchAbilityGhostSerializer.CalculateImportance(chunk);
            case 3:
                return m_RetreatAbilityGhostSerializer.CalculateImportance(chunk);
            case 4:
                return m_DefaultRhythmEngineGhostSerializer.CalculateImportance(chunk);
            case 5:
                return m_BasicUnitGhostSerializer.CalculateImportance(chunk);
            case 6:
                return m_SynchronizedSimulationTimeGhostSerializer.CalculateImportance(chunk);
            case 7:
                return m_GamePlayerGhostSerializer.CalculateImportance(chunk);
            case 8:
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
                return m_JumpAbilityGhostSerializer.WantsPredictionDelta;
            case 2:
                return m_MarchAbilityGhostSerializer.WantsPredictionDelta;
            case 3:
                return m_RetreatAbilityGhostSerializer.WantsPredictionDelta;
            case 4:
                return m_DefaultRhythmEngineGhostSerializer.WantsPredictionDelta;
            case 5:
                return m_BasicUnitGhostSerializer.WantsPredictionDelta;
            case 6:
                return m_SynchronizedSimulationTimeGhostSerializer.WantsPredictionDelta;
            case 7:
                return m_GamePlayerGhostSerializer.WantsPredictionDelta;
            case 8:
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
                return m_JumpAbilityGhostSerializer.SnapshotSize;
            case 2:
                return m_MarchAbilityGhostSerializer.SnapshotSize;
            case 3:
                return m_RetreatAbilityGhostSerializer.SnapshotSize;
            case 4:
                return m_DefaultRhythmEngineGhostSerializer.SnapshotSize;
            case 5:
                return m_BasicUnitGhostSerializer.SnapshotSize;
            case 6:
                return m_SynchronizedSimulationTimeGhostSerializer.SnapshotSize;
            case 7:
                return m_GamePlayerGhostSerializer.SnapshotSize;
            case 8:
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
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_JumpAbilityGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (JumpAbilitySnapshotData*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 2:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_MarchAbilityGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (MarchAbilitySnapshotData*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 3:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_RetreatAbilityGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (RetreatAbilitySnapshotData*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 4:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_DefaultRhythmEngineGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (RhythmEngineSnapshotData*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 5:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_BasicUnitGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (BasicUnitSnapshotData*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 6:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_SynchronizedSimulationTimeGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (SynchronizedSimulationTimeSnapshot*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 7:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_GamePlayerGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (GamePlayerSnapshot*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 8:
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
    private JumpAbilityGhostSerializer m_JumpAbilityGhostSerializer;
    private MarchAbilityGhostSerializer m_MarchAbilityGhostSerializer;
    private RetreatAbilityGhostSerializer m_RetreatAbilityGhostSerializer;
    private DefaultRhythmEngineGhostSerializer m_DefaultRhythmEngineGhostSerializer;
    private BasicUnitGhostSerializer m_BasicUnitGhostSerializer;
    private SynchronizedSimulationTimeGhostSerializer m_SynchronizedSimulationTimeGhostSerializer;
    private GamePlayerGhostSerializer m_GamePlayerGhostSerializer;
    private TeamEmptyGhostSerializer m_TeamEmptyGhostSerializer;

}

public class P4ExperimentGhostSendSystem : GhostSendSystem<GhostSerializerCollection>
{
}
