using UnityEngine;
using TMPro;

namespace ubco.hcilab.roadmap
{
    public class AppStateStatus : Singleton<AppStateStatus>
    {
        [Header("References")]
        [SerializeField] TMP_Text appState_text;
        [SerializeField] TMP_Text errorState_text;
        [SerializeField] TMP_Text interactionState_text;

        private void OnEnable()
        {
            OnAppStateChanged(InitManager.Instance.CurrentAppState);
            InitManager.Instance.AppStateChanged.AddListener(OnAppStateChanged);

            OnErrorStateChanged(GeospatialManager.Instance.CurrentErrorState, GeospatialManager.Instance.CurrentErrorMessage);
            GeospatialManager.Instance.ErrorStateChanged.AddListener(OnErrorStateChanged);

            OnInteractionStateChanged(InteractionManager.Instance.CurrentInteractionState);
            InteractionManager.Instance.InteractionStateChanged.AddListener(OnInteractionStateChanged);
        }

        private void OnAppStateChanged(AppState appstate)
        {
            appState_text.text = "App State: (v" + Application.version + "): " + appstate;
        }

        private void OnErrorStateChanged(ErrorState errorState, string message)
        {
            errorState_text.text = "Error State: " + errorState;
        }

        private void OnInteractionStateChanged(InteractionState interactionState)
        {
            interactionState_text.text = "Interaction State: " + interactionState;
        }

    }
}
