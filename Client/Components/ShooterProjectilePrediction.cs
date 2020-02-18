using System;
using Patapon.Client.Systems;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Components
{
	public struct ShooterProjectilePrediction : IComponentData
	{
		public RigidTransform Transform;
	}

	public struct ShooterProjectileVisualTarget : IComponentData
	{
		public VisualThrowableDefinition Definition;
	}
}