using System;
using DataScripts.Interface.Menu.UIECS;
using DataScripts.Interface.Popup;
using GameBase.Roles.Components;
using GameBase.Roles.Descriptions;
using P4TLB.MasterServer;
using package.stormiumteam.shared.ecs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace DataScripts.Interface.Menu.ServerRoom
{
	public class ButtonGoBackToPreviousMenu : IComponentData
	{
		public Type PreviousMenu;
		
		[UpdateInGroup(typeof(InteractionButtonSystemGroup))]
		[UpdateBefore(typeof(DisablePopupActionSystem))]
		[AlwaysSynchronizeSystem]
		public class Process : SystemBase
		{
			protected override void OnUpdate()
			{
				Entities.WithAll<UIButton.ClickedEvent>().ForEach((Entity entity, in ButtonGoBackToPreviousMenu goBack) =>
				{
					EntityManager.CreateEntity(typeof(RequestDisconnectFromServer));

					var clientMenuSystem = World.GetExistingSystem<ClientMenuSystem>();
					clientMenuSystem.SetMenu(goBack.PreviousMenu ?? clientMenuSystem.PreviousMenu);

					if (!EntityManager.HasComponent<SetEnableStatePopupAction>(entity))
						EntityManager.RemoveComponent(entity, typeof(UIButton.ClickedEvent));
				}).WithStructuralChanges().Run();
			}
		}
	}

	public struct ButtonChangeTeam : IComponentData
	{
		public int TeamTarget;

		[UpdateInGroup(typeof(PresentationSystemGroup))]
		[AlwaysSynchronizeSystem]
		public class Process : SystemBase
		{
			protected override void OnUpdate()
			{
				Entities.WithAll<UIButton.ClickedEvent>().ForEach((Entity entity, in ButtonChangeTeam button) =>
				{
					var request = EntityManager.CreateEntity(typeof(HeadOnChangeTeamRpc), typeof(SendRpcCommandRequestComponent));
					EntityManager.SetComponentData(request, new HeadOnChangeTeamRpc {Team = button.TeamTarget});

					EntityManager.RemoveComponent(entity, typeof(UIButton.ClickedEvent));
				}).WithStructuralChanges().Run();
			}
		}
	}

	public struct ButtonChangeKit : IComponentData
	{
		public P4OfficialKit Target;

		[UpdateInGroup(typeof(InteractionButtonSystemGroup))]
		[UpdateBefore(typeof(DisablePopupActionSystem))]
		[AlwaysSynchronizeSystem]
		public class Process : SystemBase
		{
			private LazySystem<EndInteractionButtonCommandBufferSystem> m_EndBuffer;
			private EntityQuery                                         m_UnitQuery;

			protected override void OnCreate()
			{
				base.OnCreate();

				m_UnitQuery = GetEntityQuery(typeof(UnitFormation), typeof(MasterServerP4UnitMasterServerEntity));
			}

			protected override void OnUpdate()
			{
				Entity localUnit = default;

				using (var entities = m_UnitQuery.ToEntityArray(Allocator.TempJob))
				{
					foreach (var ent in entities)
					{
						if (!EntityManager.TryGetComponentData(ent, out Relative<PlayerDescription> relativePlayer))
							continue;

						if (!EntityManager.HasComponent(relativePlayer.Target, typeof(GamePlayerLocalTag)))
							continue;

						localUnit = ent;
					}
				}

				if (localUnit == default)
					return;

				var localUnitMsId = EntityManager.GetComponentData<MasterServerP4UnitMasterServerEntity>(localUnit).UnitId;
				var ecb           = this.L(ref m_EndBuffer).CreateCommandBuffer();

				Entities.WithAll<UIButton.ClickedEvent>().ForEach((Entity entity, in ButtonChangeKit button) =>
				{
					var ent = EntityManager.CreateEntity(typeof(RequestSetUnitKit));
					EntityManager.SetComponentData(ent, new RequestSetUnitKit {KitId = button.Target, UnitId = localUnitMsId});
					
					Debug.Log("Changed kit!");

					ecb.RemoveComponent<UIButton.ClickedEvent>(entity);
				}).WithStructuralChanges().Run();
			}
		}
	}

	public struct ButtonChangeReadyState : IComponentData
	{
		public bool Value;

		[UpdateInGroup(typeof(PresentationSystemGroup))]
		[AlwaysSynchronizeSystem]
		public class Process : SystemBase
		{
			private readonly string ReadyText   = "Set Ready";
			private readonly string UnreadyText = "Unset Ready";

			protected override void OnUpdate()
			{
				Entities.WithAll<UIButton.ClickedEvent>().ForEach((Entity entity, ref ButtonChangeReadyState button) =>
				{
					button.Value = !button.Value;

					var request = EntityManager.CreateEntity(typeof(PlayerSetReadyRpc), typeof(SendRpcCommandRequestComponent));
					EntityManager.SetComponentData(request, new PlayerSetReadyRpc {Value = button.Value});

					EntityManager.RemoveComponent(entity, typeof(UIButton.ClickedEvent));
				}).WithStructuralChanges().Run();

				Entities.ForEach((UIButtonText text, ref ButtonChangeReadyState button) =>
				{
					text.Value = button.Value
						? UnreadyText
						: ReadyText;
				}).WithoutBurst().Run();

				if (HasSingleton<GameModeHudSettings>() && !GetSingleton<GameModeHudSettings>().EnablePreMatchInterface)
					Entities.ForEach((ref ButtonChangeReadyState button) => button.Value = false).Run();
			}
		}
	}
}