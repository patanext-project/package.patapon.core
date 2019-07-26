using System.Collections.Generic;
using package.patapon.core;
using package.stormiumteam.shared.ecs;
using Runtime.Misc;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Patapon4TLB.UI.InGame
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class ClientPresentationTransformSystemGroup : ComponentSystemGroup
	{
		private TransformSystemGroup m_Group;
		
		protected override void OnCreate()
		{
			base.OnCreate();
			m_Group = World.GetOrCreateSystem<TransformSystemGroup>();
		}

		protected override void OnUpdate()
		{
			EntityManager.CompleteAllJobs();
			m_Group.Update();
			
			base.OnUpdate();
		}
	}
}