using System;
using System.IO;
using Systems;
using Misc.Extensions;
using package.stormiumteam.shared.ecs;
using Patapon4TLB.Default.Player;
using StormiumTeam.GameBase.Systems;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DefaultNamespace
{
	[UpdateInGroup(typeof(ClientInitializationSystemGroup))]
	[UpdateAfter(typeof(UpdateInputSystem))]
	public class GrabInputSystem : BaseSyncInputSystem
	{
		public const string AssetFileName = "input_pressurekeys.inputactions";
		public const int    ActionLength  = 4;

		private InputAction[] m_Actions;

		private    UserCommand m_LocalCommand;
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

		protected override void OnUpdate()
		{
			var actions = m_LocalCommand.GetRhythmActions();
			foreach (ref var ac in actions)
			{
				ac.flags = 0;
			}

			foreach (var ev in InputEvents)
			{
				var targetKey = Array.IndexOf(m_Actions, ev.Context.action);
				if (targetKey != -1)
				{
					ref var ac = ref actions[targetKey];
					ac.FrameUpdate |= ev.Phase == InputActionPhase.Started || ev.Phase == InputActionPhase.Canceled;
					ac.IsActive    =  ev.Phase == InputActionPhase.Started || ev.Phase == InputActionPhase.Performed;
				}
			}

			InputEvents.Clear();
			
			var gamePlayer = this.GetFirstSelfGamePlayer();
			if (gamePlayer == default || !EntityManager.HasComponent<UserCommand>(gamePlayer))
				return;

			var commands = EntityManager.GetBuffer<UserCommand>(gamePlayer);
			m_LocalCommand.Tick = ServerTick.AsUInt;
			commands.Add(m_LocalCommand);
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