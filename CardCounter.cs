namespace Black_Jack;

static class CardCounter
{
    // =========================================================
    // SETTINGS
    // =========================================================

    private const int CardsPerDeck = 52;
    private const int ReshuffleAt = 62;

    private const double BaseEdgePercent = -0.4;
    private const double EdgePerTrueCount = 0.5;

    private const decimal UnitShareOfCredits = 0.005m;
    private const decimal SmallestChip = 2.5m;

    private const int InsuranceIndex = 3;

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

    public static double GetExactTrueCount(Game game)
    {
        return GetRunningCount(game) / GetDecksRemaining(game);
    }

    public static int GetTrueCount(Game game)
    {
        return (int)Math.Floor(GetExactTrueCount(game));
    }

    public static string GetTrueCountMath(Game game, bool explainRounding)
    {
        string math =
            $"TC = RC {FormatCount(GetRunningCount(game))} ÷ " +
            $"{GetDecksRemaining(game):0.0} decks" +
            $"{(explainRounding ? " left" : "")} = " +
            $"{FormatOneDecimal(GetExactTrueCount(game))}";

        string trueCount = FormatCount(GetTrueCount(game));

        return explainRounding
            ? $"{math}, rounded down to {trueCount}"
            : $"{math} → {trueCount}";
    }

    private static string FormatOneDecimal(double value)
    {
        double rounded = Math.Round(value, 1) + 0.0;

        return rounded.ToString("+0.0;-0.0;0.0");
    }

    // =========================================================
    // EDGE
    // =========================================================

    public static double GetEdgePercent(Game game)
    {
        if (IsReshuffleComing(game))
        {
            return BaseEdgePercent;
        }

        return BaseEdgePercent + (EdgePerTrueCount * GetExactTrueCount(game));
    }
    
    // =========================================================
    // EXPLANATION
    // =========================================================

