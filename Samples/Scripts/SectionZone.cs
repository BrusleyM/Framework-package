using Framework.Sequencing;
using UnityEngine;

namespace FrameworkTest
{
    [RequireComponent(typeof(Collider))]
    public class SectionZone : MonoBehaviour
    {
        [SerializeField]
        private GuidanceMode _section;

        private void OnTriggerEnter(Collider other)
        {
            if (IsPlayer(other))
            {
                GameManager.Instance?.PlayerEnteredSection(_section);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsPlayer(other))
            {
                GameManager.Instance?.PlayerLeftSection(_section);
            }
        }

        private static bool IsPlayer(Collider other)
        {
            return other.GetComponentInParent<Ellen>() != null;
        }
    }
}
