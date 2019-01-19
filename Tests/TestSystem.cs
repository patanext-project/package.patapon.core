using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using ENet;
using package.stormiumteam.networking.runtime.highlevel;
using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using Patapon4TLB.Core.Networking;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Patapon4TLB.Core.Tests
{
    public class TestSystem : ComponentSystem, INativeEventOnGUI
    {
        public Entity HostEntity;
        public string HostAddr = "127.0.0.1";
        public int HostPort = 8590;


        protected override void OnCreateManager()
        {
            Application.targetFrameRate = 96;
            
            World.GetExistingManager<AppEventSystem>()
                 .SubscribeToAll(this);
            
            var gameLogPath = Application.dataPath + "/game_log.txt";
            if (!File.Exists(gameLogPath))
                File.Create(gameLogPath);
            else
            {
                File.WriteAllText(gameLogPath, string.Empty);

            }

            Application.logMessageReceived += (condition, trace, type) => { File.AppendAllText(gameLogPath, $"<{condition}> [{type}] {trace}"); };
        }
        
        protected override void OnUpdate()
        {

            if (HostEntity == Entity.Null)
                return;
            
            var networkInstanceData = EntityManager.GetComponentData<NetworkInstanceData>(HostEntity);
            // If this is not a server, continue...
            if ((networkInstanceData.InstanceType & InstanceType.Server) == 0)
                return;

            var snapshotMgr = World.GetExistingManager<SnapshotManager>();
            var gameTime = World.GetExistingManager<StGameTimeManager>().GetTimeFromSingleton();
        }
        
        private IPAddress LocalIPAddress()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            return host
                   .AddressList
                   .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }

        public void NativeOnGUI()
        {
            var networkMgr = World.GetExistingManager<NetworkManager>();
            
            using (new GUILayout.VerticalScope())
            {
                GUI.contentColor = Color.black;
                
                GUILayout.Label("TestSystem Actions:");
                GUILayout.Space(1);

                if (!EntityManager.Exists(HostEntity))
                    DoConnectAndCreate(networkMgr);
                else
                {
                    DoCancelOrStop(networkMgr);
                }
            }
        }

        public void DoConnectAndCreate(NetworkManager networkMgr)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Server Address: ");
                HostAddr = GUILayout.TextField(HostAddr);
            }

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Server Port:   ");
                if (int.TryParse(GUILayout.TextField(HostPort.ToString()), out HostPort))
                {
                }
            }

            GUILayout.Space(5);
            if (GUILayout.Button("Connect"))
            {
                var targetEp = new IPEndPoint(IPAddress.Parse(HostAddr), HostPort);
                var localEp  = new IPEndPoint(LocalIPAddress(), 0);

                Debug.Log($"Connecting to : Host: {HostAddr}:{HostPort}, LocalIp: {LocalIPAddress().ToString()}");

                var r = networkMgr.StartClient(targetEp, null, NetDriverConfiguration.@default());

                HostEntity = r.ClientInstanceEntity;
            }

            if (GUILayout.Button("Create"))
            {
                var localEp = new IPEndPoint(IPAddress.Any, HostPort);
                var r       = networkMgr.StartServer(localEp, NetDriverConfiguration.@default());

                HostEntity = r.Entity;
            }
        }

        public void DoCancelOrStop(NetworkManager networkMgr)
        {
            var isValid = EntityManager.HasComponent(HostEntity, typeof(ValidInstanceTag));
            if (GUILayout.Button(isValid ? "Stop" : "Cancel"))
            {
                HostEntity = default;
                networkMgr.StopAll();
            }
            
            if (EntityManager.Exists(HostEntity))
            {
                var connectedInstanceBuffer = EntityManager.GetBuffer<ConnectedInstance>(HostEntity);

                GUILayout.Space(5);
                for (var i = 0; i != connectedInstanceBuffer.Length; i++)
                {
                    var connectedInstance = connectedInstanceBuffer[i];
                    if (!EntityManager.Exists(connectedInstance.Entity) || !EntityManager.HasComponent(connectedInstance.Entity, typeof(NetworkInstanceData)))
                        continue;
                    
                    var data              = EntityManager.GetComponentData<NetworkInstanceData>(connectedInstance.Entity);
                    GUILayout.Label($"Id={connectedInstance.Connection.Id}, Type={data.InstanceType}, Rtt={data.Commands.RoundTripTime}");
                }
            }
        }
    }
}