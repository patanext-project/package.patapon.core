using Unity.Entities;

namespace PataNext.Client.Core.DOTSxUI.Components
{
	public struct UIScreenParent : IComponentData
	{

	}

	public struct UIScreen : IComponentData
	{
		public struct WantToQuit : IComponentData
		{
		}

		public struct Quitting : IComponentData
		{
			public struct Finalize : IComponentData
			{
			}
		}
	}
}