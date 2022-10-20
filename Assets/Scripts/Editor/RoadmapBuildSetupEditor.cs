using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Linq;

namespace ubco.hcilab.roadmap.editor
{
    public class RoadmapBuildSetupEditor: EditorWindow
    {
        private static Dictionary<Platform, string> platformLoaderNames = new Dictionary<Platform, string> {
            {Platform.ARCore, "UnityEngine.XR.ARCore.ARCoreLoader"},
            {Platform.Oculus, "Unity.XR.Oculus.OculusLoader"},
        };

        private string buildPath;

        void OnEnable()
        {
            buildPath = EditorPrefs.GetString("OVRBuild_BuiltAPKPath", "Builds/build.apk");
        }

        void OnDisable()
        {
            EditorPrefs.SetString("OVRBuild_BuiltAPKPath", buildPath);
        }

        [MenuItem("Roadmap/BuildAndRunSetup", false, 100)]
        static void Init()
        {
            EditorWindow.GetWindow<RoadmapBuildSetupEditor>(false, "Roadmap Build Setup", true);
        }

        private void OnGUI()
        {
            SceneAsset oculusScene = (SceneAsset) EditorGUILayout.ObjectField("Oculus Scene", RoadmapSettings.instance.oculusScene, typeof(SceneAsset), false);
            SceneAsset arcoreScene = (SceneAsset) EditorGUILayout.ObjectField("ARCore Scene", RoadmapSettings.instance.arcoreScene, typeof(SceneAsset), false);

            if (oculusScene != RoadmapSettings.instance.oculusScene)
            {
                RoadmapSettings.instance.oculusScene = oculusScene;
                RoadmapSettings.instance.Save();
            }
            if (arcoreScene != RoadmapSettings.instance.arcoreScene)
            {
                RoadmapSettings.instance.arcoreScene = arcoreScene;
                RoadmapSettings.instance.Save();
            }

            string newBuildPath = EditorGUILayout.TextField("Built APK Path", buildPath);
            if (buildPath != newBuildPath)
            {
                EditorPrefs.SetString("OVRBuild_BuiltAPKPath", newBuildPath);
            }
            buildPath = newBuildPath;

            Platform prevPlatform = RoadmapSettings.instance.CurrentPlatform();
            Platform currentPlatform = (Platform) EditorGUILayout.EnumPopup("Target platform", prevPlatform);
            if (prevPlatform != currentPlatform)
            {
                RoadmapSettings.instance.SetPlatformm(currentPlatform);
                switch(currentPlatform)
                {
                    case Platform.Oculus:
                        OculusSettings();
                        break;
                    case Platform.ARCore:
                        ARCoreSettings();
                        break;
                }
            }

            if (GUILayout.Button("Start build"))
            {
                switch(currentPlatform)
                {
                    case Platform.Oculus:
                        OculusSettings();
                        EditorApplication.ExecuteMenuItem("Oculus/OVR Build/OVR Build APK... %#k");
                        break;
                    case Platform.ARCore:
                        ARCoreSettings();
                        UnityEngine.Debug.Log("Starting Android Build!");
                        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
                        buildPlayerOptions.scenes = new[] { AssetDatabase.GetAssetPath(RoadmapSettings.instance.arcoreScene) };
                        buildPlayerOptions.options = BuildOptions.None;
                        buildPlayerOptions.target = BuildTarget.Android;
                        buildPlayerOptions.locationPathName = buildPath;
                        BuildPipeline.BuildPlayer(buildPlayerOptions);
                        break;
                }
            }

            if (GUILayout.Button("Deploy"))
            {
                DeployAPK();
            }
        }

        private void OculusSettings()
        {
            SetXRLoader(Platform.Oculus);
            ActivateScene(RoadmapSettings.instance.oculusScene);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        }

        private void ARCoreSettings()
        {
            SetXRLoader(Platform.ARCore);
            ActivateScene(RoadmapSettings.instance.arcoreScene);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        }

        private static void SetXRLoader(Platform platform)
        {
            // See https://forum.unity.com/threads/editor-programmatically-set-the-vr-system-in-xr-plugin-management.972285/
            //RoadmapSettings.instance.Log();
            XRGeneralSettingsPerBuildTarget buildTargetSettings = null;
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out buildTargetSettings);
            XRGeneralSettings settings = buildTargetSettings.SettingsForBuildTarget(BuildTargetGroup.Android);
            foreach (var keyValuePair in platformLoaderNames)
            {
                if (keyValuePair.Key == platform)
                {
                    XRPackageMetadataStore.AssignLoader(settings.Manager, keyValuePair.Value, BuildTargetGroup.Android);
                }
                else
                {
                    XRPackageMetadataStore.RemoveLoader(settings.Manager, keyValuePair.Value, BuildTargetGroup.Android);
                }
            }
            EditorUtility.SetDirty(settings);
        }

