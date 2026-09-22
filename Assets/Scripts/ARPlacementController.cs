using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using UnityEngine.EventSystems;

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
        // Só permite posicionar se o modo estiver ativo
        if (interactionUI != null &&
            !interactionUI.PosicionamentoAtivo())
        {
            return;
        }

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

                objectManipulator.SetTargetObject(spawnedObject);
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