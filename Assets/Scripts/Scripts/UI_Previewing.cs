using UnityEngine;
using UnityEngine.UI;

namespace ubco.hcilab.roadmap
{
    public class UI_Previewing : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private GameObject preview;

        [Header("Debug")]
        [SerializeField] bool showLogs;

        private void Awake()
        {
            cancelButton.onClick.AddListener(CancelPreview);
            confirmButton.onClick.AddListener(ConfirmPlacement);
        }

        private void CancelPreview()
        {
           
            if (showLogs) Debug.Log("CancelPreview");
            InteractionManager.Instance.SetInteractionState(InteractionState.Placing);
            //InitManager.Instance.SetAppState(AppState.Placing);
            InteractionManager.Instance.VibrateOnce();
            preview.SetActive(false);
            //InitManager.Instance.PlaySound("CancelPlacement");
        }
        private void ConfirmPlacement()
        {
            if (showLogs) Debug.Log("ConfirmPlacement");

            PlaceablesManager.Instance.FinalizeSelectedPlaceable();
            InteractionManager.Instance.SetInteractionState(InteractionState.Placing);
            //InitManager.Instance.SetAppState(AppState.Placing);
            InteractionManager.Instance.VibrateOnce();
            preview.SetActive(false);
            //InitManager.Instance.PlaySound("AcceptPlacement");
        }

    }
}
