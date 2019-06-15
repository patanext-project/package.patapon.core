using System.Collections.Generic;
using package.patapon.core;
using P4.Core;
using Patapon4TLB.Default.Snapshot;
using StormiumTeam.GameBase;
using StormiumTeam.ThirdParty;
using TMPro;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.UI;

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

	        PortField.text = "50001";
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

				debugFrame.HostButton.interactable = debugFrame.PortField.text.Length > 0;
				debugFrame.ConnectButton.interactable = debugFrame.PortField.text.Length > 0 && debugFrame.AddressField.text.Length > 0;

				if (debugFrame.m_WantToDisconnect)
				{
					ClientServerBootstrap.StopClientWorlds(World);
					ClientServerBootstrap.StopServerWorld(World);
				}

				if (debugFrame.m_WantToConnect)
				{
					ClientServerBootstrap.CreateClientWorlds();
					
					var port = (ushort) int.Parse(debugFrame.PortField.text);
					var ep = NetworkEndPoint.Parse(debugFrame.AddressField.text, port);
					
					var clientWorld = ClientServerBootstrap.clientWorld;
					if (clientWorld != null)
					{
						foreach (var world in ClientServerBootstrap.clientWorld)
						{
							var ent = world.GetExistingSystem<NetworkStreamReceiveSystem>().Connect(ep);
						}
					}
				}

				if (debugFrame.m_WantToHost)
				{
					ClientServerBootstrap.CreateClientWorlds();
					ClientServerBootstrap.CreateServerWorld();

					var port = (ushort) int.Parse(debugFrame.PortField.text);
					
					var serverWorld = ClientServerBootstrap.serverWorld;
					if (serverWorld != null)
					{
						var ep = NetworkEndPoint.AnyIpv4;
						ep.Port = port;
						serverWorld.GetExistingSystem<NetworkStreamReceiveSystem>().Listen(ep);
					}

					var clientWorld = ClientServerBootstrap.clientWorld;
					if (clientWorld != null)
					{
						foreach (var world in ClientServerBootstrap.clientWorld)
						{
							var ep = NetworkEndPoint.LoopbackIpv4;
							ep.Port = port;
							var ent = world.GetExistingSystem<NetworkStreamReceiveSystem>().Connect(ep);
						}
					}
				}

				debugFrame.m_WantToHost = false;
				debugFrame.m_WantToConnect = false;
				debugFrame.m_WantToDisconnect = false;
				
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