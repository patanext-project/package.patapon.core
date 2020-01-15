using Patapon4TLB.Default.Player;
using Revolution;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Data;
using Unity.Entities;

namespace Patapon.Mixed.GamePlay.Abilities
{
	public class BaseRhythmAbilityProvider<TCommand, TCreate> : BaseProviderBatch<TCreate>
		where TCommand : struct, IComponentData
		where TCreate : struct, BaseRhythmAbilityProvider<TCommand, TCreate>.ICreate
	{
		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new ComponentType[]
			{
				typeof(EntityDescription),
				typeof(ActionDescription),
				typeof(RhythmAbilityState),
				typeof(TCommand),
				typeof(Owner),
				typeof(DestroyChainReaction),
				typeof(PlayEntityTag),
				typeof(GhostEntity)
			};
		}

		public override void SetEntityData(Entity entity, TCreate data)
		{
			EntityManager.ReplaceOwnerData(entity, data.Owner);
			EntityManager.SetComponentData(entity, EntityDescription.New<ActionDescription>());
			EntityManager.SetComponentData(entity, new RhythmAbilityState {Command = data.Command, TargetSelection = data.Selection});
			EntityManager.SetComponentData(entity, data.Data);
			EntityManager.SetComponentData(entity, new Owner {Target = data.Owner});
			EntityManager.SetComponentData(entity, new DestroyChainReaction(data.Owner));
		}

		public interface ICreate
		{
			Entity           Owner     { get; set; }
			Entity           Command   { get; set; }
			TCommand         Data      { get; set; }
			AbilitySelection Selection { get; set; }
		}
	}

	public class BaseRhythmAbilityProvider<TCommand> : BaseRhythmAbilityProvider<TCommand, BaseRhythmAbilityProvider<TCommand>.Create>
		where TCommand : struct, IComponentData
	{
		public struct Create : ICreate
		{
			public Entity           Owner     { get; set; }
			public Entity           Command   { get; set; }
			public TCommand         Data      { get; set; }
			public AbilitySelection Selection { get; set; }
		}
	}
}