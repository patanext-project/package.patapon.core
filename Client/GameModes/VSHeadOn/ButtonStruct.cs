using package.stormiumteam.shared.ecs;
using PataNext.Client.Core.DOTSxUI.Components;
using PataNext.Client.DataScripts.Interface.Popup;
using Unity.Entities;

namespace PataNext.Client.GameModes.VSHeadOn
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