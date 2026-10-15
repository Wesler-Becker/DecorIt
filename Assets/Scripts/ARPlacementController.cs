using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;

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
    private List<GameObject> modelOptions;
    private int modelIndex;

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

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
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
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
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(touch.touchId.ReadValue()))
        {
            return;
        }

        Vector2 touchPosition = touch.position.ReadValue();

        TryPlaceObject(touchPosition);
    }

    private void TryPlaceObject(Vector2 screenPosition)
    {
        if (UnityARHostBridge.IsPhotoOnly && !UnityARHostBridge.IsQuickAR) return;

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
            placementPose = hits[0].pose;
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

        if (spawnedObject == null)
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
            ApplyVisitSelection(spawnedObject);
            if (objectManipulator != null) objectManipulator.SetTargetObject(spawnedObject);
        }
        else
        {
            spawnedObject.transform.SetPositionAndRotation(placementPose.position, placementPose.rotation);
        }
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

        if (selection.widthCentimeters > 0f && selection.heightCentimeters > 0f)
            curtain.SetDimensions(selection.widthCentimeters, selection.heightCentimeters);

        if (Enum.TryParse(selection.panelType, out CurtainController.CurtainType type))
            curtain.SetCurtainType(type);

        foreach (MeshRenderer mesh in placedObject.GetComponentsInChildren<MeshRenderer>(true))
        {
            string partName = mesh.gameObject.name;
            if ((partName.Contains("RodBody") || partName.Contains("Finial")) &&
                CatalogSelectionSession.SelectedRodMaterial != null)
                mesh.sharedMaterial = CatalogSelectionSession.SelectedRodMaterial;
            else if (CatalogSelectionSession.SelectedFabricMaterial != null)
                mesh.sharedMaterial = CatalogSelectionSession.SelectedFabricMaterial;
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

    public void ApplyFabricMaterial(Material material, string displayName)
    {
        CatalogSelectionSession.SelectedFabricMaterial = material;
        VisitDraftSession.Current.fabricMaterialName = displayName;
        VisitDraftSession.Current.fabricMaterialPath = string.Empty;
        ApplyMaterialToSpawnedObject(material, false);
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
        UnityARHostBridge.SaveLastViewConfig();
        ClientRepository.UpdateCurrentRecordSelection(
            ClientSession.SelectedClientId,
            VisitDraftSession.TargetRoomId,
            RecordSession.CurrentRecordId,
            draft.modelName,
            draft.panelType,
            draft.fabricMaterialName,
            draft.rodMaterialName);
    }
}
