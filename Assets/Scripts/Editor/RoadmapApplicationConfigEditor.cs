using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ubco.hcilab.roadmap.editor
{
    [CustomEditor(typeof(RoadmapApplicationConfig), true)]
    [CanEditMultipleObjects]
    public class RoadmapApplicationConfigEditor : Editor
    {
        RoadmapApplicationConfig config;
        bool configChanged = false;
        (bool i, bool p) configState;
        SerializedProperty boxDisplayConfigurationProp,
            scaleHandlesConfigurationProp,
            rotationHandlesConfigurationProp;

        private void OnEnable()
        {
            config = target as RoadmapApplicationConfig;
            config.onChanged += () => {
                configChanged = true;
            };

            boxDisplayConfigurationProp = serializedObject.FindProperty("boxDisplayConfiguration");
            scaleHandlesConfigurationProp = serializedObject.FindProperty("scaleHandlesConfiguration");
            rotationHandlesConfigurationProp = serializedObject.FindProperty("rotationHandlesConfiguration");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            if (configChanged)
            {
                configState = config.VerifyDuplicates();
                configChanged = false;
            }

            if (configState.i || configState.p)
            {
                EditorGUILayout.HelpBox($"There are duplicate entries - in identifiers: {configState.i}, in prefabs: {configState.p}", MessageType.Warning);
            }

            EditorGUILayout.BeginHorizontal();

            if (configState.i)
            {
                if (GUILayout.Button(new GUIContent("Remove duplicate identifiers",
                                                    "Keeps only one of the entries with the same name")))
                {
                    config.RemoveDuplicateNames();
                }
            }

            if (configState.p)
            {
                if (GUILayout.Button(new GUIContent("Remove duplicate prefabs",
                                                    "Keeps only one of the entries with the same prefab")))
                {
                    config.RemoveDuplicatePrefabs();
                }
            }
            EditorGUILayout.EndHorizontal();

            if(GUILayout.Button(new GUIContent("Add prefabs from a folder",
                                               "Automatically add files with extension `.prefab` to the `Placables` list.")))
            {
                FileSelectionPopup window = (FileSelectionPopup)EditorWindow.GetWindow(typeof(FileSelectionPopup));

                window.SetValues((files) => {
                    foreach (string file in files)
                    {
                        config.AddPrefab(Path.GetFileNameWithoutExtension(file),
                                         AssetDatabase.LoadAssetAtPath<GameObject>(file));
                    }
                    EditorUtility.SetDirty(config);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                });
                window.ShowPopup();
            }

            if (boxDisplayConfigurationProp.objectReferenceValue == null ||
                scaleHandlesConfigurationProp.objectReferenceValue == null ||
                rotationHandlesConfigurationProp.objectReferenceValue == null)
            {
                if (GUILayout.Button(new GUIContent("Add default values for empty configs",
                                                    "Add default values for the configs.")))
                {
                    if (boxDisplayConfigurationProp.objectReferenceValue == null)
                    {
                        boxDisplayConfigurationProp.objectReferenceValue = (Object)AssetDatabase
                            .LoadAssetAtPath("Packages/ubc.ok.hcilab.roadmap-unity/Assets/Settings/BoxDisplayConfiguration.asset",
                                                                                                                  typeof(Object));
                    }
                    if (scaleHandlesConfigurationProp.objectReferenceValue == null)
                    {
                        scaleHandlesConfigurationProp.objectReferenceValue = (Object)AssetDatabase
                            .LoadAssetAtPath("Packages/ubc.ok.hcilab.roadmap-unity/Assets/Settings/ScaleHandlesConfiguration.asset",
                                             typeof(Object));
                    }
                    if (rotationHandlesConfigurationProp.objectReferenceValue == null)
                    {
                        rotationHandlesConfigurationProp.objectReferenceValue = (Object)AssetDatabase.
                            LoadAssetAtPath("Packages/ubc.ok.hcilab.roadmap-unity/Assets/Settings/RotationHandlesConfiguration.asset",
                                            typeof(Object));
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

    class FileSelectionPopup : EditorWindow
    {
        private string path;
        private string[] files;
        private System.Action<string[]> okCallback;
        private Vector2 scrollPos;
        private Object folder;
        private Object prevFolder;
        private bool checkPath = false;

        public void SetValues(System.Action<string[]> okCallback)
        {
            this.okCallback = okCallback;
        }

        bool CheckFolder()
        {
            return folder != null && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(folder));
        }

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            folder = EditorGUILayout.ObjectField("Location: ", folder, typeof(DefaultAsset), false);
            if (GUILayout.Button("Select folder", GUILayout.Width(100)))
            {
                path = EditorUtility.OpenFolderPanel("Load prefabs from direcotry", "", "");
                path = Path.GetRelativePath(Path.GetDirectoryName(Application.dataPath), path);
                checkPath = true;
            }
            EditorGUILayout.EndHorizontal();

            if (checkPath && !string.IsNullOrEmpty(path))
            {
                folder = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
                if (!CheckFolder())
                {
                    EditorGUILayout.HelpBox("Folder invalid. Consider drag and drop.", MessageType.Error);
                }
            }

            if (CheckFolder())
            {
                checkPath = false;
                path = null;

                if (folder != prevFolder)
                {
                    files = AssetDatabase.FindAssets("t:prefab", new string[] { AssetDatabase.GetAssetPath(folder) })
                        .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                        .ToArray();
                    prevFolder = folder;
                }

                GUILayout.Label("Add all prefabs in directory:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("From location: " + AssetDatabase.GetAssetPath(folder));
                EditorGUILayout.LabelField("Count: " + files.Length);
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                EditorGUILayout.BeginVertical();
                EditorGUILayout.HelpBox(string.Join("\n", files.Select(x => "- " + x)), MessageType.None);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndScrollView();

                if(GUILayout.Button("OK"))
                {
                    this.okCallback?.Invoke(files);
                    this.Close();
                }

                if(GUILayout.Button("Cancel"))
                {
                    this.Close();
                }
            }
            else
            {
                EditorGUILayout.LabelField("Drag and drop a directory from the Project window or use 'Select folder'.");
            }
        }
    }
}
