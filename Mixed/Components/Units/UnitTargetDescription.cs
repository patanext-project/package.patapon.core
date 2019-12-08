using Patapon.Mixed.Units;
using Revolution;
using StormiumTeam.GameBase;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(Relative<UnitTargetDescription>))]

namespace Patapon.Mixed.Units
{
	/// <summary>
	/// An unit need to have a target position and offset when it's using rhythm command.
	/// Even if it's in MultiPlayer, the unit has a target.
	/// </summary>
	public struct UnitTargetOffset : IComponentData
	{
		public float Value;
	}

	/// <summary>
	/// An unit that can control the target position.
	/// It may be a kacheek, simple unit, hero or hatapon controlling it.
	/// </summary>
	public struct UnitTargetControlTag : IComponentData
	{

	}

	public struct UnitTargetDescription : IEntityDescription
	{
		public struct Exclude : IComponentData
		{
		}

		public class SynchronizeSnapshot : ComponentSnapshotSystemEmpty<UnitTargetDescription>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}
}