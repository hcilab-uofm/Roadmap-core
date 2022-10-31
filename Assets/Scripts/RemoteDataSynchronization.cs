using System.Linq;
using Firebase.Database;
using System.Collections.Generic;
using UnityEngine;

namespace ubco.hcilab.roadmap
{
    public class RemoteDataSynchronization : MonoBehaviour
    {
        private const string DB_SCENE_DATA = "scene_data";
        private const string DB_SCENES = "scenes";
        private const string DB_GROUPS = "groups";

        private DatabaseReference dbReference;
        // Start is called before the first frame update
        void Start()
        {
            // Get the root reference location of the database.
            dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        }

        public void RunWithReference(string reference, System.Action<DataSnapshot> withSnapshot)
        {
            FirebaseDatabase.DefaultInstance.GetReference(reference)
                .GetValueAsync().ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError($"Error in db reading");
                    }
                    else if (task.IsCompleted)
                    {
                        DataSnapshot snapshot = task.Result;
                        withSnapshot(snapshot);
                    }

                });
        }

        private string SceneID()
        {
            return $"{PlaceablesManager.Instance.applicationConfig.BuildKey}";
        }

        private string GroupID()
        {
            if (PlaceablesManager.Instance.applicationConfig.groupID == null)
            {
                throw new UnityException($"GroupID not set");
            }
            return $"{PlaceablesManager.Instance.applicationConfig.groupID}";
        }

        /// Run callable after verifying the scene exists in "scenes"
        private void CheckSceneInScenes(System.Action callable)
        {
            RunWithReference($"/{DB_SCENES}/{SceneID()}", (snapshot) =>
            {    
                if (snapshot == null)
                {
                    // create scene and call
                    SceneInfo sceneInfo = new SceneInfo();
                    sceneInfo.groups[GroupID()] = true;

                    Dictionary<string, object> childUpdates = new Dictionary<string, object>();
                    childUpdates[$"/{DB_SCENES}/{SceneID()}"] = sceneInfo;
                    childUpdates[$"/{DB_GROUPS}/{GroupID()}/{DB_SCENES}/{SceneID()}"] = true;

                    dbReference.UpdateChildrenAsync(childUpdates).ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            Debug.LogError($"Error in db reading");
                        }
                        else if (task.IsCompleted)
                        {
                            /// If the scene was successfully updated, run callable
                            callable();
                        }
                    });
                }
                else
                {
                    callable();
                }
            });
        }

        public void SaveSceneData(LocalStorageData data)
        {
            CheckSceneInScenes(() =>
            {
                string key = dbReference.Child(DB_SCENE_DATA).Push().Key;
                RemoteStorateData sceneData = new RemoteStorateData(System.DateTime.Now.Ticks, data);

                Dictionary<string, object> childUpdates = new Dictionary<string, object>();
                childUpdates[$"/{DB_SCENE_DATA}/{key}"] = sceneData.ToDictionary();
                childUpdates[$"/{DB_SCENES}/{SceneID()}/{DB_SCENE_DATA}/{key}"] = sceneData.commit_time;

                dbReference.UpdateChildrenAsync(childUpdates);
            });
        }

        // TODO
        public void SyncWithRemote()
        {
        }

        public void OverwriteRemote()
        {
            SaveSceneData(PlaceablesManager.Instance.GetLocalStorateData());
        }

        public void OverwriteLocal()
        {
            RunWithReference($"/{DB_SCENES}/{SceneID()}/{DB_SCENE_DATA}", (snapshot) =>
            {
                if (snapshot == null)
                {
                    Debug.LogError($"No scene to sync");
                }
                else
                {
                    Dictionary<string, long> sceneData = (Dictionary<string, long>)snapshot.Value;
                    string sceneDataId = sceneData.OrderByDescending(kvp => kvp.Key).First().Key;
                    RunWithReference($"/{DB_SCENE_DATA}/{sceneDataId}", (snapshot) =>
                    {
                        RemoteStorateData remoteData = JsonUtility.FromJson<RemoteStorateData>(snapshot.GetRawJsonValue());
                        PlaceablesManager.Instance.LoadFromLocalStorageData(remoteData.data);
                    });
                }
            });
        }
    }

    [System.Serializable]
    public class RemoteStorateData
    {
        public long commit_time;
        public string platform;
        public LocalStorageData data;

        public RemoteStorateData(long commit_time, string platform, LocalStorageData data)
        {
            this.commit_time = commit_time;
            this.data = data;
            this.platform = platform;
        }

        public RemoteStorateData(long commit_time, LocalStorageData data)
        {
            this.commit_time = commit_time;
            this.data = data;
            this.platform = data.LastWrittenPlatform;
        }

        public Dictionary<string, object> ToDictionary()
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            result["commit_time"] = commit_time;
            result["data"] = JsonUtility.ToJson(data);
            result["platform"] = platform;
            return result;
        }
    }

    public class SceneInfo
    {
        public Dictionary<string, bool> groups;
        public Dictionary<string, bool> scene_data;
    }
}
