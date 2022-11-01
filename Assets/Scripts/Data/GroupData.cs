using System.Collections.Generic;

namespace ubco.hcilab
{
    [System.Serializable]
    public class GroupData
    {
        public double Latitude;
        public double Longitude;
        public double Altitude;
        public double Heading;
        public List<PlaceableObjectData> PlaceableDataList;
        public string identifier;

        public GroupData(double latitude, double longitude, double altitude, double heading, List<PlaceableObjectData> placeableDataList)
        {
            Latitude = latitude;
            Longitude = longitude;
            Altitude = altitude;
            Heading = heading;
            PlaceableDataList = placeableDataList;
            identifier = $"group_{System.Guid.NewGuid().ToString()}";
        }
    }
}
