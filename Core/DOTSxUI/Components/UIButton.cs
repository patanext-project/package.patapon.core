using Unity.Entities;

namespace PataNext.Client.Core.DOTSxUI.Components
{
	public struct UIButton : IComponentData
	{
		public struct ClickedEvent : IComponentData
		{
		}
	}

	public class UIButtonText : IComponentData
	{
		public string Value;
	}

	[UpdateInGroup(typeof(PresentationSystemGroup))]
	public class InteractionButtonSystemGroup : ComponentSystemGroup
	{
		private BeginInteractionButtonCommandBufferSystem m_Begin;
		private EndInteractionButtonCommandBufferSystem   m_End;

		protected override void OnCreate()
		{
			base.OnCreate();
			m_Begin = World.GetOrCreateSystem<BeginInteractionButtonCommandBufferSystem>();
			m_End   = World.GetOrCreateSystem<EndInteractionButtonCommandBufferSystem>();
		}

		protected override void OnUpdate()
		{
			m_Begin.Update();
			base.OnUpdate();
			m_End.Update();
		}
	}

	[DisableAutoCreation]
	public class BeginInteractionButtonCommandBufferSystem : EntityCommandBufferSystem
	{
	}

	[DisableAutoCreation]
	public class EndInteractionButtonCommandBufferSystem : EntityCommandBufferSystem
	{
	}
}