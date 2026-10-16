using System.Text;

namespace Black_Jack;

class ConsoleUI
{
    private const int TableWidth = 67;
    private const int HandPanelWidth = 26;

    private const int DealerMessageWidth = 45;
    private const int DealerMessageLines = 2;

    private const int CardHeight = 7;
    private const int FullCardWidth = 11;
    private const int CompactCardWidth = 4;
    private const int PlayerAreaHeight = 12;

    private const int RequiredWidth = TableWidth + 1;
    private const int RequiredHeight = 42;

    private const int SizeMessageWidth = 30;

    private const string FeltColor = "\u001b[32m";
    private const string RedSuitColor = "\u001b[31m";
    private const string BlackSuitColor = "\u001b[90m";
    private const string SelectionColor = "\u001b[93m";
    private const string HighlightColor = "\u001b[92m";
    private const string DefaultColor = "\u001b[0m";

    private string dealerMessage = "";

    private readonly List<string> frame = new List<string>();
    private List<string> shownLines = new List<string>();

    private readonly Dictionary<Hand, decimal> shownBets =
        new Dictionary<Hand, decimal>();

    // CHEAT

    private const string SecretCode = "cheat";
    private const string CheatColor = "\u001b[38;5;117m";

    private const int HelpWidth = 55;
    private const int HelpTopRow = 3;

    private bool cheatEnabled;
    private string typedLetters = "";
    private Action? rebuildFrame;

    private bool awaitingMove;
    private bool insuranceQuestion;

    private List<Card> seenCards = new List<Card>();
    private Card? lastCountedCard;
    private bool lastCardWasHoleCard;

    // =========================================================
    // START / STOP
    // =========================================================

    public void Initialize()
    {
        TryResizeWindow();
        Screen.Enter();
    }

    public void Shutdown()
    {
        Screen.Leave();
    }

    // =========================================================
    // MONEY
    // =========================================================

    public static string FormatMoney(decimal amount)
    {
        return amount.ToString("0.##");
    }

    // =========================================================
    // INTRO
    // =========================================================

    public bool ShowIntroScreen()
    {
        string[] logo =
        [
            @" ____  _        _    ____ _  __     _    _    ____ _  __",
            @"| __ )| |      / \  / ___| |/ /    | |  / \  / ___| |/ /",
            @"|  _ \| |     / _ \| |   | ' /  _  | | / _ \| |   | ' / ",
            @"| |_) | |___ / ___ \ |___| . \ | |_| |/ ___ \ |___| . \ ",
            @"|____/|_____/_/   \_\____|_|\_\ \___//_/   \_\____|_|\_\"
        ];

        int logoWidth =
            logo.Max(line => line.Length);

        string[] options =
        [
            "PLAY  [ 1000 CREDITS ]",
            "QUIT"
        ];

        int selectedIndex = 0;

        rebuildFrame = null;

        FlushInput();

        while (true)
        {
            StartFrame();

            foreach (string line in logo)
            {
                AddLine(
                    FeltColor +
                    CenterText(line.PadRight(logoWidth), TableWidth) +
                    DefaultColor
                );
            }

            AddLine();
            AddLine(CenterText("♠  ♥  ♦  ♣", TableWidth));
            AddLine();

            for (int i = 0; i < options.Length; i++)
            {
                if (i == selectedIndex)
                {
                    AddLine(
                        HighlightColor +
                        CenterText($"> {options[i]} <", TableWidth) +
                        DefaultColor
                    );
                }
                else
                {
                    AddLine(CenterText(options[i], TableWidth));
                }
            }

            PresentFrame();

            ConsoleKey key = ReadKey();

            if (key == ConsoleKey.UpArrow)
            {
                selectedIndex--;

                if (selectedIndex < 0)
                {
                    selectedIndex = options.Length - 1;
                }
            }
            else if (key == ConsoleKey.DownArrow)
            {
                selectedIndex++;

                if (selectedIndex >= options.Length)
                {
                    selectedIndex = 0;
                }
            }
            else if (key == ConsoleKey.Enter)
            {
                return selectedIndex == 0;
            }
        }
    }

    // =========================================================
    // BETTING
    // =========================================================

    public void ShowBettingScreen(Game game)
    {
        rebuildFrame = () => BuildBettingScreen(game);

        BuildBettingScreen(game);

        PresentFrame();
    }

