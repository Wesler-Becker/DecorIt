
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ARInteractionUI : MonoBehaviour
{
    [Header("Botões principais")]
    [SerializeField] private Button btnPosicionar;
    [SerializeField] private Button btnAjustar;

    [Header("Alternância de gesto")]
    [SerializeField] private Button btnModoGesto;
    [SerializeField] private TMP_Text textoModoGesto;

    [Header("Cores")]
    [SerializeField] private Color corInativa = Color.white;
    [SerializeField] private Color corAtiva = new Color(1f, 0.92f, 0.5f);

    private bool modoPosicionar = false;
    private bool modoAjustar = false;

    // false = profundidade; true = rotação
    private bool modoRotacao = false;

    private void Start()
    {
        btnPosicionar.onClick.AddListener(AtivarPosicionamento);
        btnAjustar.onClick.AddListener(AtivarAjuste);
        btnModoGesto.onClick.AddListener(AlternarModoGesto);

        AtualizarInterface();
    }

    private void OnDestroy()
    {
        btnPosicionar.onClick.RemoveListener(AtivarPosicionamento);
        btnAjustar.onClick.RemoveListener(AtivarAjuste);
        btnModoGesto.onClick.RemoveListener(AlternarModoGesto);
    }

    private void AtivarPosicionamento()
    {
        modoPosicionar = !modoPosicionar;

        if (modoPosicionar)
            modoAjustar = false;

        AtualizarInterface();
    }

    private void AtivarAjuste()
    {
        modoAjustar = !modoAjustar;

        if (modoAjustar)
            modoPosicionar = false;

        AtualizarInterface();
    }

    private void AlternarModoGesto()
    {
        modoRotacao = !modoRotacao;

        AtualizarInterface();
    }

    private void AtualizarInterface()
    {
        btnPosicionar.image.color =
            modoPosicionar ? corAtiva : corInativa;

        btnAjustar.image.color =
            modoAjustar ? corAtiva : corInativa;

        // O botão secundário só aparece durante o ajuste.
        btnModoGesto.gameObject.SetActive(modoAjustar);

        // O texto indica a ação que será ativada ao clicar.
        textoModoGesto.text =
            modoRotacao ? "Aproximar" : "Girar";

        Debug.Log(
            "Posicionar: " + modoPosicionar +
            " | Ajustar: " + modoAjustar +
            " | Rotação: " + modoRotacao
        );
    }

    public bool PosicionamentoAtivo()
    {
        return modoPosicionar;
    }

    public bool AjusteAtivo()
    {
        return modoAjustar;
    }

    public bool RotacaoAtiva()
    {
        return modoRotacao;
    }
}