using System.Numerics;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace PataNext.CoreAbilities.Server
{
	public struct FearSpearProjectile : IComponentData
	{
		public const float RadiusInit      = 0.1f;
		public const float RadiusExplosion = 3.9f;

		public Vector3 Gravity;
		public bool    HasExploded;

		public class Register : RegisterGameHostComponentData<FearSpearProjectile>
		{
		}
	}
}