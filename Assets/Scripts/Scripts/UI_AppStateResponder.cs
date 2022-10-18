using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ubco.hcilab.roadmap
{ 

    public class UI_AppStateResponder : UI_StateResponder
    {

        // Properties
        [SerializeField] AppState enabledStates;

        public override void Subscribe()
        {
            StateUpdated(InitManager.Instance.CurrentAppState);
            InitManager.Instance.AppStateChanged.AddListener(StateUpdated);
        }
        public override void Unsubscribe()
        {
            InitManager.Instance.AppStateChanged.RemoveListener(StateUpdated);
        }

        public virtual void StateUpdated(AppState appState)
        {
            if (showLogs) Debug.Log("StateUpdated() ", this);

            bool previousEnabledState = enabledStates.HasFlag(InitManager.Instance.PreviousAppState);
            bool currentEnabledState = enabledStates.HasFlag(appState);

            if (showLogs) Debug.Log("previous state " + InitManager.Instance.PreviousAppState, this);
            if (showLogs) Debug.Log("current state " + appState, this);

            if (showLogs) Debug.Log("previousEnabledState " + previousEnabledState, this);
            if (showLogs) Debug.Log("currentEnabledState" + currentEnabledState, this);

            // If visibility hasn't changed, return
            if (previousEnabledState == currentEnabledState)
                return;

            if (showLogs) Debug.Log("Calling UpdateVisibility(" + currentEnabledState + ")", this);
            UpdateEnabledState(currentEnabledState);

            base.StateUpdated();
        }

    }

}
