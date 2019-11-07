using UnityEngine;
using UnityEngine.Rendering;

namespace package.patapon.core
{
    [RequireComponent(typeof(SortingGroup))]
    [ExecuteAlways]
    public class SortingGroupParameters : MonoBehaviour
    {
        [SerializeField]
        private int sortingLayerId;

        [SerializeField]
        private int order;

        public int SortingLayerId
        {
            get => sortingLayerId;
            set
            {
                sortingLayerId = value;
                ForceUpdate();
            }
        }

        public int Order
        {
            get => order;
            set
            {
                order = value;
                ForceUpdate();
            }
        }

        private void OnEnable()
        {
            ForceUpdate();
        }

        private void OnValidate()
        {
            ForceUpdate();
        }

        public void ForceUpdate()
        {
            GetComponent<SortingGroup>().sortingLayerID = sortingLayerId;
            GetComponent<SortingGroup>().sortingOrder   = order;
        }
    }
}