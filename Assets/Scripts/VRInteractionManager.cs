using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace ubco.hcilab.roadmap
{
    public class VRInteractionManager : MonoBehaviour
    {
        [SerializeField] private GameObject scrollMenu;
        [SerializeField] private GameObject syncMenu;
        [SerializeField] private GameObject menuItemPrefab;
        [SerializeField] private ButtonConfigHelper menuButton;
        [SerializeField] private ButtonConfigHelper removeButton;
        [SerializeField] private ButtonConfigHelper modifyButton;
        [SerializeField] private ButtonConfigHelper syncButton;
        [SerializeField] private GameObject dialogPrefab;

        // Start is called before the first frame update
        void Start()
        {
            GridObjectCollection gridObjectCollection = scrollMenu.GetComponentInChildren<GridObjectCollection>();
            foreach (PlaceableContainer item in PlaceablesManager.Instance.applicationConfig.PlacableIdentifierList())
            {
                GameObject menuItem = GameObject.Instantiate(menuItemPrefab, gridObjectCollection.transform);
                ButtonConfigHelper buttonConfigHelper = menuItem.GetComponent<ButtonConfigHelper>();
                buttonConfigHelper.SeeItSayItLabelEnabled = false;
                buttonConfigHelper.MainLabelText = item.identifier;
                buttonConfigHelper.OnClick.AddListener(() => {
                    scrollMenu.SetActive(false);
                    PlaceableObject placableObject= PlaceablesManager.Instance.applicationConfig.GetPlacable(item);
                    placableObject.Init(item.identifier);

                    placableObject.StartPlacement();
                });
            }
            gridObjectCollection.UpdateCollection();

            menuButton.OnClick.AddListener(() =>
            {
                InteractionManager.Instance.SetRemoving(false);
                scrollMenu.SetActive(true);
            });

            removeButton.OnClick.AddListener(InteractionManager.Instance.ToggleRemove);
            InteractionManager.Instance.OnRemoveRequested += (obj) =>
            {
                Dialog dialog = Dialog.Open(dialogPrefab, DialogButtonType.Yes | DialogButtonType.No, null, "Remove Selected Object?", true);
                dialog.OnClosed += (result) =>
                {
                    if (result.Result == DialogButtonType.Yes)
                    {
                        Destroy(obj);
                    }
                };
            };

            // KLUDGE: This is not convoluted *sigh*
            modifyButton.OnClick.AddListener(InteractionManager.Instance.ModifyModeToggle);
            InteractionManager.Instance.OnModificationChanged += (modifying) =>
            {
                if (modifying)
                {
                    modifyButton.MainLabelText = "Moding: Turn On";
                }
                else
                {
                    modifyButton.MainLabelText = "Moding: Turn Off";
                }
            };

            syncButton.OnClick.AddListener(() => syncMenu.SetActive(true));
        }
    }
}
