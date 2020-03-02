using Patapon.Mixed.GamePlay.Team;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Transforms;

namespace Patapon.Mixed.GamePlay
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	public class UpdateBlockedUnitTargetPositionSystem : SystemBase
	{
		protected override void OnUpdate()
		{
			var teamRelativeFromEntity  = GetComponentDataFromEntity<Relative<TeamDescription>>(true);
			var enemiesFromTeam         = GetBufferFromEntity<TeamEnemies>(true);
			var teamBlockAreaFromEntity = GetComponentDataFromEntity<TeamBlockMovableArea>(true);

			Entities
				//.WithAll<Relative<TeamDescription>>()
				.WithAll<UnitTargetDescription>()
				.ForEach((Entity entity, ref UnitDirection direction, ref Translation translation) =>
				{
					if (!teamRelativeFromEntity.Exists(entity))
						return;
					if (!enemiesFromTeam.Exists(teamRelativeFromEntity[entity].Target))
						return;

					var enemies = enemiesFromTeam[teamRelativeFromEntity[entity].Target];
					for (var i = 0; i != enemies.Length; i++)
					{
						if (!teamBlockAreaFromEntity.Exists(enemies[i].Target))
							continue;
						var area = teamBlockAreaFromEntity[enemies[i].Target];

						// If the new position is superior the area and the previous one inferior, teleport back to the area.
						if (translation.Value.x > area.LeftX && direction.IsRight) translation.Value.x = area.LeftX;
						if (translation.Value.x < area.RightX && direction.IsLeft) translation.Value.x = area.RightX;

						// if it's inside...
						if (translation.Value.x > area.LeftX && translation.Value.x < area.RightX)
						{
							if (direction.IsLeft)
								translation.Value = area.RightX;
							else if (direction.IsRight)
								translation.Value = area.LeftX;
						}
					}
				})
				.WithReadOnly(teamRelativeFromEntity)
				.WithReadOnly(enemiesFromTeam)
				.WithReadOnly(teamBlockAreaFromEntity)
				.Schedule();
		}
	}
}