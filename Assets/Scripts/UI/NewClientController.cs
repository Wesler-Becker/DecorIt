using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class NewClientController : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_InputField phoneInput;
    [SerializeField] private TMP_InputField addressInput;
    [SerializeField] private TMP_Text errorLabel;
    public void Configure(TMP_InputField name, TMP_InputField phone, TMP_InputField address, TMP_Text error)
    { nameInput = name; phoneInput = phone; addressInput = address; errorLabel = error; }
    public void Save()
    {
        if (string.IsNullOrWhiteSpace(nameInput.text))
        { errorLabel.text = "Informe o nome do cliente."; return; }
        ClientData client = ClientRepository.Add(nameInput.text.Trim(), phoneInput.text.Trim(), addressInput.text.Trim());
        ClientSession.SelectedClientId = client.id;
        SceneManager.LoadScene("ClientProfile");
    }
    public void Back() => SceneManager.LoadScene("Clients");
}
