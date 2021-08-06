using GameHost;
using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using PataNext.Client.Graphics.Animation.Units.Base;
using RevolutionSnapshot.Core.Buffers;
using Unity.Collections;
using Unity.Entities;

namespace PataNext.Game.Abilities.Effects
{
	public abstract class RegisterStatusEffectSystemState<T> : SystemBase
		where T : struct, IStatusEffectState
	{
		public class Register : RegisterGameHostComponentData<T>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => new CustomSingleDeserializer<T, StateDeserializer>();
		}

		private struct StateDeserializer : IValueDeserializer<T>
		{
			public int Size => 28;

			public void Deserialize(EntityManager em, NativeHashMap<GhGameEntitySafe, Entity> ghEntityToUEntity, ref T component, ref DataBufferReader reader)
			{
				reader.ReadValue<GhComponentType>(); // skip StatusEffectStateBase.Type

				component.Resistance     = reader.ReadValue<float>();
				component.RegenPerSecond = reader.ReadValue<float>();
				component.Power          = reader.ReadValue<float>();
				component.Immunity       = reader.ReadValue<float>();
				component.ReceivePower   = reader.ReadValue<float>();

				reader.ReadValue<float>(); // skip StatusEffectStateBase.ImmunityExp
			}
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			World.GetOrCreateSystem<Register>();
		}

		protected override void OnUpdate()
		{
		}
	}

	public abstract class RegisterStatusEffectSystemSettings<T> : SystemBase
		where T : struct, IStatusEffectSettings
	{
		public class Register : RegisterGameHostComponentData<T>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => new CustomSingleDeserializer<T, SettingsDeserializer>();
		}

		private struct SettingsDeserializer : IValueDeserializer<T>
		{
			public int Size => 20;

			public void Deserialize(EntityManager em, NativeHashMap<GhGameEntitySafe, Entity> ghEntityToUEntity, ref T component, ref DataBufferReader reader)
			{
				reader.ReadValue<GhComponentType>(); // skip StatusEffectSettingsBase.Type

				component.Resistance     = reader.ReadValue<float>();
				component.RegenPerSecond = reader.ReadValue<float>();
				component.Power          = reader.ReadValue<float>();
				component.Immunity       = reader.ReadValue<float>();
			}
		}
		
		protected override void OnCreate()
		{
			base.OnCreate();

			World.GetOrCreateSystem<Register>();
		}
		
		protected override void OnUpdate()
		{
		}
	}
}