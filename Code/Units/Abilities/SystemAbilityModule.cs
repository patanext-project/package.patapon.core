using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Patapon4TLB.Default
{
	public class SystemAbilityModule : BaseSystemModule
	{
		public override ModuleUpdateType UpdateType => ModuleUpdateType.Job;

		private struct ValueAbility
		{
			public Entity Owner;
			public Entity Ability;
		}

		[BurstCompile]
		private struct ClearJob : IJob
		{
			public NativeHashMap<Entity, ValueAbility> HashMap;

			public void Execute()
			{
				HashMap.Clear();
			}
		}

		[BurstCompile]
		private struct SearchJob : IJobForEachWithEntity<Owner>
		{
			public NativeHashMap<Entity, ValueAbility>.Concurrent OwnerToAbilityMap;

			public void Execute(Entity e, int _, ref Owner owner)
			{
				OwnerToAbilityMap.TryAdd(owner.Target, new ValueAbility {Owner = owner.Target, Ability = e});
			}
		}

		public  EntityQuery                         Query;
		private NativeHashMap<Entity, ValueAbility> m_OwnerToAbilityMap;

		protected override void OnEnable()
		{
			m_OwnerToAbilityMap = new NativeHashMap<Entity, ValueAbility>(32, Allocator.Persistent);
		}

		protected override void OnUpdate(ref JobHandle jobHandle)
		{
			if (Query == null)
				return;

			jobHandle = new SearchJob
			{
				OwnerToAbilityMap = m_OwnerToAbilityMap.ToConcurrent()
			}.Schedule(Query, jobHandle);
		}

		protected override void OnDisable()
		{
			m_OwnerToAbilityMap.Dispose();
		}

		public Entity GetAbility(Entity owner)
		{
			return m_OwnerToAbilityMap.TryGetValue(owner, out var value) ? value.Ability : default;
		}
	}
}