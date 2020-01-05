using Discord;
using StormiumTeam.GameBase.External.Discord;
using UnityEngine;

namespace Patapon4TLB.Core
{
	public class P4DiscordSystem : BaseDiscordSystem
	{
		protected override void OnCreate()
		{
			base.OnCreate();

			Push(default);
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			if (!IsUserReady)
				return;
		}

		protected override long  ClientId                   => 609427243395055616;
		public             bool  IsConnectionLobbyRequested { get; set; }
		public             Lobby ConnectionLobby            { get; set; }

		public void TestLobby()
		{
			var d           = GetDiscord();
			var transaction = d.GetLobbyManager().GetLobbyCreateTransaction();
			transaction.SetType(LobbyType.Public);
			d.GetLobbyManager().CreateLobby(transaction, (Result result, ref Lobby lobby) =>
			{
				d.GetLobbyManager().Search(d.GetLobbyManager().GetSearchQuery(), result1 =>
				{
					var count = d.GetLobbyManager().LobbyCount();
					Debug.Log("lobby count: " + count);
					for (var i = 0; i != count; i++)
					{
						var l = d.GetLobbyManager().GetLobby(d.GetLobbyManager().GetLobbyId(i));
						Debug.Log($"{l.Id}, {l.Capacity}, {l.OwnerId}, {l.Secret}");
					}
				});
			});
		}

		public void CreateConnectionLobby()
		{
			IsConnectionLobbyRequested = true;
			var lobbyManager = GetDiscord().GetLobbyManager();
			var transaction  = lobbyManager.GetLobbyCreateTransaction();
			transaction.SetCapacity(2);
			transaction.SetMetadata($"{GetLocalUser().Id}", "0");
			transaction.SetType(LobbyType.Public);
			transaction.SetLocked(false);
			
			Debug.Log($"{GetLocalUser().Id} \"flag\"");

			lobbyManager.CreateLobby(transaction, (Result result, ref Lobby lobby) =>
			{
				IsConnectionLobbyRequested = false;
				ConnectionLobby            = lobby;
				Debug.Log("Connection lobby created!");
			});
		}

		public void DeleteConnectionLobby()
		{
			GetDiscord()
				.GetLobbyManager()
				.DeleteLobby(ConnectionLobby.Id, result => { Debug.Log("Connection lobby result deletion: " + result); });
			ConnectionLobby = default;
		}
	}
}