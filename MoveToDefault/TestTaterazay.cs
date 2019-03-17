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
				typeof(UnitDirection),
				typeof(UnitBaseSettings),
				typeof(Velocity),
				typeof(ActionContainer),
				typeof(RhythmActionController)
			);

			EntityManager.SetComponentData(taterazay, new UnitDirection {Value        = 1});
			EntityManager.SetComponentData(taterazay, new UnitBaseSettings {BaseSpeed = 6});

			var marchAction = EntityManager.CreateEntity
			(
				typeof(OwnerState<LivableDescription>),
				typeof(ActionDescription),
				typeof(TaterazayKitMarchAction.Settings)
			);

			EntityManager.ReplaceOwnerData(marchAction, taterazay);
		}

		protected override void OnUpdate()
		{

		}
	}
}