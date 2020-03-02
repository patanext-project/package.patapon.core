using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;

namespace Patapon.Server.GameModes.VSHeadOn
{
	public struct HeadOnBlockUnitTarget : IComponentData
	{
		public bool  Enabled;
		public UTick ForceStopAt;

		[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities.Interaction))]
		public class Process : AbsGameBaseSystem
		{
			protected override void OnUpdate()
			{
				var tick = ServerTick;
				Entities.ForEach((ref UnitControllerState controller, ref Velocity vel, ref HeadOnBlockUnitTarget block, in OwnerActiveAbility activeAbility) =>
				{
					if (block.Enabled)
					{
						if (activeAbility.Active != default || block.ForceStopAt < tick)
							block.Enabled = false;
					}

					if (block.Enabled)
					{
						vel.Value.x                      = 0;
						controller.ControlOverVelocity.x = true;
					}
				}).Schedule();
			}
		}
	}
}