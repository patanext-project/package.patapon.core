using System;
using System.Collections.Generic;
using package.stormium.core;
using package.stormiumteam.shared;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace P4.Core.Scripts
{
    [Flags]
    public enum CharacterController2DHitEventType
    {
        OnEnter = 1,
        OnExit = 2,
        OnStay = 4,
        OnEnterOrStay = 5
    }
    
    [Serializable]
    public struct CharacterController2DHitEvent
    {
        public CharacterController2DHitEventType Type;
        public Collision2D Collision;
    }

    [Serializable]
    public struct BufferFrameEvent
    {
        public bool IsValid;
        public List<CharacterController2DHitEvent> HitEvents;
        public int Frame;

        public BufferFrameEvent(int frame, List<CharacterController2DHitEvent> events)
        {
            Frame = frame;
            HitEvents = events;
            IsValid = true;
        }
    }
    
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public partial class CharacterController2D : MonoBehaviour, IPhysicPostSimulateItem
    {
        public Rigidbody2D Rigidbody2D;
        public float SkinWidth = 0.02f;
        public List<BufferFrameEvent> BufferFrameEvents;

        public bool IsGrounded()
        {
            return m_IsGrounded;
        }
        
        public void Teleport(Vector3 position)
        {
            Rigidbody2D.position = position;
        }

        public BufferFrameEvent GetLastMomentEvent()
        {
            return BufferFrameEvents[0];
        }

        public BufferFrameEvent GetMomentEventByIndex(int index)
        {
            if (index > m_MaxElemBuffer)
                throw new IndexOutOfRangeException($"{nameof(index)}({index}) > {nameof(m_MaxElemBuffer)}({m_MaxElemBuffer})");
            
            return BufferFrameEvents[index];
        }
        
        public BufferFrameEvent GetMomentEventByFrame(int frame)
        {
            for (int i = 0; i != m_MaxElemBuffer; i++)
            {
                var buffer = BufferFrameEvents[i];
                if (buffer.Frame == frame)
                    return buffer;
            }

            Debug.LogWarning($"No BufferFrameEvent found for frame: {frame} !");
            return default(BufferFrameEvent);
        }
        
        void IPhysicPostSimulateItem.PostSimulateItem(float dt)
        {
            if (gameObject.layer != CPhysicSettings.PhysicInteractionLayer)
            {
                Debug.Log($"{gameObject.name} is not ran by CharacterController because of layer misconfiguration.");
            }
            
            // Check if we are grounded
            var contactFilter = new ContactFilter2D();
            contactFilter.SetLayerMask(CPhysicSettings.PhysicInteractionLayerMask);

            var length = Rigidbody2D.Cast(Vector3.down, m_RaycastHitBuffer, SkinWidth);
            // TODO: This is wrong, we should make an event to all raycast so we can know if we can really be on ground
            m_IsGrounded = length > 0;
        }
    }

    public partial class CharacterController2D
    {
        private const int m_MaxElemBuffer = 10;
        private bool m_IsGrounded;
        private RaycastHit2D[] m_RaycastHitBuffer;
        
        private void Awake()
        {
            BufferFrameEvents = new List<BufferFrameEvent>(m_MaxElemBuffer + 1);
            Rigidbody2D = GetComponent<Rigidbody2D>();
            m_RaycastHitBuffer = new RaycastHit2D[16];

            for (int i = 0; i != m_MaxElemBuffer; i++)
            {
                BufferFrameEvents.Add(default(BufferFrameEvent));
            }
            
            World.Active.GetExistingManager<AppEventSystem>()
                .SubscribeToAll(this);
            
            Debug.Assert(BufferFrameEvents != null, "BufferFrameEvents != null");
            Debug.Assert(Rigidbody2D != null, "Rigidbody2D != null");
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            var ev = new CharacterController2DHitEvent();
            ev.Type = CharacterController2DHitEventType.OnEnter;
            ev.Collision = other;

            AddEvent(ev);
        }

        private void OnCollisionExit2D(Collision2D other)
        {
            var ev = new CharacterController2DHitEvent();
            ev.Type      = CharacterController2DHitEventType.OnExit;
            ev.Collision = other;

            AddEvent(ev);
        }

        private void OnCollisionStay2D(Collision2D other)
        {
            var ev = new CharacterController2DHitEvent();
            ev.Type      = CharacterController2DHitEventType.OnStay;
            ev.Collision = other;

            AddEvent(ev);
        }

        private void AddEvent(CharacterController2DHitEvent hitEvent)
        {
            BufferFrameEvent bufferFrameEvent;
            
            var currentFrame = Time.frameCount;
            if (GetLastMomentEvent().Frame == currentFrame)
            {
                bufferFrameEvent = GetLastMomentEvent();
                bufferFrameEvent.HitEvents.Add(hitEvent);

                return;
            }

            bufferFrameEvent = GetMomentEventByIndex(m_MaxElemBuffer - 1);
            var hitEvents = bufferFrameEvent.HitEvents ?? new List<CharacterController2DHitEvent>(4);
            hitEvents.Add(hitEvent);

            bufferFrameEvent.Frame = currentFrame;
            bufferFrameEvent.IsValid = true;
            bufferFrameEvent.HitEvents = hitEvents;

            // TODO for better optimization
            /*for (int i = 0; i != m_MaxElemBuffer; i++)
            {
                if (i == 0) continue;

                BufferFrameEvents[i] = BufferFrameEvents[i - 1];
            }*/
            
            BufferFrameEvents.Insert(0, bufferFrameEvent);
            BufferFrameEvents.RemoveAt(BufferFrameEvents.Count - 1);
        }
    }
}