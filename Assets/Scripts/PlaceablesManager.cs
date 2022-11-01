using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;
using TMPro;
using System;

namespace ubco.hcilab.roadmap
{
    /// <summary>
    /// Singleton Class
    /// Manages PlaceablesGroups and world placement of PlaceableObjects, as well as saving and restoring session data.
    /// </summary>
    public class PlaceablesManager : Singleton<PlaceablesManager>
    {
        [SerializeField] public RoadmapApplicationConfig applicationConfig;

        /// <summary>
        /// Required prefab for instantiating PlaceablesGroups
        /// </summary>
        [SerializeField] private GameObject _placeablesGroupPrefab;

        /// <summary>
        /// Optional visual element shown at current valid placement position
        /// </summary>
        [SerializeField] private GameObject _placingReticlePrefab;

        /// <summary>
        /// If startup geo position is greater than this distance from any previous stored PlaceablesGroup, a new one will be created. 
        /// </summary>
        [Tooltip("Distance (m) at startup past which a new Group Anchor will be created")]
        [SerializeField] private float _maxGroupDistance = 10;

        /// <summary>
        /// Get all PlaceablesGroups that exist
        /// </summary>
        public List<PlaceablesGroup> PlaceablesGroups { get => _placeablesGroups; }

        /// <summary>
        /// Get the current active session PlaceablesGroup
        /// </summary>
        public PlaceablesGroup CurrentGroup { get => _placeablesGroups.Count == 0 ? null : _placeablesGroups[_groupIndex]; }

        /// <summary>
        /// Get the currently selected prefab index
        /// </summary>
        public string CurrentPrefabIdentifier { get => _currentPrefabIdentifier; }

        /// <summary>
        /// Do we currently have a valid placement point?
        /// </summary>
        public bool ValidPlacement { get => _validPlacement; set => _validPlacement = value; }

        /// <summary>
        /// All PlaceablesGroups have finished loading
        /// </summary>
        public bool GroupsLoadingComplete { get => _loadComplete; }

        /// <summary>
        /// Raised when a Placeables position has been finalized
        /// </summary>
        [HideInInspector] public UnityEvent<GameObject> ObjectPlaced;

        /// <summary>
        /// Raised when a PlaceablesGroup has loaded
        /// </summary>
        [HideInInspector] public UnityEvent<PlaceablesGroup> GroupLoaded; /// Is null on first session

        /// <summary>
        /// Raised when all PlaceablesGroups have finished loading
        /// </summary>
        [HideInInspector] public UnityEvent<List<PlaceablesGroup>> AllGroupsLoaded; /// Is null on first session

        private List<PlaceablesGroup> _placeablesGroups = new List<PlaceablesGroup>();
        private int _groupIndex = 0;
        private bool _appStarted,
                     _loadComplete,
                     _validPlacement,
                     _placementBlocked,
                     _saveQueued;

        private GameObject _selectedPlaceable,
                           _placementReticle;

        private string _storageKey = "LocalStorageData",
                       _currentPrefabIdentifier = null,
                       _currentPlatform = null;

        private LocalStorageData newLocalStorageData;

        [SerializeField]
        private TMP_Dropdown typeOptions;
        
        [SerializeField] private Button DevButton;
        [SerializeField] private Button InfoButton;
        [SerializeField] private GameObject DevPanel;

        private string GetSaveFileLocation()
        {
            return System.IO.Path.Combine(Application.persistentDataPath, $"{applicationConfig.BuildKey}_{_storageKey}.json");
        }

        private void Start()
        {
            OnInteractionStateChanged(InteractionManager.Instance.CurrentInteractionState);
            InteractionManager.Instance.InteractionStateChanged.AddListener(OnInteractionStateChanged);

            /// Wipe local storage?
            bool wipePrefs = true;

            if (PlayerPrefs.HasKey("BuildKey") && PlayerPrefs.GetString("BuildKey") == applicationConfig.BuildKey)
                wipePrefs = false;

            if (wipePrefs)
            {
                ClearData();
            }

            // typeOptions.onValueChanged.AddListener((value) => {
            //     UpdatePrefabIdentifier(typeOptions.options[value].text);
            // });

            if (DevButton != null)
            {
                DevButton.onClick.AddListener(DevWebsite);
            }

            if (InfoButton != null)
            {
                InfoButton.onClick.AddListener(InfoWebsite);
            }

            /// Queue Load
            switch(PlatformManager.Instance.CurrentPlatform)
            {
                case Platform.ARCore:
                    GeospatialManager.Instance.InitCompleted.AddListener(OnGeoInitCompleted);
                    break;
                case Platform.Oculus:
                    RestoreSavedPlaceablesGroups();
                    break;
            }

            _currentPlatform = PlatformManager.Instance.CurrentPlatform.ToString();
        }

