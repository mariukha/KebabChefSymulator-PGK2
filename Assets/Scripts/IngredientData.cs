using UnityEngine;

[CreateAssetMenu(fileName = "NowySkladnik", menuName = "KebabGame/Skladnik")]
public class IngredientData : ScriptableObject
{
    [Header("Istniejace pola assetu")]
    public string nazwaSkladnika;
    public float cenaZakupu;
    public Sprite ikona;
    public GameObject model3D;

    [Header("Rozszerzone dane gameplayowe")]
    public IngredientKind typSkladnika = IngredientKind.Tomato;
    public IngredientProcessState stanPoczatkowy = IngredientProcessState.Raw;
    public float wartoscSprzedazy = 5f;
    public Color kolorDebug = Color.white;

    public string DisplayName
    {
        get
        {
            return string.IsNullOrWhiteSpace(nazwaSkladnika) ? name : nazwaSkladnika;
        }
    }
}
