[System.Serializable]
public class Card
{
    public int suit;
    public int cardNumber;

    public Card(int suit, int cardNumber)
    {
        this.suit = suit;
        this.cardNumber = cardNumber;
    }

    public int getSuit()
    {
        return suit;
    }

    public Card setSuit(int suit)
    {
        this.suit = suit;
        return this;
    }

    public int getCardNumber()
    {
        return cardNumber;
    }

    public Card setCardNumber(int cardNumber)
    {
        this.cardNumber = cardNumber;
        return this;
    }

    public char getSuitAsChar()
    {
        switch (suit)
        {
            case 0:
                return 'H';
            case 1:
                return 'D';
            case 2:
                return 'S';
            case 3:
                return 'C';
            default:
                return ' ';
        }
    }

    public string getSuitAsString()
    {
        switch (suit)
        {
            case 0:
                return "Hearts";
            case 1:
                return "Diamonds";
            case 2:
                return "Spades";
            case 3:
                return "Clubs";
            default:
                return "";
        }
    }

    public char getCardNumberAsChar()
    {
        switch (cardNumber)
        {
            case 0:
                return 'A';
            case 1:
            case 2:
            case 3:
            case 4:
            case 5:
            case 6:
            case 7:
            case 8:
                //return Integer.toString(cardNumber + 1).charAt(0);
                string s = "" + (cardNumber + 1);
                return s[0];
            case 9:
                return 'X';
            case 10:
                return 'J';
            case 11:
                return 'Q';
            case 12:
                return 'K';
            default:
                return ' ';
        }
    }

    public int getCardValue()
    {
        switch (cardNumber)
        {
            case 0:
            case 1:
            case 2:
            case 3:
            case 4:
            case 5:
            case 6:
            case 7:
            case 8:
                return cardNumber + 1;
            case 9:
            case 10:
            case 11:
            case 12:
                return 10;
            default:
                return -1;
        }
    }

    public bool equals(Card c)
    {
        if (this.equals(c)) return true;
        if (c == null || this.GetType() != c.GetType()) return false;
        return suit == c.suit && cardNumber == c.cardNumber;
    }

    public string toString()
    {
        return "S" + suit + "N" + cardNumber;
    }

    public string toCardString()
    {
        return "+---+\n"
                + "| " + getSuitAsChar() + " |\n"
                + "+---+\n"
                + "| " + getCardNumberAsChar() + " |\n"
                + "+---+";
    }
}
