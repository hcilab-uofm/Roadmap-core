using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ubco.hcilab.roadmap
{
    /// <summary>
    /// Singleton Class
    /// Handles AR Plane detection, touch screen input, and current interaction state.
    /// We are constantly raycasting from the camera position, looking for ARPlanes.
    /// When we hit a plane, we emit an event with the world space position of the intersection.
    /// </summary>
    public class ARInteractionManager : Singleton<ARInteractionManager>
    {
        [SerializeField]
        private TMP_Dropdown typeOptions;
        [SerializeField] private ARPlaneManager ARPlaneManager;
        [SerializeField] private ARRaycastManager ARRaycastManager;
        /// <summary>
        /// The ARPlane currently detected by raycast where the camera is pointing.
        /// Updated every frame. Null if none.
        /// </summary>
        public ARPlane CurrentARPlane { get => _currentARPlane; }

        /// <summary>
        /// All current tracked ARPlanes
        /// </summary>
        public TrackableCollection<ARPlane> AllPlanes { get => ARPlaneManager.trackables; }
        /// <summary>
        /// Point on the screen the user is touching, in screen space values
        /// </summary>
        public Vector2 TouchPoint { get => _touchPoint; }

        /// <summary>
        /// Point on the screen the user is touching, normalized
        /// </summary>
        public Vector2 NormalizedTouchPoint { get => NormalizedScreenPosition(_touchPoint); }
        /// <summary>
        /// Raised whenever the current detected ARPlane changes. Can be null.
        /// </summary>
        [HideInInspector] public UnityEvent<ARPlane> ARPlaneChanged;

        /// <summary>
        /// Raised every frame. Pose is either ray interstion with a valid ARPlane, or else the set default position.
        /// </summary>
        [HideInInspector] public UnityEvent<Pose> ARHitPoseUpdated;

        /// <summary>
        /// Raised each frame on screen swipes and provides normalized coordinates
        /// </summary>
        [HideInInspector] public UnityEvent<Vector2> TouchPositionChanged;

        private List<ARRaycastHit> _hits = new List<ARRaycastHit>();
        private List<ARPlane> _allPlanes = new List<ARPlane>();
        private ARPlane _currentARPlane;
        private Vector2 _touchPoint; /// normalized
        private Transform _fallbackPlacementTransform;
        private InteractionManager _interactionManager;

        [Header("[ AR Plane Settings ]")]
        [Tooltip("Filter raycasts for ARPlanes by type")]
        [SerializeField] private TrackableType _planeTypes;

        [Tooltip("ARPlane raycast hits beyond this distace are discarded")]
        [SerializeField] private float _maxRaycastDistance = 3;

        [Tooltip("Where to position the current Placeable when no ARPlane is found")]
        [SerializeField] private Vector3 _fallbackPlacementPos = new Vector3(0, 0, 2);

        /// <summary>
        /// Experimental ARPlane detection filtering
        /// </summary>
        [Header("[ Experimental ARPlane Culling ]")]
        [Tooltip("Use hit position on only the largest ARPlane detected by raycast")]
        [SerializeField] private bool _filterHitsBySize;

        [Tooltip("Smallest raycast detected ARPlanes will be disabled")]
        [SerializeField] private bool _cullBySizeOnHit;

        [Tooltip("Smallest ARPlanes in scene will always be disabled")]
        [SerializeField] private bool _cullAllButLargestPlane;

        [Header("[ Input Settings ]")]
        [Tooltip("Screen taps below this normalized height will be ignored")]
        [SerializeField] private float _screenTapLowerCutoff = 0.15f;

        private void Start()
        {
            _interactionManager = InteractionManager.Instance;
            _fallbackPlacementTransform = new GameObject("Fallback Placement").transform;
            _fallbackPlacementTransform.parent = _interactionManager.mainCamera.transform;
            _fallbackPlacementTransform.localPosition = _fallbackPlacementPos;
            _fallbackPlacementTransform.localRotation = Quaternion.identity;

            GeospatialManager.Instance.InitCompleted.AddListener(OnGeoInitCompleted);

            _interactionManager.VibratingOnce.AddListener(VibrateOnce);

            typeOptions.onValueChanged.AddListener((value) => {
                PlaceablesManager.Instance.UpdatePrefabIdentifier(typeOptions.options[value].text);
            });

            typeOptions.AddOptions(PlaceablesManager.Instance.applicationConfig.PlacableIdentifierList().Select(x => x.identifier).ToList());
            /// Settting initial value of the typeoptions
            PlaceablesManager.Instance.UpdatePrefabIdentifier(typeOptions.options[typeOptions.value].text);
        }

        private void OnGeoInitCompleted()
        {
            GeospatialManager.Instance.InitCompleted.RemoveListener(OnGeoInitCompleted);

            if (ARPlaneManager != null)
                ARPlaneManager.planesChanged += UpdatePlanes;

            //_run = true;
            _interactionManager.run = false;
        }

        private void Update()
        {
            if (!_interactionManager.run) return;

            // FIXME: This is handled by MRTK
            HandleScreenTap();
            HandleTapRelease();
            HandleSwipe();

            /// Optional forced culling of AR planes
            if (_cullAllButLargestPlane && AllPlanes.count > 1)
                CullPlanesBySize();

            /// Raycast for AR Planes in scene
            if (_interactionManager.interactionState == InteractionState.Placing || _interactionManager.interactionState == InteractionState.Previewing)
                DetectARPlane();
        }


        /// <summary>
        /// Maintains a list of AR Planes in scene.
        /// </summary>
        private void UpdatePlanes(ARPlanesChangedEventArgs planes)
        {
            planes.added.ForEach(plane =>
            {
                if (!_allPlanes.Contains(plane)) _allPlanes.Add(plane);
            });
            planes.removed.ForEach(plane =>
            {
                if (_allPlanes.Contains(plane)) _allPlanes.Remove(plane);
            });
        }

        /// <summary>
        /// Invokes ObjectTapped event when a placeable object is tapped.
        /// </summary>
        private void HandleScreenTap()
        {
            if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began)
            {
                _touchPoint = Input.touches[0].position;
            }

            else return;

            if (NormalizedTouchPoint.y < _screenTapLowerCutoff)
                return;

            Ray ray = Camera.main.ScreenPointToRay(_touchPoint);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                _interactionManager.TouchedObject = hit.collider.gameObject;
                _interactionManager.ObjectTapped?.Invoke(_interactionManager.TouchedObject);
            }

        }

        /// <summary>
        /// Invokes ObjectReleased event when tap is released on a touched object.
        /// </summary>
        private void HandleTapRelease()
        {
            if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Ended)
            {

                if (_interactionManager.TouchedObject != null)
                {
                    _interactionManager.ObjectReleased?.Invoke(_interactionManager.TouchedObject);
                    _interactionManager.TouchedObject = null;
                }
            }
        }

        /// <summary>
        /// Invokes TouchPositionChanged event the screen is swiped.
        /// Returned Vec2 is normalized screen position
        /// </summary>
        private void HandleSwipe()
        {
            if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Moved)
            {
                if (_interactionManager.TouchedObject != null)
                {
                    Vector2 pos = new Vector2(Input.touches[0].position.x, Input.touches[0].position.y);
                    Vector2 normalizedPos = NormalizedScreenPosition(pos);
                    TouchPositionChanged?.Invoke(normalizedPos);
                }
            }
        }

        /// <summary>
        /// Fires ARRaycastManager.Raycast 
        /// </summary>
        private void DetectARPlane()
        {
            /// Raycast from the center of the screen
            if (ARRaycastManager.Raycast(new Vector2(Screen.width / 2, Screen.height / 2), _hits, _planeTypes))
            {
                bool hitARPlane = false;
                int index = 0;
                float area = 0;

                /// Did we hit any active planes?
                foreach (ARRaycastHit hit in _hits)
                {
                    if (ARPlaneManager.GetPlane(hit.trackableId).gameObject.activeSelf && hit.distance <= _maxRaycastDistance)
                    {
                        hitARPlane = true;

                        /// Optional hit filtering by size
                        if (_filterHitsBySize || _cullBySizeOnHit)
                        {
                            Vector2 size = ARPlaneManager.GetPlane(hit.trackableId).size;
                            if (size.x * size.y > area)
                            {
                                area = size.x * size.y;
                                index = _hits.IndexOf(hit);
                            }
                        }
                    }
                }

                ARPlane plane = hitARPlane ? ARPlaneManager.GetPlane(_hits[index].trackableId) : null;

                /// Optional set the smallest hit planes inactive
                if (plane != null && _cullBySizeOnHit)
                {
                    foreach (ARRaycastHit hit in _hits)
                    {
                        ARPlaneManager.GetPlane(hit.trackableId).gameObject.SetActive(hit.trackableId == plane.trackableId);
                    }
                }

                /// Fire update event on change
                if (plane != _currentARPlane)
                {
                    _currentARPlane = plane;
                    ARPlaneChanged?.Invoke(_currentARPlane);
                }

                /// Fire pose update event
                if (hitARPlane)
                {
                    ARHitPoseUpdated?.Invoke(_hits[0].pose);
                    return;
                }
            }

            /// Fallback
            ARHitPoseUpdated?.Invoke(new Pose(_fallbackPlacementTransform.position, Quaternion.identity));

        }

        /// <summary>
        /// Disables all AR planes other than the current largest plane.
        /// </summary>
        private void CullPlanesBySize()
        {
            float area = 0;
            ARPlane biggestPlane = null;
            foreach (ARPlane plane in AllPlanes)
            {
                if (plane.size.x * plane.size.y > area)
                {
                    biggestPlane = plane;
                    area = plane.size.x * plane.size.y;
                }
            }

            foreach (ARPlane plane in AllPlanes)
                plane.gameObject.SetActive(plane == biggestPlane);
        }

        /// <summary>
        /// Send haptic pulse using AndroidHaptics.
        /// </summary>
        private void VibrateOnce()
        {
            AndroidHaptics.Vibrate(5);
        }

        /// <summary>
        /// Returns Vec2 screen position, normalized.
        /// </summary>
        /// <param name="screenPos"></param>
        /// <returns></returns>
        public Vector2 NormalizedScreenPosition(Vector2 screenPos)
        {
            float x = Remap(screenPos.x, 0, Screen.width, 0, 1);
            float y = Remap(screenPos.y, 0, Screen.height, 0, 1);

            return new Vector2(x, y);
        }

        private float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            float fromAbs = value - fromMin;
            float fromMaxAbs = fromMax - fromMin;

            float normal = fromAbs / fromMaxAbs;

            float toMaxAbs = toMax - toMin;
            float toAbs = toMaxAbs * normal;

            var to = toAbs + toMin;

            return to;
        }
    }
}
