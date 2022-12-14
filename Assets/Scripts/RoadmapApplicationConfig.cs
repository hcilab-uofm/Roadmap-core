using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ubco.hcilab.roadmap
{
    [CreateAssetMenu(fileName = "Data", menuName = "Roadmap/RoadmapApplicationData", order = 1)]
    public class RoadmapApplicationConfig : ScriptableObject
    {
        [SerializeField] public string identifier = "Data";

        [HideInInspector] [SerializeField] public string groupID;

        [Tooltip("Changing this key will wipe all saved data first time a new build is run")]
        [SerializeField] private string _buildKey = "00001";

        /// <summary>
        /// PlaceableObject prefabs available to be instantiated. At least 1 is required.
        /// Defaults to index 0. Custom UI needed to change during runtime.
        /// </summary>
        [SerializeField] private List<PlaceableContainer> placables = new List<PlaceableContainer>();

        public System.Action onChanged;
        [SerializeField][HideInInspector] private BoxDisplayConfiguration boxDisplayConfiguration;
        [SerializeField][HideInInspector] private ScaleHandlesConfiguration scaleHandlesConfiguration;
        [SerializeField][HideInInspector] private RotationHandlesConfiguration rotationHandlesConfiguration;
        [SerializeField][HideInInspector] private TranslationHandlesConfiguration translationHandlesConfig;

        public string BuildKey { get => identifier + _buildKey + groupID; private set => _buildKey = value; }

        public PlaceableObject GetPlacable(string identifier, Transform parent=null)
        {
            PlaceableContainer placable = null;
            foreach (PlaceableContainer item in placables)
            {
                if (item.identifier == identifier)
                {
                    placable = item;
                }
            }

            if (placable == null)
            {
                return null;
            }
            return SetupPrefab(placable, parent);
        }

        public PlaceableObject GetPlacable(PlaceableContainer placable, Transform parent=null)
        {
            return SetupPrefab(placable, parent);
        }

        internal IEnumerable<PlaceableContainer> PlacableIdentifierList()
        {
            return placables;
        }

        private PlaceableObject SetupPrefab(PlaceableContainer placeable, Transform parent)
        {
            GameObject newObject = GameObject.Instantiate(placeable.prefab, parent);

            AddBoundsToAllChildren(newObject);

            SetupMRTKControls(newObject);

            if (newObject.GetComponent<PlaceableObject>() == null)
            {
                newObject.AddComponent<PlaceableObject>();
            }

            PlaceableObject obj = newObject.GetComponent<PlaceableObject>();
            obj.Init(placeable.identifier);
            return obj;
        }

        // From https://gamedev.stackexchange.com/questions/129116/how-to-create-a-box-collider-that-surrounds-an-object-and-its-children
        private void AddBoundsToAllChildren(GameObject newObject)
        {
            Collider collider;
            collider = newObject.GetComponent<Collider>();
            if (collider != null)
            {
                return;
            }
            else
            {
                collider = newObject.AddComponent<BoxCollider>();   
            }
            BoxCollider boxCol = (BoxCollider)collider;
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            Renderer thisRenderer = newObject.transform.GetComponent<Renderer>();
            if (thisRenderer != null)
            {
                bounds.Encapsulate(thisRenderer.bounds);
                boxCol.center = bounds.center - newObject.transform.position;
                boxCol.size = bounds.size;
            }

            var allDescendants = newObject.GetComponentsInChildren<Transform>();
            foreach (Transform desc in allDescendants)
            {
                Renderer childRenderer = desc.GetComponent<Renderer>();
                if (childRenderer != null)
                {
                    bounds.Encapsulate(childRenderer.bounds);
                }
                boxCol.center = bounds.center - newObject.transform.position;
                boxCol.size = bounds.size;
            }
        }

        private void SetupMRTKControls(GameObject newObject)
        {
            if (newObject.GetComponent<DoubleTapToPalce>() == null)
            {
                DoubleTapToPalce tapToPlace = newObject.AddComponent<DoubleTapToPalce>();
                tapToPlace.DefaultPlacementDistance = 10;
                tapToPlace.MaxRaycastDistance = 50;
                tapToPlace.UseDefaultSurfaceNormalOffset = false;

                SolverHandler solverHandler = newObject.GetComponent<SolverHandler>();
                // switch (PlatformManager.Instance.CurrentPlatform)
                // {
                //     case Platform.Oculus:
                //         solverHandler.TrackedTargetType = Microsoft.MixedReality.Toolkit.Utilities.TrackedObjectType.ControllerRay;
                //         break;
                //     case Platform.ARCore:
                //         solverHandler.TrackedTargetType = Microsoft.MixedReality.Toolkit.Utilities.TrackedObjectType.Head;
                //         break;
                // }
                /// Note Using ControllerRay so the MRTK Pointer will be used
                solverHandler.TrackedTargetType = Microsoft.MixedReality.Toolkit.Utilities.TrackedObjectType.ControllerRay;
            }

            if (newObject.GetComponent<BoundsControl>() == null)
            {
               BoundsControl boundsControl = newObject.AddComponent<BoundsControl>();
               boundsControl.BoundsControlActivation = Microsoft.MixedReality.Toolkit.UI.BoundsControlTypes.BoundsControlActivationType.ActivateByPointer;
               boundsControl.BoxDisplayConfig = boxDisplayConfiguration;
               boundsControl.RotationHandlesConfig = rotationHandlesConfiguration;
               boundsControl.ScaleHandlesConfig = scaleHandlesConfiguration;
               boundsControl.TranslationHandlesConfig = translationHandlesConfig;
            }

            // if (newObject.GetComponent<MinMaxScaleConstraint>() == null)
            // {
            //     MinMaxScaleConstraint scaleConstraint = newObject.AddComponent<MinMaxScaleConstraint>();
            //     scaleConstraint.ScaleMaximum = 1;
            // }

            // if (newObject.GetComponent<RotationAxisConstraint>() == null)
            // {
            //     newObject.AddComponent<RotationAxisConstraint>();
            // }

            Interactable interactable = newObject.AddComponent<Interactable>();

            // Get the default configuration for the Theme engine InteractableColorTheme
            var newThemeType = ThemeDefinition.GetDefaultThemeDefinition<InteractableColorTheme>().Value;

            // Define a color for every state in our Default Interactable States
            newThemeType.StateProperties[0].Values = new List<ThemePropertyValue>()
            {
                new ThemePropertyValue() { Color = Color.white},  // Default
                new ThemePropertyValue() { Color = Color.white},  // Focus
                new ThemePropertyValue() { Color = Color.white},  // Pressed
                new ThemePropertyValue() { Color = Color.white},  // Disabled
            };

            interactable.Profiles = new List<InteractableProfileItem>()
            {
                new InteractableProfileItem()
                {
                    Themes = new List<Theme>()
                    {
                        Interactable.GetDefaultThemeAsset(new List<ThemeDefinition>() { newThemeType })
                    },
                    Target = newObject,
                },
            };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            onChanged?.Invoke();
            ConfigureMRTKAssets();
        }

        private void ConfigureMRTKAssets()
        {
            if (boxDisplayConfiguration == null)
            {
                boxDisplayConfiguration = (BoxDisplayConfiguration)AssetDatabase
                    .LoadAssetAtPath("Packages/ubc.ok.hcilab.roadmap-unity/Assets/Settings/BoxDisplayConfiguration.asset",
                                     typeof(BoxDisplayConfiguration));
            }
            if (scaleHandlesConfiguration == null)
            {
                scaleHandlesConfiguration = (ScaleHandlesConfiguration)AssetDatabase
                    .LoadAssetAtPath("Packages/ubc.ok.hcilab.roadmap-unity/Assets/Settings/ScaleHandlesConfiguration.asset",
                                     typeof(ScaleHandlesConfiguration));
            }
            if (rotationHandlesConfiguration == null)
            {
                rotationHandlesConfiguration = (RotationHandlesConfiguration)AssetDatabase.
                    LoadAssetAtPath("Packages/ubc.ok.hcilab.roadmap-unity/Assets/Settings/RotationHandlesConfiguration.asset",
                                    typeof(RotationHandlesConfiguration));
            }
            if (translationHandlesConfig == null)
            {
                translationHandlesConfig = (TranslationHandlesConfiguration)AssetDatabase.
                    LoadAssetAtPath("Packages/ubc.ok.hcilab.roadmap-unity/Assets/Settings/TranslationHandlesConfiguration.asset",
                                    typeof(TranslationHandlesConfiguration));
            }
        }

        public void AddPrefab(string identifier, GameObject prefab)
        {
            placables.Add(new PlaceableContainer(identifier, prefab));
        }

        public (bool, bool) VerifyDuplicates()
        {
            bool identifier = placables.GroupBy(x => x.identifier).Any(g => g.Count() > 1);
            bool prefabs = placables.GroupBy(x => x.prefab).Any(g => g.Count() > 1);
            return (identifier, prefabs);
        }

        public void RemoveDuplicateNames()
        {
            placables = placables.GroupBy(x => x.identifier).Select(g => g.First()).ToList();
            OnValidate();
        }

        public void RemoveDuplicatePrefabs()
        {
            placables = placables.GroupBy(x => x.prefab).Select(g => g.First()).ToList();
            OnValidate();
        }
#endif
    }

    [System.Serializable]
    public class PlaceableContainer
    {
        public string identifier;
        public GameObject prefab;

        public PlaceableContainer(string identifier, GameObject prefab)
        {
            this.identifier = identifier;
            this.prefab = prefab;
        }
    }
}
