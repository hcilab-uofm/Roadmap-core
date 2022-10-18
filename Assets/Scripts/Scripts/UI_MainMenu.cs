using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace ubco.hcilab.roadmap
{

    [System.Flags] public enum MainMenuState { None = 0, Minimized = 1, Expanded = 2, SingleIconMode = 4 }

    public class UI_MainMenu : Singleton<UI_MainMenu>
    {

        [Header("Main Menu Buttons")]
        [SerializeField] Button openMenuButton;
        [SerializeField] Button closeMenuButton;
        [SerializeField] Button developerPanelButton;
        [SerializeField] Button infoPanelPanelButton;

        /// Properties
        public MainMenuState CurrentMenuState { get { return _menuState; } }
        [SerializeField] AppState minimizedStates;

        /// Events
        [HideInInspector] public UnityEvent<MainMenuState> MenuStateChanged;

        /// Vars
        [SerializeField] private MainMenuState _menuState = MainMenuState.Expanded;

        // Debug
        [SerializeField] private bool showLogs;

        private void Start()
        {
            // Open / Close Listeners
            openMenuButton.onClick.AddListener(OpenMenu);
            closeMenuButton.onClick.AddListener(CloseMenu);

            // Set App State Listeners
            developerPanelButton.onClick.AddListener(OpenDeveloperPanel);
            infoPanelPanelButton.onClick.AddListener(OpenInfoPanel);

            SetMenuState(_menuState);
        }

        private void OpenMenu() {
            if (showLogs) Debug.Log("OpenMenu");
            UI_MainMenu.Instance.SetMenuState(MainMenuState.Expanded);
        }
        private void CloseMenu() {
            if (showLogs) Debug.Log("CloseMenu");
            UI_MainMenu.Instance.SetMenuState(MainMenuState.Minimized);
            InitManager.Instance.SetAppState(AppState.Viewing);
        }

        public void EvaluateAppState(AppState appState) {

            if (showLogs) Debug.Log("EvaluateAppState " + appState);

            if (minimizedStates.HasFlag(appState))
            {
                if (showLogs) Debug.Log("Has minimized state flag. Minimizing menu.");
                SetMenuState(MainMenuState.Minimized);
            }
            else {
                if (showLogs) Debug.Log("Does not haave minimized state flag. Expanding menu.");
                SetMenuState(MainMenuState.Expanded);
            }
        }

        public void SetMenuState(MainMenuState newState) {
            _menuState = newState;
            MenuStateChanged.Invoke(_menuState);
        }

        // # Set App States
        private void OpenDeveloperPanel() { if (showLogs) Debug.Log("OpenDeveloperPanel"); CloseMenu(); }
        private void OpenInfoPanel() { 
            if (showLogs) Debug.Log("OpenInfoPanel");
            CloseMenu();
            //InitManager.Instance.SetAppState(AppState.Info);
        }

    }
}
