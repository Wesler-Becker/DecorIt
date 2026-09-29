using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine.SceneManagement;

public class ARPhotoCapture : MonoBehaviour
{
    [SerializeField]
    private Button btnCamera;

    [SerializeField]
    private Canvas arCanvas;

    private void Start()
    {
        ARUIIconFactory.ConfigureButton(btnCamera, "Camera", "Foto", 66f, 22f);
        if (btnCamera != null)
        {
            RectTransform rect = btnCamera.transform as RectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 100f);
        }
        if (btnCamera != null) btnCamera.onClick.AddListener(TirarFoto);
        AddBackButton();
    }

    private void OnDestroy()
    {
        if (btnCamera != null) btnCamera.onClick.RemoveListener(TirarFoto);
    }

    private void TirarFoto()
    {
        StartCoroutine(CapturarTela());
    }

    private IEnumerator CapturarTela()
    {
        // Desativa os botões/interface
        if (arCanvas != null)
        {
            arCanvas.enabled = false;
        }

        yield return new WaitForEndOfFrame();

        Texture2D foto = null;

        try
        {
            // Captura a tela sem a interface
            foto = new Texture2D(
                Screen.width,
                Screen.height,
                TextureFormat.RGB24,
                false
            );

            foto.ReadPixels(
                new Rect(
                    0,
                    0,
                    Screen.width,
                    Screen.height
                ),
                0,
                0
            );

            foto.Apply();

            // Converte para PNG
            byte[] dados = foto.EncodeToPNG();

            string nomeArquivo =
                "AR_Cortinas_" +
                System.DateTime.Now.ToString(
                    "yyyyMMdd_HHmmss"
                ) +
                ".png";

            string caminho =
                Path.Combine(
                    Application.persistentDataPath,
                    nomeArquivo
                );

            File.WriteAllBytes(
                caminho,
                dados
            );

            if (ClientRepository.AddPhoto(ClientSession.SelectedClientId, VisitDraftSession.TargetRoomId,
                    RecordSession.CurrentRecordId, caminho))
                Debug.Log("Foto associada ao registro: " + caminho);

#if UNITY_ANDROID && !UNITY_EDITOR
            SalvarAndroid(dados, nomeArquivo);
#endif

            Debug.Log(
                "Foto salva em: " + caminho
            );
        }
        catch (System.Exception erro)
        {
            Debug.LogError(
                "Erro ao salvar foto: " +
                erro.Message
            );
        }

        // Libera a textura
        if (foto != null)
        {
            Destroy(foto);
        }

        // IMPORTANTE:
        // a interface sempre volta
        if (arCanvas != null)
        {
            arCanvas.enabled = true;
        }
    }

    private void AddBackButton()
    {
        if (arCanvas == null || arCanvas.transform.Find("BackToRoomButton") != null) return;
        GameObject buttonObject = new GameObject("BackToRoomButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(arCanvas.transform, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(0f, 1f); rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(28f, -45f); rect.sizeDelta = new Vector2(190f, 84f);
        Image image = buttonObject.GetComponent<Image>(); image.color = new Color(.12f, .32f, .48f, .94f);
        Button button = buttonObject.GetComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(ReturnFromAR);
        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>(); labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.offsetMin = new Vector2(66f, 4f); labelRect.offsetMax = new Vector2(-8f, -4f);
        TMP_Text label = labelObject.GetComponent<TMP_Text>(); label.text = "Voltar"; label.fontSize = 28f; label.alignment = TextAlignmentOptions.MidlineLeft; label.color = Color.white;
        GameObject iconObject = new GameObject("BackIcon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(buttonObject.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>(); iconRect.anchorMin = new Vector2(0f, 0.5f); iconRect.anchorMax = new Vector2(0f, 0.5f); iconRect.pivot = new Vector2(0.5f, 0.5f); iconRect.anchoredPosition = new Vector2(37f, 0f); iconRect.sizeDelta = new Vector2(42f, 42f);
        ARUIIconFactory.SetIcon(iconObject.GetComponent<Image>(), "Back");
    }

    private void ReturnFromAR()
    {
        string destination = string.IsNullOrEmpty(VisitDraftSession.TargetRoomId) ? "Home" : "RoomDetail";
        SceneManager.LoadScene(destination);
    }

#if UNITY_ANDROID && !UNITY_EDITOR

    private void SalvarAndroid(
    byte[] dados,
    string nomeArquivo)
{
    using (AndroidJavaClass unityPlayer =
           new AndroidJavaClass(
               "com.unity3d.player.UnityPlayer"))
    {
        using (AndroidJavaObject activity =
               unityPlayer.GetStatic<AndroidJavaObject>(
                   "currentActivity"))
        {
            using (AndroidJavaObject resolver =
                   activity.Call<AndroidJavaObject>(
                       "getContentResolver"))
            {
                // URI da coleção de imagens do MediaStore
                using (AndroidJavaClass uriClass =
                       new AndroidJavaClass(
                           "android.net.Uri"))
                {
                    using (AndroidJavaObject collectionUri =
                           uriClass.CallStatic<AndroidJavaObject>(
                               "parse",
                               "content://media/external/images/media"))
                    {
                        using (AndroidJavaObject values =
                               new AndroidJavaObject(
                                   "android.content.ContentValues"))
                        {
                            values.Call(
                                "put",
                                "_display_name",
                                nomeArquivo
                            );

                            values.Call(
                                "put",
                                "mime_type",
                                "image/png"
                            );

                            values.Call(
                                "put",
                                "relative_path",
                                "Pictures/AR Cortinas"
                            );

                            // Cria o arquivo na Galeria
                            using (AndroidJavaObject uri =
                                   resolver.Call<AndroidJavaObject>(
                                       "insert",
                                       collectionUri,
                                       values))
                            {
                                if (uri == null)
                                {
                                    Debug.LogError(
                                        "Não foi possível criar o arquivo na Galeria."
                                    );

                                    return;
                                }

                                // Abre o arquivo para gravação
                                using (AndroidJavaObject outputStream =
                                       resolver.Call<AndroidJavaObject>(
                                           "openOutputStream",
                                           uri))
                                {
                                    if (outputStream == null)
                                    {
                                        Debug.LogError(
                                            "Não foi possível abrir o arquivo para gravação."
                                        );

                                        return;
                                    }

                                    outputStream.Call(
                                        "write",
                                        dados
                                    );

                                    outputStream.Call(
                                        "close"
                                    );
                                }

                                Debug.Log(
                                    "FOTO SALVA NA GALERIA: " +
                                    uri.Call<string>("toString")
                                );
                            }
                        }
                    }
                }
            }
        }
    }
}

#endif
}
