using System;
using UnityEditor;
using UnityEngine;

namespace ubco.hcilab.roadmap.editor
{
    [FilePath("UserSettings/Roadmap.state", FilePathAttribute.Location.ProjectFolder)]
    public class RoadmapSettings : ScriptableSingleton<RoadmapSettings>
    {
        [SerializeField]
        string targetPlatform = Platform.Oculus.ToString();
        [SerializeField] public SceneAsset oculusScene;
        [SerializeField] public SceneAsset arcoreScene;

        [SerializeField] public string groupID;

        public void SetPlatformm(Platform platform)
        {
            targetPlatform = platform.ToString();
            Save(true);
        }

        public Platform CurrentPlatform()
        {
            return Enum.Parse<Platform>(targetPlatform);
        }

        public void Save()
        {
            Save(true);
        }
    }
}
