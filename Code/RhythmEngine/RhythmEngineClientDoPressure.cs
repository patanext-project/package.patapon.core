using System;
using System.IO;
using StormiumTeam.GameBase.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Patapon4TLB.Default
{
	[UpdateInGroup(typeof(RhythmEngineGroup))]
	public class RhythmEngineClientDoPressure : JobSyncInputSystem
	{
		private struct SendRpcEvent : IJobForEachWithEntity<NetworkIdComponent>
		{
			public RhythmRpcPressure           PressureEvent;
			public RpcQueue<RhythmRpcPressure> RpcQueue;

			[NativeDisableParallelForRestriction]
			public BufferFromEntity<OutgoingRpcDataStreamBufferComponent> OutgoingDataBufferFromEntity;

			public void Execute(Entity entity, int index, ref NetworkIdComponent networkIdComponent)
			{
				RpcQueue.Schedule(OutgoingDataBufferFromEntity[entity], PressureEvent);
			}
		}

		public const string AssetFileName = "input_pressurekeys.inputactions";
		public const int    ActionLength  = 4;

		public InputAction[] m_Actions;

		protected override void OnCreate()
		{
			base.OnCreate();

			if (RemoveFromServerWorld())
				return;

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

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			// this will happen if the client didn't pressed any keys (or if it's not a client at all)
			if (InputEvents.Count < 0)
				return inputDeps;

			var pressureEvent = new RhythmRpcPressure {Key = -1};
			foreach (var ev in InputEvents)
			{
				pressureEvent = new RhythmRpcPressure
				{
					Beat = -1, // right now, we can't get the beat easily
					Key  = Array.IndexOf(m_Actions, ev.action) + 1 // match RhythmKeys
				};
			}

			InputEvents.Clear();

			if (pressureEvent.Key < 0)
				return inputDeps;

			var rpcQueue = World.GetExistingSystem<P4ExperimentRpcSystem>().GetRpcQueue<RhythmRpcPressure>();

			inputDeps = new SendRpcEvent
			{
				PressureEvent                = pressureEvent,
				RpcQueue                     = rpcQueue,
				OutgoingDataBufferFromEntity = GetBufferFromEntity<OutgoingRpcDataStreamBufferComponent>()
			}.Schedule(this, inputDeps);

			return inputDeps;
		}

		protected override void OnAssetRefresh()
		{
			var actionMap = Asset.GetActionMap("Pressures");
			if (actionMap == null)
			{
				Debug.LogError("Remaking the actionmap...");

				// todo: remake the action map (and maybe save it to the file?
				return;
			}

			m_Actions = new InputAction[ActionLength];
			for (var i = 0; i != ActionLength; i++)
			{
				// we add +1 so it can match RhythmKeys constants
				var action = actionMap.GetAction("Pressure" + (i + 1));
				m_Actions[i] = action;

				action.performed += InputActionEvent;
			}
		}
	}
}