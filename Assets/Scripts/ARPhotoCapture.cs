using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;

public class ARPhotoCapture : MonoBehaviour
{
    [SerializeField]
    private Button btnCamera;

    [SerializeField]
    private Canvas arCanvas;

    private void Start()
    {
        btnCamera.onClick.AddListener(TirarFoto);
    }

    private void OnDestroy()
    {
        btnCamera.onClick.RemoveListener(TirarFoto);
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

#if UNITY_ANDROID && !UNITY_EDITOR

            SalvarAndroid(dados, nomeArquivo);

#else

            string caminho =
                Path.Combine(
                    Application.persistentDataPath,
                    nomeArquivo
                );

            File.WriteAllBytes(
                caminho,
                dados
            );

            Debug.Log(
                "Foto salva em: " + caminho
            );

#endif
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