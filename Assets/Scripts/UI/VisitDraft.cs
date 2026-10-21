using System;

[Serializable]
public sealed class VisitDraft
{
    public string clientName;
    public string phone;
    public string address;
    public string visitNotes;
    public string roomName;
    public string openingName;
    public float widthCentimeters;
    public float heightCentimeters;
    public string roomNotes;
    public string productCategory = "Cortinas";
    public string modelName = "Cortina sob medida";
    public string panelType = "DoublePanel";
    public string fabricStyle = "Natural";
    public string fabricMaterialName = "CurtainFabric";
    public string fabricMaterialPath = "Assets/Materials/CurtainFabric.mat";
    public string rodMaterialName = "RodMaterial";
    public string rodMaterialPath = "Assets/Materials/RodMaterial.mat";
    public float foldsPerMeter = 5f;
    public float foldDepthCentimeters = 6f;
    public float foldProfile = 1.15f;
    public float fabricNaturalness = 0.8f;
    public int fabricSeed = 17;
    public float opening;
}

public static class VisitDraftSession
{
    public static VisitDraft Current { get; set; } = new VisitDraft();
    public static string TargetRoomId { get; set; }
}

public static class CatalogSelectionSession
{
    public static bool ReturnToVisit { get; set; }
    public static int ResumeStepIndex { get; set; } = 3;
    public static bool ReturnToAR { get; set; }
    public static bool HasConfiguredCurtain { get; set; }
    public static CurtainViewSettings CurrentConfiguration { get; set; }
    public static UnityEngine.GameObject SelectedCurtainPrefab { get; set; }
    public static UnityEngine.Material SelectedFabricMaterial { get; set; }
    public static UnityEngine.Material SelectedRodMaterial { get; set; }
}
