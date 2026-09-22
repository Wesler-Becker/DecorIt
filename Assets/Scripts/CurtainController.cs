
using UnityEngine;

public class CurtainController : MonoBehaviour
{
    public enum CurtainType
    {
        SinglePanel,
        DoublePanel
    }

    [Header("Curtain Type")]
    [SerializeField] private CurtainType curtainType =
        CurtainType.DoublePanel;

    [Header("Total Dimensions (cm)")]
    [Min(10f)]
    [SerializeField] private float widthCm = 240f;

    [Min(10f)]
    [SerializeField] private float heightCm = 220f;

    [Header("Fold Settings")]
    [Min(1f)]
    [SerializeField] private float foldsPerMeter = 5f;

    [Min(0f)]
    [SerializeField] private float foldDepth = 0.06f;

    [Range(0.5f, 2f)]
    [SerializeField] private float foldProfile = 1f;

    [Header("Opening")]
    [Range(0f, 1f)]
    [SerializeField] private float opening = 0f;

    [Header("Curtain Rod")]
    [SerializeField] private Transform rodBody;
    [SerializeField] private Transform leftFinial;
    [SerializeField] private Transform rightFinial;

    [Header("Rod Settings (meters)")]
    [SerializeField] private float rodExtraWidth = 0.10f;
    [SerializeField] private float rodDiameter = 0.025f;
    [SerializeField] private float finialSize = 0.05f;
    [SerializeField] private float rodHeightOffset = 0.06f;

    [Header("Panels")]
    [SerializeField] private ProceduralCurtainMesh singlePanel;
    [SerializeField] private ProceduralCurtainMesh leftPanel;
    [SerializeField] private ProceduralCurtainMesh rightPanel;

    private void Start()
    {
        UpdateCurtain();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Executa a atualização ao alterar valores no Inspector
        // somente durante o Play.
        if (!Application.isPlaying)
            return;

        UpdateCurtain();
    }
#endif

    public void UpdateCurtain()
    {
        if (singlePanel == null ||
            leftPanel == null ||
            rightPanel == null)
        {
            Debug.LogWarning(
                "CurtainController: panel references are missing."
            );

            return;
        }

        float width = Mathf.Max(10f, widthCm) / 100f;
        float height = Mathf.Max(10f, heightCm) / 100f;

        if (curtainType == CurtainType.SinglePanel)
        {
            ConfigureSinglePanel(width, height);
        }
        else
        {
            ConfigureDoublePanel(width, height);
        }

        // Ajusta o varão conforme a largura total da cortina.
        UpdateRod(width);
    }

    private void ConfigureSinglePanel(
        float width,
        float height)
    {
        singlePanel.gameObject.SetActive(true);

        leftPanel.gameObject.SetActive(false);
        rightPanel.gameObject.SetActive(false);

        singlePanel.transform.localPosition = Vector3.zero;

        singlePanel.Configure(
            width,
            height,
            CalculateFoldCount(width),
            foldDepth,
            foldProfile
        );
    }

    
    private void ConfigureDoublePanel(
        float width,
        float height)
    {
        singlePanel.gameObject.SetActive(false);

        leftPanel.gameObject.SetActive(true);
        rightPanel.gameObject.SetActive(true);

        float originalPanelWidth = width / 2f;

        // Cada painel mantém ao menos 15% da largura original.
        // Isso representa o tecido recolhido na lateral.
        float minimumWidthFactor = 0.15f;

        float visibleWidthFactor = Mathf.Lerp(
            1f,
            minimumWidthFactor,
            opening
        );

        float visiblePanelWidth =
            originalPanelWidth * visibleWidthFactor;

        // Mantém a borda externa dos painéis na posição original.
        float leftPosition =
            -width / 2f + visiblePanelWidth / 2f;

        float rightPosition =
            width / 2f - visiblePanelWidth / 2f;

        leftPanel.transform.localPosition =
            new Vector3(leftPosition, 0f, 0f);

        rightPanel.transform.localPosition =
            new Vector3(rightPosition, 0f, 0f);

        // A quantidade de ondas permanece baseada na
        // largura original, representando o tecido recolhido.
        int panelFoldCount =
            CalculateFoldCount(originalPanelWidth);

        leftPanel.Configure(
            visiblePanelWidth,
            height,
            panelFoldCount,
            foldDepth,
            foldProfile
        );

        rightPanel.Configure(
            visiblePanelWidth,
            height,
            panelFoldCount,
            foldDepth,
            foldProfile
        );
    }

    private int CalculateFoldCount(float panelWidth)
    {
        return Mathf.Max(
            1,
            Mathf.RoundToInt(panelWidth * foldsPerMeter)
        );
    }

    public void SetDimensions(
        float newWidthCm,
        float newHeightCm)
    {
        widthCm = Mathf.Max(10f, newWidthCm);
        heightCm = Mathf.Max(10f, newHeightCm);

        UpdateCurtain();
    }

    public void SetCurtainType(CurtainType newType)
    {
        curtainType = newType;

        UpdateCurtain();
    }

    public void SetFoldSettings(
        float newFoldsPerMeter,
        float newFoldDepth,
        float newFoldProfile)
    {
        foldsPerMeter = Mathf.Max(1f, newFoldsPerMeter);
        foldDepth = Mathf.Max(0f, newFoldDepth);
        foldProfile = Mathf.Clamp(newFoldProfile, 0.5f, 2f);

        UpdateCurtain();
    }

    public void SetOpening(float value)
    {
    opening = Mathf.Clamp01(value);

    UpdateCurtain();
    }

    private void UpdateRod(float curtainWidth)
    {
        if (rodBody == null ||
            leftFinial == null ||
            rightFinial == null)
        {
            Debug.LogWarning(
                "CurtainController: rod references are missing."
            );

            return;
        }

        // Comprimento total do corpo do varão.
        float rodLength = curtainWidth + rodExtraWidth;

        // O cilindro padrão tem 2 unidades de altura.
        // Após sua rotação, essa dimensão representa o comprimento.
        rodBody.localScale = new Vector3(
            rodDiameter / 2f,
            rodLength / 2f,
            rodDiameter / 2f
        );

        rodBody.localRotation = Quaternion.Euler(0f, 0f, 90f);

        rodBody.localPosition = new Vector3(
            0f,
            rodHeightOffset,
            0f
        );

        // Mantém as ponteiras fora das extremidades do cilindro.
        float finialOffset = rodLength / 2f + finialSize / 2f;

        leftFinial.localPosition = new Vector3(
            -finialOffset,
            rodHeightOffset,
            0f
        );

        rightFinial.localPosition = new Vector3(
            finialOffset,
            rodHeightOffset,
            0f
        );

        // As ponteiras permanecem com tamanho fixo.
        Vector3 finialScale = new Vector3(
            finialSize,
            finialSize,
            finialSize
        );

        leftFinial.localScale = finialScale;
        rightFinial.localScale = finialScale;
    }
}