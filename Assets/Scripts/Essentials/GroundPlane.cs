using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace ubco.hcilab.roadmap
{
    public class GroundPlane : MonoBehaviour
    {
        [SerializeField] private GameObject _collisionPlane;
        private Vector3 _newPosition;
        private float _minY;

        private void Start()
        {
            _collisionPlane.SetActive(false);
        }

        private void Update()
        {
            if (ARInteractionManager.Instance.AllPlanes.count > 0)
            {
                if (!_collisionPlane.activeSelf) _collisionPlane.SetActive(true);

                /// Find and match Y val of lowest AR plane & hope that this is the "ground"
                _minY = Mathf.Infinity;         
                foreach (ARPlane plane in ARInteractionManager.Instance.AllPlanes)
                {
                    if (plane.center.y < _minY)
                        _minY = plane.center.y;
                }

                _newPosition.Set(0, _minY, 0);
                transform.position = _newPosition;
            }
            else
            {
                if (_collisionPlane.activeSelf) _collisionPlane.SetActive(false);
            }
        }
    }
}
