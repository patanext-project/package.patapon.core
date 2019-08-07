using Unity.Entities;
using Unity.Transforms;

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
}