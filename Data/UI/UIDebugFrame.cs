using System.Collections.Generic;
using System.Linq;
using package.patapon.core;
using P4.Core;
using Patapon4TLB.Default.Snapshot;
using StormiumTeam.GameBase;
using StormiumTeam.ThirdParty;
using TMPro;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;
using NotImplementedException = System.NotImplementedException;

namespace Patapon4TLB.UI
{
	public class UIDebugFrame : MonoBehaviour
	{
		public TextMeshProUGUI[] PingText;
		public TextMeshProUGUI[] CompareBeatText;

		public Toggle ToggleVoiceOverlay;

		public GameObject ConnectedFrame;
		public GameObject DisconnectedFrame;
		
		public TMP_InputField AddressField, PortField;
 		public Button ConnectButton, HostButton, DisconnectButton;

        private bool m_WantToConnect, m_WantToDisconnect, m_WantToHost;

        private void Awake()
        {
	        ConnectButton.onClick.AddListener(() => { m_WantToConnect       = true; });
	        HostButton.onClick.AddListener(() => { m_WantToHost             = true; });
	        DisconnectButton.onClick.AddListener(() => { m_WantToDisconnect = true; });
	        
	        ConnectedFrame.SetActive(false);
	        DisconnectedFrame.SetActive(false);
        }

