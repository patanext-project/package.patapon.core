using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Utility.Modules;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace PataNext.Client.Modules
{
	public class AbilityFinderSystemModule : BaseSystemModule
	{
		private NativeHashMap<Entity, ValueAbility> m_OwnerToAbilityMap;

		public          EntityQuery      Query;
		public override ModuleUpdateType UpdateType => ModuleUpdateType.All;

		protected override void OnEnable()
		{
			m_OwnerToAbilityMap = new NativeHashMap<Entity, ValueAbility>(32, Allocator.Persistent);
		}

		protected override void OnUpdate(ref JobHandle jobHandle)
		{
			if (Query == null)
				return;

			if (CurrentUpdateType == ModuleUpdateType.Job)
			{
				jobHandle = new ClearJob
				{
					HashMap        = m_OwnerToAbilityMap,
					TargetCapacity = Query.CalculateEntityCount() + 32
				}.Schedule(jobHandle);
				jobHandle = new SearchJob
				{
					OwnerToAbilityMap = m_OwnerToAbilityMap.AsParallelWriter()
				}.Schedule(Query, jobHandle);
			}
			else
			{
				new ClearJob
				{
					HashMap        = m_OwnerToAbilityMap,
					TargetCapacity = Query.CalculateEntityCount() + 32
				}.Run();
				new SearchJob
				{
					OwnerToAbilityMap = m_OwnerToAbilityMap.AsParallelWriter()
				}.Run(Query);
			}
		}

		protected override void OnDisable()
		{
			m_OwnerToAbilityMap.Dispose();
		}

		public Entity GetAbility(Entity owner)
		{
			m_OwnerToAbilityMap.TryGetValue(owner, out var value);
			return value.Ability;
		}

		private struct ValueAbility
		{
			public Entity Owner;
			public Entity Ability;
		}

		[BurstCompile]
		private struct ClearJob : IJob
		{
			public NativeHashMap<Entity, ValueAbility> HashMap;
			public int                                 TargetCapacity;

			public void Execute()
			{
				HashMap.Clear();
				if (HashMap.Capacity < TargetCapacity)
					HashMap.Capacity = TargetCapacity;
			}
		}

		[BurstCompile]
		private struct SearchJob : IJobForEachWithEntity<Owner>
		{
			public NativeHashMap<Entity, ValueAbility>.ParallelWriter OwnerToAbilityMap;

			public void Execute(Entity e, int _, ref Owner owner)
			{
				OwnerToAbilityMap.TryAdd(owner.Target, new ValueAbility {Owner = owner.Target, Ability = e});
			}
		}
	}
}