using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class ARSurfaceUI : MonoBehaviour
{
    [SerializeField]
    private Button btnSuperficies;

    [SerializeField]
    private ARPlaneManager planeManager;

    [SerializeField]
    private Color corInativa = Color.white;

    [SerializeField]
    private Color corAtiva = new Color(1f, 0.92f, 0.5f);

    private static bool? visibilidadeAoRetornarDoCatalogo;
    private bool superficiesVisiveis;

    public bool PlanesVisible => superficiesVisiveis;

    public static void PreserveVisibilityForCatalogReturn(bool visible)
    {
        visibilidadeAoRetornarDoCatalogo = visible;
    }

    private void Awake()
    {
        // New AR sessions start with planes hidden. A trip to the catalog is the
        // only scene change that restores the current choice.
        superficiesVisiveis = visibilidadeAoRetornarDoCatalogo ?? false;
        visibilidadeAoRetornarDoCatalogo = null;
    }

    public void HideLegacyButton()
    {
        if (btnSuperficies != null) btnSuperficies.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (btnSuperficies != null)
        {
            RectTransform rect = btnSuperficies.transform as RectTransform;
            rect.anchoredPosition = new Vector2(-95f, 0f);
            rect.sizeDelta = new Vector2(136f, 132f);
            ARUIIconFactory.ConfigureButton(btnSuperficies, "Planes", "Planos", 58f, 22f);
            btnSuperficies.onClick.AddListener(AlternarSuperficies);
        }

        // Monitora novos planos detectados
        if (planeManager != null)
            planeManager.trackablesChanged.AddListener(OnPlanosAlterados);

        AtualizarPlanos();
        AtualizarInterface();
    }

    private void OnDestroy()
    {
        if (btnSuperficies != null)
            btnSuperficies.onClick.RemoveListener(AlternarSuperficies);

        if (planeManager != null)
            planeManager.trackablesChanged.RemoveListener(OnPlanosAlterados);
    }

    private void AlternarSuperficies()
    {
        ToggleFromCameraMenu();
    }

    public void ToggleFromCameraMenu()
    {
        superficiesVisiveis = !superficiesVisiveis;
        AtualizarPlanos();
        AtualizarInterface();
    }

    private void OnPlanosAlterados(
        ARTrackablesChangedEventArgs<ARPlane> args)
    {
        // Aplica o estado atual também aos novos planos
        foreach (var plane in args.added)
        {
            plane.gameObject.SetActive(
                superficiesVisiveis
            );
        }
    }

    private void AtualizarPlanos()
    {
        if (planeManager == null) return;
        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(
                superficiesVisiveis
            );
        }
    }

    private void AtualizarInterface()
    {
        if (btnSuperficies != null)
            ARUIIconFactory.SetSelected(btnSuperficies, superficiesVisiveis);
    }
}
