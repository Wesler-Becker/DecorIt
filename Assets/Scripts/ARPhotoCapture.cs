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
        UnityARHostBridge.ReadLaunchData();
        ARUIIconFactory.ConfigureCameraButton(btnCamera, "Shutter", 116f, Color.clear, Color.white);
        if (btnCamera != null)
        {
            btnCamera.transform.SetParent(ARUIIconFactory.GetCameraControlsRoot(arCanvas), false);
            RectTransform rect = btnCamera.transform as RectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 84f);
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

            System.DateTime captureDate = System.DateTime.Now;
            string capturedAt = captureDate.ToString("o");
            string nomeArquivo =
                "AR_Cortinas_" +
                captureDate.ToString(
                    "yyyyMMdd_HHmmss_fff"
                ) +
                ".png";

            string clientFolder = GetSafePathSegment(ClientSession.SelectedClientId);
            string caminho =
                Path.Combine(
                    Application.persistentDataPath,
                    "DecorIt",
                    "Fotos",
                    clientFolder,
                    nomeArquivo
                );

            Directory.CreateDirectory(Path.GetDirectoryName(caminho));

            File.WriteAllBytes(
                caminho,
                dados
            );

            bool photoSaved;
            if (UnityARHostBridge.IsHostedByReact)
            {
                photoSaved = UnityARHostBridge.QueuePhotoCapture(
                    ClientSession.SelectedClientId,
                    VisitDraftSession.TargetRoomId,
                    RecordSession.CurrentRecordId,
                    caminho,
                    VisitDraftSession.Current != null ? VisitDraftSession.Current.roomName : string.Empty,
                    capturedAt);
            }
            else if (string.IsNullOrEmpty(VisitDraftSession.TargetRoomId))
            {
                photoSaved = ClientRepository.AddUnassignedPhoto(ClientSession.SelectedClientId, caminho, capturedAt);
            }
            else
            {
                photoSaved = ClientRepository.AddPhoto(ClientSession.SelectedClientId, VisitDraftSession.TargetRoomId,
                    RecordSession.CurrentRecordId, caminho, capturedAt);
            }
            if (photoSaved)
                Debug.Log(string.IsNullOrEmpty(VisitDraftSession.TargetRoomId)
                    ? "Foto associada ao cliente: " + caminho
                    : "Foto associada ao registro: " + caminho);

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
        if (arCanvas == null) return;
        RectTransform controlsRoot = ARUIIconFactory.GetCameraControlsRoot(arCanvas);
        if (controlsRoot.Find("BackToRoomButton") != null) return;
        GameObject buttonObject = new GameObject("BackToRoomButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(controlsRoot, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(0f, 1f); rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -24f); rect.sizeDelta = new Vector2(88f, 88f);
        Image image = buttonObject.GetComponent<Image>(); image.color = new Color(0.06f, 0.09f, 0.12f, 0.82f);
        Button button = buttonObject.GetComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(ReturnFromAR);
        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>(); labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.offsetMin = new Vector2(66f, 4f); labelRect.offsetMax = new Vector2(-8f, -4f);
        TMP_Text label = labelObject.GetComponent<TMP_Text>(); label.text = ""; label.gameObject.SetActive(false);
        GameObject iconObject = new GameObject("BackIcon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(buttonObject.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>(); iconRect.anchorMin = new Vector2(0f, 0.5f); iconRect.anchorMax = new Vector2(0f, 0.5f); iconRect.pivot = new Vector2(0.5f, 0.5f); iconRect.anchoredPosition = new Vector2(37f, 0f); iconRect.sizeDelta = new Vector2(42f, 42f);
        ARUIIconFactory.SetIcon(iconObject.GetComponent<Image>(), "Back");
        iconObject.SetActive(false);
        ARUIIconFactory.ConfigureCameraButton(button, "Close", 88f, Color.clear, Color.white);
    }

    private void ReturnFromAR()
    {
        if (UnityARHostBridge.ReturnToReactHost()) return;
        string destination = string.IsNullOrEmpty(VisitDraftSession.TargetRoomId) ? "Home" : "RoomDetail";
        SceneManager.LoadScene(destination);
    }

    private static string GetSafePathSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "sem_cliente";

        char[] invalidCharacters = Path.GetInvalidFileNameChars();
        char[] characters = value.ToCharArray();
        for (int index = 0; index < characters.Length; index++)
        {
            if (System.Array.IndexOf(invalidCharacters, characters[index]) >= 0)
                characters[index] = '_';
        }

        return new string(characters);
    }
}
