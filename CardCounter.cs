namespace Black_Jack;

static class CardCounter
{
    // =========================================================
    // SETTINGS
    // =========================================================

    private const int CardsPerDeck = 52;
    private const int ReshuffleAt = 62;

    // =========================================================
    // CARD VALUES (HI-LO)
    // =========================================================

    public static int GetCountValue(Card card)
    {
        return card.Rank switch
        {
            "2" or "3" or "4" or "5" or "6" => 1,
            "7" or "8" or "9" => 0,
            _ => -1
        };
    }
    
    // =========================================================
    // VISIBLE CARDS
    // =========================================================

    public static List<Card> GetVisibleTableCards(Game game)
    {
        List<Card> cards = new List<Card>();

        foreach (Hand hand in game.Player.Hands)
        {
            cards.AddRange(hand.Cards);
        }

        List<Card> dealerCards = game.Dealer.Hand.Cards;

        for (int i = 0; i < dealerCards.Count; i++)
        {
            bool isHoleCard = i == 1;

            if (isHoleCard && game.HideDealerHoleCard)
            {
                continue;
            }

            cards.Add(dealerCards[i]);
        }

        return cards;
    }
    
    // =========================================================
    // COUNT
    // =========================================================

    public static int GetRunningCount(Game game)
    {
        int count = 0;

        foreach (Card card in game.Shoe.DiscardPile)
        {
            count += GetCountValue(card);
        }

        foreach (Card card in GetVisibleTableCards(game))
        {
            count += GetCountValue(card);
        }

        return count;
    }
    
    public static int GetUnseenCardCount(Game game)
    {
        int unseen = game.Shoe.Cards.Count;

        bool holeCardHidden =
            game.HideDealerHoleCard &&
            game.Dealer.Hand.Cards.Count >= 2;

        if (holeCardHidden)
        {
            unseen++;
        }

        return unseen;
    }

    public static double GetDecksRemaining(Game game)
    {
        double decks =
            GetUnseenCardCount(game) / (double)CardsPerDeck;

        return Math.Max(decks, 0.5);
    }

    public static int GetTrueCount(Game game)
    {
        double trueCount =
            GetRunningCount(game) / GetDecksRemaining(game);

        return (int)Math.Floor(trueCount);
    }
    
    // =========================================================
    // EXPLANATION
    // =========================================================

    public static int GetCountBeforeThisRound(Game game)
    {
        int count = 0;

        foreach (Card card in game.Shoe.DiscardPile)
        {
            count += GetCountValue(card);
        }

        return count;
    }

    public static string ExplainCard(Card card)
    {
        int value = GetCountValue(card);

        if (value > 0)
        {
            return $"{card.Rank} is low (2-6): +1, the shoe got better for you";
        }

        if (value < 0)
        {
            return $"{card.Rank} is high (10-A): -1, the shoe got worse for you";
        }

        return $"{card.Rank} is neutral (7-9): 0, no change";
    }

    public static List<string> GetRoundBreakdown(Game game)
    {
        List<string> lines = new List<string>();

        int count = GetCountBeforeThisRound(game);

        lines.Add($"Count before this round: {FormatCount(count)}");

        foreach (Card card in GetVisibleTableCards(game))
        {
            count += GetCountValue(card);

            lines.Add($"{ExplainCard(card)}  →  {FormatCount(count)}");
        }

        return lines;
    }

    public static string FormatCount(int count)
    {
        return count.ToString("+0;-0;0");
    }
    
    // =========================================================
    // BET ADVICE
    // =========================================================

    public static bool IsReshuffleComing(Game game)
    {
        return game.Shoe.Cards.Count <= ReshuffleAt;
    }

    public static int GetBetUnits(Game game)
    {
        if (IsReshuffleComing(game))
        {
            return 1;
        }

        int trueCount = GetTrueCount(game);

        return trueCount switch
        {
            <= 1 => 1,
            2 => 2,
            3 => 4,
            4 => 6,
            _ => 8
        };
    }

