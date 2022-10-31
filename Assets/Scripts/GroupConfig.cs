using UnityEditor;
using UnityEngine;

namespace ubco.hcilab.roadmap
{
    [FilePath("Settings/RoadmapApplicationGroupConfig.asset", FilePathAttribute.Location.ProjectFolder)]
    public class GroupConfig : ScriptableSingleton<GroupConfig>
    {
        [SerializeField] public string groupID = null;
    }
}
