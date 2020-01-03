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
	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	[UpdateAfter(typeof(UpdateInputSystem))]
	public class GrabInputSystem : BaseSyncInputSystem<GrabInputSystem.Data>
	{
		public struct Data : IInitializeInputEventData
		{
			public bool IsActive;

			public void Initialize(InputAction.CallbackContext ctx)
			{
				IsActive = ctx.action.ReadValue<float>() > 0;
			}
		}

		public const string AssetFileName = "global_inputs.inputactions";
		public const int    ActionLength  = 4;

		private InputAction[] m_Actions;
		private InputAction m_PanningAction;

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
					ref var ac = ref actions.AsRef(targetKey);
					ac.FrameUpdate |= ev.Phase == InputActionPhase.Started || ev.Phase == InputActionPhase.Canceled;
					ac.IsActive    =  ev.Data.IsActive;
				}
			}

			m_LocalCommand.Panning = m_PanningAction.ReadValue<float>(); 

			InputEvents.Clear();

			var gamePlayer = this.GetFirstSelfGamePlayer();
			if (gamePlayer == default || !EntityManager.HasComponent<UserCommand>(gamePlayer))
				return;

			var commands = EntityManager.GetBuffer<UserCommand>(gamePlayer);
			m_LocalCommand.Tick = ServerTick.AsUInt;
			commands.AddCommandData(m_LocalCommand);

			if (!EntityManager.TryGetComponentData(gamePlayer, out GamePlayerCommand command))
				return;

			command.Base = m_LocalCommand;
			EntityManager.SetComponentData(gamePlayer, command);
		}

		protected override void OnAssetRefresh()
		{
			var actionPressureMap = Asset.FindActionMap("Pressures", true);
			var actionCameraMap = Asset.FindActionMap("Camera", true);
			
			m_Actions = new InputAction[ActionLength];

			for (var i = 0; i != ActionLength; i++)
			{
				// we add +1 so it can match RhythmKeys constants
				var action = actionPressureMap.FindAction("Pressure" + (i + 1), true);
				m_Actions[i] = action;

				AddActionEvents(action);
			}

			m_PanningAction = actionCameraMap.FindAction("Panning", true);
		}
	}
}