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
public struct GhostDeserializerCollection : IGhostDeserializerCollection
{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
    public string[] CreateSerializerNameList()
    {
        var arr = new string[]
        {
            "HeadOnFlagSerializer",
            "HeadOnStructureGhostSerializer",
            "JumpAbilityGhostSerializer",
            "MarchAbilityGhostSerializer",
            "RetreatAbilityGhostSerializer",
            "BasicTaterazayAttackAbilityGhostSerializer",
            "DefaultRhythmEngineGhostSerializer",
            "UnitTargetGhostSerializer",
            "BasicUnitGhostSerializer",
            "MpHeadOnGameModeSerializer",
            "GamePlayerGhostSerializer",
            "TeamEmptyGhostSerializer",
            "ClubGhostSerializer",
            "DefaultHealthGhostSerializer",

        };
        return arr;
    }

    public int Length => 14;
#endif
    public void Initialize(World world)
    {
        var curHeadOnFlagGhostSpawnSystem = world.GetOrCreateSystem<HeadOnFlagGhostSpawnSystem>();
        m_HeadOnFlagSnapshotDataNewGhostIds = curHeadOnFlagGhostSpawnSystem.NewGhostIds;
        m_HeadOnFlagSnapshotDataNewGhosts = curHeadOnFlagGhostSpawnSystem.NewGhosts;
        curHeadOnFlagGhostSpawnSystem.GhostType = 0;
        var curHeadOnStructureGhostSpawnSystem = world.GetOrCreateSystem<HeadOnStructureGhostSpawnSystem>();
        m_HeadOnStructureSnapshotNewGhostIds = curHeadOnStructureGhostSpawnSystem.NewGhostIds;
        m_HeadOnStructureSnapshotNewGhosts = curHeadOnStructureGhostSpawnSystem.NewGhosts;
        curHeadOnStructureGhostSpawnSystem.GhostType = 1;
        var curJumpAbilityGhostSpawnSystem = world.GetOrCreateSystem<JumpAbilityGhostSpawnSystem>();
        m_JumpAbilitySnapshotDataNewGhostIds = curJumpAbilityGhostSpawnSystem.NewGhostIds;
        m_JumpAbilitySnapshotDataNewGhosts = curJumpAbilityGhostSpawnSystem.NewGhosts;
        curJumpAbilityGhostSpawnSystem.GhostType = 2;
        var curMarchAbilityGhostSpawnSystem = world.GetOrCreateSystem<MarchAbilityGhostSpawnSystem>();
        m_MarchAbilitySnapshotDataNewGhostIds = curMarchAbilityGhostSpawnSystem.NewGhostIds;
        m_MarchAbilitySnapshotDataNewGhosts = curMarchAbilityGhostSpawnSystem.NewGhosts;
        curMarchAbilityGhostSpawnSystem.GhostType = 3;
        var curRetreatAbilityGhostSpawnSystem = world.GetOrCreateSystem<RetreatAbilityGhostSpawnSystem>();
        m_RetreatAbilitySnapshotDataNewGhostIds = curRetreatAbilityGhostSpawnSystem.NewGhostIds;
        m_RetreatAbilitySnapshotDataNewGhosts = curRetreatAbilityGhostSpawnSystem.NewGhosts;
        curRetreatAbilityGhostSpawnSystem.GhostType = 4;
        var curBasicTaterazayAttackAbilitySpawn = world.GetOrCreateSystem<BasicTaterazayAttackAbilitySpawn>();
        m_BasicTaterazayAttackAbilitySnapshotDataNewGhostIds = curBasicTaterazayAttackAbilitySpawn.NewGhostIds;
        m_BasicTaterazayAttackAbilitySnapshotDataNewGhosts = curBasicTaterazayAttackAbilitySpawn.NewGhosts;
        curBasicTaterazayAttackAbilitySpawn.GhostType = 5;
        var curDefaultRhythmEngineGhostSpawnSystem = world.GetOrCreateSystem<DefaultRhythmEngineGhostSpawnSystem>();
        m_RhythmEngineSnapshotDataNewGhostIds = curDefaultRhythmEngineGhostSpawnSystem.NewGhostIds;
        m_RhythmEngineSnapshotDataNewGhosts = curDefaultRhythmEngineGhostSpawnSystem.NewGhosts;
        curDefaultRhythmEngineGhostSpawnSystem.GhostType = 6;
        var curUnitTargetGhostSpawnSystem = world.GetOrCreateSystem<UnitTargetGhostSpawnSystem>();
        m_UnitTargetSnapshotDataNewGhostIds = curUnitTargetGhostSpawnSystem.NewGhostIds;
        m_UnitTargetSnapshotDataNewGhosts = curUnitTargetGhostSpawnSystem.NewGhosts;
        curUnitTargetGhostSpawnSystem.GhostType = 7;
        var curBasicUnitGhostSpawnSystem = world.GetOrCreateSystem<BasicUnitGhostSpawnSystem>();
        m_BasicUnitSnapshotDataNewGhostIds = curBasicUnitGhostSpawnSystem.NewGhostIds;
        m_BasicUnitSnapshotDataNewGhosts = curBasicUnitGhostSpawnSystem.NewGhosts;
        curBasicUnitGhostSpawnSystem.GhostType = 8;
        var curMpHeadOnGameModeSnapshotGhostSpawnSystem = world.GetOrCreateSystem<MpHeadOnGhostSerializer.MpHeadOnGameModeSnapshotGhostSpawnSystem>();
        m_MpHeadOnGameModeSnapshotNewGhostIds = curMpHeadOnGameModeSnapshotGhostSpawnSystem.NewGhostIds;
        m_MpHeadOnGameModeSnapshotNewGhosts = curMpHeadOnGameModeSnapshotGhostSpawnSystem.NewGhosts;
        curMpHeadOnGameModeSnapshotGhostSpawnSystem.GhostType = 9;
        var curGamePlayerGhostSpawnSystem = world.GetOrCreateSystem<GamePlayerGhostSpawnSystem>();
        m_GamePlayerSnapshotNewGhostIds = curGamePlayerGhostSpawnSystem.NewGhostIds;
        m_GamePlayerSnapshotNewGhosts = curGamePlayerGhostSpawnSystem.NewGhosts;
        curGamePlayerGhostSpawnSystem.GhostType = 10;
        var curTeamEmptyGhostSpawnSystem = world.GetOrCreateSystem<TeamEmptyGhostSpawnSystem>();
        m_TeamEmptySnapshotDataNewGhostIds = curTeamEmptyGhostSpawnSystem.NewGhostIds;
        m_TeamEmptySnapshotDataNewGhosts = curTeamEmptyGhostSpawnSystem.NewGhosts;
        curTeamEmptyGhostSpawnSystem.GhostType = 11;
        var curClubGhostSpawnSystem = world.GetOrCreateSystem<ClubGhostSpawnSystem>();
        m_ClubSnapshotDataNewGhostIds = curClubGhostSpawnSystem.NewGhostIds;
        m_ClubSnapshotDataNewGhosts = curClubGhostSpawnSystem.NewGhosts;
        curClubGhostSpawnSystem.GhostType = 12;
        var curDefaultHealthClientSpawnSystem = world.GetOrCreateSystem<DefaultHealthClientSpawnSystem>();
        m_DefaultHealthSnapshotDataNewGhostIds = curDefaultHealthClientSpawnSystem.NewGhostIds;
        m_DefaultHealthSnapshotDataNewGhosts = curDefaultHealthClientSpawnSystem.NewGhosts;
        curDefaultHealthClientSpawnSystem.GhostType = 13;

    }

