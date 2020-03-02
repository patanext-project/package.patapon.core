using DataScripts.Interface.Menu.UIECS;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;

namespace DataScripts.Interface.Popup
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
				if (popup == default && relativePopupFromEntity.Exists(entity))
					popup = relativePopupFromEntity[entity].Target; 
				
				EntityManager.SetEnabled(popup, state.Value);

				ecb.RemoveComponent<UIButton.ClickedEvent>(entity);
			}).WithStructuralChanges().Run();
		}
	}
}