using System;
using System.Collections.Generic;
using UnityEngine;

namespace PataNext.Client.Core.Addressables
{
	[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/StatusEffectAsset", order = 1)]
	public class StatusEffectAsset : ScriptableObject
	{
		public static List<StatusEffectAsset> AllAssets { get; } = new List<StatusEffectAsset>();

		[Serializable]
		public struct StringColor
		{
			public string key;
			public Color  value;
		}

		[Serializable]
		public struct StringIcon
		{
			public string key;
			public Sprite value;
		}

		public Color             defaultColor;
		public List<StringColor> idToColors;

		public Sprite           defaultIcon;
		public List<StringIcon> idToIcons;

		public bool TryGetColor(string key, out Color value, bool precise = false)
		{
			foreach (var kvp in idToColors)
				if (precise && kvp.key == key || key.Contains(kvp.key) || kvp.key.Contains(key))
				{
					value = kvp.value;
					return true;
				}

			value = defaultColor;
			return false;
		}

		public bool TryGetIcon(string key, out Sprite value, bool precise = false)
		{
			foreach (var kvp in idToIcons)
				if (precise && kvp.key == key || key.Contains(kvp.key) || kvp.key.Contains(key))
				{
					value = kvp.value;
					return true;
				}

			value = defaultIcon;
			return false;
		}

		private void OnEnable()
		{
			AllAssets.Add(this);
		}

		private void OnDisable()
		{
			AllAssets.Remove(this);
		}
	}
}