        /// <summary>
        /// This is called once, on the first accuracy event of the session, so we know it's safe to recall all stored data
        /// </summary>
        private void OnGeoInitCompleted()
        {
            RestoreSavedPlaceablesGroups();
        }

        /// <summary>
        /// Loads all data and then checks distance to all PlaceablesGroups. Creates new Group if needed.
        /// </summary>
        private async void RestoreSavedPlaceablesGroups(LocalStorageData data=null)
        {
            if (PlatformManager.Instance.CurrentPlatform == Platform.ARCore)
            {
                GeospatialManager.Instance.AccuracyImproved.RemoveListener(OnGeoInitCompleted);
            }
            LoadAll(data);

            await Task.Delay(1000);

            /// Find closest group        
            PlaceablesGroup closestGroup = null;
            float distance = Mathf.Infinity;
            bool needNewGroup = false;

            _placeablesGroups.ForEach(group =>
            {
                if (Vector3.Distance(group.transform.position, Camera.main.transform.position) < distance)
                {
                    distance = Vector3.Distance(group.transform.position, Camera.main.transform.position);
                    //Debug.Log("Distance to Group" + _placeablesGroups.IndexOf(group) + ": " + distance);
                    closestGroup = group;
                }
            });

            /// Do we need to create a new Group Anchor?
            needNewGroup = closestGroup == null || distance > _maxGroupDistance;

            if (!needNewGroup) Debug.Log("Using Group @ distance: " + distance);

            if (needNewGroup)
            {
                Debug.Log("Creating new PlaceablesGroup");
                CreateNewPlaceablesGroup().Init();
            }

            _appStarted = true;
        }

        /// <summary>
        /// Instantiates a new PlaceablesGroup from prefab
        /// </summary>
        /// <returns></returns>
        private PlaceablesGroup CreateNewPlaceablesGroup()
        {
            PlaceablesGroup newGroup = Instantiate(_placeablesGroupPrefab).GetComponent<PlaceablesGroup>();
            if (!_placeablesGroups.Contains(newGroup))
                _placeablesGroups.Add(newGroup);

            _groupIndex = _placeablesGroups.IndexOf(newGroup);

            return newGroup;
        }

        /// <summary>
        /// Responds to changes in InteractionManager's Interaction State.
        /// Sets object visibility and position updates based on current state.
        /// </summary>
        /// <param name="interactionState"></param>
        private void OnInteractionStateChanged(InteractionState interactionState)
        {
            switch(PlatformManager.Instance.CurrentPlatform)
            {
                case Platform.ARCore:
                    ARInteractionManager.Instance.ARHitPoseUpdated.RemoveListener(SetCurrentPlaceable);
                    ARInteractionManager.Instance.ARPlaneChanged.RemoveListener(OnARPlaneChanged);
                    break;
                case Platform.Oculus:
                    break;
                default:
                    throw new NotImplementedException();
            }
            InteractionManager.Instance.PlaceableHovered.RemoveListener(OnPlaceableDetected);

            if (interactionState == InteractionState.Placing)
            {
                switch(PlatformManager.Instance.CurrentPlatform)
                {
                    case Platform.ARCore:
                        ARInteractionManager.Instance.ARHitPoseUpdated.AddListener(SetCurrentPlaceable);
                        ARInteractionManager.Instance.ARPlaneChanged.AddListener(OnARPlaneChanged);
                        break;
                    case Platform.Oculus:
                        break;
                    default:
                        throw new NotImplementedException();
                }
                InteractionManager.Instance.PlaceableHovered.AddListener(OnPlaceableDetected);
            }

            SetCurrentVisibility(interactionState == InteractionState.Placing || interactionState == InteractionState.Previewing);
            SetPlacementReticle(interactionState == InteractionState.Placing);
        }

        // TODO: Revisit when placing in VR
        /// <summary>
        /// Determine if we have valid placement (current detected ARPlane is not null)
        /// </summary>
        /// <param name="plane"></param>
        private void OnARPlaneChanged(ARPlane plane)
        {
            _validPlacement = plane != null;

            if (_selectedPlaceable != null)
                _selectedPlaceable.GetComponent<PlaceableObject>().SetErrorMode(!_validPlacement);
        }

        /// <summary>
        /// Determine if placement is being blocked by another PlaceableObject
        /// </summary>
        /// <param name="detected"></param>
        private void OnPlaceableDetected(PlaceableObject detected)
        {
            _placementBlocked = detected != null;
        }

        /// <summary>
        /// Change the currently selected PlaceableObject prefab
        /// </summary>
        /// <param name="index"></param>
        public void UpdatePrefabIdentifier(string index)
        {
            // if (_prefabs.Count == 0 || index >= _prefabs.Count || index < 0) return;

            _currentPrefabIdentifier = index;

            if (_selectedPlaceable != null)
                Destroy(_selectedPlaceable);
        }

