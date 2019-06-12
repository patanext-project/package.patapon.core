using System.Collections.Generic;
using package.patapon.core;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using UnityEngine;

namespace Patapon4TLB.Default
{
	/*[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class RhythmEngineGroupServer : ComponentSystemGroup
	{
		protected override void OnCreateManager()
		{
			base.OnCreateManager();
			
			AddSystemToUpdateList(World.GetOrCreateSystem<RhythmEngineGroup>());
		}
	}
	
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class RhythmEngineGroupClient : ComponentSystemGroup
	{
		protected override void OnCreateManager()
		{
			base.OnCreateManager();
			
			AddSystemToUpdateList(World.GetOrCreateSystem<RhythmEngineGroup>());
		}
	}*/
	
	[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
	public class RhythmEngineGroup : ComponentSystemGroup
	{
		private RhythmEngineBeginBarrier m_BeginBarrier;
		private RhythmEngineEndBarrier m_EndBarrier;

		private FlowRhythmBeatEventProvider m_BeatEventProvider;
		private FlowRhythmPressureEventProvider m_PressureEventProvider;

		private List<ComponentSystemBase> m_SystemsInGroup = new List<ComponentSystemBase>();

		public override IEnumerable<ComponentSystemBase> Systems => m_SystemsInGroup;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_BeginBarrier = World.GetOrCreateSystem<RhythmEngineBeginBarrier>();
			m_EndBarrier   = World.GetOrCreateSystem<RhythmEngineEndBarrier>();

			m_BeatEventProvider     = World.GetOrCreateSystem<FlowRhythmBeatEventProvider>();
			m_PressureEventProvider = World.GetOrCreateSystem<FlowRhythmPressureEventProvider>();

			if (World.GetExistingSystem<ServerSimulationSystemGroup>() != null)
			{
				World.GetOrCreateSystem<RegisterDefaultSequenceCommands>();
			}

			SortSystemUpdateList();

			// do some tests

			var f1 = FlowRhythmEngine.GetRhythmBeat(33359, 500);
			var f2 = FlowRhythmEngine.GetRhythmBeat(33620, 500);
			Debug.Assert(f1.original == 66 && f1.correct == 67, "f1.original == 66 && f1.correct == 67");
			Debug.Assert(f2.original == 67 && f2.correct == 67, "f2.original == 67 && f2.correct == 67");
		}

		protected override void OnUpdate()
		{
			m_BeginBarrier.Update();
			
			m_BeatEventProvider.FlushDelayedEntities();
			m_PressureEventProvider.FlushDelayedEntities();
			
			base.OnUpdate();
			
			m_BeatEventProvider.FlushDelayedEntities();
			m_PressureEventProvider.FlushDelayedEntities();
			
			m_EndBarrier.Update();
		}

		public override void SortSystemUpdateList()
		{
			base.SortSystemUpdateList();
			m_SystemsInGroup = new List<ComponentSystemBase>(1 + m_systemsToUpdate.Count + 1);
			m_SystemsInGroup.Add(m_BeginBarrier);
			m_SystemsInGroup.AddRange(m_systemsToUpdate);
			m_SystemsInGroup.Add(m_EndBarrier);
		}
	}

	[DisableAutoCreation]
	public class RhythmEngineBeginBarrier : EntityCommandBufferSystem
	{
	}
	
	[DisableAutoCreation]
	public class RhythmEngineEndBarrier : EntityCommandBufferSystem
	{
	}

	[UpdateInGroup(typeof(RhythmEngineGroup))]
	public class RhythmEngineFlowSystem : FlowRhythmEngine
	{
	}
}