using System;
using PataNext.Client.Core.DOTSxUI.Components;
using Unity.Entities;

namespace PataNext.Client.Behaviors
{
	public class UIButtonActionTarget : IComponentData
	{
		public Action Value;

		public UIButtonActionTarget() {}
		public UIButtonActionTarget(Action action) => Value = action;

		[UpdateInGroup(typeof(InteractionButtonSystemGroup))]
		[AlwaysSynchronizeSystem]
		public class System : SystemBase
		{
			private EndInteractionButtonCommandBufferSystem m_EndBuffer;

			protected override void OnCreate()
			{
				m_EndBuffer = World.GetOrCreateSystem<EndInteractionButtonCommandBufferSystem>();
			}

			protected override void OnUpdate()
			{
				var ecb = m_EndBuffer.CreateCommandBuffer();

				Entities.WithAll<UIButton.ClickedEvent>().ForEach((Entity entity, in UIButtonActionTarget state) =>
				{
					state.Value();

					ecb.RemoveComponent<UIButton.ClickedEvent>(entity);
				}).WithoutBurst().Run();
			}
		}
	}
}