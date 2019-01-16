using Unity.Entities;

namespace Patapon4TLB.Core.Tests
{
    public struct PlayerCharacter : IComponentData
    {
        public Entity Owner;

        public PlayerCharacter(Entity owner)
        {
            Owner = owner;
        }
    }
}