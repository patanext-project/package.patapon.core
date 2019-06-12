using System.Collections.Generic;
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

		[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
		public class InternalSystem : GameBaseSystem
		{
			public List<(Entity connection, uint ping)> States = new List<(Entity connection, uint ping)>();

			private void GetPings(Entity entity, ref NetworkSnapshotAckComponent ackComponent)
			{
				States.Add((entity, ackComponent.LastReceivedRTT));
			}

			private EntityQueryBuilder.F_ED<NetworkSnapshotAckComponent> m_ForEachDelegate;

			protected override void OnCreate()
			{
				base.OnCreate();

				m_ForEachDelegate = GetPings;
			}

			protected override void OnUpdate()
			{
				States.Clear();
				Entities.ForEach(m_ForEachDelegate);
			}
		}

		[UpdateInGroup(typeof(PresentationSystemGroup))]
		public class System : GameBaseSystem
		{
			private InternalSystem m_InternalSystem;

			private EntityQueryBuilder.F_C<UIDebugFrame> m_ForEachDelegate;
			private char[] m_Buffer = new char[512];
			private NativeString512 m_NativeString;

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
				
				if (m_LastUpdate + 0.25f < Time.time)
				{
					m_LastUpdate = Time.time;
					Entities.ForEach(m_ForEachDelegate);
				}
			}
		}
	}
}