using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Grpc.Core;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
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

	public class MasterServerProcessRpcSystem : ComponentSystemGroup
	{
		[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
		public class SimulationGroup : ComponentSystemGroup
		{
			public void SetSystems(List<ComponentSystemBase> systems)
			{
				Debug.Log("ye");
				foreach (var system in systems)
				{
					if (m_systemsToUpdate.Any(s => s.GetType() == system.GetType()))
						continue;

					Debug.Log("add " + system.GetType());
					m_systemsToUpdate.Add(World.GetOrCreateSystem(system.GetType()));
				}

				foreach (var system in m_systemsToUpdate)
				{
					if (systems.Any(s => s.GetType() == system.GetType()))
						continue;
					
					Debug.Log("remove " + system.GetType());

					World.DestroySystem(system);
					m_systemsToUpdate.Remove(system);
				}
			}
		}

		private int m_PreviousClientCount = -1;
		private int m_PreviousServerCount = -1;

		protected override void OnUpdate()
		{
			base.OnUpdate();

			if (m_PreviousClientCount != ClientServerBootstrap.ClientCreationCount
			    || m_PreviousServerCount != ClientServerBootstrap.ServerCreationCount)
			{
				m_PreviousClientCount = ClientServerBootstrap.ClientCreationCount;
				m_PreviousServerCount = ClientServerBootstrap.ServerCreationCount;
				
				SortSystemUpdateList();
			}
		}

		public override void SortSystemUpdateList()
		{
			base.SortSystemUpdateList();

			void UpdateWorld(World world)
			{
				var group = world.GetOrCreateSystem<SimulationGroup>();
				group.SetSystems(m_systemsToUpdate);
			}

			if (ClientServerBootstrap.clientWorld != null)
				foreach (var world in ClientServerBootstrap.clientWorld)
				{
					UpdateWorld(world);
				}

			if (ClientServerBootstrap.serverWorld != null)
				UpdateWorld(ClientServerBootstrap.serverWorld);
		}
	}

	public class MasterServerSystem : ComponentSystem
	{
		private EntityQuery m_ConnectionQuery;

		public delegate void ShutDownEvent();
		public event ShutDownEvent BeforeShutdown;
		public Channel channel { get; private set; }

		protected override void OnCreate()
		{
			base.OnCreate();

			m_ConnectionQuery = GetEntityQuery(typeof(MasterServerConnection));
		}

		protected override void OnUpdate()
		{
		}

		protected override async void OnDestroy()
		{
			base.OnDestroy();

			Disconnect().Wait();
		}

		public async Task SetMasterServer(IPEndPoint endpoint)
		{
			await Disconnect();

			var entity = EntityManager.CreateEntity();
			EntityManager.AddSharedComponentData(entity, new MasterServerConnection
			{
				EndPoint = endpoint
			});
			
			channel = new Channel("localhost", 4242, ChannelCredentials.Insecure);
			await channel.ConnectAsync();
		}

		public async Task Disconnect()
		{
			Debug.Log("?");
			if (channel != null && channel.State != ChannelState.Shutdown)
			{
				BeforeShutdown?.Invoke();
				channel.ShutdownAsync();
			}
			channel = null;

			Entities.With(m_ConnectionQuery).ForEach((Entity entity, MasterServerConnection connection) =>
			{
				// TODO: real disconnection
				Debug.Log("Disconnected from " + connection.ToString());
			});

			EntityManager.DestroyEntity(m_ConnectionQuery);
		}
	}

	public class MasterServerRequestModule<TRequest, TProcessing, TCompleted> : BaseSystemModule
		where TRequest : struct, IMasterServerRequest, IComponentData
		where TProcessing : struct, IComponentData
		where TCompleted : struct, IComponentData
	{
		public struct Request
		{
			public Entity Entity;
			public TRequest Value;
		}
		
		public EntityQuery RequestQuery;
		public EntityQuery ProcessingQuery;
		public EntityQuery ResultQuery;

		private NativeList<Request> m_Requests;
		private bool m_Update;
		
		protected override void OnEnable()
		{
			// hack
			RequestQuery = System.EntityManager.CreateEntityQuery(typeof(TRequest), ComponentType.Exclude<TCompleted>());
			ProcessingQuery = System.EntityManager.CreateEntityQuery(typeof(TProcessing));
			ResultQuery = System.EntityManager.CreateEntityQuery(typeof(TCompleted));

			m_Requests = new NativeList<Request>(Allocator.Persistent);
		}

		protected override void OnUpdate(ref JobHandle jobHandle)
		{
			m_Requests.Clear();

			m_Update = true;
			
			var entityType = System.GetArchetypeChunkEntityType();
			var componentType = System.GetArchetypeChunkComponentType<TRequest>(true);
			using (var chunks = RequestQuery.CreateArchetypeChunkArray(Allocator.TempJob))
			{
				foreach (var chunk in chunks)
				{
					var entityArray = chunk.GetNativeArray(entityType);
					var requestArray = chunk.GetNativeArray(componentType);
					for (var i = 0; i != chunk.Count; i++)
					{
						m_Requests.Add(new Request
						{
							Entity = entityArray[i],
							Value = requestArray[i]
						});
					}
				}
			}
		}

		public NativeArray<Request> GetRequests()
		{
			if (!m_Update)
				throw new InvalidOperationException("This module was not even updated once!");
			
			return m_Requests;
		}

		protected override void OnDisable()
		{
			m_Requests.Dispose();
		}
	}
}