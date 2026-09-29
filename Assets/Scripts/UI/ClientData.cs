using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class PhotoData
{
    public string path;
    public string roomName;
    public string recordId;
}

[Serializable]
public sealed class VisitRecordData
{
    public string id;
    public string createdAt;
    public string notes;
    public string openingName;
    public float widthCentimeters;
    public float heightCentimeters;
    public string productCategory;
    public string modelName;
    public string panelType;
    public string fabricMaterialName;
    public string rodMaterialName;
    public List<PhotoData> photos = new List<PhotoData>();
}

[Serializable]
public sealed class RoomData
{
    public string id;
    public string name;
    public string imageLabel;
    public List<VisitRecordData> records = new List<VisitRecordData>();
}

[Serializable]
public sealed class ClientData
{
    public string id;
    public string name;
    public string phone;
    public string address;
    public List<RoomData> rooms = new List<RoomData>();
}

[Serializable]
internal sealed class ClientCollection { public List<ClientData> clients = new List<ClientData>(); }

public static class ClientSession { public static string SelectedClientId { get; set; } }
public static class RoomSession { public static string SelectedRoomId { get; set; } }
public static class RecordSession { public static string CurrentRecordId { get; set; } }

public static class ClientRepository
{
    private static string FilePath => Path.Combine(Application.persistentDataPath, "clients.json");

    public static List<ClientData> Load()
    {
        List<ClientData> clients = null;
        if (File.Exists(FilePath))
        {
            ClientCollection saved = JsonUtility.FromJson<ClientCollection>(File.ReadAllText(FilePath));
            clients = saved?.clients;
        }
        if (clients == null)
        {
            clients = new List<ClientData>
            {
                Make("Maria Oliveira", "(11) 99999-0000", "Rua das Flores, 120", "Sala de estar", "Quarto casal"),
                Make("Carlos Mendes", "(11) 98888-1234", "Av. Central, 45", "Quarto casal"),
                Make("Ana Pereira", "(11) 97777-4567", "Rua do Sol, 89", "Escritório")
            };
            Save(clients);
        }

        bool normalized = false;
        foreach (ClientData client in clients)
        {
            if (client == null) continue;
            if (string.IsNullOrEmpty(client.id)) { client.id = Guid.NewGuid().ToString("N"); normalized = true; }
            if (client.rooms == null) { client.rooms = new List<RoomData>(); normalized = true; }
            foreach (RoomData room in client.rooms)
            {
                if (room == null) continue;
                if (string.IsNullOrEmpty(room.id)) { room.id = Guid.NewGuid().ToString("N"); normalized = true; }
                if (room.records == null) { room.records = new List<VisitRecordData>(); normalized = true; }
                foreach (VisitRecordData record in room.records)
                    if (record != null && record.photos == null) { record.photos = new List<PhotoData>(); normalized = true; }
            }
        }
        if (normalized) Save(clients);
        return clients;
    }

    public static ClientData Find(string id) => string.IsNullOrEmpty(id) ? null : Load().Find(client => client != null && client.id == id);

    public static RoomData FindRoom(string clientId, string roomId) =>
        Find(clientId)?.rooms.Find(room => room != null && room.id == roomId);

    public static ClientData Add(string name, string phone = "", string address = "")
    {
        List<ClientData> clients = Load();
        var client = new ClientData { id = Guid.NewGuid().ToString("N"), name = name.Trim(), phone = phone?.Trim() ?? "", address = address?.Trim() ?? "" };
        clients.Add(client);
        Save(clients);
        return client;
    }

    public static RoomData AddRoom(string clientId, string name)
    {
        List<ClientData> clients = Load();
        ClientData client = clients.Find(item => item.id == clientId);
        if (client == null || string.IsNullOrWhiteSpace(name)) return null;
        var room = new RoomData { id = Guid.NewGuid().ToString("N"), name = name.Trim(), imageLabel = "Sem fotos" };
        client.rooms.Add(room);
        Save(clients);
        return room;
    }

    public static VisitRecordData AddRecord(string clientId, string roomId, VisitDraft draft)
    {
        List<ClientData> clients = Load();
        ClientData client = clients.Find(item => item.id == clientId);
        RoomData room = client?.rooms.Find(item => item.id == roomId);
        if (room == null || draft == null) return null;
        var record = new VisitRecordData
        {
            id = Guid.NewGuid().ToString("N"), createdAt = DateTime.Now.ToString("o"), notes = draft.visitNotes,
            openingName = draft.openingName, widthCentimeters = draft.widthCentimeters, heightCentimeters = draft.heightCentimeters,
            productCategory = draft.productCategory, modelName = draft.modelName, panelType = draft.panelType,
            fabricMaterialName = draft.fabricMaterialName, rodMaterialName = draft.rodMaterialName
        };
        room.records.Add(record);
        Save(clients);
        return record;
    }

    public static bool AddPhoto(string clientId, string roomId, string recordId, string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        List<ClientData> clients = Load();
        ClientData client = clients.Find(item => item.id == clientId);
        RoomData room = client?.rooms.Find(item => item.id == roomId);
        VisitRecordData record = room?.records.Find(item => item.id == recordId);
        if (record == null) return false;
        record.photos.Add(new PhotoData { path = path, roomName = room.name, recordId = record.id });
        int photoCount = room.records.Sum(item => item.photos.Count);
        room.imageLabel = photoCount == 1 ? "1 foto" : $"{photoCount} fotos";
        Save(clients);
        return true;
    }

    public static List<PhotoData> GetPhotos(string clientId)
    {
        var photos = new List<PhotoData>();
        ClientData client = Find(clientId);
        if (client == null) return photos;
        foreach (RoomData room in client.rooms)
            foreach (VisitRecordData record in room.records)
                foreach (PhotoData photo in record.photos)
                    if (photo != null && !string.IsNullOrWhiteSpace(photo.path)) photos.Add(photo);
        return photos;
    }

    private static void Save(List<ClientData> clients)
    {
        Directory.CreateDirectory(Application.persistentDataPath);
        File.WriteAllText(FilePath, JsonUtility.ToJson(new ClientCollection { clients = clients }, true));
    }

    private static ClientData Make(string name, string phone, string address, params string[] rooms)
    {
        var client = new ClientData { id = Guid.NewGuid().ToString("N"), name = name, phone = phone, address = address };
        foreach (string room in rooms) client.rooms.Add(new RoomData { id = Guid.NewGuid().ToString("N"), name = room, imageLabel = "Sem fotos" });
        return client;
    }
}
