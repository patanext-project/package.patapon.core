using Patapon4TLB.Core.Snapshots;
using Revolution;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Transforms;
using UnityEngine;

[assembly: RegisterGenericComponentType(typeof(Predicted<VelocityPredictedTranslation>))]

namespace Patapon4TLB.Core.Snapshots
{
	public struct VelocityPredictedTranslation : IReadWriteSnapshot<VelocityPredictedTranslation>, ISynchronizeImpl<Translation>, IInterpolatable<VelocityPredictedTranslation>
	{
		public const int    dimension = 2;
		public       float2 Value;

		public void WriteTo(DataStreamWriter writer, ref VelocityPredictedTranslation baseline, NetworkCompressionModel compressionModel)
		{
			for (var i = 0; i < dimension; i++)
				writer.WritePackedFloatDelta(Value[i], default, compressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref VelocityPredictedTranslation baseline, NetworkCompressionModel compressionModel)
		{
			for (var i = 0; i < dimension; i++)
				Value[i] = reader.ReadPackedFloatDelta(ref ctx, default, compressionModel);
		}

		public uint Tick { get; set; }

		public void SynchronizeFrom(in Translation component, in DefaultSetup setup, in SerializeClientData serializeData)
		{
			Value.x = component.Value.x;
			Value.y = component.Value.y;
		}

		public void SynchronizeTo(ref Translation component, in DeserializeClientData deserializeData)
		{
			component.Value.x = Value.x;
			component.Value.y = Value.y;
		}

		public struct Exclude : IComponentData
		{
		}
		
		public struct Use : IComponentData {}

		public class NetSynchronize : ComponentSnapshotSystemBasic<Translation, VelocityPredictedTranslation>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);

			public override NativeArray<ComponentType> EntityComponents =>
				new NativeArray<ComponentType>(3, Allocator.TempJob)
				{
					[0] = typeof(Use),
					[1] = typeof(Translation),
					[2] = typeof(Velocity)
				};
		}

		public class LocalUpdate : ComponentUpdateSystemInterpolated<Translation, VelocityPredictedTranslation>
		{
			protected override bool IsPredicted => true;
		}

		[UpdateInGroup(typeof(GhostPredictionSystemGroup))]
		public class Predict : SystemBase
		{
			protected override void OnUpdate()
			{
				var dt   = Time.DeltaTime;
				var tick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;

				var predictedFromEntity = GetComponentDataFromEntity<Predicted<VelocityPredictedTranslation>>(true); 
				Entities.ForEach((Entity entity, ref Translation translation, ref Velocity velocity) =>
				{
					if (!predictedFromEntity.Exists(entity))
						return;
					if (!BaseGhostPredictionSystemGroup.ShouldPredict(tick, predictedFromEntity[entity]))
						return;

					translation.Value += velocity.Value * dt;
				}).WithReadOnly(predictedFromEntity).Schedule();
			}
		}

		public void Interpolate(VelocityPredictedTranslation target, float factor)
		{
			Value = math.lerp(Value, target.Value, factor);
		}
	}
}