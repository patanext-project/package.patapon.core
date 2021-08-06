using Unity.Entities;
using Unity.Mathematics;

namespace PataNext.Client.Core.DOTSxUI.Components
{
	public struct UIRect : IComponentData
	{
		public float2 SizeDelta;
	}
	
	public struct UIGrid : IComponentData {}

	public struct UIGridPosition : IComponentData
	{
		public int2 Value;
	}
	
	public struct UIFirstSelected : IComponentData {}
}