using PataNext.Client.Graphics.Animation.Units.Base;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using Unity.Entities;

namespace PataNext.Client.Components.Archetypes
{
	public class DefaultUnitVisualPresentation : UnitVisualPresentation
	{
		public override void UpdateData()
		{
		}
		
		[UpdateInGroup(typeof(OrderGroup.Presentation.AfterSimulation))]
		public class LocalSystem : AbsGameBaseSystem
		{
			protected override void OnUpdate()
			{
				Entities.ForEach((DefaultUnitVisualPresentation presentation) =>
				{
					presentation.OnSystemUpdate();
				}).WithStructuralChanges().Run();
			}
		}
	}
}