        private void ActivateScene(SceneAsset sceneAsset)
        {
            if (sceneAsset == null)
            {
                throw new Exception("Respective scene is empty.");
            }

            string scenepath = AssetDatabase.GetAssetPath(sceneAsset);
            bool found = false;

            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();

            foreach (var editorScene in scenes)
            {
                if (editorScene.path != scenepath)
                {
                    editorScene.enabled = false;
                }
                else
                {
                    found = true;
                    editorScene.enabled = true;
                }
            }

            if (!found)
            {
                scenes.Add(new EditorBuildSettingsScene(scenepath, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private void DeployAPK()
        {
            /// Heavily borrowed from OVR's scripts
            string androidSdkRoot = "";

            bool useEmbedded = EditorPrefs.GetBool("SdkUseEmbedded") || string.IsNullOrEmpty(EditorPrefs.GetString("AndroidSdkRoot"));
            if (useEmbedded)
            {
                androidSdkRoot = Path.Combine(BuildPipeline.GetPlaybackEngineDirectory(BuildTarget.Android, BuildOptions.None), "SDK");
            }
            androidSdkRoot = androidSdkRoot.Replace("/", "\\");

            if (string.IsNullOrEmpty(androidSdkRoot))
            {
                UnityEngine.Debug.LogError("Android SDK root not found");
            }
            
            if (androidSdkRoot.EndsWith("\\") || androidSdkRoot.EndsWith("/"))
            {
                androidSdkRoot = androidSdkRoot.Remove(androidSdkRoot.Length - 1);
            }
            string androidPlatformToolsPath = Path.Combine(androidSdkRoot, "platform-tools");
            string adbPath = Path.Combine(androidPlatformToolsPath, "adb.exe");

            string buildFilePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), buildPath);
            UnityEngine.Debug.Log("Deploying :" + buildFilePath);
            ProcessStartInfo startInfo = new ProcessStartInfo(adbPath, $"install {buildFilePath}");
            startInfo.WorkingDirectory = androidSdkRoot;
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            StringBuilder outputStringBuilder = new StringBuilder("");
            StringBuilder errorStringBuilder = new StringBuilder("");

            Process process = Process.Start(startInfo);
            process.OutputDataReceived += new DataReceivedEventHandler((object sendingProcess, DataReceivedEventArgs args) =>
            {
                // Collect the sort command output.
                if (!string.IsNullOrEmpty(args.Data))
                {
                    // Add the text to the collected output.
                    outputStringBuilder.Append(args.Data);
                    outputStringBuilder.Append(Environment.NewLine);
                };
            });
            process.ErrorDataReceived += new DataReceivedEventHandler((object sendingProcess, DataReceivedEventArgs args) =>
            {
                    // Collect the sort command output.
                    if (!string.IsNullOrEmpty(args.Data))
                {
                        // Add the text to the collected output.
                        errorStringBuilder.Append(args.Data);
                    errorStringBuilder.Append(Environment.NewLine);
                }
            });

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
                do {} while (!process.WaitForExit(100));

                process.WaitForExit();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarningFormat("exception {0}", e.Message);
            }

            int exitCode = process.ExitCode;

            process.Close();

            string outputString = outputStringBuilder.ToString();
            string errorString = errorStringBuilder.ToString();

            outputStringBuilder = null;
            errorStringBuilder = null;

            if (!string.IsNullOrEmpty(errorString))
            {
                if (errorString.Contains("Warning"))
                {
                    UnityEngine.Debug.LogWarning(errorString);
                }
                else
                {
                    UnityEngine.Debug.LogError(errorString);
                }
            }
            else
            {
                UnityEngine.Debug.Log("Done deploying");
            }
        }
    }

    [FilePath("UserSettings/Roadmap.state", FilePathAttribute.Location.ProjectFolder)]
    public class RoadmapSettings : ScriptableSingleton<RoadmapSettings>
    {
        [SerializeField]
        string targetPlatform = Platform.Oculus.ToString();
        [SerializeField] public SceneAsset oculusScene;
        [SerializeField] public SceneAsset arcoreScene;

        [SerializeField]
        List<string> m_Strings = new List<string>();

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
