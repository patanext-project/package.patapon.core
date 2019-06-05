using JetBrains.Annotations;
using StormiumTeam.GameBase;
using Unity.Entities;

namespace Patapon4TLB.Default
{
	[UpdateInGroup(typeof(RhythmEngineGroup))]
	[UsedImplicitly]
	public class RhythmEngineCheckCommandValidity : GameBaseSystem
	{
		protected override void OnUpdate()
		{
		}
	}
}