using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

namespace ubco.hcilab.roadmap
{
    public class VR_UI_RemoteSync : MonoBehaviour
    {
        [SerializeField] private GameObject dialogPrefab;
        [SerializeField] private ButtonConfigHelper syncButton;
        [SerializeField] private ButtonConfigHelper overwriteRemoteButton;
        [SerializeField] private ButtonConfigHelper overwriteLocalButton;
        
        private RemoteDataSynchronization remoteSynchronization;
        
        // Start is called before the first frame update
        void Start()
        {
            remoteSynchronization = GetComponent<RemoteDataSynchronization>();
            syncButton.OnClick.AddListener(() =>
            {
                WithYesNoDialog(
                    () => { remoteSynchronization.SyncWithRemote(); },
                    "Sync remote and local? Will combine what's in the remote and local");
            });

            overwriteRemoteButton.OnClick.AddListener(() =>
            {
                WithYesNoDialog(
                    () => { remoteSynchronization.OverwriteRemote(); },
                    "Overwrite remote data with local data? Data on remote will be ignored.");
            });

            overwriteLocalButton.OnClick.AddListener(() =>
            {
                WithYesNoDialog(
                    () => { remoteSynchronization.OverwriteLocal(); },
                    "Overwrite local data with remote data? Local data will be ignored.");
            });
        }

        private void WithYesNoDialog(System.Action onYesCallback, string message)
        {
            Dialog dialog = Dialog.Open(dialogPrefab, DialogButtonType.Yes | DialogButtonType.No, null, message, true);
            dialog.OnClosed += (result) =>
            {
                if (result.Result == DialogButtonType.Yes)
                {
                    onYesCallback();
                }
            };
        }
    }
}
