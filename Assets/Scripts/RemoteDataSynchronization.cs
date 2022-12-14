using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.Networking;
using System.Collections;

namespace ubco.hcilab.roadmap
{
    public class RemoteDataSynchronization : MonoBehaviour
    {
        private const string SERVER_URL = "https://roadmap-ubco-default-rtdb.firebaseio.com/";
        private const string DB_SCENE_DATA = "scene_data";
        private const string DB_SCENES = "scenes";
        private const string DB_GROUPS = "groups";

        // See `docs/_calculating_translation.org` for how this is calculated
        private float vrToArLatitude_coeff = -0.00000578448593347400f;
        private float vrToArLatitude_bias = 49.94559341523613227309f;
        private float vrToArLongitude_coeff = -0.00000908882014213997f;
        private float vrToArLongitude_bias = -119.39037547356433321966f;
        private float vrToArAltitude = 440;
        private float arToVrLatitude_coeff = -172876.20913954381830990314f;
        private float arToVrLatitude_bias = 8634404.85285098478198051453f;
        private float arToVrLongitude_coeff = -110025.28208953527791891247f;
        private float arToVrLongitude_bias = -13135959.74025445058941841125f;
        private float arToVrAltitude = -440;

        private string SceneID()
        {
            return $"{PlaceablesManager.Instance.applicationConfig.BuildKey}";
        }

        private string GroupID()
        {
            if (string.IsNullOrEmpty(PlaceablesManager.Instance.applicationConfig.groupID))
            {
                throw new UnityException($"GroupID not set");
            }
            return $"{PlaceablesManager.Instance.applicationConfig.groupID}";
        }

