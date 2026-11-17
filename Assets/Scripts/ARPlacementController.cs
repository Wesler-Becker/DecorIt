using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;
using UnityEngine.SceneManagement;

public class ARPlacementController : MonoBehaviour
{
    [SerializeField]
    private ARRaycastManager raycastManager;

    [SerializeField]
    private GameObject objectToPlace;

    [SerializeField]
    private ARInteractionUI interactionUI;

    [SerializeField]
    private ARObjectManipulator objectManipulator;

    [Header("Posicionamento sem plano detectado")]
    [SerializeField, Min(0.25f)]
    private float fallbackPlacementDistance = 2f;

    private GameObject spawnedObject;
    private bool createNewObjectOnNextPlacement;
    private List<GameObject> modelOptions;
    private int modelIndex;

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    private bool IsOverUI(Vector2 position)
    {
        if (EventSystem.current == null) return false;
        uiHits.Clear();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, uiHits);
        return uiHits.Count > 0;
    }

    private void Start()
    {
        InitializeCurtainSettings();
    }

    public void InitializeCurtainSettings()
    {
        if (CatalogSelectionSession.HasConfiguredCurtain && CatalogSelectionSession.CurrentConfiguration != null &&
            CatalogSelectionSession.SelectedCurtainPrefab != null)
            return;

        VisitDraft draft = VisitDraftSession.Current ?? new VisitDraft();
        VisitDraftSession.Current = draft;
        CurtainViewSettings settings = draft.curtainSettings != null && draft.curtainSettings.layers != null &&
            draft.curtainSettings.layers.Count > 0
            ? CurtainViewSettingsStore.FromDraft(draft) : CurtainViewSettingsStore.LoadLast();
        CurtainLayerViewSettings frontLayer = settings.GetLayer(0);

        // A room's fresh measurements take precedence over the saved curtain dimensions.
        if (draft.widthCentimeters > 0f && draft.heightCentimeters > 0f)
        {
            frontLayer.widthCentimeters = draft.widthCentimeters;
            frontLayer.heightCentimeters = draft.heightCentimeters;
            if (!string.IsNullOrEmpty(draft.fabricMaterialPath) &&
                draft.fabricMaterialPath != "Assets/Materials/CurtainFabric.mat")
            {
                frontLayer.fabricMaterialPath = draft.fabricMaterialPath;
                frontLayer.fabricMaterialName = draft.fabricMaterialName;
            }
        }

        settings.SyncLegacyFromFrontLayer();
        CatalogSelectionSession.SelectedCurtainPrefab = FindModel(settings.modelName);
        if (CatalogSelectionSession.SelectedCurtainPrefab != null)
            settings.modelName = CatalogSelectionSession.SelectedCurtainPrefab.name;
        CatalogSelectionSession.CurrentConfiguration = CurtainViewSettingsStore.Clone(settings);
        CurtainViewSettingsStore.ApplyToDraft(settings, draft);
        CurtainViewSettingsStore.SaveLast(settings);
        CatalogSelectionSession.SelectedFabricMaterial = CurtainViewSettingsStore.ResolveFabric(
            settings.fabricMaterialPath, settings.fabricMaterialName);
        if (CatalogSelectionSession.SelectedFabricMaterial == null)
            CatalogSelectionSession.SelectedFabricMaterial = GetBaseFabricMaterial(false);
        if (CatalogSelectionSession.SelectedRodMaterial == null)
            CatalogSelectionSession.SelectedRodMaterial = GetBaseFabricMaterial(true);
        CatalogSelectionSession.HasConfiguredCurtain = true;
    }

    public void PrepareToPlaceNewObject()
    {
        createNewObjectOnNextPlacement = true;
    }

    public void ApplyCatalogSelection()
    {
        if (spawnedObject != null)
        {
            ApplyVisitSelection(spawnedObject);
            createNewObjectOnNextPlacement = false;
            if (objectManipulator != null) objectManipulator.SetTargetObject(spawnedObject);
        }
        else PrepareToPlaceNewObject();
        if (interactionUI != null) interactionUI.ResumeAfterCatalog(spawnedObject != null);
    }

    public string ActiveModelName => CurrentPrefab() != null ? CurrentPrefab().name : "Cortina";
    public bool HasAlternativeModels => GetModelOptions().Count > 1;
    public IReadOnlyList<GameObject> AvailableModels => GetModelOptions();

    void Update()
    {
#if UNITY_EDITOR
        HandleMouseInput();
#else
        HandleTouchInput();
#endif
    }

    private void HandleMouseInput()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        // Ignora o clique caso ele esteja sobre algum elemento da interface
        if (IsOverUI(Mouse.current.position.ReadValue()))
        {
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        TryPlaceObject(mousePosition);
    }

    private void HandleTouchInput()
    {
        if (Touchscreen.current == null)
            return;

        var touch = Touchscreen.current.primaryTouch;

        if (!touch.press.wasPressedThisFrame)
            return;

        // Ignora o toque caso ele esteja sobre a interface
        if (IsOverUI(touch.position.ReadValue()))
        {
            return;
        }

        Vector2 touchPosition = touch.position.ReadValue();

        TryPlaceObject(touchPosition);
    }

    private void TryPlaceObject(Vector2 screenPosition)
    {
        if (UnityARHostBridge.IsPhotoOnly && !UnityARHostBridge.IsQuickAR) return;
        if (!createNewObjectOnNextPlacement) return;
        InitializeCurtainSettings();

        // Só permite posicionar se o modo estiver ativo
        if (interactionUI != null &&
            !interactionUI.PosicionamentoAtivo())
        {
            return;
        }

        Debug.Log("Tentando posicionar objeto.");

        Pose placementPose;
        if (raycastManager != null && raycastManager.Raycast(
            screenPosition,
            hits,
            TrackableType.PlaneWithinPolygon))
        {
            Debug.Log("Plano detectado; usando a pose real do AR.");
            Camera camera = Camera.main;
            Vector3 cameraPosition = camera != null ? camera.transform.position : hits[0].pose.position + Vector3.forward;
            Vector3 cameraForward = camera != null ? camera.transform.forward : Vector3.back;
            placementPose = new Pose(hits[0].pose.position,
                UprightRotation(hits[0].pose.up, hits[0].pose.position, cameraPosition, cameraForward));
        }
        else
        {
            if (!TryGetCameraRelativePose(screenPosition, out placementPose))
            {
                Debug.LogWarning("Não foi possível posicionar: câmera principal indisponível.");
                return;
            }

            Debug.Log("Sem plano detectado; posicionando à frente da câmera para ajuste manual.");
        }

        if (createNewObjectOnNextPlacement)
        {
            GameObject prefabToPlace = CatalogSelectionSession.SelectedCurtainPrefab != null
                ? CatalogSelectionSession.SelectedCurtainPrefab
                : objectToPlace;

            if (prefabToPlace == null)
            {
                Debug.LogWarning("Nenhum modelo foi configurado para posicionamento.");
                return;
            }

            spawnedObject = Instantiate(prefabToPlace, placementPose.position, placementPose.rotation);
            createNewObjectOnNextPlacement = false;
            ApplyVisitSelection(spawnedObject);
            if (objectManipulator != null) objectManipulator.SetTargetObject(spawnedObject);
        }
        if (interactionUI != null) interactionUI.ConfirmObjectPlacement();
    }

    private static Quaternion UprightRotation(Vector3 surfaceNormal, Vector3 position, Vector3 cameraPosition, Vector3 cameraForward)
    {
        Vector3 towardCamera = Vector3.ProjectOnPlane(cameraPosition - position, Vector3.up);
        Vector3 facing = Vector3.ProjectOnPlane(surfaceNormal, Vector3.up);
        // A floor or ceiling does not provide a useful horizontal facing direction.
        if (facing.sqrMagnitude < 0.25f) facing = towardCamera;
        if (facing.sqrMagnitude < 0.0001f) facing = -Vector3.ProjectOnPlane(cameraForward, Vector3.up);
        if (facing.sqrMagnitude < 0.0001f) facing = Vector3.forward;
        if (towardCamera.sqrMagnitude > 0.0001f && Vector3.Dot(facing, towardCamera) < 0f) facing = -facing;
        return Quaternion.LookRotation(facing.normalized, Vector3.up);
    }

    private bool TryGetCameraRelativePose(Vector2 screenPosition, out Pose pose)
    {
        Camera arCamera = Camera.main;
        if (arCamera == null)
        {
            pose = default;
            return false;
        }

        Ray cameraRay = arCamera.ScreenPointToRay(screenPosition);
        Vector3 position = cameraRay.GetPoint(Mathf.Max(0.25f, fallbackPlacementDistance));
        Vector3 facing = Vector3.ProjectOnPlane(arCamera.transform.forward, Vector3.up);
        if (facing.sqrMagnitude < 0.0001f)
            facing = Vector3.ProjectOnPlane(cameraRay.direction, Vector3.up);

        Quaternion rotation = facing.sqrMagnitude < 0.0001f
            ? Quaternion.identity
            : Quaternion.LookRotation(-facing.normalized, Vector3.up);

        pose = new Pose(position, rotation);
        return true;
    }

    private static void ApplyVisitSelection(GameObject placedObject)
    {
        VisitDraft selection = VisitDraftSession.Current;
        CurtainController curtain = placedObject.GetComponent<CurtainController>();
        if (curtain == null) return;

        CurtainViewSettings settings = CatalogSelectionSession.CurrentConfiguration ??
            CurtainViewSettingsStore.FromDraft(selection);
        curtain.ApplySettings(settings);

        settings.EnsureLayers();
        for (int i = 0; i < settings.layers.Count; i++)
        {
            CurtainLayerViewSettings layer = settings.layers[i];
            Material material = CurtainViewSettingsStore.ResolveFabric(
                layer.fabricMaterialPath, layer.fabricMaterialName);
            if (material == null && i == 0) material = CatalogSelectionSession.SelectedFabricMaterial;
            if (material != null) curtain.SetLayerMaterial(i, material);
        }

        foreach (MeshRenderer mesh in placedObject.GetComponentsInChildren<MeshRenderer>(true))
        {
            string partName = mesh.gameObject.name;
            if ((partName.Contains("RodBody") || partName.Contains("Finial")) &&
                CatalogSelectionSession.SelectedRodMaterial != null)
                mesh.sharedMaterial = CatalogSelectionSession.SelectedRodMaterial;
        }
    }

    public Material GetBaseFabricMaterial(bool rod)
    {
        GameObject prefab = CurrentPrefab();
        if (prefab == null) return null;
        foreach (MeshRenderer renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
        {
            string partName = renderer.gameObject.name;
            bool isRod = partName.Contains("RodBody") || partName.Contains("Finial");
            if (isRod == rod && renderer.sharedMaterial != null)
                return renderer.sharedMaterial;
        }
        return null;
    }

    public void SetPanelType(CurtainController.CurtainType type)
    {
        VisitDraft selection = VisitDraftSession.Current;
        selection.panelType = type.ToString();
        if (spawnedObject != null)
        {
            CurtainController curtain = spawnedObject.GetComponent<CurtainController>();
            if (curtain != null) curtain.SetCurtainType(type);
        }
        PersistCurrentSelection();
    }

    public void ApplyFabricMaterial(Material material, string displayName, string assetPath = "")
    {
        CatalogSelectionSession.SelectedFabricMaterial = material;
        VisitDraftSession.Current.fabricMaterialName = displayName;
        VisitDraftSession.Current.fabricMaterialPath = assetPath;
        CurtainViewSettings settings = CatalogSelectionSession.CurrentConfiguration ??
            CurtainViewSettingsStore.FromDraft(VisitDraftSession.Current);
        CurtainLayerViewSettings frontLayer = settings.GetLayer(0);
        frontLayer.fabricMaterialName = displayName;
        frontLayer.fabricMaterialPath = assetPath;
        settings.SyncLegacyFromFrontLayer();
        CatalogSelectionSession.CurrentConfiguration = settings;
        CurtainController curtain = spawnedObject != null ? spawnedObject.GetComponent<CurtainController>() : null;
        if (curtain != null) curtain.SetLayerMaterial(0, material);
        PersistCurrentSelection();
    }

    public void ApplyRodMaterial(Material material, string displayName)
    {
        CatalogSelectionSession.SelectedRodMaterial = material;
        VisitDraftSession.Current.rodMaterialName = displayName;
        VisitDraftSession.Current.rodMaterialPath = string.Empty;
        ApplyMaterialToSpawnedObject(material, true);
        PersistCurrentSelection();
    }

    public void CycleModel(int direction)
    {
        List<GameObject> options = GetModelOptions();
        if (options.Count < 2) return;
        SelectModel(options[(modelIndex + direction + options.Count) % options.Count]);
    }

    public void SelectModel(GameObject selectedModel)
    {
        List<GameObject> options = GetModelOptions();
        int selectedIndex = options.IndexOf(selectedModel);
        if (selectedIndex < 0) return;
        modelIndex = selectedIndex;
        objectToPlace = selectedModel;
        CatalogSelectionSession.SelectedCurtainPrefab = objectToPlace;
        VisitDraftSession.Current.modelName = objectToPlace.name;

        if (spawnedObject != null)
        {
            Transform old = spawnedObject.transform;
            Vector3 position = old.position;
            Quaternion rotation = old.rotation;
            Destroy(spawnedObject);
            spawnedObject = Instantiate(objectToPlace, position, rotation);
            ApplyVisitSelection(spawnedObject);
            if (objectManipulator != null) objectManipulator.SetTargetObject(spawnedObject);
        }
        PersistCurrentSelection();
    }

    private void ApplyMaterialToSpawnedObject(Material material, bool rod)
    {
        if (material == null || spawnedObject == null) return;
        foreach (MeshRenderer renderer in spawnedObject.GetComponentsInChildren<MeshRenderer>(true))
        {
            string partName = renderer.gameObject.name;
            bool isRod = partName.Contains("RodBody") || partName.Contains("Finial");
            if (isRod == rod) renderer.sharedMaterial = material;
        }
    }

    private GameObject CurrentPrefab()
    {
        return CatalogSelectionSession.SelectedCurtainPrefab != null
            ? CatalogSelectionSession.SelectedCurtainPrefab
            : objectToPlace;
    }

    private GameObject FindModel(string modelName)
    {
        if (objectToPlace != null && (string.IsNullOrEmpty(modelName) || objectToPlace.name == modelName))
            return objectToPlace;
        GameObject[] resources = Resources.LoadAll<GameObject>("ARModels");
        foreach (GameObject model in resources)
            if (model != null && model.name == modelName) return model;
        return objectToPlace;
    }

    private List<GameObject> GetModelOptions()
    {
        if (modelOptions != null) return modelOptions;
        modelOptions = new List<GameObject>();
        GameObject[] resources = Resources.LoadAll<GameObject>("ARModels");
        foreach (GameObject resource in resources)
            if (resource != null && !modelOptions.Contains(resource)) modelOptions.Add(resource);
        GameObject current = CurrentPrefab();
        if (current != null && !modelOptions.Contains(current)) modelOptions.Insert(0, current);
        if (modelOptions.Count == 0 && objectToPlace != null) modelOptions.Add(objectToPlace);
        modelIndex = Mathf.Max(0, modelOptions.IndexOf(current));
        return modelOptions;
    }

    private static void PersistCurrentSelection()
    {
        VisitDraft draft = VisitDraftSession.Current;
        CurtainViewSettings settings = CatalogSelectionSession.CurrentConfiguration ??
            CurtainViewSettingsStore.FromDraft(draft);
        settings.opening = draft.opening;
        CurtainLayerViewSettings frontLayer = settings.GetLayer(0);
        frontLayer.widthCentimeters = draft.widthCentimeters > 0f ? draft.widthCentimeters : frontLayer.widthCentimeters;
        frontLayer.heightCentimeters = draft.heightCentimeters > 0f ? draft.heightCentimeters : frontLayer.heightCentimeters;
        frontLayer.panelType = draft.panelType;
        frontLayer.fabricStyle = draft.fabricStyle;
        frontLayer.fabricMaterialName = draft.fabricMaterialName;
        frontLayer.fabricMaterialPath = draft.fabricMaterialPath;
        frontLayer.foldsPerMeter = draft.foldsPerMeter;
        frontLayer.foldDepthCentimeters = draft.foldDepthCentimeters;
        frontLayer.foldProfile = draft.foldProfile;
        frontLayer.naturalness = draft.fabricNaturalness;
        frontLayer.seed = draft.fabricSeed;
        settings.modelName = draft.modelName;
        settings.rodMaterialName = draft.rodMaterialName;
        settings.rodMaterialPath = draft.rodMaterialPath;
        settings.SyncLegacyFromFrontLayer();
        CatalogSelectionSession.CurrentConfiguration = CurtainViewSettingsStore.Clone(settings);
        CurtainViewSettingsStore.ApplyToDraft(settings, draft);
        UnityARHostBridge.SaveLastViewConfig();
        CurtainViewSettingsStore.SaveLast(settings);
    }
}