    private void BuildBettingScreen(Game game)
    {
        StartFrame();

        ShowTableArea(game, true);

        AddFeltLine('=');

        if (cheatEnabled)
        {
            AddCheatLine(CardCounter.GetBetLine(game));
        }
        else
        {
            AddLine(CenterText("PLAYER", TableWidth));
        }

        AddFeltLine('─');

        AddLine(
            CenterText(
                $"CREDITS {FormatMoney(game.Player.Credits)}",
                TableWidth
            )
        );

        AddFeltLine('─');
    }

    public string GetBettingAction(Player player)
    {
        string[] options =
        [
            "PLACE BET",
            "BUY CHIPS",
            "MAIN MENU"
        ];

        int selectedIndex = 0;

        FlushInput();

        while (true)
        {
            List<string> parts = new List<string>();

            for (int i = 0; i < options.Length; i++)
            {
                if (i == selectedIndex)
                {
                    parts.Add($"> {options[i]} <");
                }
                else
                {
                    parts.Add(options[i]);
                }
            }

            string line =
                string.Join("     ", parts);

            PresentWithSelection(line);

            ConsoleKey key = ReadKey();

            if (key == ConsoleKey.LeftArrow)
            {
                selectedIndex--;

                if (selectedIndex < 0)
                {
                    selectedIndex =
                        options.Length - 1;
                }
            }
            else if (key == ConsoleKey.RightArrow)
            {
                selectedIndex++;

                if (selectedIndex >= options.Length)
                {
                    selectedIndex = 0;
                }
            }
            else if (key == ConsoleKey.Enter)
            {
                return selectedIndex switch
                {
                    0 => "bet",
                    1 => "buy",
                    2 => "menu",
                    _ => ""
                };
            }
        }
    }

    public string? ReadBuyChipsInput()
    {
        const string prompt = "Buy chips: ";
        const int inputSpace = 10;

        string promptLine =
            new string(
                ' ',
                (TableWidth - prompt.Length - inputSpace) / 2
            ) +
            prompt;

        List<string> lines = new List<string>(frame)
        {
            "",
            promptLine
        };

        Show(lines);

        FlushInput();

        Screen.ShowCursorAt(
            lines.Count,
            promptLine.Length + 1
        );

        string? input = Console.ReadLine();

        Screen.HideCursor();
        Screen.Invalidate();

        return input;
    }

    public decimal? SelectBet(Game game)
    {
        decimal[] chipValues =
        [
            2.5m,
            5m,
            10m,
            25m,
            50m,
            100m,
            500m
        ];

        int selectedIndex = 0;
        decimal bet = 0;

        rebuildFrame = null;

        FlushInput();

        while (true)
        {
            StartFrame();

            ShowTableArea(game, true);

            AddFeltLine('=');

            if (cheatEnabled)
            {
                AddCheatLine(CardCounter.GetBetLine(game));
            }
            else
            {
                AddLine(CenterText("PLACE BET", TableWidth));
            }

            AddFeltLine('─');

            AddLine(
                CenterText(
                    $"Credits: {FormatMoney(game.Player.Credits)}   |   " +
                    $"Current Bet: {FormatMoney(bet)}",
                    TableWidth
                )
            );

            AddFeltLine('─');

            AddLine(
                ColorSelection(
                    CenterText(
                        BuildChipLine(chipValues, selectedIndex),
                        TableWidth
                    )
                )
            );

            AddFeltLine('─');

            AddLine(
                CenterText(
                    "SPACE Add   BACKSPACE Reset   ENTER Confirm   ESC Back",
                    TableWidth
                )
            );

            PresentFrame();

            ConsoleKey key = ReadKey();

            if (key == ConsoleKey.LeftArrow)
            {
                selectedIndex--;

                if (selectedIndex < 0)
                {
                    selectedIndex =
                        chipValues.Length - 1;
                }
            }
            else if (key == ConsoleKey.RightArrow)
            {
                selectedIndex++;

                if (selectedIndex >= chipValues.Length)
                {
                    selectedIndex = 0;
                }
            }
            else if (key == ConsoleKey.Spacebar)
            {
                decimal chip =
                    chipValues[selectedIndex];

                if (bet + chip <= game.Player.Credits)
                {
                    bet += chip;
                }
            }
            else if (key == ConsoleKey.Backspace)
            {
                bet = 0;
            }
            else if (key == ConsoleKey.Enter)
            {
                if (bet > 0)
                {
                    return bet;
                }
            }
            else if (key == ConsoleKey.Escape)
            {
                return null;
            }
        }
    }

