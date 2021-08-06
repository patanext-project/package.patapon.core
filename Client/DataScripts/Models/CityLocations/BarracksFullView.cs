using PataNext.UnityCore.Utilities;
using UnityEngine;

namespace PataNext.Client.DataScripts.Models.CityLocations
{
	public class BarracksFullView : MonoBehaviour
	{
		public BarracksSquadView[] squads;

		public Transform              centerTransform;
		public GameObjectSwitchEnable focusUnitSwitch;
		public GameObjectSwitchEnable focusArmySwitch;

		public int FocusIndex    { get; set; }
		public int SelectedIndex { get; set; } = -1;
	}
}