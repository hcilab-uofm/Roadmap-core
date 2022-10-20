using System;
using UnityEngine;
using UnityEngine.Events;

namespace ubco.hcilab.roadmap
{
    public enum Platform { Oculus, ARCore }

    /// <summary>
    /// Singleton Class
    /// Handles switching between different platforms. Depending on which paltform is detected
    /// the appropriate components will be configured and used.
    /// </summary>
    [DefaultExecutionOrder(-100)] // We want all the configs done before any related sripts are executed
    public class PlatformManager : Singleton<PlatformManager>
    {
        [SerializeField] public Platform CurrentPlatform;

        [SerializeField] public Camera OculusCamera;
        [SerializeField] public Camera ARCoreCamera;

        [SerializeField] private GameObject OculusMenu;
        [SerializeField] private GameObject ARCoreMenu;

        [SerializeField] private GameObject PlatformSpecificManagerObject;

        public UnityEvent OculusDetected;
        public UnityEvent ARCoreDetected;

        // public Platform CurrentPlatform { get;private set; }

        void Start()
        {
            // CurrentPlatform = DetectPlatform();
            switch (CurrentPlatform)
            {
                case Platform.ARCore:
                    SetupARCore();
                    // Do any setup to deactivate other platforms
                    SetupOculus(isActive: false);
                    // Call events attached through the UI
                    ARCoreDetected?.Invoke();
                    break;
                case Platform.Oculus:
                    SetupOculus();
                    // Do any setup to deactivate other platforms
                    SetupARCore(isActive: false);
                    // Call events attached through the UI
                    OculusDetected?.Invoke();
                    break;
            }
        }

        // private Platform DetectPlatform()
        // {
        //     return Platform.ARCore;
        // }

        private void SetupARCore(bool isActive=true)
        {
            if (isActive)
            {
                InteractionManager.Instance.mainCamera = ARCoreCamera;
            }

            if (PlatformSpecificManagerObject)
            {
                PlatformSpecificManagerObject.GetComponent<ARInteractionManager>().enabled = isActive;
                PlatformSpecificManagerObject.GetComponent<GeospatialManager>().enabled = isActive;
            }

            if (ARCoreCamera != null)
            {
                ARCoreCamera.transform.root.gameObject.SetActive(isActive);
            }
            if (ARCoreMenu != null)
            {
                ARCoreMenu.gameObject.SetActive(isActive);
            }
        }

        private void SetupOculus(bool isActive=true)
        {
            if (isActive)
            {
                InteractionManager.Instance.mainCamera = OculusCamera;
            }

            if (OculusCamera != null)
            {
                OculusCamera.transform.root.gameObject.SetActive(isActive);
            }
            if (OculusMenu != null)
            {
                OculusMenu.gameObject.SetActive(isActive);
            }
        }
    }
}
