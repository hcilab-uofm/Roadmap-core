using UnityEngine;

namespace ubco.hcilab
{
    [System.Serializable]
    public class PlaceableObjectData
    {
        public string PrefabIdentifier;
        public Pose LocalPose;
        public string AuxData;
        public string identifier;
        public long lastUpdate;

        public PlaceableObjectData(string prefabIdentifier, Pose localPose, string auxData = null)
        {
            PrefabIdentifier = prefabIdentifier;
            LocalPose = localPose;
            AuxData = auxData;
            lastUpdate = System.DateTime.Now.Ticks;
            identifier = $"{prefabIdentifier}_{System.Guid.NewGuid().ToString()}";
        }

        public void Updated()
        {
            lastUpdate = System.DateTime.Now.Ticks;
        }
    }
}
