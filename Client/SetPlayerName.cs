using package.stormiumteam.shared.ecs;
using Unity.Entities;

namespace DefaultNamespace
{
	public class PlayerName : IComponentData
	{
		public LoginPhase Phase;
		public string     Value;

		public enum LoginPhase
		{
			None,
			MsId,
			DiscordName
		}
	}

	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	public class SetPlayerName : ComponentSystem
	{
		protected override void OnUpdate()
		{
			Entities.WithNone<PlayerName>().ForEach((Entity entity, ref GamePlayer gamePlayer) =>
			{
				if (gamePlayer.MasterServerId == 0)
					return;

				EntityManager.AddComponentData(entity, new PlayerName());
			});

			Entities.ForEach((Entity entity, ref GamePlayer player, PlayerName pn) =>
			{
				ResultGetUserAccountData getUserData;

				if (pn.Phase == PlayerName.LoginPhase.None)
				{
					pn.Phase = PlayerName.LoginPhase.MsId;
					if (EntityManager.TryGetComponentData(entity, out getUserData))
					{
						var id = getUserData.Login.ToString().Replace("DISCORD_", string.Empty);
						if (long.TryParse(id, out var longId))
						{
							pn.Phase = PlayerName.LoginPhase.DiscordName;
							BaseDiscordSystem.Instance.GetUser(longId, (Result result, ref User user) => { pn.Value = user.Username; });
						}
					}
					else
						pn.Value = "MasterServer#" + player.MasterServerId;
				}
				else if (pn.Phase == PlayerName.LoginPhase.MsId)
				{
					if (EntityManager.TryGetComponentData(entity, out getUserData))
					{
						var id = getUserData.Login.ToString().Replace("DISCORD_", string.Empty);
						if (long.TryParse(id, out var longId))
						{
							pn.Phase = PlayerName.LoginPhase.DiscordName;
							BaseDiscordSystem.Instance.GetUser(longId, (Result result, ref User user) => { pn.Value = user.Username; });
						}
					}
				}
			});
		}
	}
}