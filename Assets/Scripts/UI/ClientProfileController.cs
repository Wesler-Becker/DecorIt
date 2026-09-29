using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ClientProfileController : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text contactLabel;
    [SerializeField] private Transform roomContent;
    [SerializeField] private GameObject roomTemplate;
    [SerializeField] private GameObject roomsPanel;
    [SerializeField] private GameObject photosPanel;
    [SerializeField] private Transform photoContent;
    [SerializeField] private GameObject photoTemplate;

    public void Configure(TMP_Text name, TMP_Text contact, Transform content, GameObject template,
        GameObject roomPane, GameObject photoPane, Transform photos, GameObject photoCard)
    {
        nameLabel = name; contactLabel = contact; roomContent = content; roomTemplate = template;
        roomsPanel = roomPane; photosPanel = photoPane; photoContent = photos; photoTemplate = photoCard;
    }

    private void Start()
    {
        ClientData client = ClientRepository.Find(ClientSession.SelectedClientId);
        if (client == null) { SceneManager.LoadScene("Clients"); return; }
        nameLabel.text = client.name;
        string contact = string.Join("\n", new[] { client.phone, client.address }.WhereNonEmpty());
        contactLabel.text = contact;
        contactLabel.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(contact));

        if (client.rooms.Count == 0) AddEmptyRoomMessage();
        else foreach (RoomData room in client.rooms) AddRoomCard(room);

        var photos = ClientRepository.GetPhotos(client.id);
        if (photos.Count == 0) AddEmptyPhotoMessage();
        else foreach (PhotoData photo in photos) AddPhotoCard(photo);
        ShowRooms();
    }

    private void AddRoomCard(RoomData room)
    {
        GameObject card = Instantiate(roomTemplate, roomContent); card.SetActive(true);
        TMP_Text[] labels = card.GetComponentsInChildren<TMP_Text>(true);
        labels[0].text = room.name;
        labels[1].text = $"{room.records.Count} registros · {room.imageLabel}";
        string id = room.id;
        card.GetComponent<Button>().onClick.AddListener(() => OpenRoom(id));
    }

    private void AddEmptyRoomMessage()
    {
        GameObject card = Instantiate(roomTemplate, roomContent); card.SetActive(true);
        TMP_Text[] labels = card.GetComponentsInChildren<TMP_Text>(true);
        labels[0].text = "Nenhum cômodo cadastrado";
        labels[1].text = "Crie um cômodo para começar a organizar os registros.";
        Destroy(card.GetComponent<Button>());
    }

    private void AddPhotoCard(PhotoData photo)
    {
        GameObject card = Instantiate(photoTemplate, photoContent); card.SetActive(true);
        TMP_Text label = card.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = photo.roomName;
        Image image = card.GetComponentInChildren<Image>(true);
        if (image != null && File.Exists(photo.path))
        {
            var texture = new Texture2D(2, 2);
            if (texture.LoadImage(File.ReadAllBytes(photo.path)))
                image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
        }
    }

    private void AddEmptyPhotoMessage()
    {
        GameObject card = Instantiate(photoTemplate, photoContent); card.SetActive(true);
        TMP_Text label = card.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = "As fotos dos registros aparecerão aqui.";
    }

    public void NewRoom() => SceneManager.LoadScene("NewRoom");
    public void ShowRooms() { roomsPanel.SetActive(true); photosPanel.SetActive(false); }
    public void ShowPhotos() { roomsPanel.SetActive(false); photosPanel.SetActive(true); }
    public void Back() => SceneManager.LoadScene("Clients");
    private static void OpenRoom(string id) { RoomSession.SelectedRoomId = id; SceneManager.LoadScene("RoomDetail"); }
}

internal static class ClientStringExtensions
{
    public static string[] WhereNonEmpty(this string[] values) => System.Array.FindAll(values, value => !string.IsNullOrWhiteSpace(value));
}
