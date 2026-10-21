
using UnityEngine;

[ExecuteAlways]
public class CurtainController : MonoBehaviour
{
    public enum CurtainType
    {
        SinglePanel,
        DoublePanel
    }

    public enum FabricStyle
    {
        Personalizado,
        Liso,
        Natural,
        Wave
    }

    [Header("Fabric style")]
    [Tooltip("Personalizado usa Fold Settings. Os outros estilos definem a densidade e profundidade das ondas.")]
    [SerializeField] private FabricStyle fabricStyle = FabricStyle.Personalizado;
    [Tooltip("Intensidade do caimento natural, das rugas e da variacao da barra.")]
    [Range(0f, 1f)] [SerializeField] private float fabricNaturalness = 1f;
    [SerializeField] private int fabricSeed = 17;

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

    private bool updateRequested;

    private void OnEnable()
    {
        updateRequested = true;
    }

    private void Update()
    {
        if (updateRequested)
            UpdateCurtain();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Apply on the main thread, including previews outside Play mode.
        updateRequested = true;
    }
#endif

    [ContextMenu("Atualizar cortina")]
    public void UpdateCurtain()
    {
        updateRequested = false;
        if ((curtainType == CurtainType.SinglePanel && singlePanel == null) ||
            (curtainType == CurtainType.DoublePanel && (leftPanel == null || rightPanel == null)))
        {
            Debug.LogWarning(
                "CurtainController: panel references are missing."
            );

            return;
        }

        widthCm = Mathf.Clamp(widthCm, 10f, 10000f);
        heightCm = Mathf.Clamp(heightCm, 10f, 10000f);
        opening = Mathf.Clamp01(opening);
        fabricNaturalness = Mathf.Clamp01(fabricNaturalness);
        float width = widthCm / 100f;
        float height = heightCm / 100f;

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

        if (leftPanel != null) leftPanel.gameObject.SetActive(false);
        if (rightPanel != null) rightPanel.gameObject.SetActive(false);

        float visibleWidth = width * Mathf.Lerp(1f, 0.15f, opening);
        singlePanel.transform.localPosition = new Vector3((visibleWidth - width) / 2f, 0f, 0f);
        ConfigurePanel(singlePanel, visibleWidth, height, width, 0);
    }

    
    private void ConfigureDoublePanel(
        float width,
        float height)
    {
        if (singlePanel != null) singlePanel.gameObject.SetActive(false);

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

        ConfigurePanel(leftPanel, visiblePanelWidth, height, originalPanelWidth, 0);
        ConfigurePanel(rightPanel, visiblePanelWidth, height, originalPanelWidth, 101);
    }

    private void ConfigurePanel(ProceduralCurtainMesh panel, float visibleWidth,
        float panelHeight, float closedWidth, int seedOffset)
    {
        float density = Mathf.Clamp(foldsPerMeter, 1f, 32f);
        float depth = foldDepth;
        float profile = foldProfile;
        float naturalness = fabricNaturalness;
        switch (fabricStyle)
        {
            case FabricStyle.Liso:
                density = 3f;
                depth = 0.018f;
                profile = 1f;
                naturalness *= 0.65f;
                break;
            case FabricStyle.Natural:
                density = 5f;
                depth = 0.06f;
                profile = 1.15f;
                break;
            case FabricStyle.Wave:
                density = 6f;
                depth = 0.095f;
                profile = 0.8f;
                naturalness *= 0.45f;
                break;
        }

        int count = Mathf.Clamp(Mathf.RoundToInt(closedWidth * density), 1, 128);
        panel.Configure(visibleWidth, panelHeight, count, depth, profile, closedWidth,
            unchecked(fabricSeed + seedOffset), naturalness);
    }

    public void SetFabricStyle(FabricStyle style)
    {
        fabricStyle = style;
        UpdateCurtain();
    }

    public void ApplySettings(CurtainViewSettings settings)
    {
        if (settings == null) return;
        widthCm = Mathf.Clamp(settings.widthCentimeters, 10f, 10000f);
        heightCm = Mathf.Clamp(settings.heightCentimeters, 10f, 10000f);
        if (System.Enum.TryParse(settings.panelType, out CurtainType savedType))
            curtainType = savedType;
        if (System.Enum.TryParse(settings.fabricStyle, out FabricStyle savedStyle))
            fabricStyle = savedStyle;
        foldsPerMeter = Mathf.Clamp(settings.foldsPerMeter, 1f, 32f);
        foldDepth = Mathf.Clamp(settings.foldDepthCentimeters / 100f, 0f, 1f);
        foldProfile = Mathf.Clamp(settings.foldProfile, 0.5f, 2f);
        fabricNaturalness = Mathf.Clamp01(settings.naturalness);
        fabricSeed = settings.seed;
        opening = Mathf.Clamp01(settings.opening);
        UpdateCurtain();
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
        fabricStyle = FabricStyle.Personalizado;
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
