using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.Shared;
using Unity.Entities;

namespace PataNext.Client.DataScripts.Interface.GameMode.Global
{
	public delegate bool GameModeStatusFindTranslation(ref string str);

	public delegate void GameModeModifyStatus(ref GameModeHudSettings hud, Entity spectated);

	public struct GameModeStatusUpdate : IComponentData
	{
		public GameModeHudSettings Hud;
		public Entity              Spectated;
	}

	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class GameModeStatusOnUpdate : AbsGameBaseSystem
	{
		public event GameModeStatusFindTranslation OnFindTranslation;
		public event GameModeModifyStatus          OnModifyStatus;

		private LocalizationSystem m_LocalizationSystem;
		private EntityQuery        m_UpdateQuery;

		private Localization m_Local;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_LocalizationSystem = World.GetOrCreateSystem<LocalizationSystem>();
			m_Local = m_LocalizationSystem.LoadLocal("ingame_gamemode");

			m_UpdateQuery = GetEntityQuery(typeof(GameModeStatusUpdate));
			RequireSingletonForUpdate<GameModeHudSettings>();
		}

		private uint m_LastStatusTick;

		protected override void OnUpdate()
		{
			EntityManager.DestroyEntity(m_UpdateQuery);

			var settings = GetSingleton<GameModeHudSettings>();
			if (settings.StatusTick == m_LastStatusTick)
				return;

			var cameraState = this.GetComputedCameraState().StateData;
			if (cameraState.Target != default)
			{
				var str = settings.StatusMessage.ToString();
				if (TL.IsFormatted(str) && (OnFindTranslation == null || !OnFindTranslation.Invoke(ref str)))
				{
					var (header, content) = TL.From(str);
					str = m_Local[content, header];
					if (settings.StatusMessageArg0.LengthInBytes > 0
					    && settings.StatusMessageArg1.LengthInBytes > 0)
					{
						str = string.Format(str, settings.StatusMessageArg0, settings.StatusMessageArg1);
					}
					else if (settings.StatusMessageArg0.LengthInBytes > 0)
					{
						str = string.Format(str, settings.StatusMessageArg0);
					}

					settings.StatusMessage = str;
				}

				OnModifyStatus?.Invoke(ref settings, cameraState.Target);
				m_LastStatusTick = settings.StatusTick;

				EntityManager.SetComponentData(EntityManager.CreateEntity(typeof(GameModeStatusUpdate)), new GameModeStatusUpdate
				{
					Hud = settings, Spectated = cameraState.Target
				});
			}
		}

		protected override void OnStopRunning()
		{
			base.OnStopRunning();
			EntityManager.DestroyEntity(m_UpdateQuery);
		}
	}
}