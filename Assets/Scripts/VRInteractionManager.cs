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
        [SerializeField] private GameObject menuItemPrefab;
        [SerializeField] private ButtonConfigHelper modifyButton;

        private bool modifying = true;
        private List<PlaceableObject> placedObjects = new List<PlaceableObject>();
        // Start is called before the first frame update
        void Start()
        {
            GridObjectCollection gridObjectCollection = scrollMenu.GetComponentInChildren<GridObjectCollection>();
            foreach (PlaceableContainer item in PlaceablesManager.Instance.applicationConfig.PlacableIdentifierList())
            {
                Debug.Log($"{item}");
                GameObject menuItem = GameObject.Instantiate(menuItemPrefab, gridObjectCollection.transform);
                ButtonConfigHelper buttonConfigHelper = menuItem.GetComponent<ButtonConfigHelper>();
                buttonConfigHelper.SeeItSayItLabelEnabled = false;
                buttonConfigHelper.MainLabelText = item.identifier;
                buttonConfigHelper.OnClick.AddListener(() => {
                    scrollMenu.SetActive(false);
                    PlaceableObject placableObject= PlaceablesManager.Instance.applicationConfig.GetPlacable(item);
                    placedObjects.Add(placableObject);
                    placableObject.Init(item.identifier);
                    placableObject.StartPlacement();
                });
            }
            gridObjectCollection.UpdateCollection();
            // scrollMenu.gameObject.SetActive(true);
        }

        public void ModifyModeToggle()
        {
            modifying = !modifying;
            foreach (PlaceableObject item in placedObjects)
            {
                item.ModifcationActive(modifying);
            }

            if (modifying)
            {
                modifyButton.MainLabelText = "Modify Toggle Off";
            }
            else
            {
                modifyButton.MainLabelText = "Modify Toggle On";
            }
        }
    }
}
