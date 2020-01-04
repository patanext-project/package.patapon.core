using StormiumTeam.GameBase.BaseSystems;
using Unity.Entities;
using Unity.Jobs;

namespace Patapon.Mixed.GameModes.VSHeadOn
{
	public class VersusHeadOnRule : RuleBaseSystem
	{
		public RuleProperties<Data>.Property<int> BaseRespawnTime;
		public RuleProperties<Data>.Property<int> IncrementRespawnTime;
		public RuleProperties<Data>.Property<int> MaxRespawnTime;

		public RuleProperties<Data>               Properties;
		public RuleProperties<Data>.Property<int> TimeLimit;

		public override string Name => "Versus HeadOn Rules";

		protected override void OnCreate()
		{
			base.OnCreate();

			Properties           = AddRule<Data>(out var data);
			TimeLimit            = Properties.Add("Time Limit", ref data, ref data.TimeLimit);
			BaseRespawnTime      = Properties.Add("Base Respawn Time", ref data, ref data.RespawnTime);
			IncrementRespawnTime = Properties.Add("Increment Respawn Time per death", ref data, ref data.IncrementRespawnTime);
			MaxRespawnTime       = Properties.Add("Max Respawn Time", ref data, ref data.MaxRespawnTime);

			TimeLimit.Value            = 5 * 60 * 1000;
			BaseRespawnTime.Value      = 10000;
			IncrementRespawnTime.Value = 5000;
			MaxRespawnTime.Value       = 60000;
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return inputDeps;
		}

		public struct Data : IComponentData
		{
			public int TimeLimit;
			public int RespawnTime;
			public int IncrementRespawnTime;
			public int MaxRespawnTime;
		}
	}
}