        /// <summary>
        /// Update visibility of active PlaceableObject and reticle
        /// </summary>
        /// <param name="visible"></param>
        private void SetCurrentVisibility(bool visible)
        {
            if (_selectedPlaceable != null)
                _selectedPlaceable.SetActive(visible);

            if (_placementReticle != null)
                _placementReticle.SetActive(visible);
        }

        /// <summary>
        /// Create and set placement reticle if needed
        /// </summary>
        /// <param name="visible"></param>
        private void SetPlacementReticle(bool visible)
        {
            if (_placementReticle == null && _placingReticlePrefab != null)
                _placementReticle = Instantiate(_placingReticlePrefab);


            if (_placementReticle != null)
                _placementReticle.SetActive(visible);
        }

        // /// <summary>
        // /// Increment selected prefab index
        // /// </summary>
        // public void SelectNextPrefab()
        // {
        //     UpdatePrefabIndex((_currentPrefabIndex + 1) % _prefabs.Count);
        // }

        // /// <summary>
        // /// Decrement selected prefab index
        // /// </summary>
        // public void SelectPreviousPrefab()
        // {
        //     UpdatePrefabIndex(_currentPrefabIndex - 1 < 0 ? _prefabs.Count - 1 : _currentPrefabIndex - 1);
        // }

        /// <summary>
        /// Change the currently selected PlaceableObject prefab (Public)
        /// </summary>
        /// <param name="newIdentifier"></param>
        public void SetPrefabIdentifier(string newIdentifier)
        {
            UpdatePrefabIdentifier(newIdentifier);
        }

        // TODO: Needs to be used with VR as well?
        /// <summary>
        /// Responds to InteractionManager ARPlane hit update events
        /// Sets visibility, instantiates PlaceableObject prefabs if needed, and updates the active PlaceableObject's world pose
        /// </summary>
        /// <param name="newPose"></param>
        public void SetCurrentPlaceable(Pose newPose)
        {
            bool run = _appStarted && (PlatformManager.Instance.CurrentPlatform != Platform.ARCore ||
                                       GeospatialManager.Instance.CurrentErrorState == ErrorState.NoError);

            SetCurrentVisibility(run);
            if (PlatformManager.Instance.CurrentPlatform == Platform.ARCore && GeospatialManager.Instance.CurrentErrorState != ErrorState.NoError)
                return;

            _placementReticle.SetActive(_validPlacement && !_placementBlocked);

            /// Have all group anchors been resolved?
            if (!run) return;

            if (_selectedPlaceable == null)
            {
                PlaceableObject obj = applicationConfig.GetPlacable(_currentPrefabIdentifier);
                _selectedPlaceable = obj.gameObject;
                obj.Init(_currentPrefabIdentifier);
            }

            /// Update pose
            _selectedPlaceable.transform.position = newPose.position;

            /// Placement reticle
            _placementReticle.transform.position = newPose.position;
            _placementReticle.transform.rotation = newPose.rotation;
        }

        /// <summary>
        /// Called externally to finalize the placement of the active PlaceableObject
        /// </summary>
        public void FinalizeSelectedPlaceable()
        {
            if (_selectedPlaceable != null && !_placementBlocked)
            {
                PlaceableObject placeable = _selectedPlaceable.GetComponent<PlaceableObject>();

                if (placeable.IsPlaceable)
                {
                    placeable.FinalizePlacement();
                    ObjectPlaced?.Invoke(_selectedPlaceable);
                    _selectedPlaceable = null;

                    Save();
                }
            }
        }

        /// <summary>
        /// Remove a group from the list
        /// </summary>
        /// <param name="groupAnchor"></param>
        public void RemoveGroup(PlaceablesGroup groupAnchor)
        {
            if (_placeablesGroups.Contains(groupAnchor))
                _placeablesGroups.Remove(groupAnchor);
        }

        /// <summary>
        /// Public call to serialize and save all current session data
        /// Operation will be queued for next frame
        /// </summary>
        public void Save()
        {
            if (_saveQueued || !_appStarted) return;

            QueueSave();
        }

        /// <summary>
        /// Delay save operation until next frame
        /// </summary>
        private async void QueueSave()
        {
            _saveQueued = true;

            await Task.Yield();

            SaveImmediate();
        }

        /// <summary>
        /// Clear all data in Player Prefs
        /// </summary>
        public void ClearData()
        {
            DestroyAll();
            PlayerPrefs.DeleteAll();
            PlayerPrefs.SetString("BuildKey", applicationConfig.BuildKey);
            PlayerPrefs.Save();
            Debug.Log("Internal Storage Reset");

            if (_appStarted)
            {
                Debug.Log("Creating new PlaceablesGroup");
                CreateNewPlaceablesGroup().Init();
            }
        }