    public void BeginDeserialize(JobComponentSystem system)
    {
        m_HeadOnFlagSnapshotDataFromEntity = system.GetBufferFromEntity<HeadOnFlagSnapshotData>();
        m_HeadOnStructureSnapshotFromEntity = system.GetBufferFromEntity<HeadOnStructureSnapshot>();
        m_JumpAbilitySnapshotDataFromEntity = system.GetBufferFromEntity<JumpAbilitySnapshotData>();
        m_MarchAbilitySnapshotDataFromEntity = system.GetBufferFromEntity<MarchAbilitySnapshotData>();
        m_RetreatAbilitySnapshotDataFromEntity = system.GetBufferFromEntity<RetreatAbilitySnapshotData>();
        m_BasicTaterazayAttackAbilitySnapshotDataFromEntity = system.GetBufferFromEntity<BasicTaterazayAttackAbilitySnapshotData>();
        m_RhythmEngineSnapshotDataFromEntity = system.GetBufferFromEntity<RhythmEngineSnapshotData>();
        m_UnitTargetSnapshotDataFromEntity = system.GetBufferFromEntity<UnitTargetSnapshotData>();
        m_BasicUnitSnapshotDataFromEntity = system.GetBufferFromEntity<BasicUnitSnapshotData>();
        m_MpHeadOnGameModeSnapshotFromEntity = system.GetBufferFromEntity<MpHeadOnGhostSerializer.MpHeadOnGameModeSnapshot>();
        m_GamePlayerSnapshotFromEntity = system.GetBufferFromEntity<GamePlayerSnapshot>();
        m_TeamEmptySnapshotDataFromEntity = system.GetBufferFromEntity<TeamEmptySnapshotData>();
        m_ClubSnapshotDataFromEntity = system.GetBufferFromEntity<ClubSnapshotData>();
        m_DefaultHealthSnapshotDataFromEntity = system.GetBufferFromEntity<DefaultHealthSnapshotData>();

    }
    public void Deserialize(int serializer, Entity entity, uint snapshot, uint baseline, uint baseline2, uint baseline3,
        DataStreamReader reader,
        ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
    {
        switch (serializer)
        {
        case 0:
            GhostReceiveSystem<GhostDeserializerCollection>.InvokeDeserialize(m_HeadOnFlagSnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            break;
        case 1:
            GhostReceiveSystem<GhostDeserializerCollection>.InvokeDeserialize(m_HeadOnStructureSnapshotFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            break;
        case 2:
            GhostReceiveSystem<GhostDeserializerCollection>.InvokeDeserialize(m_JumpAbilitySnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            break;
        case 3:
            GhostReceiveSystem<GhostDeserializerCollection>.InvokeDeserialize(m_MarchAbilitySnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            break;
        case 4:
            GhostReceiveSystem<GhostDeserializerCollection>.InvokeDeserialize(m_RetreatAbilitySnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            break;
        case 5:
            GhostReceiveSystem<GhostDeserializerCollection>.InvokeDeserialize(m_BasicTaterazayAttackAbilitySnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            break;
        case 6:
            GhostReceiveSystem<GhostDeserializerCollection>.InvokeDeserialize(m_RhythmEngineSnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            break;
        case 7:
            GhostReceiveSystem<GhostDeserializerCollection>.InvokeDeserialize(m_UnitTargetSnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            break;
        case 8:
            GhostReceiveSystem<GhostDeserializerCollection>.InvokeDeserialize(m_BasicUnitSnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            break;
        case 9:
            GhostReceiveSystem<GhostDeserializerCollection>.InvokeDeserialize(m_MpHeadOnGameModeSnapshotFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            break;
        case 10:
            GhostReceiveSystem<GhostDeserializerCollection>.InvokeDeserialize(m_GamePlayerSnapshotFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            break;
        case 11:
            GhostReceiveSystem<GhostDeserializerCollection>.InvokeDeserialize(m_TeamEmptySnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            break;
        case 12:
            GhostReceiveSystem<GhostDeserializerCollection>.InvokeDeserialize(m_ClubSnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            break;
        case 13:
            GhostReceiveSystem<GhostDeserializerCollection>.InvokeDeserialize(m_DefaultHealthSnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
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
                m_HeadOnFlagSnapshotDataNewGhostIds.Add(ghostId);
                m_HeadOnFlagSnapshotDataNewGhosts.Add(GhostReceiveSystem<GhostDeserializerCollection>.InvokeSpawn<HeadOnFlagSnapshotData>(snapshot, reader, ref ctx, compressionModel));
                break;
            case 1:
                m_HeadOnStructureSnapshotNewGhostIds.Add(ghostId);
                m_HeadOnStructureSnapshotNewGhosts.Add(GhostReceiveSystem<GhostDeserializerCollection>.InvokeSpawn<HeadOnStructureSnapshot>(snapshot, reader, ref ctx, compressionModel));
                break;
            case 2:
                m_JumpAbilitySnapshotDataNewGhostIds.Add(ghostId);
                m_JumpAbilitySnapshotDataNewGhosts.Add(GhostReceiveSystem<GhostDeserializerCollection>.InvokeSpawn<JumpAbilitySnapshotData>(snapshot, reader, ref ctx, compressionModel));
                break;
            case 3:
                m_MarchAbilitySnapshotDataNewGhostIds.Add(ghostId);
                m_MarchAbilitySnapshotDataNewGhosts.Add(GhostReceiveSystem<GhostDeserializerCollection>.InvokeSpawn<MarchAbilitySnapshotData>(snapshot, reader, ref ctx, compressionModel));
                break;
            case 4:
                m_RetreatAbilitySnapshotDataNewGhostIds.Add(ghostId);
                m_RetreatAbilitySnapshotDataNewGhosts.Add(GhostReceiveSystem<GhostDeserializerCollection>.InvokeSpawn<RetreatAbilitySnapshotData>(snapshot, reader, ref ctx, compressionModel));
                break;
            case 5:
                m_BasicTaterazayAttackAbilitySnapshotDataNewGhostIds.Add(ghostId);
                m_BasicTaterazayAttackAbilitySnapshotDataNewGhosts.Add(GhostReceiveSystem<GhostDeserializerCollection>.InvokeSpawn<BasicTaterazayAttackAbilitySnapshotData>(snapshot, reader, ref ctx, compressionModel));
                break;
            case 6:
                m_RhythmEngineSnapshotDataNewGhostIds.Add(ghostId);
                m_RhythmEngineSnapshotDataNewGhosts.Add(GhostReceiveSystem<GhostDeserializerCollection>.InvokeSpawn<RhythmEngineSnapshotData>(snapshot, reader, ref ctx, compressionModel));
                break;
            case 7:
                m_UnitTargetSnapshotDataNewGhostIds.Add(ghostId);
                m_UnitTargetSnapshotDataNewGhosts.Add(GhostReceiveSystem<GhostDeserializerCollection>.InvokeSpawn<UnitTargetSnapshotData>(snapshot, reader, ref ctx, compressionModel));
                break;
            case 8:
                m_BasicUnitSnapshotDataNewGhostIds.Add(ghostId);
                m_BasicUnitSnapshotDataNewGhosts.Add(GhostReceiveSystem<GhostDeserializerCollection>.InvokeSpawn<BasicUnitSnapshotData>(snapshot, reader, ref ctx, compressionModel));
                break;
            case 9:
                m_MpHeadOnGameModeSnapshotNewGhostIds.Add(ghostId);
                m_MpHeadOnGameModeSnapshotNewGhosts.Add(GhostReceiveSystem<GhostDeserializerCollection>.InvokeSpawn<MpHeadOnGhostSerializer.MpHeadOnGameModeSnapshot>(snapshot, reader, ref ctx, compressionModel));
                break;
            case 10:
                m_GamePlayerSnapshotNewGhostIds.Add(ghostId);
                m_GamePlayerSnapshotNewGhosts.Add(GhostReceiveSystem<GhostDeserializerCollection>.InvokeSpawn<GamePlayerSnapshot>(snapshot, reader, ref ctx, compressionModel));
                break;
            case 11:
                m_TeamEmptySnapshotDataNewGhostIds.Add(ghostId);
                m_TeamEmptySnapshotDataNewGhosts.Add(GhostReceiveSystem<GhostDeserializerCollection>.InvokeSpawn<TeamEmptySnapshotData>(snapshot, reader, ref ctx, compressionModel));
                break;
            case 12:
                m_ClubSnapshotDataNewGhostIds.Add(ghostId);
                m_ClubSnapshotDataNewGhosts.Add(GhostReceiveSystem<GhostDeserializerCollection>.InvokeSpawn<ClubSnapshotData>(snapshot, reader, ref ctx, compressionModel));
                break;
            case 13:
                m_DefaultHealthSnapshotDataNewGhostIds.Add(ghostId);
                m_DefaultHealthSnapshotDataNewGhosts.Add(GhostReceiveSystem<GhostDeserializerCollection>.InvokeSpawn<DefaultHealthSnapshotData>(snapshot, reader, ref ctx, compressionModel));
                break;

            default:
                throw new ArgumentException("Invalid serializer type");
        }
    }

    private BufferFromEntity<HeadOnFlagSnapshotData> m_HeadOnFlagSnapshotDataFromEntity;
    private NativeList<int> m_HeadOnFlagSnapshotDataNewGhostIds;
    private NativeList<HeadOnFlagSnapshotData> m_HeadOnFlagSnapshotDataNewGhosts;
    private BufferFromEntity<HeadOnStructureSnapshot> m_HeadOnStructureSnapshotFromEntity;
    private NativeList<int> m_HeadOnStructureSnapshotNewGhostIds;
    private NativeList<HeadOnStructureSnapshot> m_HeadOnStructureSnapshotNewGhosts;
    private BufferFromEntity<JumpAbilitySnapshotData> m_JumpAbilitySnapshotDataFromEntity;
    private NativeList<int> m_JumpAbilitySnapshotDataNewGhostIds;
    private NativeList<JumpAbilitySnapshotData> m_JumpAbilitySnapshotDataNewGhosts;
    private BufferFromEntity<MarchAbilitySnapshotData> m_MarchAbilitySnapshotDataFromEntity;
    private NativeList<int> m_MarchAbilitySnapshotDataNewGhostIds;
    private NativeList<MarchAbilitySnapshotData> m_MarchAbilitySnapshotDataNewGhosts;
    private BufferFromEntity<RetreatAbilitySnapshotData> m_RetreatAbilitySnapshotDataFromEntity;
    private NativeList<int> m_RetreatAbilitySnapshotDataNewGhostIds;
    private NativeList<RetreatAbilitySnapshotData> m_RetreatAbilitySnapshotDataNewGhosts;
    private BufferFromEntity<BasicTaterazayAttackAbilitySnapshotData> m_BasicTaterazayAttackAbilitySnapshotDataFromEntity;
    private NativeList<int> m_BasicTaterazayAttackAbilitySnapshotDataNewGhostIds;
    private NativeList<BasicTaterazayAttackAbilitySnapshotData> m_BasicTaterazayAttackAbilitySnapshotDataNewGhosts;
    private BufferFromEntity<RhythmEngineSnapshotData> m_RhythmEngineSnapshotDataFromEntity;
    private NativeList<int> m_RhythmEngineSnapshotDataNewGhostIds;
    private NativeList<RhythmEngineSnapshotData> m_RhythmEngineSnapshotDataNewGhosts;
    private BufferFromEntity<UnitTargetSnapshotData> m_UnitTargetSnapshotDataFromEntity;
    private NativeList<int> m_UnitTargetSnapshotDataNewGhostIds;
    private NativeList<UnitTargetSnapshotData> m_UnitTargetSnapshotDataNewGhosts;
    private BufferFromEntity<BasicUnitSnapshotData> m_BasicUnitSnapshotDataFromEntity;
    private NativeList<int> m_BasicUnitSnapshotDataNewGhostIds;
    private NativeList<BasicUnitSnapshotData> m_BasicUnitSnapshotDataNewGhosts;
    private BufferFromEntity<MpHeadOnGhostSerializer.MpHeadOnGameModeSnapshot> m_MpHeadOnGameModeSnapshotFromEntity;
    private NativeList<int> m_MpHeadOnGameModeSnapshotNewGhostIds;
    private NativeList<MpHeadOnGhostSerializer.MpHeadOnGameModeSnapshot> m_MpHeadOnGameModeSnapshotNewGhosts;
    private BufferFromEntity<GamePlayerSnapshot> m_GamePlayerSnapshotFromEntity;
    private NativeList<int> m_GamePlayerSnapshotNewGhostIds;
    private NativeList<GamePlayerSnapshot> m_GamePlayerSnapshotNewGhosts;
    private BufferFromEntity<TeamEmptySnapshotData> m_TeamEmptySnapshotDataFromEntity;
    private NativeList<int> m_TeamEmptySnapshotDataNewGhostIds;
    private NativeList<TeamEmptySnapshotData> m_TeamEmptySnapshotDataNewGhosts;
    private BufferFromEntity<ClubSnapshotData> m_ClubSnapshotDataFromEntity;
    private NativeList<int> m_ClubSnapshotDataNewGhostIds;
    private NativeList<ClubSnapshotData> m_ClubSnapshotDataNewGhosts;
    private BufferFromEntity<DefaultHealthSnapshotData> m_DefaultHealthSnapshotDataFromEntity;
    private NativeList<int> m_DefaultHealthSnapshotDataNewGhostIds;
    private NativeList<DefaultHealthSnapshotData> m_DefaultHealthSnapshotDataNewGhosts;

}
public class P4ExperimentGhostReceiveSystem : GhostReceiveSystem<GhostDeserializerCollection>
{
}
