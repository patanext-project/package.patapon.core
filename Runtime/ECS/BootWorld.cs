using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using NotImplementedException = System.NotImplementedException;

namespace Patapon4TLB.Default
{
	public static class BootWorld
	{
		private static World m_World;
		public static World World
		{
			get => m_World ?? World.Active;
			set => m_World = value;
		}
		
		[NotClientServerSystem]
		internal class System : ComponentSystem
		{
			protected override void OnCreate()
			{
				BootWorld.World = World;
			}

			protected override void OnUpdate()
			{
			}
		}

		public static T GetOrCreateSystem<T>()
			where T : ComponentSystemBase
		{
			return World.GetOrCreateSystem<T>();
		}
	}
}