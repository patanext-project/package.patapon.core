using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay.RhythmEngine;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Transforms;

namespace Patapon.Mixed.GamePlay
{
	[UpdateInGroup(typeof(UnitInitStateSystemGroup))]
	public class UnitCalculatePlayStateSystem : JobComponentSystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var rhythmEngineRelativeFromEntity = GetComponentDataFromEntity<Relative<RhythmEngineDescription>>(true);
			var comboStateFromEntity           = GetComponentDataFromEntity<GameComboState>(true);

			inputDeps = Entities
			            .ForEach((Entity entity, ref UnitPlayState state, in UnitStatistics original) =>
			            {
				            GameComboState comboState = default;
				            if (rhythmEngineRelativeFromEntity.TryGet(entity, out var engineRelative))
					            comboState = comboStateFromEntity[engineRelative.Target];

				            state.MovementSpeed       = comboState.IsFever ? original.FeverWalkSpeed : original.BaseWalkSpeed;
				            state.AttackSpeed         = original.AttackSpeed;
				            state.MovementAttackSpeed = original.MovementAttackSpeed;
				            state.AttackSeekRange     = original.AttackSeekRange;
				            state.Weight              = original.Weight;
			            })
			            .WithReadOnly(rhythmEngineRelativeFromEntity)
			            .WithReadOnly(comboStateFromEntity)
			            .Schedule(inputDeps);

			return inputDeps;
		}
	}

	[UpdateInGroup(typeof(UnitInitStateSystemGroup))]
	[UpdateAfter(typeof(UnitCalculatePlayStateSystem))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	public class UnitCalculateSeekingSystem : JobComponentSystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var enemiesFromTeam = GetBufferFromEntity<TeamEnemies>(true);
			var seekEnemies     = new SeekEnemies(this);

			var translationFromEntity    = GetComponentDataFromEntity<Translation>(true);
			var relativeTeamFromEntity   = GetComponentDataFromEntity<Relative<TeamDescription>>(true);
			var relativeTargetFromEntity = GetComponentDataFromEntity<Relative<UnitTargetDescription>>(true);
			var ownerFromEntity          = GetComponentDataFromEntity<Owner>(true);

			inputDeps = Entities
			            .ForEach((Entity entity, ref UnitPlayState state, ref UnitEnemySeekingState seekingState) =>
			            {
				            ownerFromEntity.TryGet(entity, out var owner);

				            Relative<TeamDescription> relativeTeam;
				            if (!relativeTeamFromEntity.TryGet(entity, out relativeTeam))
					            if (!relativeTeamFromEntity.TryGet(owner.Target, out relativeTeam))
						            return;

				            Relative<UnitTargetDescription> relativeTarget;
				            if (!relativeTargetFromEntity.TryGet(entity, out relativeTarget))
					            if (!relativeTargetFromEntity.TryGet(owner.Target, out relativeTarget))
						            return;

				            var teamEnemies = enemiesFromTeam[relativeTeam.Target];
				            var allEnemies  = new NativeList<Entity>(Allocator.Temp);
				            seekEnemies.GetAllEnemies(ref allEnemies, teamEnemies);
				            seekEnemies.SeekNearest
				            (
					            translationFromEntity[relativeTarget.Target].Value, state.AttackSeekRange, allEnemies,
					            out var nearestEnemy, out var targetPosition, out var enemyDistance
				            );

				            seekingState.Enemy    = nearestEnemy;
				            seekingState.Distance = enemyDistance;
			            })
			            .WithReadOnly(enemiesFromTeam)
			            .WithReadOnly(translationFromEntity)
			            .WithReadOnly(relativeTeamFromEntity)
			            .WithReadOnly(relativeTargetFromEntity)
			            .WithReadOnly(ownerFromEntity)
			            .Schedule(inputDeps);

			return inputDeps;
		}
	}
}