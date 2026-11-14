using System;
using UnityEngine;

namespace Framework.Sequencing
{
    public class SignalCondition : MonoBehaviour, IStepCondition
    {
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

        public void Raise()
        {
            if (_isArmed)
            {
                Satisfied?.Invoke();
            }
        }
    }
}