    public static string GetBetAdvice(Game game)
    {
        int units = GetBetUnits(game);

        if (IsReshuffleComing(game))
        {
            return "Shuffle coming: count resets to 0. Bet 1 unit.";
        }

        string trueCount = FormatCount(GetTrueCount(game));

        return $"True count {trueCount}: bet {units} unit(s).";
    }
    
      // =========================================================
    // PLAY ADVICE
    // =========================================================

    public static (string Move, string Reason) GetPlayAdvice(Game game)
    {
        Hand hand = game.CurrentHand;

        int dealerCard = game.Dealer.Hand.Cards[0].Value;
        int total = hand.GetValue();
        bool isSoft = hand.HasSoftAce();
        bool canDouble = game.Player.CanDoubleDown(hand);
        bool canSplit = game.CanSplit(hand);
        int trueCount = GetTrueCount(game);

        string handName = DescribeHand(hand, total, isSoft, canSplit);
        string dealerName = dealerCard == 11 ? "A" : dealerCard.ToString();
        string situation = $"{handName} vs dealer {dealerName}";

        // PAIRS

        if (canSplit)
        {
            int pairValue = hand.Cards[0].Value;

            if (ShouldSplit(pairValue, dealerCard, trueCount))
            {
                return ("Split", $"{situation}: split.");
            }
        }

        // COUNT DEVIATIONS

        string basicMove =
            GetBasicMove(total, isSoft, dealerCard, canDouble);

        if (!isSoft)
        {
            string? deviation =
                GetDeviation(total, dealerCard, canDouble, trueCount);

            if (deviation != null && deviation != basicMove)
            {
                return (
                    deviation,
                    $"{situation}: basic says {basicMove}, " +
                    $"but true count {FormatCount(trueCount)} says {deviation}."
                );
            }
        }

        // BASIC STRATEGY

        return (basicMove, $"{situation}: basic strategy says {basicMove}.");
    }

    private static string DescribeHand(
        Hand hand,
        int total,
        bool isSoft,
        bool canSplit
    )
    {
        if (canSplit)
        {
            return $"Pair of {hand.Cards[0].Rank}s";
        }

        return isSoft ? $"Soft {total}" : $"Hard {total}";
    }
    
    private static bool ShouldSplit(
        int pairValue,
        int dealerCard,
        int trueCount
    )
    {
        switch (pairValue)
        {
            case 11:
                return true;

            case 10:
                return (dealerCard == 5 && trueCount >= 5) ||
                       (dealerCard == 6 && trueCount >= 4);

            case 9:
                return dealerCard <= 6 ||
                       dealerCard == 8 ||
                       dealerCard == 9;

            case 8:
                return true;

            case 7:
                return dealerCard <= 7;

            case 6:
                return dealerCard <= 6;

            case 5:
                return false;

            case 4:
                return dealerCard == 5 || dealerCard == 6;

            default:
                return dealerCard <= 7;
        }
    }
    
