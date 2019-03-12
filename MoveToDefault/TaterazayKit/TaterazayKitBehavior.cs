using System;
using package.patapon.core;
using package.StormiumTeam.GameBase;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;

namespace Patapon4TLB.Default
{
	public struct TaterazayKitBehaviorData : IComponentData
	{
		
	}
	
	public class TaterazayKitBehaviorSystem : GameBaseSystem
	{
		private ComponentQueryBuilder m_QueryMarchBehavior;

		private Entity m_MarchCommand;

		protected override void OnCreateManager()
		{
			base.OnCreateManager();

			var cmdBuilder = World.GetOrCreateManager<FlowCommandBuilder>();

			m_QueryMarchBehavior = Entities.WithAll<ActionTag, TaterazayKitMarchAction, OwnerState<LivableDescription>>();
			m_MarchCommand = cmdBuilder.GetOrCreate(new NativeArray<FlowCommandSequence>(4, Allocator.Temp)
			{
				[0] = new FlowCommandSequence(0, FlowRhythmEngine.KeyPata),
				[1] = new FlowCommandSequence(1, FlowRhythmEngine.KeyPata),
				[2] = new FlowCommandSequence(2, FlowRhythmEngine.KeyPata),
				[3] = new FlowCommandSequence(3, FlowRhythmEngine.KeyPon)
			});
		}

		protected override void OnUpdate()
		{
			// March
			m_QueryMarchBehavior.ForEach((ref TaterazayKitMarchAction.Settings marchSettings, ref OwnerState<LivableDescription> livableOwner) =>
			{
				var livable = livableOwner.Target;

				if (!EntityManager.HasComponent<RhythmActionController>(livable))
					throw new InvalidOperationException($"Livable {livable} has no '{nameof(RhythmActionController)}'");
			});
		}
	}
}