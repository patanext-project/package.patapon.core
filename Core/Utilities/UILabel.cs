using System;
using System.Collections.Generic;
using TMPro;

namespace PataNext.UnityCore.Utilities
{
	[Serializable]
	public class UILabel<T>
	{
		public TextMeshProUGUI component;

		private bool hasBeenSet;
		private T    currentValue;

		public T Value
		{
			get => currentValue;
			set
			{
				if (hasBeenSet && EqualityComparer<T>.Default.Equals(value, currentValue))
					return;

				hasBeenSet   = true;
				currentValue = value;

				component.SetText(Format != null ? Format(value) : value.ToString());
			}
		}

		public Func<T, string> Format { get; set; }
	}
}