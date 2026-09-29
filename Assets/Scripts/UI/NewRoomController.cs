using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class NewRoomController : MonoBehaviour
{
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_Text errorLabel;
    public void Configure(TMP_InputField input, TMP_Text error) { roomNameInput = input; errorLabel = error; }
    public void Save()
    {
        if (string.IsNullOrWhiteSpace(roomNameInput.text)) { errorLabel.text = "Informe o nome do cômodo."; return; }
        RoomData room = ClientRepository.AddRoom(ClientSession.SelectedClientId, roomNameInput.text);
        if (room == null) { errorLabel.text = "Não foi possível salvar o cômodo."; return; }
        SceneManager.LoadScene("ClientProfile");
    }
    public void Back() => SceneManager.LoadScene("ClientProfile");
}
