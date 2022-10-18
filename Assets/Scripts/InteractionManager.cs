using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace ubco.hcilab.roadmap
{
    public enum InteractionState { None, Placing, Previewing }

    /// <summary>
    /// Singleton Class
    /// Handles AR Plane detection, touch screen input, and current interaction state.
    /// We are constantly raycasting from the camera position, looking for ARPlanes.
    /// When we hit a plane, we emit an event with the world space position of the intersection.
    /// </summary>
    public class InteractionManager : Singleton<InteractionManager>
    {
        [HideInInspector] public Camera mainCamera;

        [SerializeField] private GameObject preview;

        /// <summary>
        /// Get the current Interaction State
        /// </summary>
        public InteractionState CurrentInteractionState { get => interactionState; set => SetInteractionState(value); }

        /// <summary>
        /// ARPlane raycast hits beyond this distace are discarded
        /// </summary>
        public float MaxInteractionDistance { get => _maxRaycastDistance; }

        /// <summary>
        /// Raised whenever the Interaction State is changed
        /// </summary>        
        [HideInInspector] public UnityEvent<InteractionState> InteractionStateChanged;

        /// <summary>
        /// Raised when a Placeable Object is tapped, returns it's GameObject.
        /// </summary> 
        [HideInInspector] public UnityEvent<GameObject> ObjectTapped;

        /// <summary>
        /// Raised when a previously touched Placeable Object is now released, returns its GameObject
        /// </summary>
        [HideInInspector] public UnityEvent<GameObject> ObjectReleased;

        /// <summary>
        /// Fires every frame, returns a Placeable Object if a raycast from the camera hits or null if none
        /// </summary>
        [HideInInspector] public UnityEvent<PlaceableObject> PlaceableHovered;

        /// <summary>
        /// Fires whenever a VibrateOnce is called.
        /// </summary>
        [HideInInspector] public UnityEvent VibratingOnce;

        [HideInInspector] public InteractionState interactionState = InteractionState.None;
        [HideInInspector] public GameObject touchedObject;
        [HideInInspector] public bool run;
        private RaycastHit _raycastHit;
        private Ray _ray;
        [SerializeField] private Button StartButton;
        [SerializeField] private GameObject StartButtonLabel;

        [Tooltip("Filter raycasts for ARPlanes")]
        public LayerMask PlanesLayerMask; /// using: 1

        [Tooltip("Filter raycasts for Placeable Objects")]
        public LayerMask PlaceablesLayerMask; /// using: 3
        
        [Tooltip("Raycast hits beyond this distace are discarded")]
        [SerializeField] private float _maxRaycastDistance = 3;

        private void Start()
        {
            if (StartButton != null)
                StartButton.onClick.AddListener(ChangeMode);
        }

        private void ChangeMode()
        {
            if(StartButtonLabel.GetComponent<TextMeshProUGUI>().text == "Add Model")
            {
                StartButtonLabel.GetComponent<TextMeshProUGUI>().text = "Stop";
                SetInteractionState(InteractionState.Placing);
                run = true;
            }
            else
            {
                StartButtonLabel.GetComponent<TextMeshProUGUI>().text = "Add Model";
                SetInteractionState(InteractionState.None);
                run = false;
            }
        }

        private void Update()
        {
            if (!run) return;

            if(interactionState == InteractionState.Previewing) {
                if (preview != null)
                {
                    preview.SetActive(true);
                }
            }

            /// Raycast for Placeables in scene
            if (interactionState == InteractionState.Placing)
                DetectPlaceables();
        }

        /// <summary>
        /// Invokes PlaceableHovered event when camera is pointed at a Placeable object.
        /// </summary>
        private void DetectPlaceables()
        {
            _ray = Camera.main.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));

            PlaceableObject placeable = null;
            if (Physics.Raycast(_ray, out _raycastHit, _maxRaycastDistance, PlaceablesLayerMask))
            {
                placeable = _raycastHit.collider.transform.GetComponentInParent<PlaceableObject>();
            }

            PlaceableHovered?.Invoke(placeable);
        }

        /// <summary>
        /// Invokes InteractionStateChanged event. Used to control whether we're in Placing or Previewing modes.
        /// </summary>
        public void SetInteractionState(InteractionState interactionState)
        {
            if (this.interactionState != interactionState)
            {
                this.interactionState = interactionState;
                InteractionStateChanged?.Invoke(interactionState);
            }
        }

        /// <summary>
        /// Send haptic pulse using AndroidHaptics.
        /// </summary>
        public void VibrateOnce()
        {
            VibratingOnce?.Invoke();
        }
    }
}
