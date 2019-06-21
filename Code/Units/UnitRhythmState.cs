using package.patapon.core;
using Unity.Entities;

namespace Patapon4TLB.Core
{
	public struct UnitRhythmState : IComponentData
	{
		public GameComboState Combo;
	}
}