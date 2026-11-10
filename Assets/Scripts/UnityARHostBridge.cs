using System.IO;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Receives the selected DecorIt record when AR is opened from the React Native host.</summary>
public static class UnityARHostBridge
{
    [Serializable]
    private sealed class CapturedPhotoEvent
    {
        public string clientId;
        public string roomId;
        public string recordId;
        public string path;
        public string roomName;
        public string capturedAt;
    }

    private static bool hasReadHostedLaunchData;
    public static bool IsHostedByReact { get; private set; }
    public static bool IsPhotoOnly { get; private set; }
    public static bool IsQuickAR { get; private set; }
    public static bool IsCatalogPreview { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OpenRequestedARScene()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        ReadLaunchData();
        if (IsHostedByReact)
            SceneManager.LoadScene(IsCatalogPreview ? "Catalog" : "AR_Test");
#endif
    }

    public static void ReadLaunchData()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intent = activity.Call<AndroidJavaObject>("getIntent"))
            {
                IsHostedByReact = intent.Call<bool>("getBooleanExtra", "decorit_rn_host", false);
                IsPhotoOnly = IsHostedByReact && string.IsNullOrEmpty(intent.Call<string>("getStringExtra", "decorit_room_id"));
                IsQuickAR = IsHostedByReact && intent.Call<bool>("getBooleanExtra", "decorit_quick_ar", false);
                IsCatalogPreview = IsHostedByReact && intent.Call<bool>("getBooleanExtra", "decorit_catalog_preview", false);
                if (!IsHostedByReact) return;
                if (!hasReadHostedLaunchData)
                {
                    CatalogSelectionSession.SelectedCurtainPrefab = null;
                    CatalogSelectionSession.SelectedFabricMaterial = null;
                    CatalogSelectionSession.SelectedRodMaterial = null;
                    hasReadHostedLaunchData = true;
                }
                ClientSession.SelectedClientId = intent.Call<string>("getStringExtra", "decorit_client_id");
                RoomSession.SelectedRoomId = intent.Call<string>("getStringExtra", "decorit_room_id");
                VisitDraftSession.TargetRoomId = RoomSession.SelectedRoomId;
                RecordSession.CurrentRecordId = intent.Call<string>("getStringExtra", "decorit_record_id");
                if (IsCatalogPreview)
                {
                    CatalogSelectionSession.CurrentConfiguration = CurtainViewSettingsStore.LoadLast();
                    VisitDraftSession.Current = new VisitDraft();
                }
                string draftJson = intent.Call<string>("getStringExtra", "decorit_draft_json");
                VisitDraft draft = string.IsNullOrWhiteSpace(draftJson) ? null : JsonUtility.FromJson<VisitDraft>(draftJson);
                if (draft != null) VisitDraftSession.Current = draft;
                if (draft != null && !string.IsNullOrWhiteSpace(draft.modelName))
                {
                    foreach (GameObject model in Resources.LoadAll<GameObject>("ARModels"))
                    {
                        if (model != null && model.name == draft.modelName)
                        {
                            CatalogSelectionSession.SelectedCurtainPrefab = model;
                            break;
                        }
                    }
                }
                if (!IsCatalogPreview) SaveLastViewConfig();
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogError("Não foi possível receber os dados do app DecorIt: " + exception.Message);
        }
#endif
    }

    public static void SaveLastViewConfig()
    {
        try
        {
            string path = Path.Combine(Application.persistentDataPath, "last_ar_config.json");
            File.WriteAllText(path, JsonUtility.ToJson(VisitDraftSession.Current));
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning("Não foi possível salvar a última configuração AR: " + exception.Message);
        }
    }

    public static bool QueuePhotoCapture(string clientId, string roomId, string recordId, string path, string roomName, string capturedAt)
    {
        if (!IsHostedByReact || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(path)) return false;
        try
        {
            string directory = Path.Combine(Application.persistentDataPath, "DecorIt", "PhotoOutbox");
            Directory.CreateDirectory(directory);
            string eventPath = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".json");
            string temporaryPath = eventPath + ".tmp";
            var photoEvent = new CapturedPhotoEvent
            {
                clientId = clientId,
                roomId = roomId ?? string.Empty,
                recordId = recordId ?? string.Empty,
                path = path,
                roomName = roomName ?? string.Empty,
                capturedAt = capturedAt ?? string.Empty,
            };
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(photoEvent));
            File.Move(temporaryPath, eventPath);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError("Não foi possível enviar os dados da foto ao app: " + exception.Message);
            return false;
        }
    }

    public static bool ReturnToReactHost()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!IsHostedByReact) return false;
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intent = new AndroidJavaObject("android.content.Intent"))
            {
                intent.Call<AndroidJavaObject>("setClassName", activity, "com.weslerbecker.decorit.MainActivity");
                intent.Call<AndroidJavaObject>("addFlags", 0x34000000);
                if (IsQuickAR && !string.IsNullOrWhiteSpace(ClientSession.SelectedClientId))
                {
                    intent.Call<AndroidJavaObject>("setAction", "android.intent.action.VIEW");
                    using (var uriClass = new AndroidJavaClass("android.net.Uri"))
                    using (var returnUri = uriClass.CallStatic<AndroidJavaObject>("parse", "decorit://client/" + ClientSession.SelectedClientId))
                    {
                        intent.Call<AndroidJavaObject>("setData", returnUri);
                    }
                }
                activity.Call("startActivity", intent);
                activity.Call("finish");
            }
            return true;
        }
        catch (System.Exception exception)
        {
            Debug.LogError("Não foi possível retornar ao app DecorIt: " + exception.Message);
            return false;
        }
#else
        return false;
#endif
    }
}
