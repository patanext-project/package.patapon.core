using System;
using System.Linq;
using Grpc.Core;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Patapon4TLB.Core.MasterServer
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.Default)]
	[AlwaysUpdateSystem]
	public abstract class BaseSystemMasterServerService : ComponentSystem
	{
		private static bool m_IgnoreAddSystem;

		private bool m_IsShutdown;
		private ModuleRegister m_ModuleRegister;
		
		protected override void OnStartRunning()
		{
			MasterServerSystem.Instance.BeforeShutdown += OnBeforeShutdown;
				
			if (!m_IgnoreAddSystem)
			{
				m_IgnoreAddSystem = true;
				// Also add them to client and server worlds...
				foreach (var world in World.AllWorlds)
				{
					var group = (ComponentSystemGroup) world.GetExistingSystem<ClientSimulationSystemGroup>()
					            ?? world.GetExistingSystem<ServerSimulationSystemGroup>();
					
					if (group != null && group.Systems.Count(s => s.GetType() == GetType()) == 0)
					{
						group.AddSystemToUpdateList(world.GetOrCreateSystem(GetType()));
					}
				}

				m_IgnoreAddSystem = false;
			}
		}

		protected override void OnUpdate()
		{

		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			if (!m_IsShutdown)
			{
				m_IsShutdown = true;
				OnShutdown();
			}
		}

		protected virtual void OnShutdown()
		{
		}

		private void OnBeforeShutdown()
		{
			MasterServerSystem.Instance.BeforeShutdown -= OnBeforeShutdown;
			if (!m_IsShutdown)
			{
				m_IsShutdown = true;
				OnShutdown();
			}
		}
		
		protected void GetModule<TModule>(out TModule module)
			where TModule : BaseSystemModule, new()
		{
			if (m_ModuleRegister == null)
				m_ModuleRegister = new ModuleRegister(this);
			m_ModuleRegister.GetModule(out module);
		}
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Default)]
	public abstract class BaseServiceClientImplementation<TService> : ComponentSystem
		where TService : ClientBase
	{
		public TService Service { get; private set; }

		protected override void OnUpdate()
		{
			if (StaticMasterServer.channel != null
			    && !StaticMasterServer.HasClient<TService>())
			{
				StaticMasterServer.AddClient(() => { return Service = OnAddClient(StaticMasterServer.channel); });
			}
		}

		protected virtual TService OnAddClient(Channel channel)
		{
			return (TService) Activator.CreateInstance(typeof(TService), channel);
		}
	}
}