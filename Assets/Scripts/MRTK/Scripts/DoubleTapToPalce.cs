using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using UnityEngine;

namespace ubco.hcilab.roadmap
{
    public class DoubleTapToPalce: TapToPlace, IMixedRealityPointerHandler
    {
        // FIXME: This can cause issues with polymorphism
        /// <inheritdoc/>
        public new void OnPointerClicked(MixedRealityPointerEventData eventdata)
        {
            if (!IsBeingPlaced)
            {
                if ((Time.time - LastTimeClicked) > DoubleClickTimeout)
                {
                    LastTimeClicked = Time.time;
                    return;
                }
                StartPlacement();
            }
            else
            {
                // KLUDGE: Does the double click need to be checked here as well?
                StopPlacement();
            }            
        }
    }
}
