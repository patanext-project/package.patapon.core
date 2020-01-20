using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcsComponents.MasterServer;
using Grpc.Core;
using P4TLB.MasterServer;
using Unity.Entities;
using UnityEngine;

namespace Patapon4TLB.Core.MasterServer
{
	public class MasterServerManagePendingEventSystem : BaseSystemMasterServerService
	{
		private double                             m_NextCheck;
		private AsyncUnaryCall<CheckEventResponse> m_ServerTask;

		private Dictionary<string, bool> m_PendingEvents;

		protected override void OnCreate()
		{
			base.OnCreate();
			m_PendingEvents = new Dictionary<string, bool>();
		}

		public bool IsPending(string eventId)
		{
			m_PendingEvents.TryGetValue(eventId, out var pending);
			return pending;
		}

		public void DeleteEvent(string eventId)
		{
			m_PendingEvents.Remove(eventId);
		}

		protected override void OnUpdate()
		{
			if (!StaticMasterServer.HasClient<PendingEventService.PendingEventServiceClient>())
				return;

			if (!HasSingleton<ConnectedMasterServerClient>())
				return;

			var client = StaticMasterServer.GetClient<PendingEventService.PendingEventServiceClient>();
			if (m_ServerTask != null)
			{
				if (m_ServerTask.ResponseAsync.IsCompleted)
				{
					if (m_ServerTask.ResponseAsync.Exception != null)
					{
						Debug.LogException(m_ServerTask.ResponseAsync.Exception.Flatten());
					}
					else
					{
						var response = m_ServerTask.ResponseAsync.Result;
						if (response.Success)
						{
							foreach (var ev in response.Events)
							{
								m_PendingEvents[ev.Name] = true;
								Debug.Log($"{GetSingleton<ConnectedMasterServerClient>().UserLogin} -> {ev.Name}");
							}
						}
						else
						{
							Debug.LogError("Disconnecting from masterserver...");
							MasterServerSystem.Instance.Disconnect();
						}
					}

					m_ServerTask = null;
					m_NextCheck  = Time.ElapsedTime + 0.4;
				}

				return;
			}

			if (m_NextCheck > Time.ElapsedTime)
				return;

			m_ServerTask = client.GetPendingAsync(new CheckEventRequest {ClientToken = GetSingleton<ConnectedMasterServerClient>().Token.ToString()});
		}
	}
}