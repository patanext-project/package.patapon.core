using Unity.Entities;

namespace Patapon4TLB.Default
{
	public struct FormationRoot : IComponentData
	{
	}

	public struct InFormation : IComponentData
	{
		public Entity Root;
	}

	public struct FormationChild : IBufferElementData
	{
		public Entity Value;
	}

	public struct FormationParent : IComponentData
	{
		public Entity Value;
	}

	public struct ArmyFormation : IComponentData
	{
		// useless no?
		public int MasterServerId;
	}

	public struct UnitFormation : IComponentData
	{
		public int MasterServerId;
	}

	// indicate if this formation is used in-game
	public struct GameFormationTag : IComponentData
	{
	}

	public struct FormationTeam : IComponentData
	{
		public int TeamIndex;
	}
}