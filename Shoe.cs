namespace Black_Jack;

// Game flow and Rules

class Shoe
{
    public List<Deck> Decks { get; } = new List<Deck>();
    public List<Card> Cards { get; } = new List<Card>();
    public List<Card> DiscardPile { get; } = new List<Card>();
    
    public void CreateShoe()
    {
        for (int i = 0; i < 6; i++)
        {
            Deck deck = new Deck();
            deck.CreateDeck();
            
            Decks.Add(deck);
        }
        
        foreach (Deck deck in Decks)
        {
            foreach (Card card in deck.Cards)
            {
                Cards.Add(card);
            }
        }
    }

    public void Shuffle()
    {
        Random random = new Random();
        
        for (int i = Cards.Count -1; i > 0; i--)
        {
            int j = random.Next(i + 1);

            Card temp = Cards[i];
            Cards[i] = Cards[j];
            Cards[j] = temp;
        }
    }

    public Card DrawCard()
    {
        int lastIndex = Cards.Count - 1;

        Card drawnCard = Cards[lastIndex];
        
        Cards.RemoveAt(lastIndex);

        return drawnCard;
    }

    public void DiscardHand(List<Card> hand)
    {
        foreach (Card card in hand)
        {
            DiscardPile.Add(card);  
        }
        
        hand.Clear();
    }

    public void ReshuffleIfNeeded()
    {
        if (Cards.Count <= 62)
        {
            foreach (Card card in DiscardPile)
            {
                Cards.Add(card);
            }
            
            DiscardPile.Clear();

            Shuffle();
        }
    }
}