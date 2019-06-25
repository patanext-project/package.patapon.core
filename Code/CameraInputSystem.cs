using System.IO;
using StormiumTeam.GameBase.Systems;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Patapon4TLB.Core
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class CameraInputSystem : BaseSyncInputSystem
	{
		public const string      AssetFileName = "input_camera.inputactions";
		public       InputAction PanAction;

		public float CurrentPanning { get; private set; }

		protected override void OnCreate()
		{
			base.OnCreate();

			var path = Application.streamingAssetsPath + "/" + AssetFileName;
			if (File.Exists(path))
			{
				var asset = ScriptableObject.CreateInstance<InputActionAsset>();
				asset.LoadFromJson(File.ReadAllText(path));

				Refresh(asset);
			}
			else
			{
				Debug.LogError($"The file '{path}' don't exist.");
			}
		}

		protected override void OnAssetRefresh()
		{
			var actionMap = Asset.GetActionMap("Camera");
			if (actionMap == null)
			{
				Debug.LogError("Remaking the actionmap...");

				// todo: remake the action map (and maybe save it to the file?
				return;
			}

			PanAction = actionMap.GetAction("Panning");
			AddActionEvents(PanAction);
		}

		protected override void OnUpdate()
		{
			foreach (var ev in InputEvents)
			{
				CurrentPanning = ev.ReadValue<float>();
			}
			InputEvents.Clear();
		}
	}
}