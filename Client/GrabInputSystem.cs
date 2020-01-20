using System;
using System.IO;
using System.Numerics;
using Systems;
using Misc.Extensions;
using package.stormiumteam.shared.ecs;
using Patapon4TLB.Default.Player;
using StormiumTeam.GameBase.Systems;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.InputSystem;
using Vector2 = UnityEngine.Vector2;

namespace DefaultNamespace
{
	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	[UpdateAfter(typeof(UpdateInputSystem))]
	public class GrabInputSystem : BaseSyncInputSystem<GrabInputSystem.Data>
	{
		public const string AssetFileName = "global_inputs.inputactions";
		public const int    ActionLength  = 4;

		private InputAction[] m_Actions;

		private UserCommand m_LocalCommand;
		private InputAction m_PanningAction;
		private InputAction m_AbilitySelectionAction;

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
			var actions                              = m_LocalCommand.GetRhythmActions();
			foreach (ref var ac in actions) ac.flags = 0;

			foreach (var ev in InputEvents)
			{
				var targetKey = Array.IndexOf(m_Actions, ev.Context.action);
				if (targetKey != -1)
				{
					ref var ac = ref actions.AsRef(targetKey);
					ac.FrameUpdate |= ev.Phase == InputActionPhase.Started || ev.Phase == InputActionPhase.Canceled;
					ac.IsActive    =  ev.Data.IsActive;

					if (ac.WasPressed)
					{
						m_LocalCommand.LastActionIndex = targetKey;
						m_LocalCommand.LastActionFrame = ServerTick.AsUInt;
					}
				}
			}

			m_LocalCommand.Panning = m_PanningAction.ReadValue<float>();
			if (m_AbilitySelectionAction.ReadValue<Vector2>().magnitude > 0.1f)
			{
				m_LocalCommand.IsSelectingAbility = true;
				m_LocalCommand.Ability            = GetAbility(m_AbilitySelectionAction.ReadValue<Vector2>());
			}
			else
				m_LocalCommand.IsSelectingAbility = false;

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

			if (EntityManager.HasComponent<CommandInterFrame>(gamePlayer))
			{
				var interframeBuffer = EntityManager.GetBuffer<CommandInterFrame>(gamePlayer);
				interframeBuffer.Add(new CommandInterFrame {Base = m_LocalCommand});
			}
		}

		private static void update_distance(ref float distance, Vector2 curr, Vector2 target, ref AbilitySelection currentSelection, in AbilitySelection targetSelection)
		{
			var nd = Vector2.Distance(curr, target);
			if (nd < distance)
			{
				distance         = nd;
				currentSelection = targetSelection;
			}
		}

		private static AbilitySelection GetAbility(Vector2 vec)
		{
			var selection       = AbilitySelection.Horizontal;
			var nearestDistance = float.MaxValue;

			update_distance(ref nearestDistance, vec, Vector2.left, ref selection, AbilitySelection.Horizontal);
			update_distance(ref nearestDistance, vec, Vector2.right, ref selection, AbilitySelection.Horizontal);
			update_distance(ref nearestDistance, vec, Vector2.up, ref selection, AbilitySelection.Top);
			update_distance(ref nearestDistance, vec, Vector2.down, ref selection, AbilitySelection.Bottom);
			return selection;
		}

		protected override void OnAssetRefresh()
		{
			var actionPressureMap = Asset.FindActionMap("Pressures", true);
			var actionCameraMap   = Asset.FindActionMap("Camera", true);
			var actionAbilityMap  = Asset.FindActionMap("Ability", true);

			m_Actions = new InputAction[ActionLength];

			for (var i = 0; i != ActionLength; i++)
			{
				// we add +1 so it can match RhythmKeys constants
				var action = actionPressureMap.FindAction("Pressure" + (i + 1), true);
				m_Actions[i] = action;

				AddActionEvents(action);
			}

			m_PanningAction          = actionCameraMap.FindAction("Panning", true);
			m_AbilitySelectionAction = actionAbilityMap.FindAction("Selection", true);
		}

		public struct Data : IInitializeInputEventData
		{
			public bool IsActive;

			public void Initialize(InputAction.CallbackContext ctx)
			{
				IsActive = ctx.action.ReadValue<float>() > 0;
			}
		}
	}
}