using DataScripts.Interface.Menu.UIECS;
using DataScripts.Interface.Popup;
using package.stormiumteam.shared.ecs;
using Unity.Entities;

namespace GameModes.VSHeadOn
{
	public struct ButtonSpectate : IComponentData
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
				Entities.WithAll<UIButton.ClickedEvent>().ForEach((Entity entity, in ButtonSpectate button) =>
				{
					var reqEnt = EntityManager.CreateEntity(typeof(HeadOnSpectateRpc), typeof(SendRpcCommandRequestComponent));
					EntityManager.SetOrAddComponentData(reqEnt, new HeadOnSpectateRpc {GhostId = 0});
				}).WithStructuralChanges().Run();
			}
		}
	}
}