    private string BuildChipLine(
        decimal[] chipValues,
        int selectedIndex
    )
    {
        List<string> chips = new List<string>();

        for (int i = 0; i < chipValues.Length; i++)
        {
            string chip = FormatMoney(chipValues[i]);

            if (i == selectedIndex)
            {
                chips.Add($"> [{chip}] <");
            }
            else
            {
                chips.Add($"[{chip}]");
            }
        }

        return string.Join("   ", chips);
    }

    // =========================================================
    // BOARD
    // =========================================================

    public void ShowBoard(Game game)
    {
        rebuildFrame = () => BuildBoard(game);

        BuildBoard(game);

        PresentFrame();
    }

    private void BuildBoard(Game game)
    {
        StartFrame();

        TrackCountedCards(game);

        ShowTableArea(game, false);

        ShowPlayerStatus(game);
    }

    private void ShowTableArea(Game game, bool betting)
    {
        // DEALER

        AddFeltLine('=');

        if (cheatEnabled)
        {
            AddCheatLine(CardCounter.GetStatsLine(game));
        }
        else
        {
            AddLine(
                CenterText(
                    "DEALER",
                    TableWidth
                )
            );
        }

        AddFeltLine('=');

        AddLine();

        if (game.Dealer.Hand.Cards.Count > 0)
        {
            if (game.HideDealerHoleCard)
            {
                AddLine(
                    CenterText(
                        "[ ? ]",
                        TableWidth
                    )
                );
            }
            else
            {
                AddLine(
                    CenterText(
                        $"[ {game.Dealer.Hand.GetValue()} ]",
                        TableWidth
                    )
                );
            }
        }
        else
        {
            // Reserve the dealer total row.
            AddLine();
        }

        ShowDealerCards(game);

        AddLine();

        AddFeltLine('─');

        AddLine();

        // PLAYER CARDS

        ShowPlayerHands(game);

        AddLine();

        // COMMENT AREA

        ShowDealerMessage(
            cheatEnabled ? GetCheatNote(game, betting) : ""
        );

        AddLine();
    }

    // =========================================================
    // MESSAGE / COMMENT AREA
    // =========================================================

    public void SetDealerMessage(string message)
    {
        dealerMessage = message;
    }

    public void ClearDealerMessage()
    {
        dealerMessage = "";
    }

    private void ShowDealerMessage(string cheatNote)
    {
        string[] messageLines =
            WrapDealerMessage(dealerMessage);

        bool showCheatNote =
            cheatNote != "" &&
            messageLines[DealerMessageLines - 1] == "";

        string top =
            "┌" +
            new string('─', DealerMessageWidth) +
            "┐";

        string bottom =
            "└" +
            new string('─', DealerMessageWidth) +
            "┘";

        AddLine(
            CenterText(
                top,
                TableWidth
            )
        );

        for (int i = 0; i < DealerMessageLines; i++)
        {
            if (showCheatNote && i == DealerMessageLines - 1)
            {
                AddLine(
                    new string(
                        ' ',
                        (TableWidth - (DealerMessageWidth + 2)) / 2
                    ) +
                    "│" +
                    CheatColor +
                    Fit(cheatNote, DealerMessageWidth) +
                    DefaultColor +
                    "│"
                );

                continue;
            }

            string middle =
                "│" +
                CenterText(
                    messageLines[i],
                    DealerMessageWidth
                ) +
                "│";

            AddLine(
                CenterText(
                    middle,
                    TableWidth
                )
            );
        }

        AddLine(
            CenterText(
                bottom,
                TableWidth
            )
        );
    }

    private string[] WrapDealerMessage(string message)
    {
        string[] result =
            new string[DealerMessageLines];

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = "";
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return result;
        }

        List<string> lines =
            new List<string>();

        string currentLine = "";

