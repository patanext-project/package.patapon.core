using Unity.Entities;

namespace Patapon4TLB.Core.Tests
{
    // WARNING: We should never do this in real code, it's just to ease the testing.
    public struct PlayerToCharacterLink : IComponentData
    {
        public Entity Character;

        public PlayerToCharacterLink(Entity character)
        {
            Character = character;
        }
    }
}