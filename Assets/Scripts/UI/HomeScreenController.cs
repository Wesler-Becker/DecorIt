using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class HomeScreenController : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Button newVisitButton;
    [SerializeField] private UnityEngine.UI.Button clientsButton;
    [SerializeField] private UnityEngine.UI.Button arButton;
    [SerializeField] private UnityEngine.UI.Button syncButton;
    [SerializeField] private GameObject noticePanel;
    [SerializeField] private TMP_Text noticeText;

    private Coroutine noticeRoutine;

    public void Configure(
        UnityEngine.UI.Button newVisit,
        UnityEngine.UI.Button clients,
        UnityEngine.UI.Button ar,
        UnityEngine.UI.Button sync,
        GameObject notice,
        TMP_Text noticeLabel)
    {
        newVisitButton = newVisit;
        clientsButton = clients;
        arButton = ar;
        syncButton = sync;
        noticePanel = notice;
        noticeText = noticeLabel;
    }

    private void Awake()
    {
        if (newVisitButton != null && newVisitButton.image != null)
            newVisitButton.image.raycastTarget = true;

        newVisitButton?.onClick.AddListener(OnNewVisit);
        clientsButton?.onClick.AddListener(OnClients);
        arButton?.onClick.AddListener(OpenAR);
        syncButton?.onClick.AddListener(OnSync);

        if (noticePanel != null)
            noticePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        newVisitButton?.onClick.RemoveListener(OnNewVisit);
        clientsButton?.onClick.RemoveListener(OnClients);
        arButton?.onClick.RemoveListener(OpenAR);
        syncButton?.onClick.RemoveListener(OnSync);
    }

    private void OnNewVisit()
    {
        ClientSession.SelectedClientId = null;
        SceneManager.LoadScene("Clients");
    }

    private void OnClients()
    {
        ClientSession.SelectedClientId = null;
        SceneManager.LoadScene("Clients");
    }

    private void OnSync()
    {
        ShowNotice("Sincronização será conectada ao armazenamento local.");
    }

    private void OpenAR()
    {
        SceneManager.LoadScene("AR_Test");
    }

    private void ShowNotice(string message)
    {
        if (noticePanel == null || noticeText == null)
            return;

        if (noticeRoutine != null)
            StopCoroutine(noticeRoutine);

        noticeRoutine = StartCoroutine(ShowNoticeRoutine(message));
    }

    private IEnumerator ShowNoticeRoutine(string message)
    {
        noticeText.text = message;
        noticePanel.SetActive(true);
        yield return new WaitForSeconds(2.5f);
        noticePanel.SetActive(false);
        noticeRoutine = null;
    }
}
