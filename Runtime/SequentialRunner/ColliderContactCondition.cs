using System;
using UnityEngine;

namespace Framework.Sequencing
{
    public class ColliderContactCondition : MonoBehaviour, IStepCondition
    {
        [SerializeField]
        private LayerMask _contactingLayers = ~0;

        private bool _isArmed;

        public event Action Satisfied;

        public void Arm()
        {
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

        private void OnTriggerEnter(Collider other)
        {
            SatisfyWhenLayerMatches(other.gameObject);
        }

        private void SatisfyWhenLayerMatches(GameObject contacting)
        {
            var isLayerAccepted = (_contactingLayers.value & (1 << contacting.layer)) != 0;
            if (_isArmed && isLayerAccepted)
            {
                Satisfied?.Invoke();
            }
        }
    }
}
