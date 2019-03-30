using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics
{
    // A collector which stores every hit up to the length of the provided native array.
    public struct MaxHitsCollector<T> : ICollector<T> where T : struct, IQueryResult
    {
        private int   m_NumHits;
        public  bool  EarlyOutOnFirstHit => false;
        public  float MaxFraction        { get; }
        public  int   NumHits            => m_NumHits;

        public NativeArray<T> AllHits;

        public MaxHitsCollector(float maxFraction, ref NativeArray<T> allHits)
        {
            MaxFraction = maxFraction;
            AllHits     = allHits;
            m_NumHits   = 0;
        }

        #region

        public bool AddHit(T hit)
        {
            Assert.IsTrue(hit.Fraction < MaxFraction);
            Assert.IsTrue(m_NumHits < AllHits.Length);
            AllHits[m_NumHits] = hit;
            m_NumHits++;
            return true;
        }

        public void TransformNewHits(int oldNumHits, float oldFraction, Math.MTransform transform, uint numSubKeyBits, uint subKey)
        {
            for (int i = oldNumHits; i < m_NumHits; i++)
            {
                T hit = AllHits[i];
                hit.Transform(transform, numSubKeyBits, subKey);
                AllHits[i] = hit;
            }
        }

        public void TransformNewHits(int oldNumHits, float oldFraction, Math.MTransform transform, int rigidBodyIndex)
        {
            for (int i = oldNumHits; i < m_NumHits; i++)
            {
                T hit = AllHits[i];
                hit.Transform(transform, rigidBodyIndex);
                AllHits[i] = hit;
            }
        }

        #endregion
    }
}