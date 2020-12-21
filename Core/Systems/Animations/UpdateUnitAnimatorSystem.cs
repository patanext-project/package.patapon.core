using System;
using PataNext.Client.Graphics.Animation.Units.Base;
using StormiumTeam.GameBase;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Playables;

namespace PataNext.Client.Graphics.Animation.Base
{
	[UpdateInGroup(typeof(OrderGroup.Presentation.CharacterAnimation), OrderLast = true)]
	[UpdateAfter(typeof(ClientUnitAnimationGroup))]
	public class UpdateUnitAnimatorSystem : SystemBase
	{
		protected override void OnUpdate()
		{
			var dt = Time.DeltaTime;
			Entities.ForEach((UnitVisualAnimation visualAnimation) =>
			{
				//visualAnimation.Graph.Evaluate(dt);
			}).WithStructuralChanges().Run();
		}
	}
}