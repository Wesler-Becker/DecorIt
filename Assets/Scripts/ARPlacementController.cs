using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class ARPlacementController : MonoBehaviour
{
    [SerializeField]
    private ARRaycastManager raycastManager;

    [SerializeField]
    private GameObject objectToPlace;

    private GameObject spawnedObject;

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

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

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        TryPlaceObject(mousePosition);
    }

    private void HandleTouchInput()
    {
        if (Touchscreen.current == null)
            return;

        if (!Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return;

        Vector2 touchPosition =
            Touchscreen.current.primaryTouch.position.ReadValue();

        TryPlaceObject(touchPosition);
    }

    private void TryPlaceObject(Vector2 screenPosition)
    {
        Debug.Log("Tentando posicionar objeto.");

        if (raycastManager.Raycast(
            screenPosition,
            hits,
            TrackableType.PlaneWithinPolygon))
        {
            Debug.Log("Plano encontrado.");

            Pose hitPose = hits[0].pose;

            if (spawnedObject == null)
            {
                Debug.Log("Criando objeto.");

                spawnedObject = Instantiate(
                    objectToPlace,
                    hitPose.position,
                    hitPose.rotation
                );
            }
            else
            {
                Debug.Log("Movendo objeto.");

                spawnedObject.transform.SetPositionAndRotation(
                    hitPose.position,
                    hitPose.rotation
                );
            }
        }
        else
        {
            Debug.Log("Nenhum plano encontrado no ponto selecionado.");
        }
    }
}