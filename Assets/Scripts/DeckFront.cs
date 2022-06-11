using UnityEngine;

[CreateAssetMenu(fileName = "Deck", menuName = "ScriptableObjects/DeckFront", order = 1)]
public class DeckFront : ScriptableObject
{
    [Header("Name")]
    public string deckName;

    [Header("Spades")]
    public Sprite aceSpades;
    public Sprite twoSpades;
    public Sprite threeSpades;
    public Sprite fourSpades;
    public Sprite fiveSpades;
    public Sprite sixSpades;
    public Sprite sevenSpades;
    public Sprite eightSpades;
    public Sprite nineSpades;
    public Sprite tenSpades;
    public Sprite jackSpades;
    public Sprite queenSpades;
    public Sprite kingSpades;

    [Header("Clubs")]
    public Sprite aceClubs;
    public Sprite twoClubs;
    public Sprite threeClubs;
    public Sprite fourClubs;
    public Sprite fiveClubs;
    public Sprite sixClubs;
    public Sprite sevenClubs;
    public Sprite eightClubs;
    public Sprite nineClubs;
    public Sprite tenClubs;
    public Sprite jackClubs;
    public Sprite queenClubs;
    public Sprite kingClubs;

    [Header("Hearts")]
    public Sprite aceHearts;
    public Sprite twoHearts;
    public Sprite threeHearts;
    public Sprite fourHearts;
    public Sprite fiveHearts;
    public Sprite sixHearts;
    public Sprite sevenHearts;
    public Sprite eightHearts;
    public Sprite nineHearts;
    public Sprite tenHearts;
    public Sprite jackHearts;
    public Sprite queenHearts;
    public Sprite kingHearts;

    [Header("Diamonds")]
    public Sprite aceDiamonds;
    public Sprite twoDiamonds;
    public Sprite threeDiamonds;
    public Sprite fourDiamonds;
    public Sprite fiveDiamonds;
    public Sprite sixDiamonds;
    public Sprite sevenDiamonds;
    public Sprite eightDiamonds;
    public Sprite nineDiamonds;
    public Sprite tenDiamonds;
    public Sprite jackDiamonds;
    public Sprite queenDiamonds;
    public Sprite kingDiamonds;
}
