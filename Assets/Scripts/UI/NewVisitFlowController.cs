using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class NewVisitFlowController : MonoBehaviour
{
    [SerializeField] private GameObject[] steps;
    [SerializeField] private TMP_Text stepLabel;
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text errorLabel;
    [SerializeField] private TMP_Text summaryLabel;
    [SerializeField] private GameObject nextButton;
    [SerializeField] private TMP_Text modelSelectionLabel;
    [SerializeField] private UnityEngine.UI.Button editCatalogButton;
    [SerializeField] private TMP_InputField clientNameInput;
    [SerializeField] private TMP_InputField phoneInput;
    [SerializeField] private TMP_InputField addressInput;
    [SerializeField] private TMP_InputField visitNotesInput;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_InputField openingNameInput;
    [SerializeField] private TMP_InputField widthInput;
    [SerializeField] private TMP_InputField heightInput;
    [SerializeField] private TMP_InputField roomNotesInput;

    private readonly string[] titles = { "Cliente", "Visita", "Cômodo e medidas", "Modelo e material", "Revisão" };
    private int currentStep;

    public void Configure(GameObject[] stepPanels, TMP_Text progress, TMP_Text title, TMP_Text error,
        TMP_Text summary, GameObject next, TMP_Text modelSelection, UnityEngine.UI.Button editCatalog,
        TMP_InputField clientName, TMP_InputField phone, TMP_InputField address,
        TMP_InputField visitNotes, TMP_InputField roomName, TMP_InputField openingName,
        TMP_InputField width, TMP_InputField height, TMP_InputField roomNotes)
    {
        steps = stepPanels;
        stepLabel = progress;
        titleLabel = title;
        errorLabel = error;
        summaryLabel = summary;
        nextButton = next;
        modelSelectionLabel = modelSelection;
        editCatalogButton = editCatalog;
        clientNameInput = clientName;
        phoneInput = phone;
        addressInput = address;
        visitNotesInput = visitNotes;
        roomNameInput = roomName;
        openingNameInput = openingName;
        widthInput = width;
        heightInput = height;
        roomNotesInput = roomNotes;
    }

    private void Start()
    {
        if (CatalogSelectionSession.ReturnToVisit)
        {
            CatalogSelectionSession.ReturnToVisit = false;
            RestoreInputs();
            ShowStep(Mathf.Clamp(CatalogSelectionSession.ResumeStepIndex, 0, steps.Length - 1));
            return;
        }

        ClientData selected = ClientRepository.Find(ClientSession.SelectedClientId);
        if (selected == null) { ShowStep(0); return; }
        RoomData selectedRoom = ClientRepository.FindRoom(selected.id, VisitDraftSession.TargetRoomId);
        if (selectedRoom == null) { SceneManager.LoadScene("ClientProfile"); return; }
        VisitDraftSession.Current = new VisitDraft();
        VisitDraftSession.Current.clientName = selected.name;
        VisitDraftSession.Current.roomName = selectedRoom.name;
        RecordSession.CurrentRecordId = null;
        CatalogSelectionSession.SelectedCurtainPrefab = null;
        CatalogSelectionSession.SelectedFabricMaterial = null;
        CatalogSelectionSession.SelectedRodMaterial = null;
        clientNameInput.text = selected.name;
        phoneInput.text = selected.phone;
        addressInput.text = selected.address;
        roomNameInput.text = selectedRoom.name;
        roomNameInput.interactable = false;
        SaveInputs();
        ShowStep(1);
    }

    public void Next()
    {
        if (!ValidateCurrentStep()) return;
        SaveInputs();
        if (currentStep == 2)
        {
            CatalogSelectionSession.ResumeStepIndex = 2;
            SceneManager.LoadScene("Catalog");
            return;
        }
        ShowStep(Mathf.Min(currentStep + 1, steps.Length - 1));
    }

    public void EditCatalog()
    {
        SaveInputs();
        CatalogSelectionSession.ResumeStepIndex = 3;
        SceneManager.LoadScene("Catalog");
    }

    public void Back()
    {
        if (currentStep == 1 && ClientRepository.Find(ClientSession.SelectedClientId) != null)
        {
            SceneManager.LoadScene(string.IsNullOrEmpty(VisitDraftSession.TargetRoomId) ? "ClientProfile" : "RoomDetail");
            return;
        }
        if (currentStep == 3) { ShowStep(2); return; }
        if (currentStep == 0) SceneManager.LoadScene("Home");
        else ShowStep(currentStep - 1);
    }

    public void SaveAndReturnHome()
    {
        SaveInputs();
        SaveRecord();
        SceneManager.LoadScene("RoomDetail");
    }

    public void OpenAR()
    {
        SaveInputs();
        SaveRecord();
        SceneManager.LoadScene("AR_Test");
    }

    private void ShowStep(int index)
    {
        currentStep = index;
        for (int i = 0; i < steps.Length; i++) steps[i].SetActive(i == index);
        stepLabel.text = $"ETAPA {index + 1} DE {steps.Length}";
        titleLabel.text = titles[index];
        errorLabel.text = string.Empty;
        if (nextButton != null) nextButton.SetActive(index < steps.Length - 1);
        if (index == 3) RefreshModelSelection();
        if (index == steps.Length - 1) RefreshSummary();
    }

    private bool ValidateCurrentStep()
    {
        errorLabel.text = string.Empty;
        if (currentStep == 0 && string.IsNullOrWhiteSpace(clientNameInput.text))
            return Fail("Informe o nome do cliente.");
        if (currentStep == 2 && string.IsNullOrEmpty(VisitDraftSession.TargetRoomId) && string.IsNullOrWhiteSpace(roomNameInput.text))
            return Fail("Informe o nome do cômodo.");
        if (currentStep == 2 && (!TryPositive(widthInput.text, out _) || !TryPositive(heightInput.text, out _)))
            return Fail("Informe largura e altura maiores que zero.");
        return true;
    }

    private bool Fail(string message)
    {
        errorLabel.text = message;
        return false;
    }

    private static bool TryPositive(string value, out float result)
    {
        string normalized = value.Trim().Replace(',', '.');
        return float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out result) && result > 0f;
    }

    private void SaveInputs()
    {
        VisitDraft draft = VisitDraftSession.Current;
        draft.clientName = clientNameInput.text.Trim();
        draft.phone = phoneInput.text.Trim();
        draft.address = addressInput.text.Trim();
        draft.visitNotes = visitNotesInput.text.Trim();
        draft.roomName = roomNameInput.text.Trim();
        draft.openingName = openingNameInput.text.Trim();
        if (TryPositive(widthInput.text, out float width)) draft.widthCentimeters = width;
        if (TryPositive(heightInput.text, out float height)) draft.heightCentimeters = height;
        draft.roomNotes = roomNotesInput.text.Trim();
    }

    private void RestoreInputs()
    {
        VisitDraft draft = VisitDraftSession.Current;
        if (draft == null) return;
        clientNameInput.text = draft.clientName ?? string.Empty;
        phoneInput.text = draft.phone ?? string.Empty;
        addressInput.text = draft.address ?? string.Empty;
        visitNotesInput.text = draft.visitNotes ?? string.Empty;
        roomNameInput.text = draft.roomName ?? string.Empty;
        openingNameInput.text = draft.openingName ?? string.Empty;
        widthInput.text = draft.widthCentimeters > 0f ? draft.widthCentimeters.ToString("0.#", CultureInfo.InvariantCulture) : string.Empty;
        heightInput.text = draft.heightCentimeters > 0f ? draft.heightCentimeters.ToString("0.#", CultureInfo.InvariantCulture) : string.Empty;
        roomNotesInput.text = draft.roomNotes ?? string.Empty;
    }

    private void RefreshModelSelection()
    {
        VisitDraft draft = VisitDraftSession.Current;
        modelSelectionLabel.text = $"{draft.productCategory} · {draft.modelName}\n{(draft.panelType == "SinglePanel" ? "Painel único" : "Painel duplo")}\nTecido: {draft.fabricMaterialName}\nVarão e ponteiras: {draft.rodMaterialName}";
    }

    private void RefreshSummary()
    {
        VisitDraft draft = VisitDraftSession.Current;
        summaryLabel.text =
            $"<b>Cliente</b>\n{draft.clientName}\n{draft.phone}\n{Fallback(draft.address)}\n\n" +
            $"<b>Visita</b>\n{DateTime.Now:dd/MM/yyyy}\n{Fallback(draft.visitNotes)}\n\n" +
            $"<b>Cômodo</b>\n{draft.roomName} · {Fallback(draft.openingName)}\n" +
            $"{draft.widthCentimeters:0.#} cm × {draft.heightCentimeters:0.#} cm\n{Fallback(draft.roomNotes)}\n\n" +
            $"<b>Produto</b>\n{draft.modelName} · {(draft.panelType == "SinglePanel" ? "Painel único" : "Painel duplo")}\n" +
            $"Tecido: {draft.fabricMaterialName} · Varão: {draft.rodMaterialName}";
    }

    private void SaveRecord()
    {
        if (string.IsNullOrEmpty(ClientSession.SelectedClientId) || string.IsNullOrEmpty(VisitDraftSession.TargetRoomId))
        {
            errorLabel.text = "Este registro precisa estar associado a um cômodo.";
            return;
        }
        if (string.IsNullOrEmpty(RecordSession.CurrentRecordId))
        {
            VisitRecordData record = ClientRepository.AddRecord(ClientSession.SelectedClientId, VisitDraftSession.TargetRoomId, VisitDraftSession.Current);
            if (record != null) RecordSession.CurrentRecordId = record.id;
        }
    }

    private static string Fallback(string value) => string.IsNullOrWhiteSpace(value) ? "Não informado" : value;
}
