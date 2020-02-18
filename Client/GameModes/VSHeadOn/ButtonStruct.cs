using DataScripts.Interface.Menu.UIECS;
using DataScripts.Interface.Popup;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes.VSHeadOn;
using Rpc;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace GameModes.VSHeadOn
{
	public struct ButtonSpectate : IComponentData
	{
		public int Id;

		[UpdateInGroup(typeof(InteractionButtonSystemGroup))]
		[UpdateBefore(typeof(DisablePopupActionSystem))]
		[AlwaysSynchronizeSystem]
		public class Process : JobComponentSystem
		{
			private LazySystem<EndInteractionButtonCommandBufferSystem> m_EndBuffer;

			protected override JobHandle OnUpdate(JobHandle inputDeps)
			{
				Entities.WithAll<UIButton.ClickedEvent>().ForEach((Entity entity, in ButtonSpectate button) =>
				{
					var reqEnt = EntityManager.CreateEntity(typeof(HeadOnSpectateRpc), typeof(SendRpcCommandRequestComponent));
					EntityManager.SetOrAddComponentData(reqEnt, new HeadOnSpectateRpc {GhostId = 0});
				}).WithStructuralChanges().Run();

				return default;
			}
		}
	}
}