    public static string GetCardNote(
        string cardLabel,
        Card card,
        bool isHoleCard,
        Game game
    )
    {
        string name = isHoleCard ? $"Hole card {cardLabel}" : cardLabel;
        string runningCount = FormatCount(GetRunningCount(game));

        return GetCountValue(card) switch
        {
            > 0 => $"{name} is low (2-6): +1. RC now {runningCount}",
            < 0 => $"{name} is high (10-A): -1. RC now {runningCount}",
            _ => $"{name} is neutral (7-9): 0. RC now {runningCount}"
        };
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

    private static decimal GetBankroll(Game game)
    {
        decimal bankroll = game.Player.Credits;

        foreach (Hand hand in game.Player.Hands)
        {
            bankroll += hand.Bet + hand.InsuranceBet;
        }

        return bankroll;
    }

    private static decimal RoundDownToChip(decimal amount)
    {
        return Math.Floor(amount / SmallestChip) * SmallestChip;
    }

    public static decimal GetUnitSize(Game game)
    {
        decimal chips = Math.Round(
            GetBankroll(game) * UnitShareOfCredits / SmallestChip,
            MidpointRounding.AwayFromZero
        );

        return Math.Max(chips * SmallestChip, SmallestChip);
    }

    public static decimal GetSuggestedBet(Game game)
    {
        decimal bet = GetBetUnits(game) * GetUnitSize(game);
        decimal affordable = RoundDownToChip(GetBankroll(game));

        return Math.Max(Math.Min(bet, affordable), SmallestChip);
    }

    private static string GetBetLabel(int units)
    {
        return units switch
        {
            1 => "MIN",
            2 => "SMALL",
            4 => "MEDIUM",
            6 => "HIGH",
            _ => "MAX"
        };
    }

    public static string GetBetLine(Game game)
    {
        string bet =
            $"BET {ConsoleUI.FormatMoney(GetSuggestedBet(game))} " +
            $"({GetBetLabel(GetBetUnits(game))}): ";

        if (IsReshuffleComing(game))
        {
            return bet + "shuffle next round, the count resets to 0";
        }

        int trueCount = GetTrueCount(game);
        string tc = $"TC {FormatCount(trueCount)}";

        if (trueCount < 0)
        {
            return bet + $"{tc}, many 10s and aces are gone. House edge";
        }

        if (trueCount == 0)
        {
            return bet + $"{tc}, a normal shoe. The house has a small edge";
        }

        if (trueCount == 1)
        {
            return bet + $"{tc}, about break-even. Raise from TC +2";
        }

        return bet + $"{tc}, extra 10s and aces left. Your edge";
    }

    public static string GetBetNote(Game game)
    {
        if (IsReshuffleComing(game))
        {
            return $"{ReshuffleAt} or fewer cards left: dealer reshuffles";
        }

        return GetTrueCountMath(game, false);
    }

    // =========================================================
    // PLAY ADVICE
    // =========================================================

    private record PlayAdvice(
        string Move,
        string HandName,
        string DealerName,
        string Why,
        string CountNote
    );

    private record Deviation(
        int Total,
        int Dealer,
        int Index,
        bool AtOrAbove,
        string Move
    );

    private static readonly Deviation[] HardDeviations =
    [
        new Deviation(16, 10, 0, true, "Stand"),
        new Deviation(16, 9, 5, true, "Stand"),
        new Deviation(15, 10, 4, true, "Stand"),

        new Deviation(13, 2, -2, false, "Hit"),
        new Deviation(13, 3, -3, false, "Hit"),

        new Deviation(12, 2, 3, true, "Stand"),
        new Deviation(12, 3, 2, true, "Stand"),
        new Deviation(12, 4, -1, false, "Hit"),
        new Deviation(12, 5, -3, false, "Hit"),
        new Deviation(12, 6, -2, false, "Hit"),

        new Deviation(10, 10, 4, true, "Double"),
        new Deviation(10, 11, 3, true, "Double"),
        new Deviation(9, 2, 1, true, "Double"),
        new Deviation(9, 7, 3, true, "Double")
    ];

    private static readonly Deviation[] TenSplitDeviations =
    [
        new Deviation(20, 5, 5, true, "Split"),
        new Deviation(20, 6, 4, true, "Split")
    ];

    public static bool IsHandWaitingForMove(Game game)
    {
        return game.PlayerTurn &&
               game.Dealer.Hand.Cards.Count >= 2 &&
               game.CurrentHandIndex < game.Player.Hands.Count &&
               game.CurrentHand.Cards.Count >= 2 &&
               !game.CurrentHand.IsFinished;
    }

    public static string GetMoveLine(Game game)
    {
        PlayAdvice advice = GetPlayAdvice(game);

        return
            $"{advice.Move.ToUpper()}: {advice.HandName} " +
            $"vs {advice.DealerName}. {advice.Why}";
    }

    public static string GetCountNote(Game game)
    {
        return GetPlayAdvice(game).CountNote;
    }

    private static PlayAdvice GetPlayAdvice(Game game)
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

        int pairValue = canSplit ? hand.Cards[0].Value : 0;
        bool isTenPair = pairValue == 10;

        Deviation? deviation = isTenPair
            ? TenSplitDeviations.FirstOrDefault(d => d.Dealer == dealerCard)
            : FindHardDeviation(total, isSoft, dealerCard, canDouble);

        // PAIRS

        if (canSplit && !isTenPair && ShouldSplit(pairValue, dealerCard))
        {
            return new PlayAdvice(
                "Split",
                handName,
                dealerName,
                GetSplitWhy(pairValue, total, dealerName, dealerCard),
                "The count doesn't change this play"
            );
        }

        // COUNT DEVIATIONS

        string basicMove =
            GetBasicMove(total, isSoft, dealerCard, canDouble);

        if (
            deviation != null &&
            IsIndexReached(deviation, trueCount) &&
            deviation.Move != basicMove
        )
        {
            string sign = deviation.AtOrAbove ? "≥" : "≤";

            return new PlayAdvice(
                deviation.Move,
                handName,
                dealerName,
                GetDeviationWhy(deviation.Move, total, dealerName),
                $"Basic says {basicMove}, but TC {FormatCount(trueCount)} " +
                $"{sign} {FormatCount(deviation.Index)} → {deviation.Move}"
            );
        }

        // BASIC STRATEGY

        string idealMove =
            GetBasicMove(total, isSoft, dealerCard, true);

        string why = GetBasicWhy(
            total,
            isSoft,
            dealerCard,
            dealerName,
            basicMove,
            idealMove,
            isTenPair
        );

        string countNote = deviation == null
            ? "The count doesn't change this play"
            : $"Count: {deviation.Move.ToLower()} instead at " +
              $"TC {FormatCount(deviation.Index)} or " +
              $"{(deviation.AtOrAbove ? "higher" : "lower")}";

        return new PlayAdvice(basicMove, handName, dealerName, why, countNote);
    }

    private static Deviation? FindHardDeviation(
        int total,
        bool isSoft,
        int dealerCard,
        bool canDouble
    )
    {
        if (isSoft)
        {
            return null;
        }

        return HardDeviations.FirstOrDefault(d =>
            d.Total == total &&
            d.Dealer == dealerCard &&
            (d.Move != "Double" || canDouble)
        );
    }

    private static bool IsIndexReached(Deviation deviation, int trueCount)
    {
        return deviation.AtOrAbove
            ? trueCount >= deviation.Index
            : trueCount <= deviation.Index;
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
            string rank = hand.Cards[0].Rank;

            return rank == "A" ? "Pair of Aces" : $"Pair of {rank}s";
        }

