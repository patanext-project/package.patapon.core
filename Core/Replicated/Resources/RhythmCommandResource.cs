using System;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using GameHost.Simulation.Utility.Resource;
using GameHost.Simulation.Utility.Resource.Interfaces;
using PataNext.Module.Simulation.Resources;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(GameResource<RhythmCommandResource>))]

namespace PataNext.Module.Simulation.Resources
{
	public readonly struct RhythmCommandResource : IGameResourceDescription, IEquatable<RhythmCommandResource>
	{
		public readonly ComponentType Identifier;

		public class Register : RegisterGameHostComponentData<RhythmCommandResource>
		{
		}

		public RhythmCommandResource(ComponentType identifier)
		{
			Identifier = identifier;
		}

		public bool Equals(RhythmCommandResource other)
		{
			return Identifier.Equals(other.Identifier);
		}

		public override bool Equals(object obj)
		{
			return obj is RhythmCommandResource other && Equals(other);
		}

		public override int GetHashCode()
		{
			return Identifier.GetHashCode();
		}
	}
}