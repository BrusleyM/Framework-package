using UnityEngine;
using UnityEngine.Events;

namespace Framework
{
    public class SnapObject : MonoBehaviour
    {
        [SerializeField]
        private string _objectName;
        [SerializeField]
        UnityEvent _onSnapped;
        public UnityEvent OnSnapped => _onSnapped;

        private void OnCollisionEnter(Collision collision)
        {
            var _object = collision.gameObject;
            if (_object.name == _objectName)
            {
                _object.transform.position = transform.position;
                _object.transform.rotation = transform.rotation;
                _onSnapped?.Invoke();
            }
        }
    }
}
