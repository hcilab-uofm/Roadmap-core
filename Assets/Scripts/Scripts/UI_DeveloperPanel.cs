using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ubco.hcilab.roadmap
{
    public class UI_DeveloperPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Button closeDeveloperPanelButton;

        private void Awake()
        {
            closeDeveloperPanelButton.onClick.AddListener(CloseDeveloperPanel);
        }

        private void CloseDeveloperPanel() {
            InitManager.Instance.SetAppState(InitManager.Instance.PreviousAppState);
        }
    }
}
