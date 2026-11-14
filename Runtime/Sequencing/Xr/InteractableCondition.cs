using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Framework.Sequencing.Xr
{
    public class InteractableCondition : MonoBehaviour, IStepCondition
    {
        [SerializeField]
        private XRBaseInteractable _interactable;
        [SerializeField]
        private InteractionMoment _moment = InteractionMoment.SelectEntered;

        public event Action Satisfied;

        public void Arm()
        {
            Disarm();
            switch (_moment)
            {
                case InteractionMoment.HoverEntered:
                    _interactable.hoverEntered.AddListener(OnInteracted);
                    break;
                case InteractionMoment.SelectEntered:
                    _interactable.selectEntered.AddListener(OnInteracted);
                    break;
                case InteractionMoment.Activated:
                    _interactable.activated.AddListener(OnInteracted);
                    break;
            }
        }

        public void Disarm()
        {
            _interactable.hoverEntered.RemoveListener(OnInteracted);
            _interactable.selectEntered.RemoveListener(OnInteracted);
            _interactable.activated.RemoveListener(OnInteracted);
        }

        private void OnInteracted(BaseInteractionEventArgs arguments)
        {
            Satisfied?.Invoke();
        }
    }
}
