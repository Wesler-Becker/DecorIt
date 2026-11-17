using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Receives the selected DecorIt record when AR is opened from the React Native host.</summary>
public static class UnityARHostBridge
{
    private static bool hasReadHostedLaunchData;
    public static bool IsHostedByReact { get; private set; }
    public static bool IsPhotoOnly { get; private set; }
    public static bool IsQuickAR { get; private set; }
    public static bool IsCatalogPreview { get; private set; }
    public static bool IsDarkTheme { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OpenRequestedARScene()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        ReadLaunchData();
        string targetScene = IsHostedByReact && IsCatalogPreview ? "Catalog" : "AR_Test";
        LoadStartupScene(targetScene);
#else
        if (SceneManager.GetActiveScene().name == "UnityBootstrap")
            LoadStartupScene("AR_Test");
#endif
    }

    private static void LoadStartupScene(string sceneName)
    {
        if (SceneManager.GetActiveScene().name == sceneName) return;
        Debug.Log("Unity bootstrap abrindo a cena " + sceneName + ".");
        SceneManager.LoadScene(sceneName);
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
                IsDarkTheme = intent.Call<bool>("getBooleanExtra", "decorit_dark_theme", false);
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
        if (VisitDraftSession.Current?.curtainSettings != null)
            SaveCurtainSettings(JsonUtility.ToJson(VisitDraftSession.Current.curtainSettings));
    }

    public static string LoadCurtainSettings()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!IsHostedByReact) return string.Empty;
        try {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                return activity.Call<string>("loadCurtainSettings");
        } catch (Exception error) { Debug.LogError("Não foi possível ler a configuração AR: " + error.Message); }
#endif
        return string.Empty;
    }

    public static void SaveCurtainSettings(string settings)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!IsHostedByReact) return;
        try {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                activity.Call("saveCurtainSettings", settings);
        } catch (Exception error) { Debug.LogError("Não foi possível salvar a configuração AR: " + error.Message); }
#endif
    }

    public static bool QueuePhotoCapture(string clientId, string roomId, string recordId, string path, string roomName, string capturedAt)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!IsHostedByReact || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(path)) return false;
        try
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                return activity.Call<bool>("saveCapturedPhoto", clientId, roomId ?? "", recordId ?? "", path, roomName ?? "", capturedAt ?? "");
        }
        catch (Exception exception)
        {
            Debug.LogError("Não foi possível enviar os dados da foto ao app: " + exception.Message);
            return false;
        }
#else
        return false;
#endif
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
