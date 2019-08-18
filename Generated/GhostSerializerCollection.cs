using System;
using Patapon4TLB.Core;
using Patapon4TLB.Core.BasicUnitSnapshot;
using Patapon4TLB.Default;
using Patapon4TLB.Default.Attack;
using Patapon4TLB.Default.Snapshot;
using Patapon4TLB.GameModes;
using Patapon4TLB.GameModes.Snapshot;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
public struct GhostSerializerCollection : IGhostSerializerCollection
{
    public int FindSerializer(EntityArchetype arch)
    {
        if (m_HeadOnFlagSerializer.CanSerialize(arch))
            return 0;
        if (m_HeadOnStructureGhostSerializer.CanSerialize(arch))
            return 1;
        if (m_JumpAbilityGhostSerializer.CanSerialize(arch))
            return 2;
        if (m_MarchAbilityGhostSerializer.CanSerialize(arch))
            return 3;
        if (m_RetreatAbilityGhostSerializer.CanSerialize(arch))
            return 4;
        if (m_BasicTaterazayAttackAbilityGhostSerializer.CanSerialize(arch))
            return 5;
        if (m_DefaultRhythmEngineGhostSerializer.CanSerialize(arch))
            return 6;
        if (m_UnitTargetGhostSerializer.CanSerialize(arch))
            return 7;
        if (m_BasicUnitGhostSerializer.CanSerialize(arch))
            return 8;
        if (m_MpHeadOnGameModeSerializer.CanSerialize(arch))
            return 9;
        if (m_GamePlayerGhostSerializer.CanSerialize(arch))
            return 10;
        if (m_TeamEmptyGhostSerializer.CanSerialize(arch))
            return 11;
        if (m_ClubGhostSerializer.CanSerialize(arch))
            return 12;
        if (m_DefaultHealthGhostSerializer.CanSerialize(arch))
            return 13;

        throw new ArgumentException("Invalid serializer type");
    }

    public void BeginSerialize(ComponentSystemBase system)
    {
        m_HeadOnFlagSerializer.BeginSerialize(system);
        m_HeadOnStructureGhostSerializer.BeginSerialize(system);
        m_JumpAbilityGhostSerializer.BeginSerialize(system);
        m_MarchAbilityGhostSerializer.BeginSerialize(system);
        m_RetreatAbilityGhostSerializer.BeginSerialize(system);
        m_BasicTaterazayAttackAbilityGhostSerializer.BeginSerialize(system);
        m_DefaultRhythmEngineGhostSerializer.BeginSerialize(system);
        m_UnitTargetGhostSerializer.BeginSerialize(system);
        m_BasicUnitGhostSerializer.BeginSerialize(system);
        m_MpHeadOnGameModeSerializer.BeginSerialize(system);
        m_GamePlayerGhostSerializer.BeginSerialize(system);
        m_TeamEmptyGhostSerializer.BeginSerialize(system);
        m_ClubGhostSerializer.BeginSerialize(system);
        m_DefaultHealthGhostSerializer.BeginSerialize(system);

    }

    public int CalculateImportance(int serializer, ArchetypeChunk chunk)
    {
        switch (serializer)
        {
            case 0:
                return m_HeadOnFlagSerializer.CalculateImportance(chunk);
            case 1:
                return m_HeadOnStructureGhostSerializer.CalculateImportance(chunk);
            case 2:
                return m_JumpAbilityGhostSerializer.CalculateImportance(chunk);
            case 3:
                return m_MarchAbilityGhostSerializer.CalculateImportance(chunk);
            case 4:
                return m_RetreatAbilityGhostSerializer.CalculateImportance(chunk);
            case 5:
                return m_BasicTaterazayAttackAbilityGhostSerializer.CalculateImportance(chunk);
            case 6:
                return m_DefaultRhythmEngineGhostSerializer.CalculateImportance(chunk);
            case 7:
                return m_UnitTargetGhostSerializer.CalculateImportance(chunk);
            case 8:
                return m_BasicUnitGhostSerializer.CalculateImportance(chunk);
            case 9:
                return m_MpHeadOnGameModeSerializer.CalculateImportance(chunk);
            case 10:
                return m_GamePlayerGhostSerializer.CalculateImportance(chunk);
            case 11:
                return m_TeamEmptyGhostSerializer.CalculateImportance(chunk);
            case 12:
                return m_ClubGhostSerializer.CalculateImportance(chunk);
            case 13:
                return m_DefaultHealthGhostSerializer.CalculateImportance(chunk);

        }

        throw new ArgumentException("Invalid serializer type");
    }

