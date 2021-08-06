using GameHost;
using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using JetBrains.Annotations;
using PataNext.Module.Simulation.Components.Army;
using RevolutionSnapshot.Core.Buffers;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Roles.Interfaces;
using StormiumTeam.GameBase.Systems.Roles;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

[assembly: RegisterGenericComponentType(typeof(Relative<ArmyFormationDescription>))]
[assembly: RegisterGenericComponentType(typeof(OwnedRelative<ArmyFormationDescription>))]
[assembly: RegisterGenericComponentType(typeof(Relative<ArmySquadDescription>))]
[assembly: RegisterGenericComponentType(typeof(OwnedRelative<ArmySquadDescription>))]
[assembly: RegisterGenericComponentType(typeof(Relative<ArmyUnitDescription>))]
[assembly: RegisterGenericComponentType(typeof(OwnedRelative<ArmyUnitDescription>))]

namespace PataNext.Module.Simulation.Components.Army
{
	internal class EmptySerializer<T> : ICustomComponentDeserializer where T : struct
	{
		public int Size => UnsafeUtility.SizeOf<T>();
		public void      BeginDeserialize(SystemBase system)
		{
		}

		public JobHandle Deserialize(EntityManager   entityManager, ICustomComponentArchetypeAttach attach, NativeArray<GhGameEntitySafe> gameEntities, NativeArray<Entity> output, DataBufferReader reader)
		{
			return default;
		}
	}
	
	public struct ArmyFormationDescription : IEntityDescription
	{
		public class Register : RegisterGameHostComponentData<ArmyFormationDescription> {}

		public class RegisterRelative : Relative<ArmyFormationDescription>.Register
		{
			public override ICustomComponentDeserializer BurstKnowDeserializer()
			{
				return new CustomSingleDeserializer<Relative<ArmyFormationDescription>, Relative<ArmyFormationDescription>.ValueDeserializer>();
			}
		}

		public class RegisterContainer : BuildContainerSystem<ArmyFormationDescription>
		{
			public class Register : RegisterGameHostComponentBuffer<OwnedRelative<ArmyFormationDescription>>
			{
				protected override ICustomComponentDeserializer CustomDeserializer => new EmptySerializer<OwnedRelative<ArmyFormationDescription>>();
			}
		}
	}

	public struct ArmySquadDescription : IEntityDescription
	{
		public class Register : RegisterGameHostComponentData<ArmySquadDescription> {}
		
		public class RegisterRelative : Relative<ArmySquadDescription>.Register
		{
			public override ICustomComponentDeserializer BurstKnowDeserializer()
			{
				return new CustomSingleDeserializer<Relative<ArmySquadDescription>, Relative<ArmySquadDescription>.ValueDeserializer>();
			}
		}

		public class RegisterContainer : BuildContainerSystem<ArmySquadDescription>
		{
			public class Register : RegisterGameHostComponentBuffer<OwnedRelative<ArmySquadDescription>>
			{
				protected override ICustomComponentDeserializer CustomDeserializer => new EmptySerializer<OwnedRelative<ArmySquadDescription>>();
			}
		}
	}

	public struct ArmyUnitDescription : IEntityDescription
	{
		public class Register : RegisterGameHostComponentData<ArmyUnitDescription> {}
		
		public class RegisterRelative : Relative<ArmyUnitDescription>.Register
		{
			public override ICustomComponentDeserializer BurstKnowDeserializer()
			{
				return new CustomSingleDeserializer<Relative<ArmyUnitDescription>, Relative<ArmyUnitDescription>.ValueDeserializer>();
			}
		}

		public class RegisterContainer : BuildContainerSystem<ArmyUnitDescription>
		{
			public class Register : RegisterGameHostComponentBuffer<OwnedRelative<ArmyUnitDescription>>
			{
				protected override ICustomComponentDeserializer CustomDeserializer => new EmptySerializer<OwnedRelative<ArmyUnitDescription>>();
			}
		}
	}
}