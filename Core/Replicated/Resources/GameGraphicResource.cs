using System;
using GameHost.Native;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using GameHost.Simulation.Utility.Resource;
using GameHost.Simulation.Utility.Resource.Interfaces;
using PataNext.Module.Simulation.Components.GamePlay;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(GameResource<GameGraphicResource>))]

namespace PataNext.Module.Simulation.Components.GamePlay
{
	public struct GameGraphicResource : IGameResourceDescription, IEquatable<GameGraphicResource>
	{
		public readonly CharBuffer128 Value;

		public class Register : RegisterGameHostComponentData<GameGraphicResource>
		{
		}

		public GameGraphicResource(CharBuffer128 value)
		{
			Value = value;
		}

		public GameGraphicResource(string value)
		{
			Value = CharBufferUtility.Create<CharBuffer128>(value);
		}

		public static implicit operator GameGraphicResource(string value)
		{
			return new GameGraphicResource(value);
		}


		public bool Equals(GameGraphicResource other)
		{
			return Value.Equals(other.Value);
		}

		public override bool Equals(object obj)
		{
			return obj is GameGraphicResource other && Equals(other);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}
	}
}