        foreach (string word in message.Split(' '))
        {
            if (word.Length > DealerMessageWidth)
            {
                if (!string.IsNullOrEmpty(currentLine))
                {
                    lines.Add(currentLine);
                    currentLine = "";
                }

                lines.Add(
                    word[..DealerMessageWidth]
                );

                continue;
            }

            string candidate =
                string.IsNullOrEmpty(currentLine)
                    ? word
                    : currentLine + " " + word;

            if (candidate.Length <= DealerMessageWidth)
            {
                currentLine = candidate;
            }
            else
            {
                lines.Add(currentLine);
                currentLine = word;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
        {
            lines.Add(currentLine);
        }

        for (
            int i = 0;
            i < DealerMessageLines &&
            i < lines.Count;
            i++
        )
        {
            result[i] = lines[i];
        }

        if (lines.Count > DealerMessageLines)
        {
            string last =
                result[DealerMessageLines - 1];

            if (last.Length > DealerMessageWidth - 3)
            {
                last =
                    last[..(DealerMessageWidth - 3)];
            }

            result[DealerMessageLines - 1] =
                last + "...";
        }

        return result;
    }

    // =========================================================
    // INSURANCE
    // =========================================================

    public bool AskInsurance()
    {
        int selectedIndex = 1;

        SetCheatState(ref insuranceQuestion, true);

        FlushInput();

        try
        {
            while (true)
            {
                string line =
                    selectedIndex == 0
                        ? "> YES <     NO"
                        : "YES     > NO <";

                PresentWithSelection(line);

                ConsoleKey key = ReadKey();

                if (
                    key == ConsoleKey.LeftArrow ||
                    key == ConsoleKey.RightArrow
                )
                {
                    selectedIndex =
                        selectedIndex == 0
                            ? 1
                            : 0;
                }
                else if (key == ConsoleKey.Enter)
                {
                    return selectedIndex == 0;
                }
            }
        }
        finally
        {
            insuranceQuestion = false;
        }
    }

    // =========================================================
    // PLAYER ACTIONS
    // =========================================================

    public string GetPlayerAction(Game game)
    {
        List<(string Label, string Value)> options =
        [
            ("Hit", "hit"),
            ("Stand", "stand")
        ];

        if (game.Player.CanDoubleDown(game.CurrentHand))
        {
            options.Add(
                ("Double", "double")
            );
        }

        if (game.CanSplit(game.CurrentHand))
        {
            options.Add(
                ("Split", "split")
            );
        }

        int selectedIndex = 0;

        SetCheatState(ref awaitingMove, true);

        FlushInput();

        try
        {
            while (true)
            {
                List<string> parts =
                    new List<string>();

                for (int i = 0; i < options.Count; i++)
                {
                    if (i == selectedIndex)
                    {
                        parts.Add(
                            $"> {options[i].Label} <"
                        );
                    }
                    else
                    {
                        parts.Add(
                            options[i].Label
                        );
                    }
                }

                string line =
                    string.Join(
                        "     ",
                        parts
                    );

                PresentWithSelection(line);

                ConsoleKey key = ReadKey();

                if (key == ConsoleKey.LeftArrow)
                {
                    selectedIndex--;

                    if (selectedIndex < 0)
                    {
                        selectedIndex =
                            options.Count - 1;
                    }
                }
                else if (key == ConsoleKey.RightArrow)
                {
                    selectedIndex++;

                    if (selectedIndex >= options.Count)
                    {
                        selectedIndex = 0;
                    }
                }
                else if (key == ConsoleKey.Enter)
                {
                    return options[selectedIndex].Value;
                }
            }
        }
        finally
        {
            awaitingMove = false;
        }
    }

    // =========================================================
    // PLAYER HANDS
    // =========================================================

    private void ShowPlayerHands(Game game)
    {
        ForgetOldBets(game);

        int handCount = game.Player.Hands.Count;

        if (handCount == 0)
        {
            for (
                int i = 0;
                i < PlayerAreaHeight;
                i++
            )
            {
                AddLine();
            }

            return;
        }

        int gap =
            handCount <= 2 ? 4 :
            handCount == 3 ? 2 :
            1;

        int panelWidth =
            Math.Min(
                HandPanelWidth,
                (TableWidth - ((handCount - 1) * gap)) / handCount
            );

        int totalWidth =
            (handCount * panelWidth) +
            ((handCount - 1) * gap);

        int leftPadding =
            Math.Max(
                0,
                (TableWidth - totalWidth) / 2
            );

        List<string[]> renderedHands =
            new List<string[]>();

        for (int i = 0; i < handCount; i++)
        {
            bool isCurrent =
                game.PlayerTurn &&
                i == game.CurrentHandIndex;

            renderedHands.Add(
                RenderHand(
                    game.Player.Hands[i],
                    i + 1,
                    isCurrent,
                    panelWidth
                )
            );
        }

        for (int row = 0; row < PlayerAreaHeight; row++)
        {
            string line =
                new string(' ', leftPadding);

            for (
                int handIndex = 0;
                handIndex < renderedHands.Count;
                handIndex++
            )
            {
                line += renderedHands[handIndex][row];

                if (handIndex < renderedHands.Count - 1)
                {
                    line += new string(' ', gap);
                }
            }

            AddLine(ColorSuits(line));
        }
    }

    private string[] RenderHand(
        Hand hand,
        int handNumber,
        bool isCurrent,
        int width
    )
    {
        List<string> lines =
            new List<string>();

        if (isCurrent)
        {
            lines.Add(
                Fit(
                    $"▶ HAND {handNumber} ◀",
                    width
                )
            );
        }
        else
        {
            lines.Add(
                Fit(
                    $"HAND {handNumber}",
                    width
                )
            );
        }

        lines.Add(
            Fit(
                $"[ Bet {FormatMoney(GetShownBet(hand))} ]",
                width
            )
        );

        lines.Add(Fit("", width));

        lines.AddRange(
            RenderCards(
                hand.Cards,
                -1,
                width
            )
        );

        lines.Add(
            Fit(
                $"[ {hand.GetValue()} ]",
                width
            )
        );

        if (hand.IsBusted)
        {
            lines.Add(Fit("BUST", width));
        }
        else if (hand.IsStanding)
        {
            lines.Add(Fit("STAND", width));
        }
        else
        {
            lines.Add(Fit("", width));
        }

        return lines.ToArray();
    }

    // =========================================================
    // BET DISPLAY
    // =========================================================

    private decimal GetShownBet(Hand hand)
    {
        if (hand.Bet > 0)
        {
            shownBets[hand] = hand.Bet;

            return hand.Bet;
        }

        return shownBets.TryGetValue(hand, out decimal bet)
            ? bet
            : 0;
    }

    private void ForgetOldBets(Game game)
    {
        List<Hand> oldHands =
            shownBets.Keys
                .Where(hand => !game.Player.Hands.Contains(hand))
                .ToList();

        foreach (Hand hand in oldHands)
        {
            shownBets.Remove(hand);
        }
    }

    // =========================================================
    // DEALER CARDS
    // =========================================================

    private void ShowDealerCards(Game game)
    {
        if (game.Dealer.Hand.Cards.Count == 0)
        {
            for (
                int row = 0;
                row < CardHeight;
                row++
            )
            {
                AddLine();
            }

            return;
        }

        int hiddenIndex =
            game.HideDealerHoleCard ? 1 : -1;

        string[] rows =
            RenderCards(
                game.Dealer.Hand.Cards,
                hiddenIndex,
                TableWidth
            );

        foreach (string row in rows)
        {
            AddLine(ColorSuits(row));
        }
    }

    // =========================================================
    // CARD LAYOUT
    // =========================================================

    private string[] RenderCards(
        List<Card> cards,
        int hiddenIndex,
        int width
    )
    {
        string[] rows = new string[CardHeight];

        int count = cards.Count;
        int firstShown = 0;

        bool fullLastCard =
            FullCardWidth + (CompactCardWidth * (count - 1)) <= width;

        if (!fullLastCard)
        {
            int maxStrips =
                (width - 1) / CompactCardWidth;

            if (count > maxStrips)
            {
                firstShown =
                    count - Math.Max(1, maxStrips - 1);
            }
        }

        for (int row = 0; row < CardHeight; row++)
        {
            string line = "";

            if (firstShown > 0)
            {
                line += GetMoreCardsRow(firstShown, row);
            }

            for (int i = firstShown; i < count; i++)
            {
                bool isHidden = i == hiddenIndex;
                bool isLastCard = i == count - 1;

                if (isLastCard && fullLastCard)
                {
                    line +=
                        isHidden
                            ? GetHiddenCardRow(row)
                            : GetFullCardRow(cards[i], row);
                }
                else
                {
                    line +=
                        isHidden
                            ? GetHiddenCompactRow(row)
                            : GetCompactCardRow(cards[i], row);
                }
            }

            if (!fullLastCard)
            {
                line += GetCardEdgeRow(row);
            }

            rows[row] = Fit(line, width);
        }

        return rows;
    }

    // =========================================================
    // CARD GRAPHICS
    // =========================================================

    private string GetFullCardRow(
        Card card,
        int row
    )
    {
        string suit =
            GetSuitSymbol(card.Suit);

        string top =
            $"{card.Rank}{suit}"
                .PadRight(9);

        string bottom =
            $"{suit}{card.Rank}"
                .PadLeft(9);

        return row switch
        {
            0 => "┌─────────┐",
            1 => $"│{top}│",
            2 => "│         │",
            3 => $"│    {suit}    │",
            4 => "│         │",
            5 => $"│{bottom}│",
            6 => "└─────────┘",
            _ => ""
        };
    }

    private string GetCompactCardRow(
        Card card,
        int row
    )
    {
        string suit =
            GetSuitSymbol(card.Suit);

        string label =
            $"{card.Rank}{suit}";

        return row switch
        {
            0 => "┌───",
            1 => $"│{label.PadRight(3)}",
            2 => "│   ",
            3 => "│   ",
            4 => "│   ",
            5 => "│   ",
            6 => "└───",
            _ => ""
        };
    }

    private string GetHiddenCardRow(int row)
    {
        return row switch
        {
            0 => "┌─────────┐",
            1 => "│  /\\_/\\  │",
            2 => "│ ( o.o ) │",
            3 => "│  > ^ <  │",
            4 => "│ ( o.o ) │",
            5 => "│  \\/_\\/  │",
            6 => "└─────────┘",
            _ => ""
        };
    }

    private string GetHiddenCompactRow(int row)
    {
        return row switch
        {
            0 => "┌───",
            6 => "└───",
            _ => "│░░░"
        };
    }

    private string GetMoreCardsRow(
        int hiddenCount,
        int row
    )
    {
        string label =
            $"+{hiddenCount}";

        return row switch
        {
            0 => "┌───",
            1 => $"│{label.PadRight(3)}",
            6 => "└───",
            _ => "│   "
        };
    }

    private string GetCardEdgeRow(int row)
    {
        return row switch
        {
            0 => "┐",
            6 => "┘",
            _ => "│"
        };
    }

    // =========================================================
    // LOWER PLAYER PANEL
    // =========================================================

    private void ShowPlayerStatus(Game game)
    {
        AddFeltLine('=');

        if (cheatEnabled)
        {
            AddCheatLine(GetCheatAdvice(game));
        }
        else
        {
            AddLine(
                CenterText(
                    "PLAYER",
                    TableWidth
                )
            );
        }

        AddFeltLine('─');

        if (game.Player.Hands.Count > 0)
        {
            int handIndex =
                game.CurrentHandIndex;

            if (
                handIndex >=
                game.Player.Hands.Count
            )
            {
                handIndex =
                    game.Player.Hands.Count - 1;
            }

            Hand hand =
                game.Player.Hands[handIndex];

            string statusLine =
                $"HAND {handIndex + 1}  |  " +
                $"SUM {hand.GetValue()}  |  " +
                $"BET {FormatMoney(GetShownBet(hand))}  |  " +
                $"CREDITS {FormatMoney(game.Player.Credits)}";

            AddLine(
                CenterText(
                    statusLine,
                    TableWidth
                )
            );
        }
        else
        {
            AddLine(
                CenterText(
                    $"CREDITS {FormatMoney(game.Player.Credits)}",
                    TableWidth
                )
            );
        }

        AddFeltLine('─');
    }

    // =========================================================
    // CHEAT
    // =========================================================

    private void SetCheatState(ref bool state, bool value)
    {
        state = value;

        if (cheatEnabled)
        {
            rebuildFrame?.Invoke();
        }
    }

    private void AddCheatLine(string text)
    {
        AddLine(CheatColor + Fit(text, TableWidth) + DefaultColor);
    }

    private string GetCheatAdvice(Game game)
    {
        if (insuranceQuestion)
        {
            return CardCounter.GetInsuranceLine(game);
        }

        if (CardCounter.IsHandWaitingForMove(game))
        {
            return CardCounter.GetMoveLine(game);
        }

        return CardCounter.GetTrueCountMath(game, true);
    }

    private string GetCheatNote(Game game, bool betting)
    {
        if (betting)
        {
            return CardCounter.GetBetNote(game);
        }

        if (insuranceQuestion)
        {
            return CardCounter.GetInsuranceNote(game);
        }

        if (awaitingMove && CardCounter.IsHandWaitingForMove(game))
        {
            return CardCounter.GetCountNote(game);
        }

        if (lastCountedCard == null)
        {
            return "";
        }

        string cardLabel =
            lastCountedCard.Rank + GetSuitSymbol(lastCountedCard.Suit);

        return CardCounter.GetCardNote(
            cardLabel,
            lastCountedCard,
            lastCardWasHoleCard,
            game
        );
    }

    private void TrackCountedCards(Game game)
    {
        List<Card> visible = CardCounter.GetVisibleTableCards(game);

        if (visible.Count == 0)
        {
            seenCards.Clear();
            lastCountedCard = null;

            return;
        }

        bool newRound = seenCards.Any(card => !visible.Contains(card));

        if (newRound)
        {
            seenCards.Clear();
        }

        Card? newest =
            visible.LastOrDefault(card => !seenCards.Contains(card));

        if (newest != null)
        {
            lastCountedCard = newest;
            lastCardWasHoleCard = game.Dealer.Hand.Cards.IndexOf(newest) == 1;
        }

        seenCards = visible;
    }

    private void ShowCheatHelp()
    {
        string[] text =
        [
            "",
            "Every card you see changes the running count:",
            "  2 3 4 5 6 = +1    7 8 9 = 0    10 J Q K A = -1",
            "",
            "RC     Running count: all seen cards added up",
            "TC     True count: RC ÷ decks left, rounded down",
            "Decks  Cards not seen yet ÷ 52",
            $"Edge   Your edge, estimated: {-0.4:0.0}% + {0.5:0.0}% per TC",
            $"Bet    In credits. 1 unit = {0.5:0.0}% of your credits,",
            "       1-8 units as the TC goes up",
            "",
            "High TC = more 10s and aces left: more blackjacks",
            "(paid 3:2), better doubles, more dealer busts.",
            "Low TC = the house has the edge, so bet small.",
            "",
            "PLAYER row: the best move and why. It uses basic",
            "strategy, and the count changes some plays.",
            "Box: the last card's count, or the count math.",
            "The hole card counts when the dealer flips it.",
            "",
            "F1 = show this again    cheat + Enter = turn off",
            "",
            "Press any key to continue"
        ];

        const string title = " CARD COUNTING (HI-LO) ";

        int innerWidth = HelpWidth - 2;
        int titleLeft = (innerWidth - title.Length) / 2;

        List<string> box =
        [
            "┌" +
            new string('─', titleLeft) +
            title +
            new string('─', innerWidth - titleLeft - title.Length) +
            "┐"
        ];

        foreach (string line in text)
        {
            string inner = line.StartsWith("Press")
                ? CenterText(line, innerWidth)
                : (" " + line).PadRight(innerWidth);

            box.Add("│" + inner + "│");
        }

        box.Add("└" + new string('─', innerWidth) + "┘");

        List<string> lines = new List<string>(frame);

        while (lines.Count < HelpTopRow + box.Count)
        {
            lines.Add("");
        }

        for (int i = 0; i < box.Count; i++)
        {
            lines[HelpTopRow + i] =
                CheatColor + CenterText(box[i], TableWidth) + DefaultColor;
        }

        Show(lines);

        WaitForAnyKey();
    }

    private void WaitForAnyKey()
    {
        FlushInput();

        while (!Console.KeyAvailable)
        {
            if (Screen.HasWindowResized())
            {
                EnsureWindowSize();
                Screen.Present(shownLines, TableWidth);
            }

            Thread.Sleep(30);
        }

        Console.ReadKey(true);
    }

    // =========================================================
    // COLORS / SYMBOLS
    // =========================================================

    private string GetSuitSymbol(string suit)
    {
        return suit switch
        {
            "Hearts" => "♥",
            "Diamonds" => "♦",
            "Clubs" => "♣",
            "Spades" => "♠",
            _ => "?"
        };
    }

    private string ColorSuits(string text)
    {
        StringBuilder result = new StringBuilder();
        string currentColor = DefaultColor;

        foreach (char character in text)
        {
            string color = character switch
            {
                '♥' or '♦' => RedSuitColor,
                '♣' or '♠' => BlackSuitColor,
                _ => DefaultColor
            };

            if (color != currentColor)
            {
                result.Append(color);
                currentColor = color;
            }

            result.Append(character);
        }

        if (currentColor != DefaultColor)
        {
            result.Append(DefaultColor);
        }

        return result.ToString();
    }

    private string ColorSelection(string text)
    {
        StringBuilder result = new StringBuilder();
        string currentColor = DefaultColor;

        foreach (char character in text)
        {
            string color =
                character == '>' || character == '<'
                    ? SelectionColor
                    : DefaultColor;

            if (color != currentColor)
            {
                result.Append(color);
                currentColor = color;
            }

            result.Append(character);
        }

        if (currentColor != DefaultColor)
        {
            result.Append(DefaultColor);
        }

        return result.ToString();
    }

    // =========================================================
    // FRAME
    // =========================================================

    private void StartFrame()
    {
        EnsureWindowSize();
        frame.Clear();
    }

    private void AddLine(string text = "")
    {
        frame.Add(text);
    }

    private void AddFeltLine(
        char character = '='
    )
    {
        AddLine(
            FeltColor +
            new string(character, TableWidth) +
            DefaultColor
        );
    }

    private void PresentFrame()
    {
        Show(frame);
    }

    private void PresentWithSelection(string text)
    {
        List<string> lines = new List<string>(frame)
        {
            ColorSelection(
                CenterText(text, TableWidth)
            )
        };

        Show(lines);
    }

    private void Show(List<string> lines)
    {
        shownLines = new List<string>(lines);

        Screen.Present(shownLines, TableWidth);
    }

    // =========================================================
    // INPUT
    // =========================================================

    private ConsoleKey ReadKey()
    {
        while (true)
        {
            while (!Console.KeyAvailable)
            {
                if (Screen.HasWindowResized())
                {
                    EnsureWindowSize();
                    Screen.Present(shownLines, TableWidth);
                }

                Thread.Sleep(30);
            }

            ConsoleKeyInfo keyInfo = Console.ReadKey(true);

            // SECRET CODE

            if (char.IsLetter(keyInfo.KeyChar))
            {
                typedLetters += char.ToLower(keyInfo.KeyChar);

                if (typedLetters.Length > SecretCode.Length)
                {
                    typedLetters =
                        typedLetters[^SecretCode.Length..];
                }

                continue;
            }

            if (
                keyInfo.Key == ConsoleKey.Enter &&
                typedLetters == SecretCode
            )
            {
                typedLetters = "";
                cheatEnabled = !cheatEnabled;

                rebuildFrame?.Invoke();

                if (cheatEnabled)
                {
                    ShowCheatHelp();
                }

                return ConsoleKey.NoName;
            }

            if (keyInfo.Key == ConsoleKey.F1 && cheatEnabled)
            {
                typedLetters = "";

                ShowCheatHelp();

                return ConsoleKey.NoName;
            }

            typedLetters = "";

            return keyInfo.Key;
        }
    }

    private static void FlushInput()
    {
        while (Console.KeyAvailable)
        {
            Console.ReadKey(true);
        }
    }

    // =========================================================
    // WINDOW SIZE
    // =========================================================

    private static bool WindowIsLargeEnough()
    {
        return
            Console.WindowWidth >= RequiredWidth &&
            Console.WindowHeight >= RequiredHeight;
    }

    private static void TryResizeWindow()
    {
        if (WindowIsLargeEnough() || !OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            Console.SetBufferSize(
                Math.Max(Console.BufferWidth, RequiredWidth),
                Math.Max(Console.BufferHeight, RequiredHeight)
            );

            Console.SetWindowSize(
                Math.Max(Console.WindowWidth, RequiredWidth),
                Math.Max(Console.WindowHeight, RequiredHeight)
            );
        }
        catch (Exception)
        {
        }
    }

    private void EnsureWindowSize()
    {
        if (WindowIsLargeEnough())
        {
            return;
        }

        TryResizeWindow();

        while (!WindowIsLargeEnough())
        {
            string[] message =
            [
                "Window too small.",
                "",
                $"Required: {RequiredWidth} x {RequiredHeight}",
                $"Current:  {Console.WindowWidth} x {Console.WindowHeight}",
                "",
                "Resize the window to continue."
            ];

            Screen.Present(message, SizeMessageWidth);

            FlushInput();

            Thread.Sleep(100);
        }

        Screen.Invalidate();
    }

    // =========================================================
    // TEXT
    // =========================================================

    private string CenterText(
        string text,
        int width
    )
    {
        if (text.Length >= width)
        {
            return text;
        }

        int leftPadding =
            (width - text.Length) / 2;

        int rightPadding =
            width -
            text.Length -
            leftPadding;

        return
            new string(' ', leftPadding) +
            text +
            new string(' ', rightPadding);
    }

    private string Fit(
        string text,
        int width
    )
    {
        string centered = CenterText(text, width);

        return centered.Length > width
            ? centered[..width]
            : centered;
    }
}