        /// <summary>
        /// Serialize and save all current session data to Player Prefs
        /// </summary>
        private void SaveImmediate()
        {
            int count = 0;

            /// Find out if there's anything to save?
            _placeablesGroups.ForEach(group =>
            {
                group.Placeables.ForEach(p =>
                {
                    if (p.State == PlaceableState.Finalized)
                        count++;
                });
            });

            if (count == 0)
            {
                _saveQueued = false;
                return;
            }

            LocalStorageData storageData = GetLocalStorateData(true);
            string jsonString = JsonUtility.ToJson(storageData);
            PlayerPrefs.SetString(_storageKey, jsonString);

            System.IO.File.WriteAllText(GetSaveFileLocation(), jsonString);
            PlayerPrefs.Save();
            _saveQueued = false;
        }

        public LocalStorageData GetLocalStorateData(bool printInfo=false)
        {
            List<GroupData> groupDataList = new List<GroupData>();

            _placeablesGroups.ForEach(group =>
            {
                groupDataList.Add(group.GroupData);

                group.GroupData.PlaceableDataList = new List<PlaceableObjectData>();

                group.Placeables.ForEach(placeable =>
                {
                    if (placeable.State == PlaceableState.Finalized)
                        group.GroupData.PlaceableDataList.Add(placeable.GetData());
                });

                Debug.Log("Processing Group + " + group.GroupData.PlaceableDataList.Count + " Placeables @ " +
                          group.GroupData.Latitude.ToString("F2") + " | " +
                          group.GroupData.Longitude.ToString("F2") + " | " +
                          group.GroupData.Altitude.ToString("F2"));
            });

            return new LocalStorageData(groupDataList, _currentPlatform);
        }

        /// <summary>
        /// Load and deserialize previous session data from Player Prefs
        /// </summary>
        public void LoadAll(LocalStorageData storageData = null)
        {
            if (storageData == null)
            {
                if (!PlayerPrefs.HasKey(_storageKey))
                {
                    Debug.Log("Nothing to load");
                    GroupLoaded?.Invoke(null);
                    _loadComplete = true;
                    AllGroupsLoaded?.Invoke(null);
                    return;
                }

                // TODO: Use storageData.LastWrittenPlatform to tranlate between coordinate spaces;
                storageData = JsonUtility.FromJson<LocalStorageData>(PlayerPrefs.GetString(_storageKey));
            }

            storageData.Groups.ForEach(groupData =>
            {
                PlaceablesGroup group = CreateNewPlaceablesGroup();
                group.Restore(groupData);

                Transform groupTransform = group.transform;

                groupData.PlaceableDataList.ForEach(placeableData =>
                {
                    PlaceableObject placeable = applicationConfig.GetPlacable(placeableData.PrefabIdentifier, groupTransform);
                    placeable.Restore(placeableData, group);
                });

                Debug.Log("Loaded Group + " + group.Placeables.Count + " Placeables @ " +
                                              groupData.Latitude.ToString("F2") + " | " +
                                              groupData.Longitude.ToString("F2") + " | " +
                                              groupData.Altitude.ToString("F2"));

                GroupLoaded?.Invoke(group);
            });

            _loadComplete = true;
            AllGroupsLoaded?.Invoke(_placeablesGroups);
        }

        /// <summary>
        /// Destroy all PlaceablesGroups in the scene
        /// </summary>
        public void DestroyAll()
        {
            for (int i = _placeablesGroups.Count - 1; i >= 0; i--)
            {
                Destroy(_placeablesGroups[i].gameObject);
                _placeablesGroups.Remove(_placeablesGroups[i]);
            }

            _groupIndex = 0;
        }

        public void LoadFromLocalStorageData(LocalStorageData data)
        {
            /// This is called from the serverthread, need to updaate in mainthread,
            /// hence saving and checking in update.
            newLocalStorageData = data;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                SaveImmediate();
        }

        private void OnApplicationQuit()
        {
            if (_appStarted)
                SaveImmediate();
        }

        private void DevWebsite()
        {
            DevPanel.SetActive(true);
        }

        private void InfoWebsite()
        {
            Application.OpenURL("https://github.com/hcilab-uofm/Roadmap/blob/main/README.md");
            //Application.OpenURL("https://lyonscrawl.github.io/");
        }

        void Update()
        {
            if (newLocalStorageData != null)
            {
                DestroyAll();
                RestoreSavedPlaceablesGroups(newLocalStorageData);
                SaveImmediate();
                newLocalStorageData = null;
            }
        }
    }
}