    private static string GetBasicMove(
        int total,
        bool isSoft,
        int dealerCard,
        bool canDouble
    )
    {
        string DoubleOr(string fallback)
        {
            return canDouble ? "Double" : fallback;
        }

        bool dealerWeak = dealerCard >= 2 && dealerCard <= 6;

        // SOFT TOTALS

        if (isSoft)
        {
            if (total >= 20) return "Stand";

            if (total == 19)
            {
                return dealerCard == 6 ? DoubleOr("Stand") : "Stand";
            }

            if (total == 18)
            {
                if (dealerWeak) return DoubleOr("Stand");
                if (dealerCard <= 8) return "Stand";
                return "Hit";
            }

            if (total == 17)
            {
                return dealerCard >= 3 && dealerCard <= 6
                    ? DoubleOr("Hit")
                    : "Hit";
            }

            if (total == 15 || total == 16)
            {
                return dealerCard >= 4 && dealerCard <= 6
                    ? DoubleOr("Hit")
                    : "Hit";
            }

            if (total == 13 || total == 14)
            {
                return dealerCard == 5 || dealerCard == 6
                    ? DoubleOr("Hit")
                    : "Hit";
            }

            return "Hit";
        }

        // HARD TOTALS

        if (total >= 17) return "Stand";

        if (total >= 13) return dealerWeak ? "Stand" : "Hit";

        if (total == 12)
        {
            return dealerCard >= 4 && dealerCard <= 6 ? "Stand" : "Hit";
        }

        if (total == 11) return DoubleOr("Hit");

        if (total == 10)
        {
            return dealerCard <= 9 ? DoubleOr("Hit") : "Hit";
        }

        if (total == 9)
        {
            return dealerCard >= 3 && dealerCard <= 6
                ? DoubleOr("Hit")
                : "Hit";
        }

        return "Hit";
    }
    
    private static string? GetDeviation(
        int total,
        int dealerCard,
        bool canDouble,
        int trueCount
    )
    {
        if (total == 16 && dealerCard == 10 && trueCount >= 0) return "Stand";
        if (total == 16 && dealerCard == 9 && trueCount >= 5) return "Stand";
        if (total == 15 && dealerCard == 10 && trueCount >= 4) return "Stand";

        if (total == 13 && dealerCard == 2 && trueCount <= -2) return "Hit";
        if (total == 13 && dealerCard == 3 && trueCount <= -3) return "Hit";

        if (total == 12 && dealerCard == 2 && trueCount >= 3) return "Stand";
        if (total == 12 && dealerCard == 3 && trueCount >= 2) return "Stand";
        if (total == 12 && dealerCard == 4 && trueCount < 0) return "Hit";
        if (total == 12 && dealerCard == 5 && trueCount <= -3) return "Hit";
        if (total == 12 && dealerCard == 6 && trueCount <= -2) return "Hit";

        if (canDouble)
        {
            if (total == 10 && dealerCard == 10 && trueCount >= 4) return "Double";
            if (total == 10 && dealerCard == 11 && trueCount >= 3) return "Double";
            if (total == 9 && dealerCard == 2 && trueCount >= 1) return "Double";
            if (total == 9 && dealerCard == 7 && trueCount >= 3) return "Double";
        }

        return null;
    }
    
    // =========================================================
    // INSURANCE ADVICE
    // =========================================================

    public static bool ShouldTakeInsurance(Game game)
    {
        return GetTrueCount(game) >= 3;
    }
    
    // =========================================================
    // SUMMARY
    // =========================================================

    public static string GetSummaryLine(Game game, bool showPlay)
    {
        string summary =
            $"RC {FormatCount(GetRunningCount(game))}  " +
            $"TC {FormatCount(GetTrueCount(game))}  " +
            $"Decks {GetDecksRemaining(game):0.0}  " +
            $"Bet {GetBetUnits(game)}u";

        if (showPlay && IsHandWaitingForMove(game))
        {
            summary += $"  Play {GetPlayAdvice(game).Move}";

            if (IsInsuranceQuestion(game))
            {
                string insure = ShouldTakeInsurance(game) ? "Yes" : "No";

                summary += $"  Insure {insure}";
            }
        }

        return summary;
    }

    private static bool IsHandWaitingForMove(Game game)
    {
        return game.PlayerTurn &&
               game.Dealer.Hand.Cards.Count >= 2 &&
               game.CurrentHandIndex < game.Player.Hands.Count &&
               game.CurrentHand.Cards.Count >= 2 &&
               !game.CurrentHand.IsFinished;
    }

    private static bool IsInsuranceQuestion(Game game)
    {
        return game.Player.Hands.Count == 1 &&
               game.Player.Hands[0].Cards.Count == 2 &&
               game.CanTakeInsurance(game.Player.Hands[0]);
    }
}
