using System.Collections.Generic;
using package.patapon.core;
using Patapon4TLB.Default;
using Patapon4TLBCore;
using StormiumTeam.GameBase;
using StormiumTeam.ThirdParty;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Revolution.NetCode;
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

		public TMP_InputField AddressField,  PortField;
		public Button         ConnectButton, HostButton, DisconnectButton;

		public TMP_InputField UsernameField;

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
			private struct AliveComponent : IComponentData
			{}
			
			public List<(Entity connection, float ping)> States = new List<(Entity connection, float ping)>();

			private void GetPings(Entity entity, ref NetworkSnapshotAckComponent ackComponent)
			{
				States.Add((entity, ackComponent.EstimatedRTT));
			}

			private EntityQueryBuilder.F_ED<NetworkSnapshotAckComponent> m_ForEachDelegate;

			public int  ClientActivationBeat, ServerActivationBeat;
			public int  TimeDiff;
			public int  ClientCmdBeat, ServerCmdBeat;
			public bool IsCmdClient,   IsCmdServer;
			public bool HasConnection;

			public bool Alive => this.HasSingleton<AliveComponent>();

			protected override void OnCreate()
			{
				base.OnCreate();

				m_ForEachDelegate = GetPings;
				EntityManager.CreateEntity(typeof(AliveComponent));
			}

			protected override void OnUpdate()
			{
				States.Clear();
				Entities.ForEach(m_ForEachDelegate);
				Entities.WithAll<RhythmEngineSimulateTag>().ForEach((ref RhythmEngineProcess process, ref RhythmPredictedProcess predictedProcess, ref GameCommandState gameCommandState, ref RhythmCurrentCommand currentCommand, ref GamePredictedCommandState predictedCommand) =>
				{
					var activationBeat = process.GetActivationBeat(500);
					var flowBeat       = process.GetFlowBeat(500);

					ClientActivationBeat = activationBeat;
					ServerActivationBeat = predictedProcess.Beat;
					TimeDiff             = process.Milliseconds - (int) (predictedProcess.Time * 1000);


					IsCmdServer = gameCommandState.StartTime <= process.Milliseconds && gameCommandState.EndTime > process.Milliseconds;
					IsCmdClient = currentCommand.ActiveAtTime <= process.Milliseconds && predictedCommand.State.EndTime > process.Milliseconds;

					ServerCmdBeat = gameCommandState.StartTime;
					ClientCmdBeat = currentCommand.CustomEndTime;
				});

				var driver = World.GetExistingSystem<NetworkStreamReceiveSystem>().Driver;
				HasConnection = driver.LocalEndPoint().IsValid;
			}
		}

		[UpdateInGroup(typeof(PresentationSystemGroup))]
		public class System : GameBaseSystem
		{
			private InternalSystem   m_InternalSystem;
			private P4GameRuleSystem m_GameRuleSystem;

			private EntityQueryBuilder.F_C<UIDebugFrame> m_ForEachDelegate;
			private char[]                               m_Buffer = new char[512];
			private NativeString512                      m_NativeString;

			private float m_LastUpdate;

			private void CleanWorld(World world)
			{
				using (var entities = world.EntityManager.GetAllEntities(Allocator.TempJob))
					world.EntityManager.DestroyEntity(entities);
			}

			private void SetState(World world, bool enabled)
			{
				void EnableSystem<T>()
					where T : ComponentSystemBase
				{
					var s = world.GetExistingSystem<T>();
					if (s != null)
						s.Enabled = enabled;
				}

				EnableSystem<TickClientInitializationSystem>();
				EnableSystem<TickClientSimulationSystem>();
				EnableSystem<TickClientPresentationSystem>();
				EnableSystem<TickServerInitializationSystem>();
				EnableSystem<TickServerSimulationSystem>();
			}

			private unsafe void ForEach(UIDebugFrame debugFrame)
			{
				debugFrame.ConnectedFrame.SetActive(!m_InternalSystem.Alive);
				debugFrame.DisconnectedFrame.SetActive(m_InternalSystem.Alive);

				debugFrame.HostButton.interactable    = debugFrame.PortField.text.Length > 0;
				debugFrame.ConnectButton.interactable = debugFrame.PortField.text.Length > 0 && debugFrame.AddressField.text.Length > 0;



				if (debugFrame.m_WantToDisconnect)
				{
					if (ClientServerBootstrap.clientWorld != null)
						foreach (var world in ClientServerBootstrap.clientWorld)
						{
							CleanWorld(world);
							SetState(world, false);
						}

					if (ClientServerBootstrap.serverWorld != null)
					{
						CleanWorld(ClientServerBootstrap.serverWorld);
						SetState(ClientServerBootstrap.serverWorld, false);
					}
				}

				if (debugFrame.m_WantToConnect)
				{
					var port = (ushort) int.Parse(debugFrame.PortField.text);
					var ep   = NetworkEndPoint.Parse(debugFrame.AddressField.text, port);

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
					if (ClientServerBootstrap.clientWorld != null)
						foreach (var world in ClientServerBootstrap.clientWorld)
							SetState(world, false);

					if (ClientServerBootstrap.serverWorld != null)
						SetState(ClientServerBootstrap.serverWorld, true);

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

				debugFrame.m_WantToHost       = false;
				debugFrame.m_WantToConnect    = false;
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
					debugFrame.PingText[txt].text = str;
				}

				l = StringFormatter.Write(ref m_Buffer, 0, "C: {0}\nS: {1}\nC cmd|sb: {2}  {4}\nS cmd|sb: {3}  {5}",
					m_InternalSystem.ClientActivationBeat, m_InternalSystem.ServerActivationBeat,
					m_InternalSystem.IsCmdClient ? 1 : 0, m_InternalSystem.IsCmdServer ? 1 : 0,
					m_InternalSystem.ClientCmdBeat, m_InternalSystem.ServerCmdBeat);
				fixed (char* ptr = m_Buffer)
				{
					m_NativeString.CopyFrom(ptr, l);
				}

				str = m_NativeString.ToString();
				for (var txt = 0; txt != debugFrame.CompareBeatText.Length; txt++)
				{
					debugFrame.CompareBeatText[txt].text = str;
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