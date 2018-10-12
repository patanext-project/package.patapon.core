using UnityEngine;
using UnityEngine.Rendering;

namespace package.patapon.core
{
    [RequireComponent(typeof(SortingGroup))]
    public class SortingGroupFloatAnimation : MonoBehaviour
    {
        private                  SortingGroup m_SortingGroup;
        [SerializeField] private float        m_OrderInLayer;

        public float OrderInLayer
        {
            get { return m_OrderInLayer; }
            set { m_OrderInLayer = value; }
        }

        private void OnEnable()
        {
            m_SortingGroup = GetComponent<SortingGroup>();
        }

        private void Update()
        {
            m_SortingGroup.sortingOrder = Mathf.FloorToInt(m_OrderInLayer);
        }

        private void OnDisable()
        {
            m_SortingGroup = null;
        }
    }
}