using Unity.Entities;

namespace Patapon4TLB.Core.MasterServer.P4.EntityDescription
{
	public struct MasterServerP4UnitMasterServerEntity : IMasterServerEntityDescription<MasterServerP4UnitMasterServerEntity>
	{
		public ulong UnitId;

		public bool Equals(MasterServerP4UnitMasterServerEntity other)
		{
			return UnitId == other.UnitId;
		}

		public override bool Equals(object obj)
		{
			return obj is MasterServerP4UnitMasterServerEntity other && Equals(other);
		}

		public override int GetHashCode()
		{
			return UnitId.GetHashCode();
		}
	}
}