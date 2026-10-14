using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Receives the selected DecorIt record when AR is opened from the React Native host.</summary>
public static class UnityARHostBridge
{
    public static bool IsHostedByReact { get; private set; }
    public static bool IsPhotoOnly { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OpenRequestedARScene()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        ReadLaunchData();
        if (IsHostedByReact) SceneManager.LoadScene("AR_Test");
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
                if (!IsHostedByReact) return;
                ClientSession.SelectedClientId = intent.Call<string>("getStringExtra", "decorit_client_id");
                RoomSession.SelectedRoomId = intent.Call<string>("getStringExtra", "decorit_room_id");
                VisitDraftSession.TargetRoomId = RoomSession.SelectedRoomId;
                RecordSession.CurrentRecordId = intent.Call<string>("getStringExtra", "decorit_record_id");
                string draftJson = intent.Call<string>("getStringExtra", "decorit_draft_json");
                VisitDraft draft = string.IsNullOrWhiteSpace(draftJson) ? null : JsonUtility.FromJson<VisitDraft>(draftJson);
                if (draft != null) VisitDraftSession.Current = draft;
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogError("Não foi possível receber os dados do app DecorIt: " + exception.Message);
        }
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
