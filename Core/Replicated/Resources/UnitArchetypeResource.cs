using System;
using GameHost.Native;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using GameHost.Simulation.Utility.Resource;
using GameHost.Simulation.Utility.Resource.Interfaces;
using PataNext.Module.Simulation.Resources;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(GameResource<UnitArchetypeResource>))]

namespace PataNext.Module.Simulation.Resources
{
	public readonly struct UnitArchetypeResource : IGameResourceDescription, IEquatable<UnitArchetypeResource>
	{
		public readonly CharBuffer64 Value;

		public class Register : RegisterGameHostComponentData<UnitArchetypeResource>
		{
		}

		public UnitArchetypeResource(CharBuffer64 value)
		{
			Value = value;
		}

		public UnitArchetypeResource(string value)
		{
			Value = CharBufferUtility.Create<CharBuffer64>(value);
		}

		public bool Equals(UnitArchetypeResource other)
		{
			return Value.Equals(other.Value);
		}

		public override bool Equals(object obj)
		{
			return obj is UnitArchetypeResource other && Equals(other);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}
	}
}