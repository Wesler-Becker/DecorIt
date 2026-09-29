using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class CatalogScreenController : MonoBehaviour
{
    [SerializeField] private TMP_Text categoryDescription;
    [SerializeField] private TMP_Text modelDescription;
    [SerializeField] private TMP_Text selectionSummary;
    [SerializeField] private UnityEngine.UI.Image fabricSwatch;
    [SerializeField] private UnityEngine.UI.Image rodSwatch;
    [SerializeField] private UnityEngine.UI.Button singlePanelButton;
    [SerializeField] private UnityEngine.UI.Button doublePanelButton;
    [SerializeField] private UnityEngine.UI.Button applyButton;
    [SerializeField] private GameObject curtainPrefab;
    [SerializeField] private Material fabricMaterial;
    [SerializeField] private Material rodMaterial;

    public void Configure(TMP_Text category, TMP_Text model, TMP_Text summary,
        UnityEngine.UI.Image fabricColor, UnityEngine.UI.Image rodColor,
        UnityEngine.UI.Button single, UnityEngine.UI.Button doublePanel, UnityEngine.UI.Button apply,
        GameObject curtain, Material fabric, Material rod)
    {
        categoryDescription = category;
        modelDescription = model;
        selectionSummary = summary;
        fabricSwatch = fabricColor;
        rodSwatch = rodColor;
        singlePanelButton = single;
        doublePanelButton = doublePanel;
        applyButton = apply;
        curtainPrefab = curtain;
        fabricMaterial = fabric;
        rodMaterial = rod;
    }

    private void Start()
    {
        VisitDraft draft = VisitDraftSession.Current;
        if (fabricMaterial != null) fabricSwatch.color = fabricMaterial.color;
        if (rodMaterial != null) rodSwatch.color = rodMaterial.color;
        SetCategory("Cortinas");
        UpdateSelection(draft);
    }

    public void ShowCurtains() => SetCategory("Cortinas");
    public void ShowBlinds() => SetCategory("Persianas");
    public void ShowAccessories() => SetCategory("Acessórios");

    public void SelectSinglePanel()
    {
        VisitDraftSession.Current.productCategory = "Cortinas";
        VisitDraftSession.Current.modelName = curtainPrefab != null ? curtainPrefab.name : "Cortina sob medida";
        VisitDraftSession.Current.panelType = "SinglePanel";
        UpdateSelection(VisitDraftSession.Current);
    }

    public void SelectDoublePanel()
    {
        VisitDraftSession.Current.productCategory = "Cortinas";
        VisitDraftSession.Current.modelName = curtainPrefab != null ? curtainPrefab.name : "Cortina sob medida";
        VisitDraftSession.Current.panelType = "DoublePanel";
        UpdateSelection(VisitDraftSession.Current);
    }

    public void SelectFabric()
    {
        VisitDraft draft = VisitDraftSession.Current;
        draft.fabricMaterialName = fabricMaterial != null ? fabricMaterial.name : "CurtainFabric";
        draft.fabricMaterialPath = "Assets/Materials/CurtainFabric.mat";
        UpdateSelection(draft);
    }

    public void SelectRodMaterial()
    {
        VisitDraft draft = VisitDraftSession.Current;
        draft.rodMaterialName = rodMaterial != null ? rodMaterial.name : "RodMaterial";
        draft.rodMaterialPath = "Assets/Materials/RodMaterial.mat";
        UpdateSelection(draft);
    }

    public void ApplySelection()
    {
        CatalogSelectionSession.SelectedCurtainPrefab = curtainPrefab;
        CatalogSelectionSession.SelectedFabricMaterial = fabricMaterial;
        CatalogSelectionSession.SelectedRodMaterial = rodMaterial;
        CatalogSelectionSession.ReturnToVisit = true;
        CatalogSelectionSession.ResumeStepIndex = 3;
        SceneManager.LoadScene("NewVisit");
    }

    public void BackToVisit()
    {
        CatalogSelectionSession.ReturnToVisit = true;
        SceneManager.LoadScene("NewVisit");
    }

    private void SetCategory(string category)
    {
        applyButton.interactable = category == "Cortinas";
        if (category == "Cortinas")
        {
            categoryDescription.text = "Modelo disponível";
            modelDescription.text = (curtainPrefab != null ? curtainPrefab.name : "Cortina sob medida") +
                "\nO tecido é gerado pelo ProceduralCurtainMesh. Escolha painel único ou duplo.";
            singlePanelButton.interactable = true;
            doublePanelButton.interactable = true;
        }
        else
        {
            categoryDescription.text = "Modelos a adicionar";
            modelDescription.text = $"Ainda não há modelos de {category.ToLowerInvariant()} no projeto. Esta área já está pronta para receber os assets pré-definidos.";
            singlePanelButton.interactable = false;
            doublePanelButton.interactable = false;
        }
    }

    private void UpdateSelection(VisitDraft draft)
    {
        if (selectionSummary == null || draft == null) return;
        selectionSummary.text = $"Selecionado\n{draft.modelName} · {(draft.panelType == "SinglePanel" ? "Painel único" : "Painel duplo")}\n" +
            $"Tecido: {draft.fabricMaterialName}\nVarão e ponteiras: {draft.rodMaterialName}";
    }
}
