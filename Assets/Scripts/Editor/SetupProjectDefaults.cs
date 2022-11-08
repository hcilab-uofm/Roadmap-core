using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

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

        [MenuItem("Roadmap/Apply MRTK shader patch", false, 200)]
        static void ApplyPatch()
        {
            string patchFile = Path.Join(Application.dataPath, "../MRTK_StandardShader_fix.patch");
            if (File.Exists(patchFile) || AssetDatabase.CopyAsset("Packages/ubc.ok.hcilab.roadmap-unity/Media/MRTK_StandardShader_fix.patch", "MRTK_StandardShader_fix.patch"))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo("git", $"apply {patchFile}");
                startInfo.WorkingDirectory = Path.GetDirectoryName(patchFile);
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;

                RoadmapBuildSetupEditor.RunProcess(startInfo);
                Debug.Log($"Deleting patch file.");
                File.Delete(patchFile);
            }
            else
            {
                Debug.LogError($"Failed to apply patch");
            }
        }
    }
}
