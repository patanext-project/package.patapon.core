using PataNext.Client.Core.DOTSxUI.Components;
using StormiumTeam.GameBase.Roles.Components;
using Unity.Entities;

namespace PataNext.Client.DataScripts.Interface.Popup
{
	public struct SetEnableStatePopupAction : IComponentData
	{
		public Entity Popup;
		public bool Value;
	}

	[UpdateInGroup(typeof(InteractionButtonSystemGroup))]
	[AlwaysSynchronizeSystem]
	public class DisablePopupActionSystem : SystemBase
	{
		private EndInteractionButtonCommandBufferSystem m_EndBuffer;

		protected override void OnCreate()
		{
			m_EndBuffer = World.GetOrCreateSystem<EndInteractionButtonCommandBufferSystem>();
		}

		protected override void OnUpdate()
		{
			var ecb = m_EndBuffer.CreateCommandBuffer();
			var relativePopupFromEntity = GetComponentDataFromEntity<Relative<PopupDescription>>(true);
			
			Entities.WithAll<UIButton.ClickedEvent>().ForEach((Entity entity, in SetEnableStatePopupAction state) =>
			{
				var popup = state.Popup;
				if (popup == default && relativePopupFromEntity.HasComponent(entity))
					popup = relativePopupFromEntity[entity].Target; 
				
				EntityManager.SetEnabled(popup, state.Value);

				ecb.RemoveComponent<UIButton.ClickedEvent>(entity);
			}).WithStructuralChanges().Run();
		}
	}
}