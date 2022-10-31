using System.IO;
using UnityEditor;
using UnityEngine;

namespace ubco.hcilab.roadmap.editor
{
    public class SetupProjectDefaults
    {
        [MenuItem("Roadmap/Setup Project Defaults", false, 100)]
        static void SetupProject()
        {
            Debug.Log($"Setting bundle name to: 'ca.ubco.hcilab.roadmap'");
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "ca.ubco.hcilab.roadmap");
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, "ca.ubco.hcilab.roadmap");

            if (!Directory.Exists("Assets/StreamingAssets"))
            {
                Directory.CreateDirectory("Assets/StreamingAssets");
            }

            if (AssetDatabase.CopyAsset("Packages/ubc.ok.hcilab.roadmap-unity/Assets/Settings/google-services.json", "Assets/StreamingAssets/google-services.json"))
            {
                Debug.Log($"Copied firebase config (google-services.json)");
            }
            else
            {
                Debug.LogError($"Filed copying firebase config (google-services.json)");
            }

            if (AssetDatabase.CopyAsset("Packages/ubc.ok.hcilab.roadmap-unity/Assets/Settings/GoogleService-Info.plist", "Assets/StreamingAssets/GoogleService-Info.plist"))
            {
                Debug.Log($"Copied firebase config (GoogleService-Info.plist)");
            }
            else
            {
                Debug.LogError($"Filed copying firebase config (GoogleService-Info.plist)");
            }
        }
    }
}
