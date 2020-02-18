using System;
using UnityEngine;
using UnityEngine.UI;

namespace DataScripts.Interface
{
	public class ButtonPendingEventBehaviour : MonoBehaviour
	{
		[NonSerialized] public bool HasPendingClickEvent;

		private void OnEnable()
		{
			HasPendingClickEvent = false;
			GetComponent<Button>().onClick.AddListener(() => HasPendingClickEvent = true);
		}

		private void OnDisable()
		{
			GetComponent<Button>().onClick.RemoveAllListeners();
		}
	}
}