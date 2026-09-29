
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
        ConfigureLayout(btnPosicionar, new Vector2(1f, 0f), new Vector2(-110f, 95f));
        ConfigureLayout(btnAjustar, new Vector2(0f, 0f), new Vector2(110f, 95f));
        ConfigureLayout(btnModoGesto, new Vector2(0f, 0f), new Vector2(95f, 235f));
        ARUIIconFactory.ConfigureButton(btnPosicionar, "Position", "Posicionar", 58f, 21f, 160f);
        ARUIIconFactory.ConfigureButton(btnAjustar, "Adjust", "Ajustar", 58f, 21f, 160f);
        ARUIIconFactory.ConfigureButton(btnModoGesto, "Rotate", "Girar", 58f, 22f);
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
        ARUIIconFactory.SetSelected(btnPosicionar, modoPosicionar);
        ARUIIconFactory.SetSelected(btnAjustar, modoAjustar);

        // O botão secundário só aparece durante o ajuste.
        btnModoGesto.gameObject.SetActive(modoAjustar);

        // O texto indica a ação que será ativada ao clicar.
        textoModoGesto.text = modoRotacao ? "Aproximar" : "Girar";
        ARUIIconFactory.ConfigureButton(btnModoGesto, modoRotacao ? "Zoom" : "Rotate", textoModoGesto.text, 58f, 22f);

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

    private static void ConfigureLayout(Button button, Vector2 anchor, Vector2 position)
    {
        if (button == null) return;
        RectTransform rect = button.transform as RectTransform;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
    }
}
