using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Patapon4TLB.Default
{
	public class SystemAbilityModule : BaseSystemModule
	{
		private struct CopyJob<T> : IJob
			where T : struct
		{
			[DeallocateOnJobCompletion]
			public NativeArray<T> Source;

			public NativeList<T> Destination;

			public void Execute()
			{
				Destination.Clear();
				Destination.AddRange(Source);
			}
		}

		public EntityQuery        Query;
		public NativeList<Entity> EntityArray;
		public NativeList<Owner>  OwnerArray;

		protected override void OnEnable()
		{
			EntityArray = new NativeList<Entity>(Allocator.Persistent);
			OwnerArray = new NativeList<Owner>(Allocator.Persistent);
		}

		protected override void OnUpdate(ref JobHandle jobHandle)
		{
			if (Query == null)
				return;

			Query.AddDependency(jobHandle);
			var tmpEntities   = Query.ToEntityArray(Allocator.TempJob, out var dep1);
			var tmpOwnerArray = Query.ToComponentDataArray<Owner>(Allocator.TempJob, out var dep2);

			jobHandle = JobHandle.CombineDependencies(jobHandle, dep1, dep2);
			jobHandle = new CopyJob<Entity> {Source = tmpEntities, Destination = EntityArray}.Schedule(jobHandle);
			jobHandle = new CopyJob<Owner> {Source = tmpOwnerArray, Destination = OwnerArray}.Schedule(jobHandle);
		}

		protected override void OnDisable()
		{
			EntityArray.Dispose();
			OwnerArray.Dispose();
		}

		public Entity FindFromOwner(Entity owner)
		{
			for (var ab = 0; ab != EntityArray.Length; ab++)
			{
				if (OwnerArray[ab].Target == owner)
					return EntityArray[ab];
			}

			return Entity.Null;
		}
	}
}