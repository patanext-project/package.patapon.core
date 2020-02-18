using System;
using StormiumTeam.GameBase;
using UnityEngine;
using UnityEngine.UI;

namespace DataScripts.Interface.Menu.ServerList
{
	public class ServerListGoBackButtonPresentation : MonoBehaviour
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