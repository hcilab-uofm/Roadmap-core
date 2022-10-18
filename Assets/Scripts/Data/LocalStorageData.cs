using System.Collections.Generic;

namespace ubco.hcilab
{
    [System.Serializable]
    public class LocalStorageData
    {
        public List<GroupData> Groups;
        public LocalStorageData(List<GroupData> groups)
        {
            Groups = groups;
        }
    }
}
