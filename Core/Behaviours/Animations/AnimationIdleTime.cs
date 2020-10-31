using Unity.Entities;

namespace PataNext.Client.Graphics.Animation.Components
{
	public struct AnimationIdleTime : IComponentData
	{
		public float Value;

		public class System : SystemBase
		{
			protected override void OnUpdate()
			{
				var dt = Time.DeltaTime;
				Entities.ForEach((ref AnimationIdleTime idleTime) =>
				{
					idleTime.Value += dt;
					if (idleTime.Value < 0)
						idleTime.Value = 0;
				}).Schedule();
			}
		}
	}
}