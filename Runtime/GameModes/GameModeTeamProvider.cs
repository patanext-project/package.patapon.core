using Patapon4TLBCore;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.NetCode;

namespace Patapon4TLB.GameModes
{
	public class GameModeTeamProvider : BaseProviderBatch<GameModeTeamProvider.Create>
	{
		public struct Create
		{
			
		}

		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new ComponentType[]
			{
				typeof(TeamDescription),
				typeof(TeamAllies),
				typeof(TeamEnemies),
				typeof(TeamBlockMovableArea),
				typeof(GhostComponent),
				typeof(PlayEntityTag),
			};
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			
		}
	}
}