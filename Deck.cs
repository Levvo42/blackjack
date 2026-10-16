namespace Black_Jack;

// Defines what a DECK is - a collection of CARD(s).

class Deck
{
    public List<Card> Cards { get; } = new List<Card>();
    
    public void CreateDeck()
    {
        string[] suits = { "Hearts", "Spades",  "Clubs", "Diamonds" };
        
        string[] ranks  =
        {
            "A", "2", "3", "4", "5", "6", "7",
            "8", "9", "10", "J", "Q", "K"
        };
        
        foreach (string suit in suits)
        {
            foreach (string rank in ranks)
            {
                Card card = new Card();
                
                card.Suit = suit;
                card.Rank = rank;

                if (rank == "A")
                {
                    card.Value = 11;
                }
                else if (rank == "J" || rank == "Q" || rank == "K")
                {
                    card.Value = 10;
                }
                else
                {
                    card.Value = int.Parse(rank);
                }

                Cards.Add(card);
            }
        }
    }
};




