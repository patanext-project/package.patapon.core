using package.StormiumTeam.GameBase;
using StormiumTeam.GameBase;
using Unity.Entities;

namespace Patapon4TLB.Default
{
	public class TestTaterazay : ComponentSystem
	{
		protected override void OnStartRunning()
		{
			var taterazay = EntityManager.CreateEntity
			(
				typeof(LivableDescription),
				typeof(TaterazayKitDescription),
				typeof(TaterazayKitBehaviorData),
				typeof(ActionContainer),
				typeof(RhythmActionController)
			);

			var marchAction = EntityManager.CreateEntity
			(
				typeof(OwnerState<LivableDescription>),
				typeof(ActionDescription),
				typeof(TaterazayKitMarchAction)
			);
			
			EntityManager.ReplaceOwnerData(marchAction, taterazay);
		}

		protected override void OnUpdate()
		{
			
		}
	}
}