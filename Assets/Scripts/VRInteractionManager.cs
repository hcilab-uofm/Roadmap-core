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

                    placableObject.tapToPlace.OnPlacingStarted.AddListener(() => InteractionManager.Instance.SetInteractionState(InteractionState.Placing));

                    System.Action callable = () => {
                        InteractionManager.Instance.SetInteractionState(InteractionState.None);
                        placableObject.FinalizePlacement();
                        PlaceablesManager.Instance.ObjectPlaced?.Invoke(placableObject.gameObject);

                        PlaceablesManager.Instance.Save();
                    };

                    placableObject.tapToPlace.OnPlacingStopped.AddListener(() => callable());

                    placableObject.boundsControl.RotateStopped.AddListener(() => callable());

                    placableObject.boundsControl.ScaleStopped.AddListener(() => callable());

                    placableObject.StartPlacement();
                });
            }
            gridObjectCollection.UpdateCollection();
            // scrollMenu.gameObject.SetActive(true);
        }
    }
}
