using System;
using System.IO;
using Patapon4TLB.Default.Player;
using StormiumTeam.GameBase.Systems;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DefaultNamespace
{
	public class GrabInputSystem : JobSyncInputSystem
	{
		public const string AssetFileName = "input_pressurekeys.inputactions";
		public const int    ActionLength  = 4;

		private InputAction[] m_Actions;

		private UserCommand m_LocalCommand;
		public ref UserCommand LocalCommand => ref m_LocalCommand;
		
		protected override void OnCreate()
		{
			base.OnCreate();
			
			var path = Application.streamingAssetsPath + "/" + AssetFileName;
			if (File.Exists(path))
			{
				var asset = ScriptableObject.CreateInstance<InputActionAsset>();
				asset.LoadFromJson(File.ReadAllText(path));

				try
				{
					Refresh(asset);
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
					Application.Quit();
				}
			}
			else
			{
				Debug.LogError($"The file '{path}' don't exist.");
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var actions = m_LocalCommand.GetRhythmActions();
			foreach (var ev in InputEvents)
			{
				var targetKey = Array.IndexOf(m_Actions, ev.action);
				if (targetKey != -1)
				{
					ref var ac = ref actions[targetKey];
					ac.FrameUpdate = ev.started || ev.canceled;
					ac.IsActive    = ev.started || ev.performed;
				}
			}

			InputEvents.Clear();

			return inputDeps;
		}

		protected override void OnAssetRefresh()
		{
			var actionMap = Asset.FindActionMap("Pressures", true);
			
			m_Actions = new InputAction[ActionLength];
			
			for (var i = 0; i != ActionLength; i++)
			{
				// we add +1 so it can match RhythmKeys constants
				var action = actionMap.FindAction("Pressure" + (i + 1), true);
				m_Actions[i] = action;

				AddActionEvents(action);
			}
		}
	}
}