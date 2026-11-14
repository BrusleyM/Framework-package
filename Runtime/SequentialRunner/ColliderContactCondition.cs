using System;
using UnityEngine;

namespace Framework.Sequencing
{
    public class ColliderContactCondition : MonoBehaviour, IStepCondition
    {
        [SerializeField]
        private LayerMask _contactingLayers;

        private bool _isArmed;

        public event Action Satisfied;

        public void Arm()
        {
            if (_contactingLayers.value == 0)
            {
                Debug.LogWarning($"{name} accepts no layers, so it can never be satisfied.", this);
            }

            _isArmed = true;
        }

        public void Disarm()
        {
            _isArmed = false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            SatisfyWhenLayerMatches(collision.gameObject);
        }

        private void OnCollisionStay(Collision collision)
        {
            SatisfyWhenLayerMatches(collision.gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            SatisfyWhenLayerMatches(other.gameObject);
        }

        private void OnTriggerStay(Collider other)
        {
            SatisfyWhenLayerMatches(other.gameObject);
        }

        private void SatisfyWhenLayerMatches(GameObject contacting)
        {
            if (!_isArmed)
            {
                return;
            }

            var isLayerAccepted = (_contactingLayers.value & (1 << contacting.layer)) != 0;
            if (isLayerAccepted)
            {
                _isArmed = false;
                Satisfied?.Invoke();
            }
        }
    }
}
