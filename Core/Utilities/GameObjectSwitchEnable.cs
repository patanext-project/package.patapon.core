using System;
using UnityEngine;

namespace PataNext.UnityCore.Utilities
{
	[Serializable]
	public class GameObjectSwitchEnable
	{
		private bool active;
		
		public GameObject target;

		public bool LastActiveState
		{
			get;
			set;
		}
		
		public bool Active
		{
			get => active;
			set
			{
				LastActiveState = value;
				
				if (active == value)
					return;

				active = value;
				target.SetActive(active);
			}
		}

		public void ForceActive(bool enable, bool followUp = true)
		{
			target.SetActive(enable);
			if (followUp)
				active = enable;
		}

		private void OnEnable()
		{
			active = target.activeSelf;
		}

		public static implicit operator GameObject(GameObjectSwitchEnable switchEnable)
		{
			return switchEnable.target;
		}
	}
}