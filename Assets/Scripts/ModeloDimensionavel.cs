
using UnityEngine;

public class ModeloDimensionavel : MonoBehaviour
{
    [Header("Componentes do modelo")]
    [SerializeField] private Transform tecido;
    [SerializeField] private Transform trilho;

    [Header("Medidas iniciais em centímetros")]
    [SerializeField] private float larguraCm = 180f;
    [SerializeField] private float alturaCm = 140f;

    [Header("Configuração")]
    [SerializeField] private float sobraTrilhoCm = 10f;

    private void Start()
    {
        AplicarMedidas(larguraCm, alturaCm);
    }

    public void AplicarMedidas(float novaLarguraCm, float novaAlturaCm)
    {
        // Evita medidas inválidas.
        if (novaLarguraCm <= 0 || novaAlturaCm <= 0)
        {
            Debug.LogWarning("As medidas devem ser maiores que zero.");
            return;
        }

        larguraCm = novaLarguraCm;
        alturaCm = novaAlturaCm;

        // No Unity, trabalharemos com metros.
        float largura = larguraCm / 100f;
        float altura = alturaCm / 100f;
        float sobraTrilho = sobraTrilhoCm / 100f;

        // Ajusta o tecido.
        tecido.localScale = new Vector3(
            largura,
            altura,
            0.02f
        );

        // Mantém o topo do tecido na origem do modelo.
        tecido.localPosition = new Vector3(
            0f,
            -altura / 2f,
            0f
        );

        // Ajusta apenas o comprimento do trilho.
        trilho.localScale = new Vector3(
            largura + sobraTrilho,
            0.025f,
            0.04f
        );

        trilho.localPosition = Vector3.zero;

        Debug.Log(
            "Modelo ajustado: " +
            larguraCm + " cm x " +
            alturaCm + " cm"
        );
    }
}