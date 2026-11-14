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
            if (_interactable == null)
            {
                throw new InvalidOperationException($"{name} needs an XRBaseInteractable to wait on.");
            }

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

            if (IsAlreadyHappening())
            {
                Satisfied?.Invoke();
            }
        }

        public void Disarm()
        {
            if (_interactable == null)
            {
                return;
            }

            _interactable.hoverEntered.RemoveListener(OnInteracted);
            _interactable.selectEntered.RemoveListener(OnInteracted);
            _interactable.activated.RemoveListener(OnInteracted);
        }

        private void Reset()
        {
            _interactable = GetComponent<XRBaseInteractable>();
        }

        private void OnDestroy()
        {
            Disarm();
        }

        private bool IsAlreadyHappening()
        {
            switch (_moment)
            {
                case InteractionMoment.HoverEntered:
                    return _interactable.isHovered;
                case InteractionMoment.SelectEntered:
                    return _interactable.isSelected;
                default:
                    return false;
            }
        }

        private void OnInteracted(BaseInteractionEventArgs arguments)
        {
            Satisfied?.Invoke();
        }
    }
}
