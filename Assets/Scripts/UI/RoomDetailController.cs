using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class RoomDetailController : MonoBehaviour
{
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private Transform recordContent;
    [SerializeField] private GameObject recordTemplate;
    [SerializeField] private Transform photoContent;
    [SerializeField] private GameObject photoTemplate;

    public void Configure(TMP_Text title, Transform records, GameObject recordCard, Transform photos, GameObject photoCard)
    { titleLabel = title; recordContent = records; recordTemplate = recordCard; photoContent = photos; photoTemplate = photoCard; }

    private void Start()
    {
        RoomData room = ClientRepository.FindRoom(ClientSession.SelectedClientId, RoomSession.SelectedRoomId);
        if (room == null) { SceneManager.LoadScene("ClientProfile"); return; }
        titleLabel.text = room.name;
        if (room.records.Count == 0) AddTextCard(recordContent, recordTemplate, "Nenhum registro neste cômodo", "Crie um registro para guardar medidas, modelo e fotos.");
        else foreach (VisitRecordData record in room.records) AddRecord(record);
        bool hasPhotos = false;
        foreach (VisitRecordData record in room.records)
            foreach (PhotoData photo in record.photos) { AddPhoto(photo); hasPhotos = true; }
        if (!hasPhotos) AddTextCard(photoContent, photoTemplate, "Nenhuma foto registrada", "As capturas feitas durante a visualização em RA aparecerão aqui.");
    }

    private void AddRecord(VisitRecordData record)
    {
        GameObject card = Instantiate(recordTemplate, recordContent); card.SetActive(true);
        TMP_Text[] labels = card.GetComponentsInChildren<TMP_Text>(true);
        DateTime.TryParse(record.createdAt, out DateTime date);
        labels[0].text = date == default ? "Registro" : $"Registro · {date:dd/MM/yyyy HH:mm}";
        labels[1].text = $"{record.widthCentimeters:0.#} × {record.heightCentimeters:0.#} cm · {record.modelName}\n{record.photos.Count} fotos";
        Destroy(card.GetComponent<Button>());
    }

    private void AddPhoto(PhotoData photo)
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

    private static void AddTextCard(Transform parent, GameObject template, string title, string subtitle)
    {
        GameObject card = Instantiate(template, parent); card.SetActive(true);
        TMP_Text[] labels = card.GetComponentsInChildren<TMP_Text>(true);
        labels[0].text = title; labels[1].text = subtitle;
        Destroy(card.GetComponent<Button>());
    }

    public void NewRecord()
    {
        VisitDraftSession.TargetRoomId = RoomSession.SelectedRoomId;
        RecordSession.CurrentRecordId = null;
        SceneManager.LoadScene("NewVisit");
    }
    public void Back() => SceneManager.LoadScene("ClientProfile");
}
