using System.Collections.Generic;
using package.stormiumteam.networking;
using package.stormiumteam.networking.runtime.highlevel;
using package.stormiumteam.networking.runtime.lowlevel;
using Patapon4TLB.Core;
using Runtime.Data;
using StormiumShared.Core.Networking;
using Unity.Collections;
using Unity.Entities;

namespace Stormium.Core
{
	public abstract class GameBaseSystem : ComponentSystem
	{		
		public P4GameManager       GameMgr        { get; private set; }
		public StormiumGameServerManager ServerMgr      { get; private set; }
		public EntityModelManager        EntityModelMgr { get; private set; }
		public P4GameTimeManager         TimeMgr        { get; private set; }
		public NetPatternSystem          PatternSystem  { get; private set; }

		public int Tick      => TimeMgr.GetTimeFromSingleton().Tick;
		public int TickDelta => TimeMgr.GetTimeFromSingleton().DeltaTick;

		public PatternBank LocalBank => PatternSystem.GetLocalBank();

		protected override void OnCreateManager()
		{			
			GameMgr        = World.GetOrCreateManager<P4GameManager>();
			ServerMgr      = World.GetOrCreateManager<StormiumGameServerManager>();
			EntityModelMgr = World.GetOrCreateManager<EntityModelManager>();
			TimeMgr        = World.GetOrCreateManager<P4GameTimeManager>();
			PatternSystem  = World.GetOrCreateManager<NetPatternSystem>();

			m_PlayerGroup = GetComponentGroup
			(
				typeof(P4GamePlayer)
			);
		}
		
		private ComponentGroup m_PlayerGroup;
		public Entity GetFirstSelfGamePlayer()
		{
			using (var entityArray = m_PlayerGroup.ToEntityArray(Allocator.TempJob))
			using (var playerArray = m_PlayerGroup.ToComponentDataArray<P4GamePlayer>(Allocator.TempJob))
			{
				for (var i = 0; i != playerArray.Length; i++)
					if (playerArray[i].IsSelf == 1)
						return entityArray[i];
			}

			return default;
		}
	}

	[AlwaysUpdateSystem]
	public abstract class GameBaseSyncMessageSystem : GameBaseSystem
	{
		protected delegate void OnReceiveMessage(NetworkInstanceData networkInstance, Entity client, DataBufferReader data);
		
		private Dictionary<int, OnReceiveMessage> m_ActionForPattern;
		private ComponentGroup m_NetworkGroup;

		protected override void OnCreateManager()
		{
			base.OnCreateManager();
			m_ActionForPattern = new Dictionary<int, OnReceiveMessage>();
			m_NetworkGroup = GetComponentGroup
			(
				typeof(NetworkInstanceData),
				typeof(NetworkInstanceToClient)
			);
		}

		protected override void OnUpdate()
		{
			var networkMgr    = World.GetExistingManager<NetworkManager>();
			var patternSystem = World.GetExistingManager<NetPatternSystem>();
			
			using (var entityArray = m_NetworkGroup.ToEntityArray(Allocator.TempJob))
			using (var dataArray = m_NetworkGroup.ToComponentDataArray<NetworkInstanceData>(Allocator.TempJob))
			{
				for (var i = 0; i != entityArray.Length; i++)
				{
					var entity = entityArray[i];
					var data   = dataArray[i];
					if (data.InstanceType != InstanceType.LocalServer)
						continue;

					var evBuffer = EntityManager.GetBuffer<EventBuffer>(entity);
					for (var j = 0; j != evBuffer.Length; j++)
					{
						var ev = evBuffer[j].Event;
						if (ev.Type != NetworkEventType.DataReceived)
							continue;
						
						var foreignEntity = networkMgr.GetNetworkInstanceEntity(ev.Invoker.Id);
						var exchange      = patternSystem.GetLocalExchange(ev.Invoker.Id);
						var buffer        = BufferHelper.ReadEventAndGetPattern(ev, exchange, out var patternId);
						var clientEntity  = EntityManager.GetComponentData<NetworkInstanceToClient>(foreignEntity).Target;
						
						if (m_ActionForPattern.ContainsKey(patternId))
							m_ActionForPattern[patternId](data, clientEntity, new DataBufferReader(buffer, buffer.CurrReadIndex, buffer.Length));
					}
				}
			}
		}
		
		protected PatternResult AddMessage(OnReceiveMessage func, byte version = 0)
		{
			var patternName = $"auto.{GetType().Name}.{func.Method.Name}";
			var result      = LocalBank.Register(new PatternIdent(patternName, version));

			m_ActionForPattern[result.Id] = func;

			return result;
		}
		
		protected void SyncToServer(PatternResult result, DataBufferWriter syncData)
		{
			if (ServerMgr.ConnectedServerEntity == default)
				return;

			var instanceData = EntityManager.GetComponentData<NetworkInstanceData>(ServerMgr.ConnectedServerEntity);
			using (var buffer = BufferHelper.CreateFromPattern(result.Id, length: sizeof(byte) + sizeof(int) + syncData.Length))
			{
				buffer.WriteBuffer(syncData);
				
				instanceData.Commands.Send(buffer, default, Delivery.Reliable | Delivery.Unsequenced);
			}
		}
	}
}