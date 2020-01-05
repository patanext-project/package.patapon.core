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

		protected override long ClientId => 609427243395055616;
	}
}