    public bool WantsPredictionDelta(int serializer)
    {
        switch (serializer)
        {
            case 0:
                return m_HeadOnFlagSerializer.WantsPredictionDelta;
            case 1:
                return m_HeadOnStructureGhostSerializer.WantsPredictionDelta;
            case 2:
                return m_JumpAbilityGhostSerializer.WantsPredictionDelta;
            case 3:
                return m_MarchAbilityGhostSerializer.WantsPredictionDelta;
            case 4:
                return m_RetreatAbilityGhostSerializer.WantsPredictionDelta;
            case 5:
                return m_BasicTaterazayAttackAbilityGhostSerializer.WantsPredictionDelta;
            case 6:
                return m_DefaultRhythmEngineGhostSerializer.WantsPredictionDelta;
            case 7:
                return m_UnitTargetGhostSerializer.WantsPredictionDelta;
            case 8:
                return m_BasicUnitGhostSerializer.WantsPredictionDelta;
            case 9:
                return m_MpHeadOnGameModeSerializer.WantsPredictionDelta;
            case 10:
                return m_GamePlayerGhostSerializer.WantsPredictionDelta;
            case 11:
                return m_TeamEmptyGhostSerializer.WantsPredictionDelta;
            case 12:
                return m_ClubGhostSerializer.WantsPredictionDelta;
            case 13:
                return m_DefaultHealthGhostSerializer.WantsPredictionDelta;

        }

        throw new ArgumentException("Invalid serializer type");
    }

    public int GetSnapshotSize(int serializer)
    {
        switch (serializer)
        {
            case 0:
                return m_HeadOnFlagSerializer.SnapshotSize;
            case 1:
                return m_HeadOnStructureGhostSerializer.SnapshotSize;
            case 2:
                return m_JumpAbilityGhostSerializer.SnapshotSize;
            case 3:
                return m_MarchAbilityGhostSerializer.SnapshotSize;
            case 4:
                return m_RetreatAbilityGhostSerializer.SnapshotSize;
            case 5:
                return m_BasicTaterazayAttackAbilityGhostSerializer.SnapshotSize;
            case 6:
                return m_DefaultRhythmEngineGhostSerializer.SnapshotSize;
            case 7:
                return m_UnitTargetGhostSerializer.SnapshotSize;
            case 8:
                return m_BasicUnitGhostSerializer.SnapshotSize;
            case 9:
                return m_MpHeadOnGameModeSerializer.SnapshotSize;
            case 10:
                return m_GamePlayerGhostSerializer.SnapshotSize;
            case 11:
                return m_TeamEmptyGhostSerializer.SnapshotSize;
            case 12:
                return m_ClubGhostSerializer.SnapshotSize;
            case 13:
                return m_DefaultHealthGhostSerializer.SnapshotSize;

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
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_HeadOnFlagSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (HeadOnFlagSnapshotData*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 1:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_HeadOnStructureGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (HeadOnStructureSnapshot*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 2:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_JumpAbilityGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (JumpAbilitySnapshotData*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 3:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_MarchAbilityGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (MarchAbilitySnapshotData*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 4:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_RetreatAbilityGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (RetreatAbilitySnapshotData*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 5:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_BasicTaterazayAttackAbilityGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (BasicTaterazayAttackAbilitySnapshotData*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 6:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_DefaultRhythmEngineGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (RhythmEngineSnapshotData*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 7:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_UnitTargetGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (UnitTargetSnapshotData*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 8:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_BasicUnitGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (BasicUnitSnapshotData*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 9:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_MpHeadOnGameModeSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (MpHeadOnGhostSerializer.MpHeadOnGameModeSnapshot*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 10:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_GamePlayerGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (GamePlayerSnapshot*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 11:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_TeamEmptyGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (TeamEmptySnapshotData*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 12:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_ClubGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (ClubSnapshotData*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }
            case 13:
            {
                return GhostSendSystem<GhostSerializerCollection>.InvokeSerialize(m_DefaultHealthGhostSerializer, serializer,
                    chunk, startIndex, currentTick, currentSnapshotEntity, (DefaultHealthSnapshotData*)currentSnapshotData,
                    ghosts, ghostEntities, baselinePerEntity, availableBaselines,
                    dataStream, compressionModel);
            }

            default:
                throw new ArgumentException("Invalid serializer type");
        }
    }
    private HeadOnFlagSerializer m_HeadOnFlagSerializer;
    private HeadOnStructureGhostSerializer m_HeadOnStructureGhostSerializer;
    private JumpAbilityGhostSerializer m_JumpAbilityGhostSerializer;
    private MarchAbilityGhostSerializer m_MarchAbilityGhostSerializer;
    private RetreatAbilityGhostSerializer m_RetreatAbilityGhostSerializer;
    private BasicTaterazayAttackAbilityGhostSerializer m_BasicTaterazayAttackAbilityGhostSerializer;
    private DefaultRhythmEngineGhostSerializer m_DefaultRhythmEngineGhostSerializer;
    private UnitTargetGhostSerializer m_UnitTargetGhostSerializer;
    private BasicUnitGhostSerializer m_BasicUnitGhostSerializer;
    private MpHeadOnGhostSerializer.MpHeadOnGameModeSerializer m_MpHeadOnGameModeSerializer;
    private GamePlayerGhostSerializer m_GamePlayerGhostSerializer;
    private TeamEmptyGhostSerializer m_TeamEmptyGhostSerializer;
    private ClubGhostSerializer m_ClubGhostSerializer;
    private DefaultHealthGhostSerializer m_DefaultHealthGhostSerializer;

}

public class P4ExperimentGhostSendSystem : GhostSendSystem<GhostSerializerCollection>
{
}
