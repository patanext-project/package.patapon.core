using System;
using System.Net;
using Unity.Entities;
using UnityEngine;

namespace Patapon4TLB.Core.MasterServer
{
	public struct MasterServerConnection : ISharedComponentData, IEquatable<MasterServerConnection>
	{
		public IPEndPoint EndPoint;

		public bool Equals(MasterServerConnection other)
		{
			return Equals(EndPoint, other.EndPoint);
		}

		public override bool Equals(object obj)
		{
			return obj is MasterServerConnection other && Equals(other);
		}

		public override int GetHashCode()
		{
			return (EndPoint != null ? EndPoint.GetHashCode() : 0);
		}
	}
	
	public class MasterServerSystem : ComponentSystem
	{
		private EntityQuery m_ConnectionQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_ConnectionQuery = GetEntityQuery(typeof(MasterServerConnection));
		}

		protected override void OnUpdate()
		{
			
		}

		public void SetMasterServer(IPEndPoint endpoint)
		{
			Disconnect();

			var entity = EntityManager.CreateEntity();
			EntityManager.AddSharedComponentData(entity, new MasterServerConnection
			{
				EndPoint = endpoint
			});
		}

		public void Disconnect()
		{
			Entities.With(m_ConnectionQuery).ForEach((Entity entity, MasterServerConnection connection) => 
			{
				// TODO: real disconnection
				Debug.Log("Disconnected from " + connection.ToString());
			});
			
			EntityManager.DestroyEntity(m_ConnectionQuery);
		}
	}
}