        [UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
		public class InternalSystem : GameBaseSystem
		{
			public List<(Entity connection, uint ping)> States = new List<(Entity connection, uint ping)>();

			private void GetPings(Entity entity, ref NetworkSnapshotAckComponent ackComponent)
			{
				States.Add((entity, ackComponent.LastReceivedRTT));
			}

			private EntityQueryBuilder.F_ED<NetworkSnapshotAckComponent> m_ForEachDelegate;

			public int  ClientBeat, ServerBeat;
			public int  TimeDiff;
			public int  ClientCmdBeat, ServerCmdBeat;
			public bool IsCmdClient,   IsCmdServer;
			public bool HasConnection;

			protected override void OnCreate()
			{
				base.OnCreate();

				m_ForEachDelegate = GetPings;
			}

			protected override void OnUpdate()
			{
				States.Clear();
				Entities.ForEach(m_ForEachDelegate);
				Entities.WithAll<RhythmEngineSimulateTag>().ForEach((ref RhythmEngineProcess process, ref RhythmPredictedProcess predictedProcess, ref GameCommandState gameCommandState, ref RhythmCurrentCommand currentCommand, ref GamePredictedCommandState predictedCommand) =>
				{
					ClientBeat = process.Beat;
					ServerBeat = predictedProcess.Beat;
					TimeDiff   = process.TimeTick - (int) (predictedProcess.Time * 1000);


					IsCmdServer = gameCommandState.StartBeat <= process.Beat && gameCommandState.EndBeat > process.Beat;
					IsCmdClient = currentCommand.ActiveAtBeat <= process.Beat && predictedCommand.EndBeat > process.Beat;

					ServerCmdBeat = gameCommandState.StartBeat;
					ClientCmdBeat = currentCommand.ActiveAtBeat;
				});

				var driver = World.GetExistingSystem<NetworkStreamReceiveSystem>().Driver;
				HasConnection = driver.LocalEndPoint().IsValid;
			}
		}

		[UpdateInGroup(typeof(PresentationSystemGroup))]
		public class System : GameBaseSystem
		{
			private InternalSystem m_InternalSystem;
			private P4GameRuleSystem m_GameRuleSystem;

			private EntityQueryBuilder.F_C<UIDebugFrame> m_ForEachDelegate;
			private char[]                               m_Buffer = new char[512];
			private NativeString512                      m_NativeString;

			private float m_LastUpdate;

			private unsafe void ForEach(UIDebugFrame debugFrame)
			{
				debugFrame.ConnectedFrame.SetActive(m_InternalSystem != null);
				debugFrame.DisconnectedFrame.SetActive(m_InternalSystem == null);

				if (debugFrame.m_WantToDisconnect)
				{
					if (ClientServerBootstrap.clientWorld != null)
					{
						foreach (var world in ClientServerBootstrap.clientWorld)
						{
							var tickClientPresentationGroup = World.GetExistingSystem<TickClientPresentationSystem>();
							var tickClientSimulationGroup   = World.GetExistingSystem<TickClientSimulationSystem>();

							tickClientPresentationGroup.RemoveSystemFromUpdateList(world.GetExistingSystem<ClientPresentationSystemGroup>());
							tickClientSimulationGroup.RemoveSystemFromUpdateList(world.GetExistingSystem<ClientSimulationSystemGroup>());

							world.Dispose();
						}
					}

					if (ClientServerBootstrap.serverWorld != null)
					{
						var tickServerSimulationGroup = World.GetExistingSystem<TickServerSimulationSystem>();
						foreach (var system in tickServerSimulationGroup.Systems)
						{
							Debug.Log($"{system.World} {system.GetType().Name}");
						}
						var simulationGroup = ClientServerBootstrap.serverWorld.GetExistingSystem<ServerSimulationSystemGroup>();
						
						Debug.Log($"{simulationGroup.World} {tickServerSimulationGroup.Systems.ToList().IndexOf(simulationGroup)}");
						
						tickServerSimulationGroup.RemoveSystemFromUpdateList(simulationGroup);

						ClientServerBootstrap.serverWorld.Dispose();
					}

					ClientServerBootstrap.clientWorld = null;
					ClientServerBootstrap.serverWorld = null;
				}

				if (debugFrame.m_WantToConnect)
				{
					
				}

				if (debugFrame.m_WantToHost)
				{
					
				}
				
				m_GameRuleSystem.VoiceOverlayProperty.Value = debugFrame.ToggleVoiceOverlay.isOn;

				if (m_InternalSystem == null || m_InternalSystem.States.Count <= 0)
					return;

				var l = StringFormatter.Write(ref m_Buffer, 0, "RTT: {0}", (int) m_InternalSystem.States[0].ping);
				fixed (char* ptr = m_Buffer)
				{
					m_NativeString.CopyFrom(ptr, l);
				}

				var str = m_NativeString.ToString();
				for (var txt = 0; txt != debugFrame.PingText.Length; txt++)
				{
					debugFrame.PingText[txt].text = m_NativeString.ToString();
				}

				l = StringFormatter.Write(ref m_Buffer, 0, "C: {0}\nS: {1}\nC cmd|sb: {2}  {4}\nS cmd|sb: {3}  {5}",
					m_InternalSystem.ClientBeat, m_InternalSystem.ServerBeat,
					m_InternalSystem.IsCmdClient ? 1 : 0, m_InternalSystem.IsCmdServer ? 1 : 0,
					m_InternalSystem.ClientCmdBeat, m_InternalSystem.ServerCmdBeat);
				fixed (char* ptr = m_Buffer)
				{
					m_NativeString.CopyFrom(ptr, l);
				}

				str = m_NativeString.ToString();
				for (var txt = 0; txt != debugFrame.CompareBeatText.Length; txt++)
				{
					debugFrame.CompareBeatText[txt].text = m_NativeString.ToString();
				}
			}

			protected override void OnCreate()
			{
				base.OnCreate();
				m_ForEachDelegate = ForEach;

				m_GameRuleSystem = World.GetOrCreateSystem<P4GameRuleSystem>();

				RequireForUpdate(GetEntityQuery(typeof(UIDebugFrame)));
			}

			protected override void OnUpdate()
			{
				var clientWorld = GetActiveClientWorld();
				m_InternalSystem = clientWorld?.GetOrCreateSystem<InternalSystem>();

				Entities.ForEach(m_ForEachDelegate);
			}
		}
	}
}