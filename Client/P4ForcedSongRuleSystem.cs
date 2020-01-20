using StormiumTeam.GameBase.BaseSystems;
using Unity.Collections;
using Unity.Entities;

namespace DefaultNamespace
{
	public struct ForcedSongRule : IComponentData
	{
		public NativeString64 SongId;
	}
	
	public class P4ForcedSongRuleSystem : RuleBaseSystem<ForcedSongRule>
	{
		public RuleProperties<ForcedSongRule>.Property<NativeString64> SongId;
		
		protected override void AddRuleProperties()
		{
			SongId = Rule.Add(d => d.SongId);
		}

		protected override void SetDefaultProperties()
		{
			SongId.Value = "dottama_gacheen";
		}
	}
}