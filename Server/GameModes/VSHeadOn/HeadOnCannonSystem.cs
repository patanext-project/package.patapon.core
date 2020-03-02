using System;
using Systems.GamePlay.CYari;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Patapon.Server.GameModes.VSHeadOn
{
	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities.Interaction))]
	public class HeadOnCannonSystem : AbsGameBaseSystem
	{
		private LazySystem<CannonProjectile.Provider> m_ProviderSystem;

		protected override void OnUpdate()
		{
			var addQueue = m_ProviderSystem.Get(World).GetEntityDelayedStream()
			                               .AsParallelWriter();

			var tick                   = ServerTick;
			var relativeTeamFromEntity = GetComponentDataFromEntity<Relative<TeamDescription>>();
			var directionFromEntity    = GetComponentDataFromEntity<UnitDirection>(true);

			var rand = new Random((uint) Environment.TickCount);
			Entities
				.ForEach((Entity ent, ref HeadOnCannon cannon, ref LivableHealth health, in DynamicBuffer<HeadOnCannon.Launch> launchBuffer, in LocalToWorld ltw, in Owner owner) =>
				{
					var currentTeam = relativeTeamFromEntity[ent];
					if (currentTeam.Target != relativeTeamFromEntity[owner.Target].Target)
					{
						currentTeam                 = relativeTeamFromEntity[owner.Target];
						relativeTeamFromEntity[ent] = currentTeam;
					}

					if (currentTeam.Target == Entity.Null)
						return;

					if (health.ShouldBeDead())
					{
						health.IsDead = true;
						cannon.Active = false;
					}

					if (!cannon.Active || health.IsDead)
						return;

					rand.state += (uint) ent.Index;

					if (cannon.NextShootTick <= tick)
					{
						cannon.NextShootTick = UTick.AddMs(tick, (int) (cannon.ShootPerSecond * 1000));

						var launch = launchBuffer[cannon.Cycle];
						launch.velocity.x *= directionFromEntity[currentTeam.Target].Value;

						var startPos = ltw.Position;
						var offset   = cannon.ShootOffset;
						offset.x *= directionFromEntity[currentTeam.Target].Value;
						startPos += new float3(offset, 0);

						addQueue.Enqueue(new CannonProjectile.Create
						{
							Owner       = ent,
							Position    = startPos,
							Velocity    = new float3(launch.velocity + 0.5f * rand.NextFloat(), 0),
							Gravity     = new float3(cannon.Gravity, 0),
							StartDamage = 20
						});

						cannon.Cycle++;
						if (cannon.Cycle >= launchBuffer.Length)
							cannon.Cycle = 0;
					}
				})
				.WithNativeDisableParallelForRestriction(relativeTeamFromEntity)
				.WithReadOnly(directionFromEntity)
				.Schedule();
		}
	}
}