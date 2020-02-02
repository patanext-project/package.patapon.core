using UnityEngine;

namespace DataScripts.Models.GameMode.Structures
{
	public abstract class MaterialPropertyBase : MonoBehaviour
	{
		public abstract void RenderOn(MaterialPropertyBlock mpb);
	}
	
	public abstract class MaterialPropertyBase<T> : MaterialPropertyBase
		where T : unmanaged
	{
		public abstract string PropertyId { get; }

		[SerializeField]
		private string overridePropertyId;

		[field: SerializeField]
		public virtual T Value { get; set; }

		public override void RenderOn(MaterialPropertyBlock mpb)
		{
			var property = string.IsNullOrEmpty(overridePropertyId) ? PropertyId : overridePropertyId;

			if (Value is float f32)
				mpb.SetFloat(property, f32);
			if (Value is int int32)
				mpb.SetInt(property, int32);
			if (Value is Color color)
				mpb.SetColor(property, color);
		}
	}
}