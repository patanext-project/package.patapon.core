using Revolution;
using Unity.Entities;

namespace Patapon.Mixed.GamePlay
{
	// Tag this to any projectile that was throw from a weapon.
	public struct UnitWeaponProjectile : IComponentData
	{
		public class EmptySerializer : ComponentSnapshotSystemTag<UnitWeaponProjectile>
		{
		}
	}
}