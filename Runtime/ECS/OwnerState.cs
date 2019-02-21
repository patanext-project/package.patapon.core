using System;
using package.stormiumteam.networking.runtime.lowlevel;
using StormiumShared.Core.Networking;
using Unity.Entities;

namespace Stormium.Core
{
    public interface IOwnerDescription : IComponentData
    {
    }

    public struct LivableDescription : IOwnerDescription
    {
    }

    public struct CharacterDescription : IOwnerDescription
    {
    }

    public struct PlayerDescription : IOwnerDescription
    {
    }

    public struct ActionDescription : IOwnerDescription
    {
    }

    public struct ProjectileDescription : IOwnerDescription
    {
    }

    public static class OwnerState
    {
        public static void SetOwnerData(this EntityManager entityManager, Entity source, Entity owner)
        {
            // todo: get all owner types, then get the one from the owner entity, compare them and add them to the source as OwnerState<T>
            throw new NotImplementedException();
        }
    }

    public struct OwnerState<TOwnerDescription> : IStateData, IComponentData, ISerializableAsPayload
        where TOwnerDescription : struct, IOwnerDescription
    {
        public Entity Target;

        public void Write(ref DataBufferWriter data, SnapshotReceiver receiver, StSnapshotRuntime runtime)
        {
            data.WriteValue(Target);
        }

        public void Read(ref DataBufferReader data, SnapshotSender sender, StSnapshotRuntime runtime)
        {
            Target = runtime.EntityToWorld(data.ReadValue<Entity>());
        }

        public class Streamer : SnapshotEntityDataManualValueTypeStreamer<OwnerState<TOwnerDescription>>
        {
        }
    }
}