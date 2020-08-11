using System;
using GameHost.InputBackendFeature.BaseSystems;
using GameHost.InputBackendFeature.Interfaces;
using GameHost.InputBackendFeature.Layouts;
using GameHost.Inputs.DefaultActions;
using RevolutionSnapshot.Core.Buffers;
using Unity.Collections;
using UnityEngine.InputSystem.Controls;
using Unity.Entities;

namespace PataNext.Game.Inputs.Actions
{
	public struct RhythmInputAction : IInputAction
	{
		// use the same layout as the PressAction one
		public class Layout : PressAction.Layout
		{
			public Layout(string id, params CInput[] inputs) : base(id, inputs)
			{
			}
		}

		public uint DownCount, UpCount;
		public bool Active;

		public TimeSpan ActiveTime;

		public bool HasBeenPressed => DownCount > 0;
		public bool IsSliding      => ActiveTime.TotalSeconds > 0.4;

		public class System : InputActionSystemBase<RhythmInputAction, Layout>
		{
			protected override void OnUpdate()
			{
				var currentLayout = EntityManager.GetComponentData<InputCurrentLayout>(GetSingletonEntity<InputCurrentLayout>());

				foreach (var entity in InputQuery.ToEntityArray(Allocator.Temp))
				{
					var layouts = GetLayouts(entity);
					if (!layouts.TryGetOrDefault(currentLayout.Id, out var layout))
						return;

					var action = EntityManager.GetComponentData<RhythmInputAction>(entity);
					action.DownCount = 0;
					action.UpCount   = 0;
					action.Active    = false;

					for (var i = 0; i < layout.Inputs.Count; i++)
					{
						var input = layout.Inputs[i];
						if (Backend.GetInputControl(input.Target) is ButtonControl buttonControl)
						{
							action.DownCount += buttonControl.wasPressedThisFrame ? 1u : 0;
							action.UpCount   += buttonControl.wasReleasedThisFrame ? 1u : 0;
							action.Active    |= buttonControl.isPressed;
						}
					}

					if (action.Active)
						action.ActiveTime += TimeSpan.FromSeconds(Time.DeltaTime);
					else
						action.ActiveTime = TimeSpan.Zero;

					EntityManager.SetComponentData(entity, action);
				}
			}
		}

		public void Serialize(ref DataBufferWriter buffer)
		{
			buffer.WriteValue(DownCount);
			buffer.WriteValue(UpCount);
			buffer.WriteValue(Active);
			buffer.WriteValue(ActiveTime);
		}

		public void Deserialize(ref DataBufferReader buffer)
		{
			DownCount  = buffer.ReadValue<uint>();
			UpCount    = buffer.ReadValue<uint>();
			Active     = buffer.ReadValue<bool>();
			ActiveTime = buffer.ReadValue<TimeSpan>();
		}
	}
}