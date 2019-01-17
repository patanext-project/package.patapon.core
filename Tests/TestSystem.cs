using System;
using System.Net;
using ENet;
using package.stormiumteam.networking.runtime.highlevel;
using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using Patapon4TLB.Core.Networking;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
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
            World.GetExistingManager<AppEventSystem>()
                 .SubscribeToAll(this);
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
            //snapshotMgr.GenerateSnapshot();
        }

        public void NativeOnGUI()
        {
            var networkMgr = World.GetExistingManager<NetworkManager>();
            
            using (new GUILayout.VerticalScope())
            {
                GUI.contentColor = Color.black;
                
                GUILayout.Label("TestSystem Actions:");
                GUILayout.Space(1);

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
                    if (EntityManager.Exists(HostEntity))
                        networkMgr.Stop(HostEntity, true);
                    
                    var targetEp = new IPEndPoint(IPAddress.Parse(HostAddr), HostPort);
                    var localEp = new IPEndPoint(IPAddress.Loopback, 0);
                    var r = networkMgr.StartClient(targetEp, localEp, NetDriverConfiguration.@default());

                    HostEntity = r.ClientInstanceEntity;
                }

                if (GUILayout.Button("Create"))
                {
                    if (EntityManager.Exists(HostEntity))
                        networkMgr.Stop(HostEntity, true);
                    
                    var localEp  = new IPEndPoint(IPAddress.Any, HostPort);
                    var r = networkMgr.StartServer(localEp, NetDriverConfiguration.@default());

                    HostEntity = r.Entity;
                }
            }
        }
    }
}