        return isSoft ? $"Soft {total}" : $"Hard {total}";
    }

    // WHY TEXTS

    private static string GetSplitWhy(
        int pairValue,
        int total,
        string dealerName,
        int dealerCard
    )
    {
        if (pairValue == 11)
        {
            return "Two hands that each start on 11";
        }

        if (pairValue == 8)
        {
            return "16 is the worst hand; two 8s do better";
        }

        if (pairValue == 9)
        {
            return "Two hands of 9 beat one 18 here";
        }

        if (dealerCard <= 6)
        {
            return $"Dealer {dealerName} is weak: two hands earn more";
        }

        return $"Two hands beat one weak {total} here";
    }

    private static string GetDeviationWhy(
        string move,
        int total,
        string dealerName
    )
    {
        return move switch
        {
            "Stand" => $"Extra 10s left: hitting {total} busts more",
            "Hit" => $"Few 10s left: hitting {total} is safer",
            "Double" => $"Extra 10s left: doubling {total} pays more",
            _ => $"Extra 10s left: dealer {dealerName} busts more"
        };
    }

    private static string GetBasicWhy(
        int total,
        bool isSoft,
        int dealerCard,
        string dealerName,
        string basicMove,
        string idealMove,
        bool isTenPair
    )
    {
        if (isTenPair)
        {
            return "20 is too strong to break up";
        }

        if (total == 21)
        {
            return "21 can't improve, so always stand";
        }

        if (idealMove == "Double" && basicMove != "Double")
        {
            return $"Double is best, but not allowed: {basicMove.ToLower()}";
        }

        bool dealerWeak = dealerCard >= 2 && dealerCard <= 6;

        // SOFT TOTALS

        if (isSoft)
        {
            if (basicMove == "Double")
            {
                return $"Can't bust, and dealer {dealerName} is weak";
            }

            if (basicMove == "Stand" && total >= 19)
            {
                return $"{total} is strong, a hit rarely helps";
            }

            if (basicMove == "Stand")
            {
                return $"18 holds up well vs a dealer {dealerName}";
            }

            if (total == 18)
            {
                return $"18 is behind vs {dealerName}; a hit can't bust";
            }

            return $"Soft {total} can't bust on one hit";
        }

        // HARD TOTALS

        if (total >= 17)
        {
            return $"Hitting {total} busts too often";
        }

        if (total >= 12 && basicMove == "Stand")
        {
            return $"Dealer {dealerName} busts often, let them draw";
        }

        if (total >= 13)
        {
            return $"{total} rarely wins vs a dealer {dealerName}";
        }

        if (total == 12)
        {
            return "Only a 10 can bust 12, so take a card";
        }

        if (basicMove == "Double")
        {
            return total switch
            {
                11 => "A 10 makes 21, the best double there is",
                10 => "A 10 or ace makes 20-21, so double",
                _ => $"Dealer {dealerName} is weak, so double your 9"
            };
        }

        if (total == 10 || total == 11)
        {
            return $"Dealer {dealerName} is too strong to double";
        }

        if (total == 9 && !dealerWeak)
        {
            return "9 can't bust; doubling doesn't pay here";
        }

        return $"{total} can't bust, so take a card";
    }

    private static bool ShouldSplit(
        int pairValue,
        int dealerCard
    )
    {
        switch (pairValue)
        {
            case 11:
                return true;

            case 10:
                return false;

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
    
    // =========================================================
    // INSURANCE ADVICE
    // =========================================================

    public static bool ShouldTakeInsurance(Game game)
    {
        return GetTrueCount(game) >= InsuranceIndex;
    }

    public static string GetInsuranceLine(Game game)
    {
        string answer = ShouldTakeInsurance(game) ? "YES" : "NO";

        return
            $"INSURANCE {answer}: TC {FormatCount(GetTrueCount(game))}. " +
            $"Hi-Lo takes it at TC {FormatCount(InsuranceIndex)} or more";
    }

    public static string GetInsuranceNote(Game game)
    {
        int tensSeen = game.Shoe.DiscardPile
            .Concat(GetVisibleTableCards(game))
            .Count(card => card.Value == 10);

        int tensInShoe = game.Shoe.Decks.Count * 16;
        int tensLeft = Math.Max(tensInShoe - tensSeen, 0);

        double share = tensLeft / (double)GetUnseenCardCount(game);

        return $"Tens left {share:0%}. Pays 2:1, so it needs 33%+";
    }

    // =========================================================
    // STATS
    // =========================================================

    public static string GetStatsLine(Game game)
    {
        return
            $"RC {FormatCount(GetRunningCount(game))}   " +
            $"TC {FormatCount(GetTrueCount(game))}   " +
            $"Decks {GetDecksRemaining(game):0.0}   " +
            $"Edge ~{FormatOneDecimal(GetEdgePercent(game))}% (est.)   " +
            $"Bet {ConsoleUI.FormatMoney(GetSuggestedBet(game))}";
    }
}
