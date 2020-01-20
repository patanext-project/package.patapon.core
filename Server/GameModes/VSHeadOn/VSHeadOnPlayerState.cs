using Unity.Entities;

namespace Patapon.Server.GameModes.VSHeadOn
{
	public struct HeadOnSpectating : IComponentData
	{
		public double SwitchDelay;
		public int UnitIndex;
	}

	public struct HeadOnPlaying : IComponentData
	{
		
	}
}