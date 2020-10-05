using System.Numerics;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace PataNext.Module.Simulation.Components.GamePlay.Units
{
	public struct UnitEnemySeekingState : IComponentData
	{
		public Entity Enemy;
		public float  Distance;

		public Entity  SelfEnemy;
		public Vector3 SelfPosition;
		public float   SelfDistance;

		public class Register : RegisterGameHostComponentData<UnitEnemySeekingState>
		{
		}
	}
}