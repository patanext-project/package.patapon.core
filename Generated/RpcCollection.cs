using System;
using Patapon4TLB.Core;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Data;
using Unity.Entities;
using Unity.Networking.Transport;
using Unity.NetCode;

public struct RpcCollection : IRpcCollection
{
    static Type[] s_RpcTypes = new Type[]
    {
        typeof(RhythmRpcClientRecover),
        typeof(RhythmRpcNewClientCommand),
        typeof(RhythmRpcPressureFromClient),
        typeof(RhythmRpcPressureFromServer),
        typeof(RhythmRpcServerSendCommandChain),
        typeof(SynchronizeRelativeSystem<RhythmEngineDescription>.SendAllRpc),
        typeof(SynchronizeRelativeSystem<RhythmEngineDescription>.SendUpdateRpc),
        typeof(SynchronizeRelativeSystem<RhythmEngineDescription>.SendDeltaRpc),
        typeof(SynchronizeRelativeSystem<UnitTargetDescription>.SendAllRpc),
        typeof(SynchronizeRelativeSystem<UnitTargetDescription>.SendUpdateRpc),
        typeof(SynchronizeRelativeSystem<UnitTargetDescription>.SendDeltaRpc),
        typeof(SynchronizeRelativeSystem<UnitDescription>.SendAllRpc),
        typeof(SynchronizeRelativeSystem<UnitDescription>.SendUpdateRpc),
        typeof(SynchronizeRelativeSystem<UnitDescription>.SendDeltaRpc),
        typeof(SynchronizeRelativeSystem<HitShapeDescription>.SendAllRpc),
        typeof(SynchronizeRelativeSystem<HitShapeDescription>.SendUpdateRpc),
        typeof(SynchronizeRelativeSystem<HitShapeDescription>.SendDeltaRpc),
        typeof(SynchronizeRelativeSystem<MovableDescription>.SendAllRpc),
        typeof(SynchronizeRelativeSystem<MovableDescription>.SendUpdateRpc),
        typeof(SynchronizeRelativeSystem<MovableDescription>.SendDeltaRpc),
        typeof(SynchronizeRelativeSystem<LivableDescription>.SendAllRpc),
        typeof(SynchronizeRelativeSystem<LivableDescription>.SendUpdateRpc),
        typeof(SynchronizeRelativeSystem<LivableDescription>.SendDeltaRpc),
        typeof(SynchronizeRelativeSystem<CharacterDescription>.SendAllRpc),
        typeof(SynchronizeRelativeSystem<CharacterDescription>.SendUpdateRpc),
        typeof(SynchronizeRelativeSystem<CharacterDescription>.SendDeltaRpc),
        typeof(SynchronizeRelativeSystem<PlayerDescription>.SendAllRpc),
        typeof(SynchronizeRelativeSystem<PlayerDescription>.SendUpdateRpc),
        typeof(SynchronizeRelativeSystem<PlayerDescription>.SendDeltaRpc),
        typeof(SynchronizeRelativeSystem<ActionDescription>.SendAllRpc),
        typeof(SynchronizeRelativeSystem<ActionDescription>.SendUpdateRpc),
        typeof(SynchronizeRelativeSystem<ActionDescription>.SendDeltaRpc),
        typeof(SynchronizeRelativeSystem<ProjectileDescription>.SendAllRpc),
        typeof(SynchronizeRelativeSystem<ProjectileDescription>.SendUpdateRpc),
        typeof(SynchronizeRelativeSystem<ProjectileDescription>.SendDeltaRpc),
        typeof(SynchronizeRelativeSystem<TeamDescription>.SendAllRpc),
        typeof(SynchronizeRelativeSystem<TeamDescription>.SendUpdateRpc),
        typeof(SynchronizeRelativeSystem<TeamDescription>.SendDeltaRpc),
        typeof(SynchronizeRelativeSystem<ClubDescription>.SendAllRpc),
        typeof(SynchronizeRelativeSystem<ClubDescription>.SendUpdateRpc),
        typeof(SynchronizeRelativeSystem<ClubDescription>.SendDeltaRpc),
        typeof(ClientLoadedRpc),
        typeof(PlayerConnectedRpc),
        typeof(TargetDamageEventRpc),
        typeof(UpdateServerMapRpc),

    };
    public void ExecuteRpc(int type, DataStreamReader reader, ref DataStreamReader.Context ctx, Entity connection, EntityCommandBuffer.Concurrent commandBuffer, int jobIndex)
    {
        switch (type)
        {
            case 0:
            {
                var tmp = new RhythmRpcClientRecover();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 1:
            {
                var tmp = new RhythmRpcNewClientCommand();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 2:
            {
                var tmp = new RhythmRpcPressureFromClient();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 3:
            {
                var tmp = new RhythmRpcPressureFromServer();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 4:
            {
                var tmp = new RhythmRpcServerSendCommandChain();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 5:
            {
                var tmp = new SynchronizeRelativeSystem<RhythmEngineDescription>.SendAllRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 6:
            {
                var tmp = new SynchronizeRelativeSystem<RhythmEngineDescription>.SendUpdateRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 7:
            {
                var tmp = new SynchronizeRelativeSystem<RhythmEngineDescription>.SendDeltaRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 8:
            {
                var tmp = new SynchronizeRelativeSystem<UnitTargetDescription>.SendAllRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 9:
            {
                var tmp = new SynchronizeRelativeSystem<UnitTargetDescription>.SendUpdateRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 10:
            {
                var tmp = new SynchronizeRelativeSystem<UnitTargetDescription>.SendDeltaRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 11:
            {
                var tmp = new SynchronizeRelativeSystem<UnitDescription>.SendAllRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 12:
            {
                var tmp = new SynchronizeRelativeSystem<UnitDescription>.SendUpdateRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 13:
            {
                var tmp = new SynchronizeRelativeSystem<UnitDescription>.SendDeltaRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 14:
            {
                var tmp = new SynchronizeRelativeSystem<HitShapeDescription>.SendAllRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 15:
            {
                var tmp = new SynchronizeRelativeSystem<HitShapeDescription>.SendUpdateRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 16:
            {
                var tmp = new SynchronizeRelativeSystem<HitShapeDescription>.SendDeltaRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 17:
            {
                var tmp = new SynchronizeRelativeSystem<MovableDescription>.SendAllRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 18:
            {
                var tmp = new SynchronizeRelativeSystem<MovableDescription>.SendUpdateRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 19:
            {
                var tmp = new SynchronizeRelativeSystem<MovableDescription>.SendDeltaRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 20:
            {
                var tmp = new SynchronizeRelativeSystem<LivableDescription>.SendAllRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 21:
            {
                var tmp = new SynchronizeRelativeSystem<LivableDescription>.SendUpdateRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 22:
            {
                var tmp = new SynchronizeRelativeSystem<LivableDescription>.SendDeltaRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 23:
            {
                var tmp = new SynchronizeRelativeSystem<CharacterDescription>.SendAllRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 24:
            {
                var tmp = new SynchronizeRelativeSystem<CharacterDescription>.SendUpdateRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 25:
            {
                var tmp = new SynchronizeRelativeSystem<CharacterDescription>.SendDeltaRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 26:
            {
                var tmp = new SynchronizeRelativeSystem<PlayerDescription>.SendAllRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 27:
            {
                var tmp = new SynchronizeRelativeSystem<PlayerDescription>.SendUpdateRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 28:
            {
                var tmp = new SynchronizeRelativeSystem<PlayerDescription>.SendDeltaRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 29:
            {
                var tmp = new SynchronizeRelativeSystem<ActionDescription>.SendAllRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 30:
            {
                var tmp = new SynchronizeRelativeSystem<ActionDescription>.SendUpdateRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 31:
            {
                var tmp = new SynchronizeRelativeSystem<ActionDescription>.SendDeltaRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 32:
            {
                var tmp = new SynchronizeRelativeSystem<ProjectileDescription>.SendAllRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 33:
            {
                var tmp = new SynchronizeRelativeSystem<ProjectileDescription>.SendUpdateRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 34:
            {
                var tmp = new SynchronizeRelativeSystem<ProjectileDescription>.SendDeltaRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 35:
            {
                var tmp = new SynchronizeRelativeSystem<TeamDescription>.SendAllRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 36:
            {
                var tmp = new SynchronizeRelativeSystem<TeamDescription>.SendUpdateRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 37:
            {
                var tmp = new SynchronizeRelativeSystem<TeamDescription>.SendDeltaRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 38:
            {
                var tmp = new SynchronizeRelativeSystem<ClubDescription>.SendAllRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 39:
            {
                var tmp = new SynchronizeRelativeSystem<ClubDescription>.SendUpdateRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 40:
            {
                var tmp = new SynchronizeRelativeSystem<ClubDescription>.SendDeltaRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 41:
            {
                var tmp = new ClientLoadedRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 42:
            {
                var tmp = new PlayerConnectedRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 43:
            {
                var tmp = new TargetDamageEventRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 44:
            {
                var tmp = new UpdateServerMapRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }

        }
    }

    public int GetRpcFromType<T>() where T : struct, IRpcCommand
    {
        for (int i = 0; i < s_RpcTypes.Length; ++i)
        {
            if (s_RpcTypes[i] == typeof(T))
                return i;
        }

        return -1;
    }
}

public class P4ExperimentRpcSystem : RpcSystem<RpcCollection>
{
    protected override void OnCreate()
    {
        base.OnCreate();

        World.GetOrCreateSystem<RpcQueueSystem<RhythmRpcClientRecover>>().SetTypeIndex(0);
        World.GetOrCreateSystem<RpcQueueSystem<RhythmRpcNewClientCommand>>().SetTypeIndex(1);
        World.GetOrCreateSystem<RpcQueueSystem<RhythmRpcPressureFromClient>>().SetTypeIndex(2);
        World.GetOrCreateSystem<RpcQueueSystem<RhythmRpcPressureFromServer>>().SetTypeIndex(3);
        World.GetOrCreateSystem<RpcQueueSystem<RhythmRpcServerSendCommandChain>>().SetTypeIndex(4);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<RhythmEngineDescription>.SendAllRpc>>().SetTypeIndex(5);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<RhythmEngineDescription>.SendUpdateRpc>>().SetTypeIndex(6);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<RhythmEngineDescription>.SendDeltaRpc>>().SetTypeIndex(7);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<UnitTargetDescription>.SendAllRpc>>().SetTypeIndex(8);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<UnitTargetDescription>.SendUpdateRpc>>().SetTypeIndex(9);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<UnitTargetDescription>.SendDeltaRpc>>().SetTypeIndex(10);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<UnitDescription>.SendAllRpc>>().SetTypeIndex(11);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<UnitDescription>.SendUpdateRpc>>().SetTypeIndex(12);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<UnitDescription>.SendDeltaRpc>>().SetTypeIndex(13);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<HitShapeDescription>.SendAllRpc>>().SetTypeIndex(14);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<HitShapeDescription>.SendUpdateRpc>>().SetTypeIndex(15);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<HitShapeDescription>.SendDeltaRpc>>().SetTypeIndex(16);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<MovableDescription>.SendAllRpc>>().SetTypeIndex(17);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<MovableDescription>.SendUpdateRpc>>().SetTypeIndex(18);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<MovableDescription>.SendDeltaRpc>>().SetTypeIndex(19);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<LivableDescription>.SendAllRpc>>().SetTypeIndex(20);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<LivableDescription>.SendUpdateRpc>>().SetTypeIndex(21);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<LivableDescription>.SendDeltaRpc>>().SetTypeIndex(22);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<CharacterDescription>.SendAllRpc>>().SetTypeIndex(23);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<CharacterDescription>.SendUpdateRpc>>().SetTypeIndex(24);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<CharacterDescription>.SendDeltaRpc>>().SetTypeIndex(25);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<PlayerDescription>.SendAllRpc>>().SetTypeIndex(26);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<PlayerDescription>.SendUpdateRpc>>().SetTypeIndex(27);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<PlayerDescription>.SendDeltaRpc>>().SetTypeIndex(28);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<ActionDescription>.SendAllRpc>>().SetTypeIndex(29);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<ActionDescription>.SendUpdateRpc>>().SetTypeIndex(30);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<ActionDescription>.SendDeltaRpc>>().SetTypeIndex(31);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<ProjectileDescription>.SendAllRpc>>().SetTypeIndex(32);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<ProjectileDescription>.SendUpdateRpc>>().SetTypeIndex(33);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<ProjectileDescription>.SendDeltaRpc>>().SetTypeIndex(34);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<TeamDescription>.SendAllRpc>>().SetTypeIndex(35);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<TeamDescription>.SendUpdateRpc>>().SetTypeIndex(36);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<TeamDescription>.SendDeltaRpc>>().SetTypeIndex(37);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<ClubDescription>.SendAllRpc>>().SetTypeIndex(38);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<ClubDescription>.SendUpdateRpc>>().SetTypeIndex(39);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<ClubDescription>.SendDeltaRpc>>().SetTypeIndex(40);
        World.GetOrCreateSystem<RpcQueueSystem<ClientLoadedRpc>>().SetTypeIndex(41);
        World.GetOrCreateSystem<RpcQueueSystem<PlayerConnectedRpc>>().SetTypeIndex(42);
        World.GetOrCreateSystem<RpcQueueSystem<TargetDamageEventRpc>>().SetTypeIndex(43);
        World.GetOrCreateSystem<RpcQueueSystem<UpdateServerMapRpc>>().SetTypeIndex(44);

    }
}
