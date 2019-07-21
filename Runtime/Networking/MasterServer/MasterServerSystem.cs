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
using ILogger = Grpc.Core.Logging.ILogger;

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
	{}

	[NotClientServerSystem]
	public class MasterServerSystem : ComponentSystem
	{
		private EntityQuery m_ConnectionQuery;

		public delegate void ShutDownEvent();
		public event ShutDownEvent BeforeShutdown;
		public Channel channel { get; private set; }

		public class Logger : ILogger
		{
			public string Prefix;
			
			public ILogger ForType<T>()
			{
				return new Logger {Prefix = typeof(T).Name};
			}

			public void Debug(string message)
			{
				UnityEngine.Debug.Log($"{Prefix}:debug -> {message}");
			}

			public void Debug(string format, params object[] formatArgs)
			{
				Debug(string.Format(format, formatArgs));
			}

			public void Info(string message)
			{
				UnityEngine.Debug.Log($"{Prefix}:info -> {message}");
			}

			public void Info(string format, params object[] formatArgs)
			{
				Info(string.Format(format, formatArgs));
			}

			public void Warning(string message)
			{
				UnityEngine.Debug.Log($"{Prefix}:warning -> {message}");
			}

			public void Warning(string format, params object[] formatArgs)
			{
				Warning(string.Format(format, formatArgs));
			}

			public void Warning(Exception exception, string message)
			{
				Warning($"thrown {exception}, msg: {message}");
			}

			public void Error(string message)
			{
				UnityEngine.Debug.Log($"{Prefix}:error -> {message}");
			}

			public void Error(string format, params object[] formatArgs)
			{
				Error(string.Format(format, formatArgs));
			}

			public void Error(Exception exception, string message)
			{
				Warning($"thrown {exception}, msg: {message}");
			}
		}
		
		protected override void OnCreate()
		{
			base.OnCreate();

			m_ConnectionQuery = GetEntityQuery(typeof(MasterServerConnection));
			GrpcEnvironment.SetLogger(new Logger());
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
				//channel.ShutdownAsync();
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
			RequestQuery = System.EntityManager.CreateEntityQuery(typeof(TRequest), ComponentType.Exclude<TProcessing>(), ComponentType.Exclude<TCompleted>());
			ProcessingQuery = System.EntityManager.CreateEntityQuery(typeof(TRequest), typeof(TProcessing));
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

		public void AddProcessTagToAllRequests()
		{
			System.EntityManager.AddComponent(RequestQuery, typeof(TProcessing));
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