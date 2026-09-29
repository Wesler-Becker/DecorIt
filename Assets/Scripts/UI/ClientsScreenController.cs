using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ClientsScreenController : MonoBehaviour
{
    [SerializeField] private Transform listContent;
    [SerializeField] private GameObject cardTemplate;

    public void Configure(Transform content, GameObject template) { listContent = content; cardTemplate = template; }

    private void Start()
    {
        foreach (ClientData client in ClientRepository.Load())
        {
            GameObject card = Instantiate(cardTemplate, listContent);
            card.name = "Client_" + client.id;
            card.SetActive(true);
            TMP_Text[] labels = card.GetComponentsInChildren<TMP_Text>(true);
            labels[0].text = client.name;
            labels[1].text = string.IsNullOrWhiteSpace(client.phone) ? "Telefone não informado" : client.phone;
            labels[2].text = client.rooms.Count == 1 ? "1 cômodo cadastrado" : $"{client.rooms.Count} cômodos cadastrados";
            string id = client.id;
            card.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => OpenClient(id));
        }
    }

    public void OpenNewClient() => SceneManager.LoadScene("NewClient");
    public void BackHome() => SceneManager.LoadScene("Home");
    private void OpenClient(string id) { ClientSession.SelectedClientId = id; SceneManager.LoadScene("ClientProfile"); }
}
