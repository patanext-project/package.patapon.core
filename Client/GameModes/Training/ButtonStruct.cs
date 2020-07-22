using DataScripts.Interface.Menu.UIECS;
using DataScripts.Interface.Popup;
using package.stormiumteam.shared.ecs;
using Unity.Entities;

namespace Patapon4TLB.GameModes.Training
{
	public struct ButtonChangeKit : IComponentData
	{
		public int Id;

		[UpdateInGroup(typeof(InteractionButtonSystemGroup))]
		[UpdateBefore(typeof(DisablePopupActionSystem))]
		[AlwaysSynchronizeSystem]
		public class Process : SystemBase
		{
			private LazySystem<EndInteractionButtonCommandBufferSystem> m_EndBuffer;

			protected override void OnUpdate()
			{
				Entities.WithAll<UIButton.ClickedEvent>().ForEach((Entity entity, in ButtonChangeKit button) =>
				{
					var reqEnt = EntityManager.CreateEntity(typeof(TrainingRoomSetKit), typeof(SendRpcCommandRequestComponent));
					EntityManager.SetOrAddComponentData(reqEnt, new TrainingRoomSetKit {KitId = button.Id});
				}).WithStructuralChanges().Run();
			}
		}
	}
}