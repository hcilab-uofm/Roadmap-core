using System;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ubco.hcilab.roadmap.editor
{
    [CustomEditor(typeof(RoadmapApplicationConfig), true)]
    [CanEditMultipleObjects]
    public class RoadmapApplicationConfigEditor : Editor
    {
        RoadmapApplicationConfig config;
        private void OnEnable()
        {
            config = target as RoadmapApplicationConfig;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            if(GUILayout.Button(new GUIContent("Add prefabs from a folder",
                                               "Automatically add files with extension `.prefab` to the `Placables` list.")))
            {
                string path = EditorUtility.OpenFolderPanel("Load prefabs from direcotry", "", "");
                string[] files = Directory.GetFiles(path, "*.prefab", SearchOption.AllDirectories);

                FileSelectionPopup window = (FileSelectionPopup)EditorWindow.GetWindow(typeof(FileSelectionPopup));
                window.SetValues(path, files, () => {
                    foreach (string file in files)
                    {
                        Debug.Log(Path.GetRelativePath(Path.GetDirectoryName(Application.dataPath), file));
                        config.AddPrefab(Path.GetFileNameWithoutExtension(file),
                                         AssetDatabase.LoadAssetAtPath<GameObject>(Path.GetRelativePath(Path.GetDirectoryName(Application.dataPath),
                                                                                                        file)));
                    }
                    EditorUtility.SetDirty(config);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                });
                window.ShowPopup();
            }
            serializedObject.ApplyModifiedProperties();
        }
    }

    class FileSelectionPopup : EditorWindow
    {
        private string path;
        private string[] files;
        private System.Action okCallback;

        public void SetValues(string path, string[] files, System.Action okCallback)
        {
            this.path = path;
            this.files = files;
            this.okCallback = okCallback;
        }

        void OnGUI()
        {
            GUILayout.Label("Add all prefabs in directory:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"From location: {path}         Count: " + files.Length);
            EditorGUILayout.HelpBox(string.Join("\n", files), MessageType.None);
            if(GUILayout.Button("OK"))
            {
                this.okCallback?.Invoke();
                this.Close();
            }

            if(GUILayout.Button("Cancel"))
            {
                this.Close();
            }
        }
    }
}
