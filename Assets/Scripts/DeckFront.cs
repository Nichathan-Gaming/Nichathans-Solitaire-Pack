using UnityEngine;

[CreateAssetMenu(fileName = "Deck", menuName = "ScriptableObjects/DeckFront", order = 1)]
public class DeckFront : ScriptableObject
{
    [Header("Name")]
    public string deckName;

    [Header("Spades, Clubs, Hearts, Diamonds")]
    public Suit[] deck;

    [System.Serializable]
    public class Suit
    {
        public Sprite[] cards;
    }
}