        /// Run callable after verifying the scene exists in "scenes"
        private void CheckSceneInScenes(System.Action callable)
        {
            ProcessRequest($"/{DB_SCENES}/{SceneID()}", HTTPMethod.GET, (dataString) =>
            {
                if (string.IsNullOrEmpty(dataString) || dataString == "null")
                {
                    ProcessRequest($"/{DB_SCENES}/{SceneID()}/{DB_GROUPS}/{GroupID()}", HTTPMethod.PUT, data: JsonConvert.SerializeObject(true), action: (_) =>
                    {
                        ProcessRequest($"/{DB_GROUPS}/{GroupID()}/{DB_SCENES}/{SceneID()}", HTTPMethod.PUT, data:  JsonConvert.SerializeObject(true), action: (_) =>
                        {
                            callable();
                        });
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
                RemoteStorageData sceneData = new RemoteStorageData(System.DateTime.Now.Ticks, data);

                ProcessRequest($"/{DB_SCENE_DATA}", HTTPMethod.POST, (nameString) =>
                {
                    string name = JsonConvert.DeserializeAnonymousType(nameString, new { name = "" }).name;
                    ProcessRequest($"/{DB_SCENE_DATA}/{name}", HTTPMethod.PUT, (dataString) =>
                    {
                        ProcessRequest($"/{DB_SCENES}/{SceneID()}/{DB_SCENE_DATA}/{name}", HTTPMethod.PUT, null, JsonConvert.SerializeObject(sceneData.commit_time));
                    }, JsonConvert.SerializeObject(sceneData, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
                }, JsonConvert.SerializeObject(true));

            });
        }

        public void SyncWithRemote()
        {
            LocalStorageData localData = PlaceablesManager.Instance.GetLocalStorateData();

            ProcessRemoteStorageData((remoteDataStorage) =>
            {
                LocalStorageData remoteData = remoteDataStorage.GetData();

                Dictionary<string, GroupData> remoteDataDict = remoteData.Groups.ToDictionary(item => item.identifier);
                Dictionary<string, GroupData> localDataDict = localData.Groups.ToDictionary(item => item.identifier);

                Dictionary<string, PlaceableObjectData> remotePlaceables, localPlaceables;
                Dictionary<string, byte> groupStates = new Dictionary<string, byte>(); // 1 - remote, 2 - local, 3 - both

                Dictionary<string, GroupData> groupData = new Dictionary<string, GroupData>();

                localData.Groups.ForEach(_group =>
                {
                    /// Combine data that has common keys in both
                    if (remoteDataDict.ContainsKey(_group.identifier))
                    {
                        localPlaceables = _group.PlaceableDataList.ToDictionary(item => item.identifier);
                        remotePlaceables = remoteDataDict[_group.identifier].PlaceableDataList.ToDictionary(item => item.identifier);

                        /// Also have to decide which data of the group to use.
                        /// Setting up to select based on the one with the placeable with the latest update.
                        long remoteLatestUpdate = remotePlaceables.Select(x => x.Value.lastUpdate).Max();
                        bool useRemote = true;

                        foreach(KeyValuePair<string, PlaceableObjectData> localPlaceableKVP in localPlaceables)
                        {
                            /// When a plceable is in both, pick the one that has the largest timestamp
                            if (remotePlaceables.ContainsKey(localPlaceableKVP.Key))
                            {
                                if (localPlaceableKVP.Value.lastUpdate > remotePlaceables[localPlaceableKVP.Key].lastUpdate)
                                {
                                    remotePlaceables[localPlaceableKVP.Key] = localPlaceableKVP.Value;
                                }
                            }
                            /// Add placeables only in local
                            else
                            {
                                remotePlaceables[localPlaceableKVP.Key] = localPlaceableKVP.Value;
                            }

                            /// Checking if there is a local updated later than remoteLatestUpdate
                            if (localPlaceableKVP.Value.lastUpdate > remoteLatestUpdate)
                            {
                                useRemote = false;
                            }
                        }

                        /// Selecting based on the one with the placeable with the latest update.
                        /// NOTE: The placeable is relative to the group origin, hence,
                        /// the group origin is the one that has to have the coordinate transformation
                        /// placeable data should be usable as is
                        if (useRemote)
                        {
                            groupData[_group.identifier] = remoteDataDict[_group.identifier];
                            groupStates[_group.identifier] = 1;
                        }
                        else
                        {
                            groupData[_group.identifier] = _group;
                            groupStates[_group.identifier] = 2;
                        }

                        /// Making sure the PlaceableDataList is the combined one
                        groupData[_group.identifier].PlaceableDataList = remotePlaceables.Values.ToList();
                    }
                    /// _group is only in localData, add as is
                    else
                    {
                        groupData[_group.identifier] = _group;
                        groupStates[_group.identifier] = 2;
                    }
                });

                /// Adding groups only in remote
                remoteData.Groups.ForEach(_group =>
                {
                    groupData[_group.identifier] = _group;
                    groupStates[_group.identifier] = 1;
                });

                /// if the platforms are different transfrom data
                if (remoteData.LastWrittenPlatform != localData.LastWrittenPlatform)
                {
                    switch (System.Enum.Parse<Platform>(localData.LastWrittenPlatform))
                    {
                        case Platform.Oculus:
                            foreach (var _group in groupData)
                            {
                                if (groupStates[_group.Key] == 1)
                                {
                                    ArToVr(_group.Value);
                                }
                            }
                            break;
                        case Platform.ARCore:
                            foreach (var _group in groupData)
                            {
                                if (groupStates[_group.Key] == 1)
                                {
                                    VrtoAr(_group.Value);
                                }
                            }
                            break;
                        default:
                            throw new System.NotImplementedException();
                    }
                }

                /// localData has the current platform set as LastWrittenPlatform
                LocalStorageData result = new LocalStorageData(groupData.Values.ToList(), localData.LastWrittenPlatform);

                /// Write local data
                PlaceablesManager.Instance.LoadFromLocalStorageData(result);
                /// Write remote data
                SaveSceneData(result);
            });
        }

        public void OverwriteRemote()
        {
            SaveSceneData(PlaceablesManager.Instance.GetLocalStorateData());
        }

        public void OverwriteLocal()
        {
            ProcessRemoteStorageData((remoteData) =>
            {
                LocalStorageData data = remoteData.GetData();
                Platform lastWrittenPlatform = System.Enum.Parse<Platform>(data.LastWrittenPlatform);
                if (lastWrittenPlatform != PlatformManager.Instance.CurrentPlatform)
                {
                    foreach (var _group in data.Groups)
                    {
                        switch (lastWrittenPlatform)
                        {
                            case Platform.Oculus:
                                VrtoAr(_group);
                                break;
                            case Platform.ARCore:
                                ArToVr(_group);
                                break;
                            default:
                                throw new System.NotImplementedException();
                        }
                    }
                }

                PlaceablesManager.Instance.LoadFromLocalStorageData(data);
            });
        }

        public void ProcessRemoteStorageData(System.Action<RemoteStorageData> callback)
        {
            ProcessRequest($"/{DB_SCENES}/{SceneID()}/{DB_SCENE_DATA}", HTTPMethod.GET, (idStrings) =>
            {
                Dictionary<string, long> sceneData = JsonConvert.DeserializeObject<Dictionary<string, long>>(idStrings);
                string sceneDataId = sceneData.OrderByDescending(kvp => kvp.Key).First().Key;
                ProcessRequest($"/{DB_SCENE_DATA}/{sceneDataId}", HTTPMethod.GET, (dataString) =>
                {
                    RemoteStorageData remoteData = JsonUtility.FromJson<RemoteStorageData>(dataString);
                    callback(remoteData);
                });
            });
        }

        protected GroupData VrtoAr(GroupData data)
        {
            data.Latitude = vrToArLatitude_coeff * data.Latitude + vrToArLatitude_bias;
            data.Longitude = vrToArLongitude_coeff * data.Latitude + vrToArLongitude_bias;
            data.Altitude = data.Altitude + vrToArAltitude;
            return data;
        }

        protected GroupData ArToVr(GroupData data)
        {
            data.Latitude = arToVrLatitude_coeff * data.Latitude + arToVrLatitude_bias;
            data.Longitude = arToVrLongitude_coeff * data.Longitude + arToVrLongitude_bias;
            data.Altitude = data.Altitude + arToVrAltitude;
            return data;
        }

        protected void ProcessRequest(string endpoint, HTTPMethod method, System.Action<string> action=null, string data="")
        {
            StartCoroutine(GetJsonUrl(endpoint, method, action, data));
        }

        protected UnityWebRequest GetMethod(HTTPMethod method, string url, string data)
        {
            switch (method)
            {
                case HTTPMethod.GET:
                    return UnityWebRequest.Get(url);
                case HTTPMethod.POST:
                    return UnityWebRequest.Post(url, data);
                case HTTPMethod.PUT:
                    return UnityWebRequest.Put(url, data);
                default:
                    throw new System.NotImplementedException();
            }
        }

        protected IEnumerator GetJsonUrl(string endpoint, HTTPMethod method, System.Action<string> action=null, string data="")
        {
            string url = $"{SERVER_URL}{endpoint}.json";
            using (UnityWebRequest webRequest = GetMethod(method, url, data))
            {
                webRequest.timeout = 5;
                yield return webRequest.SendWebRequest();

                bool error;
#if UNITY_2020_OR_NEWER
                error = webRequest.result != UnityWebRequest.Result.Success;
#else
#pragma warning disable
                error = webRequest.isHttpError || webRequest.isNetworkError;
#pragma warning restore
#endif

                if (error)
                {
                    Debug.LogError($"Request for {url} failed with: {webRequest.error}");
                    yield break;
                }

                if (action != null)
                {
                    action(System.Text.Encoding.UTF8.GetString(webRequest.downloadHandler.data));
                }
            }
        }
    }

    [System.Serializable]
    public class RemoteStorageData
    {
        public long commit_time;
        public string platform;
        public LocalStorageData data;// LocalStorageData

        public RemoteStorageData(long commit_time, string platform, LocalStorageData data)
        {
            this.commit_time = commit_time;
            this.data = data;
            this.platform = platform;
        }

        public RemoteStorageData(long commit_time, LocalStorageData data)
        {
            this.commit_time = commit_time;
            this.data = data;
            this.platform = data.LastWrittenPlatform;
        }

        public LocalStorageData GetData()
        {
            return data;
        }
    }

    public enum HTTPMethod {
        GET, POST, PUT
    }
}
