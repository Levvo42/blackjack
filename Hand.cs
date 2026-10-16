namespace Black_Jack;

class Hand
{
    public List<Card> Cards { get; } = new List<Card>();

    public decimal Bet { get; set; }
    
    public decimal InsuranceBet { get; set; }

    public bool IsStanding { get; set; }

    public bool IsBusted => GetValue() > 21;
    
    public bool IsFinished => IsStanding || IsBusted;

    public int GetValue()
    {
        int total = 0;
        int aces = 0;

        foreach (Card card in Cards)
        {
            total += card.Value;

            if (card.Rank == "A")
            {
                aces++;
            }
        }

        while (total > 21 && aces > 0)
        {
            total -= 10;
            aces--;
        }

        return total;
    }

    public bool HasBlackjack()
    {
        return Cards.Count == 2 && GetValue() == 21;
    }

    public bool HasSoftAce()
    {
        int total = 0;
        int aces = 0;

        foreach (Card card in Cards)
        {
            total += card.Value;

            if (card.Rank == "A")
            {
                aces++;
            }
        }

        while (total > 21 && aces > 0)
        {
            total -= 10;
            aces--;
        }

        return aces > 0;
    }
}