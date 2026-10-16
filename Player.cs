namespace Black_Jack;

class Player
{
    public string Name { get; set; }

    public decimal Credits { get; private set; } = 1000m;

    public List<Hand> Hands { get; } = new List<Hand>();
    
    public bool PlaceBet(Hand hand, decimal amount)
    {
        if (amount <= 0 || amount > Credits)
        {
            return false;
        }

        Credits -= amount;
        hand.Bet = amount;

        return true;
    }
    
    public void AddCredits(decimal amount)
    {
        Credits += amount;
    }
    
    public void RemoveCredits(decimal amount)
    {
        Credits -= amount;
    }
    
    public bool CanDoubleDown(Hand hand)
    {
        return hand.Cards.Count == 2 &&
               Credits >= hand.Bet;
    }
    
    public bool DoubleDown(Hand hand)
    {
        if (!CanDoubleDown(hand))
        {
            return false;
        }

        Credits -= hand.Bet;
        hand.Bet *= 2;

        return true;
    }
    
    public void BuyChips(decimal amount)
    {
        if (amount > 0)
        {
            Credits += amount;
        }
    }
    
}