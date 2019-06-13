using System.Collections.Generic;
using package.patapon.core;
using Patapon4TLB.Default.Snapshot;
using StormiumTeam.GameBase;
using StormiumTeam.ThirdParty;
using TMPro;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using NotImplementedException = System.NotImplementedException;

namespace Patapon4TLB.UI
{
	public class UIDebugFrame : MonoBehaviour
	{
		public TextMeshProUGUI[] PingText;
		public TextMeshProUGUI[] CompareBeatText;

		[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
		public class InternalSystem : GameBaseSystem
		{
			public List<(Entity connection, uint ping)> States = new List<(Entity connection, uint ping)>();

			private void GetPings(Entity entity, ref NetworkSnapshotAckComponent ackComponent)
			{
				States.Add((entity, ackComponent.LastReceivedRTT));
			}

			private EntityQueryBuilder.F_ED<NetworkSnapshotAckComponent> m_ForEachDelegate;

			public int ClientBeat, ServerBeat;
			public int TimeDiff;
			public int ClientCmdBeat, ClientCmd, ServerCmdBeat, ServerCmd;

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

					bool isCmdServer = false, isCmdClient = false;

					isCmdServer = gameCommandState.StartBeat <= process.Beat && gameCommandState.EndBeat > process.Beat;
					isCmdClient = currentCommand.ActiveAtBeat <= process.Beat && predictedCommand.EndBeat > process.Beat;

					ServerCmdBeat = gameCommandState.StartBeat;
					ClientCmdBeat = currentCommand.ActiveAtBeat;
					
					ServerCmd = -1;

					if (isCmdClient)
					{
						ClientCmd = EntityManager.GetComponentData<RhythmCommandId>(currentCommand.CommandTarget).Value;
					}
					else ClientCmd = -1;
				});
			}
		}

		[UpdateInGroup(typeof(PresentationSystemGroup))]
		public class System : GameBaseSystem
		{
			private InternalSystem m_InternalSystem;

			private EntityQueryBuilder.F_C<UIDebugFrame> m_ForEachDelegate;
			private char[]                               m_Buffer = new char[512];
			private NativeString512                      m_NativeString;

			private float m_LastUpdate;

			private unsafe void ForEach(UIDebugFrame debugFrame)
			{
				if (m_InternalSystem.States.Count < 0)
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
					m_InternalSystem.ClientCmd, m_InternalSystem.ServerCmd,
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