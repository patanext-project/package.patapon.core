using System;
using P4.Core.Code.Networking;
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
        typeof(ClientLoadedRpc),
        typeof(PlayerConnectedRpc),
        typeof(TargetDamageEventRpc),

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
                var tmp = new ClientLoadedRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 7:
            {
                var tmp = new PlayerConnectedRpc();
                tmp.Deserialize(reader, ref ctx);
                tmp.Execute(connection, commandBuffer, jobIndex);
                break;
            }
            case 8:
            {
                var tmp = new TargetDamageEventRpc();
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
        World.GetOrCreateSystem<RpcQueueSystem<ClientLoadedRpc>>().SetTypeIndex(6);
        World.GetOrCreateSystem<RpcQueueSystem<PlayerConnectedRpc>>().SetTypeIndex(7);
        World.GetOrCreateSystem<RpcQueueSystem<TargetDamageEventRpc>>().SetTypeIndex(8);

    }
}
