using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class ARObjectManipulator : MonoBehaviour
{
    [SerializeField]
    private ARInteractionUI interactionUI;

    [Header("Sensibilidade do movimento")]
    [SerializeField]
    private float movementSensitivity = 0.001f;

    [Header("Sensibilidade da profundidade")]
    [SerializeField]
    private float depthSensitivity = 0.005f;

    [Header("Sensibilidade da rotação")]
    [SerializeField]
    private float rotationSensitivity = 0.5f;

    private GameObject targetObject;

    private Vector2 lastTouchPosition;

    // Dados utilizados pelo gesto de dois dedos
    private float lastPinchDistance;
    private float lastPinchAngle;

    private bool wasDragging = false;
    private bool wasUsingTwoFingers = false;

    public void SetTargetObject(GameObject obj)
    {
        targetObject = obj;
    }

    private void Update()
    {
        if (targetObject == null)
            return;

        if (interactionUI == null)
            return;

        // Só permite manipulação quando o modo Ajustar estiver ativo
        if (!interactionUI.AjusteAtivo())
        {
            wasDragging = false;
            wasUsingTwoFingers = false;
            return;
        }

#if UNITY_EDITOR
        HandleMouse();
#else
        HandleTouch();
#endif
    }

    // ============================================================
    // MOUSE - utilizado para testar no Unity Editor
    // ============================================================

    private void HandleMouse()
    {
        if (Mouse.current == null)
            return;

        Vector2 currentPosition =
            Mouse.current.position.ReadValue();

        // ========================================================
        // BOTÃO ESQUERDO = MOVIMENTAÇÃO
        // ========================================================

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            lastTouchPosition = currentPosition;
            wasDragging = true;
        }

        if (Mouse.current.leftButton.isPressed)
        {
            Vector2 delta =
                currentPosition - lastTouchPosition;

            MoveObject(delta);

            lastTouchPosition = currentPosition;
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            wasDragging = false;
        }

        // ========================================================
        // BOTÃO DO MEIO (SCROLL) = ROTAÇÃO
        // ========================================================

        // Quando acabou de pressionar o scroll,
        // apenas registra a posição do mouse.
        // NÃO altera a rotação neste momento.
        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            lastTouchPosition = currentPosition;
        }

        // Enquanto o scroll estiver pressionado,
        // utiliza somente o deslocamento do mouse.
        if (Mouse.current.middleButton.isPressed)
        {
            Vector2 delta =
                currentPosition - lastTouchPosition;

            RotateObject(
                delta.x * rotationSensitivity
            );

            lastTouchPosition = currentPosition;
        }

        // ========================================================
        // RODA DO MOUSE = PROFUNDIDADE
        // ========================================================

        float scroll =
            Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) > 0.01f)
        {
            MoveDepth(
                scroll * depthSensitivity
            );
        }
    }

    // ============================================================
    // TOUCH - utilizado no celular
    // ============================================================

    private void HandleTouch()
    {
        if (Touchscreen.current == null)
            return;

        var touches = Touchscreen.current.touches;

        int activeTouches = 0;

        TouchControl firstTouch = null;
        TouchControl secondTouch = null;

        foreach (var touch in touches)
        {
            if (touch.press.isPressed)
            {
                if (activeTouches == 0)
                    firstTouch = touch;

                if (activeTouches == 1)
                    secondTouch = touch;

                activeTouches++;
            }
        }

        // ========================================================
        // DOIS DEDOS
        // ========================================================

        if (activeTouches >= 2)
        {
            wasDragging = false;

            HandleTwoFingerGesture(
                firstTouch,
                secondTouch
            );

            return;
        }

        // Não estamos mais usando dois dedos
        wasUsingTwoFingers = false;

        // ========================================================
        // UM DEDO
        // ========================================================

        var primaryTouch =
            Touchscreen.current.primaryTouch;

        if (primaryTouch.press.isPressed)
        {
            Vector2 currentPosition =
                primaryTouch.position.ReadValue();

            if (!wasDragging)
            {
                lastTouchPosition = currentPosition;
                wasDragging = true;
                return;
            }

            Vector2 delta =
                currentPosition - lastTouchPosition;

            MoveObject(delta);

            lastTouchPosition = currentPosition;
        }
        else
        {
            wasDragging = false;
        }
    }

    // ============================================================
    // GESTO DE DOIS DEDOS
    // ============================================================

    
private void HandleTwoFingerGesture(
    TouchControl firstTouch,
    TouchControl secondTouch)
{
    Vector2 firstPosition = firstTouch.position.ReadValue();
    Vector2 secondPosition = secondTouch.position.ReadValue();

    float currentDistance = Vector2.Distance(
        firstPosition,
        secondPosition
    );

    Vector2 direction = secondPosition - firstPosition;

    float currentAngle =
        Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

    // Inicializa os valores ao começar o gesto.
    if (!wasUsingTwoFingers)
    {
        lastPinchDistance = currentDistance;
        lastPinchAngle = currentAngle;

        wasUsingTwoFingers = true;
        return;
    }

    if (interactionUI.RotacaoAtiva())
    {
        // MODO ROTAÇÃO:
        // Ignora completamente a distância entre os dedos.
        float angleDelta = Mathf.DeltaAngle(
            lastPinchAngle,
            currentAngle
        );

        RotateObject(angleDelta * rotationSensitivity);
    }
    else
    {
        // MODO PROFUNDIDADE:
        // Ignora completamente o ângulo entre os dedos.
        float distanceDelta =
            currentDistance - lastPinchDistance;

        MoveDepth(distanceDelta * depthSensitivity);
    }

    // Atualiza ambos os valores para o próximo frame.
    lastPinchDistance = currentDistance;
    lastPinchAngle = currentAngle;
}

    // ============================================================
    // MOVIMENTO HORIZONTAL / VERTICAL
    // ============================================================

    private void MoveObject(Vector2 delta)
    {
        Camera cam = Camera.main;

        if (cam == null)
            return;

        Vector3 horizontal =
            cam.transform.right * delta.x;

        Vector3 vertical =
            cam.transform.up * delta.y;

        targetObject.transform.position +=
            (horizontal + vertical)
            * movementSensitivity;
    }

    // ============================================================
    // PROFUNDIDADE
    // ============================================================

    private void MoveDepth(float amount)
    {
        Camera cam = Camera.main;

        if (cam == null)
            return;

        targetObject.transform.position +=
            cam.transform.forward * amount;
    }

    // ============================================================
    // ROTAÇÃO
    // ============================================================

    private void RotateObject(float angle)
    {
        targetObject.transform.Rotate(
            Vector3.up,
            -angle,
            Space.World
        );
    }
}