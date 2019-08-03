using System;
using P4.Core.Code.Networking;
using Patapon4TLB.Core;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
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
        typeof(SetPlayerNameRpc),
        typeof(SynchronizeRelativeSystem<RhythmEngineDescription>.SendAllRpc),
        typeof(SynchronizeRelativeSystem<RhythmEngineDescription>.SendDeltaRpc),
        typeof(SynchronizeRelativeSystem<UnitDescription>.SendAllRpc),
        typeof(SynchronizeRelativeSystem<UnitDescription>.SendDeltaRpc),
        typeof(ClientLoadedRpc),
        typeof(PlayerConnectedRpc),
        typeof(TargetDamageEventRpc),
        typeof(SynchronizeRelativeSystem<ColliderDescription>.SendAllRpc),
        typeof(SynchronizeRelativeSystem<ColliderDescription>.SendDeltaRpc),
        typeof(SynchronizeRelativeSystem<MovableDescription>.SendAllRpc),
        typeof(SynchronizeRelativeSystem<MovableDescription>.SendDeltaRpc),
        typeof(SynchronizeRelativeSystem<LivableDescription>.SendAllRpc),
        typeof(SynchronizeRelativeSystem<LivableDescription>.SendDeltaRpc),
        typeof(SynchronizeRelativeSystem<CharacterDescription>.SendAllRpc),
        typeof(SynchronizeRelativeSystem<CharacterDescription>.SendDeltaRpc),
        typeof(SynchronizeRelativeSystem<PlayerDescription>.SendAllRpc),
        typeof(SynchronizeRelativeSystem<PlayerDescription>.SendDeltaRpc),
        typeof(SynchronizeRelativeSystem<ActionDescription>.SendAllRpc),
        typeof(SynchronizeRelativeSystem<ActionDescription>.SendDeltaRpc),
        typeof(SynchronizeRelativeSystem<ProjectileDescription>.SendAllRpc),
        typeof(SynchronizeRelativeSystem<ProjectileDescription>.SendDeltaRpc),
        typeof(SynchronizeRelativeSystem<TeamDescription>.SendAllRpc),
        typeof(SynchronizeRelativeSystem<TeamDescription>.SendDeltaRpc),

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
                var tmp = new SetPlayerNameRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 6:
            {
                var tmp = new SynchronizeRelativeSystem<RhythmEngineDescription>.SendAllRpc();
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
                var tmp = new SynchronizeRelativeSystem<UnitDescription>.SendAllRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 9:
            {
                var tmp = new SynchronizeRelativeSystem<UnitDescription>.SendDeltaRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 10:
            {
                var tmp = new ClientLoadedRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 11:
            {
                var tmp = new PlayerConnectedRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 12:
            {
                var tmp = new TargetDamageEventRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 13:
            {
                var tmp = new SynchronizeRelativeSystem<ColliderDescription>.SendAllRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 14:
            {
                var tmp = new SynchronizeRelativeSystem<ColliderDescription>.SendDeltaRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 15:
            {
                var tmp = new SynchronizeRelativeSystem<MovableDescription>.SendAllRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 16:
            {
                var tmp = new SynchronizeRelativeSystem<MovableDescription>.SendDeltaRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 17:
            {
                var tmp = new SynchronizeRelativeSystem<LivableDescription>.SendAllRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 18:
            {
                var tmp = new SynchronizeRelativeSystem<LivableDescription>.SendDeltaRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 19:
            {
                var tmp = new SynchronizeRelativeSystem<CharacterDescription>.SendAllRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 20:
            {
                var tmp = new SynchronizeRelativeSystem<CharacterDescription>.SendDeltaRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 21:
            {
                var tmp = new SynchronizeRelativeSystem<PlayerDescription>.SendAllRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 22:
            {
                var tmp = new SynchronizeRelativeSystem<PlayerDescription>.SendDeltaRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 23:
            {
                var tmp = new SynchronizeRelativeSystem<ActionDescription>.SendAllRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 24:
            {
                var tmp = new SynchronizeRelativeSystem<ActionDescription>.SendDeltaRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 25:
            {
                var tmp = new SynchronizeRelativeSystem<ProjectileDescription>.SendAllRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 26:
            {
                var tmp = new SynchronizeRelativeSystem<ProjectileDescription>.SendDeltaRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 27:
            {
                var tmp = new SynchronizeRelativeSystem<TeamDescription>.SendAllRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 28:
            {
                var tmp = new SynchronizeRelativeSystem<TeamDescription>.SendDeltaRpc();
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
    protected override void OnCreateManager()
    {
        base.OnCreateManager();

        World.GetOrCreateSystem<RpcQueueSystem<RhythmRpcClientRecover>>().SetTypeIndex(0);
        World.GetOrCreateSystem<RpcQueueSystem<RhythmRpcNewClientCommand>>().SetTypeIndex(1);
        World.GetOrCreateSystem<RpcQueueSystem<RhythmRpcPressureFromClient>>().SetTypeIndex(2);
        World.GetOrCreateSystem<RpcQueueSystem<RhythmRpcPressureFromServer>>().SetTypeIndex(3);
        World.GetOrCreateSystem<RpcQueueSystem<RhythmRpcServerSendCommandChain>>().SetTypeIndex(4);
        World.GetOrCreateSystem<RpcQueueSystem<SetPlayerNameRpc>>().SetTypeIndex(5);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<RhythmEngineDescription>.SendAllRpc>>().SetTypeIndex(6);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<RhythmEngineDescription>.SendDeltaRpc>>().SetTypeIndex(7);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<UnitDescription>.SendAllRpc>>().SetTypeIndex(8);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<UnitDescription>.SendDeltaRpc>>().SetTypeIndex(9);
        World.GetOrCreateSystem<RpcQueueSystem<ClientLoadedRpc>>().SetTypeIndex(10);
        World.GetOrCreateSystem<RpcQueueSystem<PlayerConnectedRpc>>().SetTypeIndex(11);
        World.GetOrCreateSystem<RpcQueueSystem<TargetDamageEventRpc>>().SetTypeIndex(12);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<ColliderDescription>.SendAllRpc>>().SetTypeIndex(13);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<ColliderDescription>.SendDeltaRpc>>().SetTypeIndex(14);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<MovableDescription>.SendAllRpc>>().SetTypeIndex(15);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<MovableDescription>.SendDeltaRpc>>().SetTypeIndex(16);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<LivableDescription>.SendAllRpc>>().SetTypeIndex(17);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<LivableDescription>.SendDeltaRpc>>().SetTypeIndex(18);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<CharacterDescription>.SendAllRpc>>().SetTypeIndex(19);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<CharacterDescription>.SendDeltaRpc>>().SetTypeIndex(20);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<PlayerDescription>.SendAllRpc>>().SetTypeIndex(21);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<PlayerDescription>.SendDeltaRpc>>().SetTypeIndex(22);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<ActionDescription>.SendAllRpc>>().SetTypeIndex(23);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<ActionDescription>.SendDeltaRpc>>().SetTypeIndex(24);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<ProjectileDescription>.SendAllRpc>>().SetTypeIndex(25);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<ProjectileDescription>.SendDeltaRpc>>().SetTypeIndex(26);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<TeamDescription>.SendAllRpc>>().SetTypeIndex(27);
        World.GetOrCreateSystem<RpcQueueSystem<SynchronizeRelativeSystem<TeamDescription>.SendDeltaRpc>>().SetTypeIndex(28);

    }
}
