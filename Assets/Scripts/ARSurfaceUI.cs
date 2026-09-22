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

    private bool superficiesVisiveis = true;

    private void Start()
    {
        btnSuperficies.onClick.AddListener(AlternarSuperficies);

        // Monitora novos planos detectados
        planeManager.trackablesChanged.AddListener(
            OnPlanosAlterados
        );

        AtualizarInterface();
    }

    private void OnDestroy()
    {
        btnSuperficies.onClick.RemoveListener(
            AlternarSuperficies
        );

        planeManager.trackablesChanged.RemoveListener(
            OnPlanosAlterados
        );
    }

    private void AlternarSuperficies()
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
        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(
                superficiesVisiveis
            );
        }
    }

    private void AtualizarInterface()
    {
        btnSuperficies.image.color =
            superficiesVisiveis
                ? corAtiva
                : corInativa;
    }
}