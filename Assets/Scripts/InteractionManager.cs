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
        [HideInInspector] public GameObject TouchedObject { get => touchedObject; set
            {
                touchedObject = value;
                OnTouchObjectChanged?.Invoke(touchedObject);
            }
        }
        [HideInInspector] public bool run;
        private RaycastHit _raycastHit;
        private Ray _ray;

        [Tooltip("Filter raycasts for ARPlanes")]
        public LayerMask PlanesLayerMask; /// using: 1

        [Tooltip("Filter raycasts for Placeable Objects")]
        public LayerMask PlaceablesLayerMask; /// using: 3
        
        [Tooltip("Raycast hits beyond this distace are discarded")]
        [SerializeField] private float _maxRaycastDistance = 3;

        private bool modifying = true;
        private bool removing = false;
        private GameObject touchedObject;

        public System.Action<bool> OnChangeMode;
        public System.Action<bool> OnModificationChanged;
        public System.Action<GameObject> OnTouchObjectChanged;
        public System.Action<GameObject> OnRemoveRequested;

        private void Start()
        {

        }

        public void ChangeMode()
        {
            if(!run)
            {
                SetInteractionState(InteractionState.Placing);
                run = true;
            }
            else
            {
                SetInteractionState(InteractionState.None);
                run = false;
            }
            OnChangeMode?.Invoke(run);
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

        /// <summary>
        /// Callback for buttons to toggle the modifications state of Placeables
        /// </summary>
        public void ModifyModeToggle()
        {
            SetRemoving(false);
            modifying = !modifying;
            foreach (PlaceablesGroup _group in PlaceablesManager.Instance.PlaceablesGroups)
            {
                foreach (PlaceableObject item in _group.Placeables)
                {
                    item.ModifcationActive(modifying);
                }
            }
            OnModificationChanged?.Invoke(modifying);
        }

        /// <summary>
        /// Callback for buttons to toggle the remove on interaction.
        /// </summary>
        public void ToggleRemove()
        {
            SetRemoving(!removing);
        }

        public void SetRemoving(bool removing)
        {
            this.removing = removing;
            if (this.removing)
            {
                TouchedObject = null;
                OnTouchObjectChanged += RemoveOnTouch;
            }
            else
            {
                OnTouchObjectChanged -= RemoveOnTouch;
            }
        }

        /// Wrapper for the callback used in ToggleRemove
        private void RemoveOnTouch(GameObject obj)
        {
            OnRemoveRequested?.Invoke(obj);
        }
    }
}
