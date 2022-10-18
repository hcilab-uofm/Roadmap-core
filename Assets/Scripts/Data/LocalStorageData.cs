using System.Collections.Generic;

namespace ubco.hcilab
{
    [System.Serializable]
    public class LocalStorageData
    {
        public List<GroupData> Groups;
        public string LastWrittenPlatform;
        public LocalStorageData(List<GroupData> groups, string lastWrittenPlatform)
        {
            Groups = groups;
            LastWrittenPlatform = lastWrittenPlatform;
        }
    }
}
