using DataScripts.Interface.Menu.UIECS;
using DataScripts.Interface.Popup;
using P4TLB.MasterServer;
using package.stormiumteam.shared.ecs;
using Patapon4TLB.Core.MasterServer.Data;
using Patapon4TLB.Core.MasterServer.P4.EntityDescription;
using Patapon4TLB.Default;
using Rpc;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

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