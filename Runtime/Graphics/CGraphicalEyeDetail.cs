using System.Collections.Generic;
using UnityEngine;

namespace package.patapon.core
{
    [ExecuteInEditMode]
    public class CGraphicalEyeDetail : MonoBehaviour
    {
        [SerializeField] private Vector2   m_PositionOffset;
        [SerializeField] private Vector2   m_Aim;
        [SerializeField] private Transform m_Root;

        public int Id => m_Root.GetInstanceID();

        public Vector2 PositionOffset
        {
            get { return m_PositionOffset; }
            set { m_PositionOffset = value; }
        }

        public Vector2 Aim
        {
            get { return m_Aim; }
            set { m_Aim = value; }
        }

        public Transform Root
        {
            get { return m_Root; }
            set { m_Root = value; }
        }

        private int m_PreviousId = 0;

        private void OnEnable()
        {
            m_PreviousId                                 = Id;
            CGraphicalStackEyeDetail.StackProperties[Id] = this;
        }

        private void OnDisable()
        {
            m_PreviousId = 0;
            CGraphicalStackEyeDetail.StackProperties.Remove(Id);
        }
    }

    public static class CGraphicalStackEyeDetail
    {
        public static Dictionary<int, CGraphicalEyeDetail> StackProperties;

        static CGraphicalStackEyeDetail()
        {
            StackProperties = new Dictionary<int, CGraphicalEyeDetail>();
        }

        public static CGraphicalEyeDetail Get(int id)
        {
            CGraphicalEyeDetail eyeDetail;
            if (!StackProperties.TryGetValue(id, out eyeDetail))
            {
                return null;
            }

            return eyeDetail;
        }
    }
}