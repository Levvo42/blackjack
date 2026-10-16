namespace Black_Jack;

// Game flow and rules

class Game
{
    public Player Player { get; } = new Player();
    public Dealer Dealer { get; } = new Dealer();
    public Shoe Shoe { get; } = new Shoe();

    // SETTINGS

    public bool DealerHitsSoft17 { get; set; } = true;
    public int MaxPlayerHands { get; set; } = 4;

    // ROUND STATE

    public bool HideDealerHoleCard { get; private set; } = true;
    public bool PlayerTurn { get; private set; }
    public bool DealerTurn { get; private set; }

    public int CurrentHandIndex { get; private set; }
    public Hand CurrentHand => Player.Hands[CurrentHandIndex];

    public void RoundStart()
    {
        Player.Hands.Clear();
        Player.Hands.Add(new Hand());

        CurrentHandIndex = 0;

        HideDealerHoleCard = true;
        PlayerTurn = true;
        DealerTurn = false;
    }

    // DEALING

    public void DealCardToPlayer(Hand hand)
    {
        hand.Cards.Add(Shoe.DrawCard());
    }

    public void DealCardToDealer()
    {
        Dealer.Hand.Cards.Add(Shoe.DrawCard());
    }

    public void HitPlayer(Hand hand)
    {
        hand.Cards.Add(Shoe.DrawCard());
    }

    // INITIAL BLACKJACK

    public RoundResult? GetInitialBlackjackResult()
    {
        bool playerBlackjack = Player.Hands[0].HasBlackjack();
        bool dealerBlackjack = Dealer.Hand.HasBlackjack();

        if (playerBlackjack && dealerBlackjack)
        {
            return RoundResult.Push;
        }

        if (playerBlackjack)
        {
            return RoundResult.Blackjack;
        }

        if (dealerBlackjack)
        {
            return RoundResult.Lose;
        }

        return null;
    }

    // INSURANCE

    public bool CanTakeInsurance(Hand hand)
    {
        return Dealer.Hand.Cards.Count > 0 &&
               Dealer.Hand.Cards[0].Rank == "A" &&
               hand.InsuranceBet == 0 &&
               Player.Credits >= hand.Bet / 2;
    }

    public bool TakeInsurance(Hand hand)
    {
        if (!CanTakeInsurance(hand))
        {
            return false;
        }

        decimal insuranceAmount = hand.Bet / 2;

        Player.RemoveCredits(insuranceAmount);
        hand.InsuranceBet = insuranceAmount;

        return true;
    }

    public bool ResolveInsurance(Hand hand)
    {
        if (hand.InsuranceBet <= 0)
        {
            return false;
        }

        bool insuranceWon = Dealer.Hand.HasBlackjack();

        if (insuranceWon)
        {
            Player.AddCredits(hand.InsuranceBet * 3);
        }

        hand.InsuranceBet = 0;

        return insuranceWon;
    }

    // SPLIT

    public bool CanSplit(Hand hand)
    {
        return hand.Cards.Count == 2 &&
               hand.Cards[0].Rank == hand.Cards[1].Rank &&
               Player.Credits >= hand.Bet &&
               Player.Hands.Count < MaxPlayerHands;
    }

    public bool Split(Hand hand)
    {
        if (!CanSplit(hand))
        {
            return false;
        }

        Hand newHand = new Hand();

        newHand.Cards.Add(hand.Cards[1]);
        hand.Cards.RemoveAt(1);

        newHand.Bet = hand.Bet;
        Player.RemoveCredits(hand.Bet);

        Player.Hands.Insert(CurrentHandIndex + 1, newHand);

        return true;
    }

    // DOUBLE DOWN

    public bool DoubleDown(Hand hand)
    {
        if (!Player.DoubleDown(hand))
        {
            return false;
        }

        hand.Cards.Add(Shoe.DrawCard());
        hand.IsStanding = true;

        return true;
    }

    // HAND FLOW

    public bool MoveToNextHand()
    {
        CurrentHandIndex++;

        while (CurrentHandIndex < Player.Hands.Count)
        {
            if (!Player.Hands[CurrentHandIndex].IsFinished)
            {
                return true;
            }

            CurrentHandIndex++;
        }

        return false;
    }

    public void EndPlayerTurn()
    {
        PlayerTurn = false;
        DealerTurn = true;
    }

    // DEALER

    public void RevealDealerHoleCard()
    {
        HideDealerHoleCard = false;
    }

    public bool ShouldDealerHit()
    {
        int value = Dealer.Hand.GetValue();

        return value < 17 ||
               (value == 17 &&
                Dealer.Hand.HasSoftAce() &&
                DealerHitsSoft17);
    }

    public void HitDealer()
    {
        Dealer.Hand.Cards.Add(Shoe.DrawCard());
    }

    public void EndDealerTurn()
    {
        DealerTurn = false;
    }

    // RESULTS

    public RoundResult GetHandResult(Hand hand)
    {
        int playerTotal = hand.GetValue();
        int dealerTotal = Dealer.Hand.GetValue();

        if (hand.IsBusted)
        {
            return RoundResult.Lose;
        }

        if (dealerTotal > 21)
        {
            return RoundResult.Win;
        }

        if (dealerTotal > playerTotal)
        {
            return RoundResult.Lose;
        }

        if (dealerTotal < playerTotal)
        {
            return RoundResult.Win;
        }

        return RoundResult.Push;
    }

    public void Payout(RoundResult result, Hand hand)
    {
        if (result == RoundResult.Win)
        {
            Player.AddCredits(hand.Bet * 2);
        }
        else if (result == RoundResult.Blackjack)
        {
            Player.AddCredits(hand.Bet * 2.5m);
        }
        else if (result == RoundResult.Push)
        {
            Player.AddCredits(hand.Bet);
        }

        hand.Bet = 0;
    }

    public void RoundEnd()
    {
        foreach (Hand hand in Player.Hands)
        {
            Shoe.DiscardHand(hand.Cards);
        }

        Shoe.DiscardHand(Dealer.Hand.Cards);
        Shoe.ReshuffleIfNeeded();

        PlayerTurn = false;
        DealerTurn = false;
    }
}