using System;
using DataScripts.Interface.Menu.UIECS;
using DataScripts.Interface.Popup;
using P4TLB.MasterServer;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes;
using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon.Mixed.Units;
using Patapon4TLB.Core.MasterServer.Data;
using Patapon4TLB.Core.MasterServer.P4.EntityDescription;
using Patapon4TLB.Default;
using Rpc;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using UnityEngine;

namespace DataScripts.Interface.Menu.ServerRoom
{
	public class ButtonGoBackToPreviousMenu : IComponentData
	{
		public Type PreviousMenu;
		
		[UpdateInGroup(typeof(PresentationSystemGroup))]
		[AlwaysSynchronizeSystem]
		public class Process : JobComponentSystem
		{
			protected override JobHandle OnUpdate(JobHandle inputDeps)
			{
				Entities.WithAll<UIButton.ClickedEvent>().ForEach((Entity entity, in ButtonGoBackToPreviousMenu goBack) =>
				{
					EntityManager.CreateEntity(typeof(RequestDisconnectFromServer));

					var clientMenuSystem = World.GetExistingSystem<ClientMenuSystem>();
					clientMenuSystem.SetMenu(goBack.PreviousMenu ?? clientMenuSystem.PreviousMenu);

					EntityManager.RemoveComponent(entity, typeof(UIButton.ClickedEvent));
				}).WithStructuralChanges().Run();

				return default;
			}
		}
	}

	public struct ButtonChangeTeam : IComponentData
	{
		public int TeamTarget;

		[UpdateInGroup(typeof(PresentationSystemGroup))]
		[AlwaysSynchronizeSystem]
		public class Process : JobComponentSystem
		{
			protected override JobHandle OnUpdate(JobHandle inputDeps)
			{
				Entities.WithAll<UIButton.ClickedEvent>().ForEach((Entity entity, in ButtonChangeTeam button) =>
				{
					var request = EntityManager.CreateEntity(typeof(HeadOnChangeTeamRpc), typeof(SendRpcCommandRequestComponent));
					EntityManager.SetComponentData(request, new HeadOnChangeTeamRpc {Team = button.TeamTarget});

					EntityManager.RemoveComponent(entity, typeof(UIButton.ClickedEvent));
				}).WithStructuralChanges().Run();

				return default;
			}
		}
	}

	public struct ButtonChangeKit : IComponentData
	{
		public P4OfficialKit Target;

		[UpdateInGroup(typeof(InteractionButtonSystemGroup))]
		[UpdateBefore(typeof(DisablePopupActionSystem))]
		[AlwaysSynchronizeSystem]
		public class Process : JobComponentSystem
		{
			private LazySystem<EndInteractionButtonCommandBufferSystem> m_EndBuffer;
			private EntityQuery                                         m_UnitQuery;

			protected override void OnCreate()
			{
				base.OnCreate();

				m_UnitQuery = GetEntityQuery(typeof(UnitFormation), typeof(MasterServerP4UnitMasterServerEntity));
			}

			protected override JobHandle OnUpdate(JobHandle inputDeps)
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
					return default;

				var localUnitMsId = EntityManager.GetComponentData<MasterServerP4UnitMasterServerEntity>(localUnit).UnitId;
				var ecb           = this.L(ref m_EndBuffer).CreateCommandBuffer();

				Entities.WithAll<UIButton.ClickedEvent>().ForEach((Entity entity, in ButtonChangeKit button) =>
				{
					var ent = EntityManager.CreateEntity(typeof(RequestSetUnitKit));
					EntityManager.SetComponentData(ent, new RequestSetUnitKit {KitId = button.Target, UnitId = localUnitMsId});
					
					Debug.Log("Changed kit!");

					ecb.RemoveComponent<UIButton.ClickedEvent>(entity);
				}).WithStructuralChanges().Run();

				return default;
			}
		}
	}

	public struct ButtonChangeReadyState : IComponentData
	{
		public bool Value;

		[UpdateInGroup(typeof(PresentationSystemGroup))]
		[AlwaysSynchronizeSystem]
		public class Process : JobComponentSystem
		{
			private readonly string ReadyText   = "Set Ready";
			private readonly string UnreadyText = "Unset Ready";

			protected override JobHandle OnUpdate(JobHandle inputDeps)
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
				{
					Entities.ForEach((ref ButtonChangeReadyState button) => button.Value = false).Run();
				}

				return default;
			}
		}
	}
}