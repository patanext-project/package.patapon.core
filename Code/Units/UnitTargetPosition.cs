using Unity.Entities;
using Unity.Mathematics;

namespace Patapon4TLB.Core
{
	/// <summary>
	/// An unit need to have a target position when it's using rhythm command.
	/// Even if it's in MultiPlayer, the unit has a target.
	/// </summary>
	public struct UnitTargetPosition : IComponentData
	{
		public float3 Value;
	}
}