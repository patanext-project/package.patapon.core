using Patapon.Mixed.GamePlay.RhythmEngine;
using Patapon.Mixed.GamePlay.Units;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using UnityEngine;

namespace Patapon.Mixed.GamePlay
{

	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	[UpdateBefore(typeof(ActionSystemGroup))]
	public class UpdateRhythmAbilityState : JobComponentSystem
	{
		[BurstDiscard]
		private static void NonBurst_ErrorNoOwnerOrNoRelative(Entity owner, Entity entity)
		{
			if (owner == default)
				Debug.LogError($"Default owner found on " + entity);
		}

		[BurstDiscard]
		private static void NonBurst_ErrorNoRhythmEngine(Entity target, Entity entity)
		{
			Debug.LogError($"No RhythmEngine found on target({target}) of ability({entity})");
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var relativeRhythmEngineFromEntity    = GetComponentDataFromEntity<Relative<RhythmEngineDescription>>(true);
			var rhythmEngineDescriptionFromEntity = GetComponentDataFromEntity<RhythmEngineDescription>(true);
			var rhythmEngineProcessFromEntity     = GetComponentDataFromEntity<FlowEngineProcess>(true);
			var rhythmCurrentCommandFromEntity    = GetComponentDataFromEntity<RhythmCurrentCommand>(true);
			var gameCommandStateFromEntity        = GetComponentDataFromEntity<GameCommandState>(true);
			var gameComboStateFromEntity          = GetComponentDataFromEntity<GameComboState>(true);

			return Entities
			       .ForEach((Entity entity, ref RhythmAbilityState abilityState, in Owner owner) =>
			       {
				       if (owner.Target == default || !relativeRhythmEngineFromEntity.Exists(owner.Target))
				       {
					       NonBurst_ErrorNoOwnerOrNoRelative(owner.Target, entity);
					       return;
				       }

				       var engine = relativeRhythmEngineFromEntity[owner.Target].Target;
				       if (!rhythmEngineDescriptionFromEntity.Exists(engine))
				       {
					       NonBurst_ErrorNoRhythmEngine(engine, entity);
					       return;
				       }

				       abilityState.Engine = engine;
				       abilityState.Calculate(rhythmCurrentCommandFromEntity[engine],
					       gameCommandStateFromEntity[engine],
					       gameComboStateFromEntity[engine],
					       rhythmEngineProcessFromEntity[engine]);
			       })
			       .WithReadOnly(relativeRhythmEngineFromEntity)
			       .WithReadOnly(rhythmEngineDescriptionFromEntity)
			       .WithReadOnly(rhythmEngineProcessFromEntity)
			       .WithReadOnly(rhythmCurrentCommandFromEntity)
			       .WithReadOnly(gameCommandStateFromEntity)
			       .WithReadOnly(gameComboStateFromEntity)
			       .Schedule(inputDeps);
		}
	}
}