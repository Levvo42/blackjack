using System.Text;
using Black_Jack;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

ConsoleUI ui = new ConsoleUI();

ui.Initialize();

Console.CancelKeyPress += (sender, e) => ui.Shutdown();

bool applicationRunning = true;

// -------------------------
// TIMING
// -------------------------

const int DealDelay = 900;
const int ActionDelay = 1100;
const int SplitDelay = 750;
const int DoubleDelay = 1100;
const int DealerDelay = 1400;
const int ShortMessageDelay = 800;
const int MessageDelay = 1800;
const int ResultDelay = 5000;

while (applicationRunning)
{
    // -------------------------
    // INTRO SCREEN
    // -------------------------

    if (!ui.ShowIntroScreen())
    {
        break;
    }

    Game game = new Game();

    game.Shoe.CreateShoe();
    game.Shoe.Shuffle();

    bool gameRunning = true;

    decimal? RunBettingPhase(Game game)
    {
        while (true)
        {
            ui.ShowBettingScreen(game);

            string action =
                ui.GetBettingAction(game.Player);

            if (action == "menu")
            {
                return null;
            }

            if (action == "buy")
            {
                if (
                    decimal.TryParse(
                        ui.ReadBuyChipsInput(),
                        out decimal amount
                    )
                )
                {
                    game.Player.BuyChips(amount);
                }

                continue;
            }

            if (action == "bet")
            {
                decimal? betAmount =
                    ui.SelectBet(game);

                if (betAmount == null)
                {
                    continue;
                }

                return betAmount.Value;
            }
        }
    }
    bool finishedRoundOnTable = false;
    while (gameRunning)
    {
        decimal? betAmount =
            RunBettingPhase(game);

        if (betAmount == null)
        {
            gameRunning = false;
            continue;
        }

        // Previous round stays visible until the
        // player has chosen the next bet.
        if (finishedRoundOnTable)
        {
            game.RoundEnd();
            finishedRoundOnTable = false;
        }

        // NOW the new round actually begins.
        game.RoundStart();

        ui.ClearDealerMessage();

        game.Player.PlaceBet(
            game.Player.Hands[0],
            betAmount.Value
        );

        // INITIAL DEAL continues here...

        // -------------------------
        // INITIAL DEAL
        // -------------------------

        ui.SetDealerMessage("Dealing...");

        game.DealCardToPlayer(game.Player.Hands[0]);
        ui.ShowBoard(game);
        Thread.Sleep(DealDelay);

        game.DealCardToDealer();
        ui.ShowBoard(game);
        Thread.Sleep(DealDelay);

        game.DealCardToPlayer(game.Player.Hands[0]);
        ui.ShowBoard(game);
        Thread.Sleep(DealDelay);

        game.DealCardToDealer();
        ui.ShowBoard(game);
        Thread.Sleep(DealDelay);

        ui.ClearDealerMessage();
        ui.ShowBoard(game);

        // -------------------------
        // INSURANCE
        // -------------------------

        if (game.CanTakeInsurance(game.Player.Hands[0]))
        {
            decimal insuranceAmount =
                game.Player.Hands[0].Bet / 2;

            ui.SetDealerMessage(
                $"Dealer shows an Ace. Insurance for {ConsoleUI.FormatMoney(insuranceAmount)}?"
            );

            ui.ShowBoard(game);

            if (ui.AskInsurance())
            {
                game.TakeInsurance(game.Player.Hands[0]);

                ui.SetDealerMessage(
                    $"Insurance placed: {ConsoleUI.FormatMoney(insuranceAmount)}."
                );

                ui.ShowBoard(game);
                Thread.Sleep(MessageDelay);

                bool insuranceWon =
                    game.ResolveInsurance(game.Player.Hands[0]);

                if (insuranceWon)
                {
                    ui.SetDealerMessage("Insurance pays 2 to 1.");
                }
                else
                {
                    ui.SetDealerMessage("No Blackjack. Insurance lost.");
                }

                ui.ShowBoard(game);
                Thread.Sleep(MessageDelay);
            }
            else
            {
                ui.SetDealerMessage("No insurance.");

                ui.ShowBoard(game);
                Thread.Sleep(ShortMessageDelay);
            }
        }

        // -------------------------
        // INITIAL BLACKJACK
        // -------------------------

        RoundResult? blackjackResult =
            game.GetInitialBlackjackResult();

        if (blackjackResult.HasValue)
        {
            game.EndPlayerTurn();
            game.RevealDealerHoleCard();

            game.Payout(
                blackjackResult.Value,
                game.Player.Hands[0]
            );

            if (blackjackResult.Value == RoundResult.Blackjack)
            {
                ui.SetDealerMessage("Blackjack. You win!");
            }
            else if (blackjackResult.Value == RoundResult.Push)
            {
                ui.SetDealerMessage("Blackjack. Push.");
            }
            else
            {
                ui.SetDealerMessage("Dealer Blackjack.");
            }

            ui.ShowBoard(game);

            Thread.Sleep(ResultDelay);

            finishedRoundOnTable = true;
            continue;
        }

        // -------------------------
        // PLAYER HANDS
        // -------------------------

        while (game.PlayerTurn)
        {
            string input = ui.GetPlayerAction(game);

            if (input == "hit")
            {
                int handNumber = game.CurrentHandIndex + 1;

                ui.SetDealerMessage($"Hand {handNumber} hits.");

                game.HitPlayer(game.CurrentHand);

                ui.ShowBoard(game);
                Thread.Sleep(ActionDelay);

                if (game.CurrentHand.IsBusted)
                {
                    ui.SetDealerMessage($"Hand {handNumber} busts.");

                    ui.ShowBoard(game);
                    Thread.Sleep(MessageDelay);

                    if (!game.MoveToNextHand())
                    {
                        game.EndPlayerTurn();
                    }
                    else
                    {
                        ui.ShowBoard(game);
                    }
                }
            }
            else if (input == "split")
            {
                Hand splitHand = game.CurrentHand;
                int handNumber = game.CurrentHandIndex + 1;

                if (game.Split(splitHand))
                {
                    ui.SetDealerMessage($"Hand {handNumber} splits.");

                    // Show the two separated one-card hands
                    ui.ShowBoard(game);
                    Thread.Sleep(SplitDelay);

                    // Deal replacement card to original hand
                    game.DealCardToPlayer(splitHand);

                    ui.ShowBoard(game);
                    Thread.Sleep(SplitDelay);

                    // Deal replacement card to new hand
                    Hand newHand =
                        game.Player.Hands[
                            game.CurrentHandIndex + 1
                        ];

                    game.DealCardToPlayer(newHand);

                    ui.ShowBoard(game);
                    Thread.Sleep(SplitDelay);
                }
            }
            else if (input == "double")
            {
                int handNumber = game.CurrentHandIndex + 1;
                Hand doubledHand = game.CurrentHand;

                if (game.DoubleDown(doubledHand))
                {
                    ui.SetDealerMessage(
                        $"Hand {handNumber} doubles to {ConsoleUI.FormatMoney(doubledHand.Bet)}."
                    );

                    ui.ShowBoard(game);
                    Thread.Sleep(DoubleDelay);

                    if (doubledHand.IsBusted)
                    {
                        ui.SetDealerMessage($"Hand {handNumber} busts.");

                        ui.ShowBoard(game);
                        Thread.Sleep(MessageDelay);
                    }

                    if (!game.MoveToNextHand())
                    {
                        game.EndPlayerTurn();
                    }
                    else
                    {
                        ui.ShowBoard(game);
                    }
                }
            }
            else if (input == "stand")
            {
                int handNumber = game.CurrentHandIndex + 1;
                Hand standingHand = game.CurrentHand;

                standingHand.IsStanding = true;

                ui.SetDealerMessage($"Hand {handNumber} stands.");

                ui.ShowBoard(game);
                Thread.Sleep(ShortMessageDelay);

                if (!game.MoveToNextHand())
                {
                    game.EndPlayerTurn();
                }
                else
                {
                    ui.ShowBoard(game);
                }
            }
        }

        // -------------------------
        // DEALER
        // -------------------------

        if (game.Player.Hands.Any(hand => !hand.IsBusted))
        {
            game.RevealDealerHoleCard();

            ui.SetDealerMessage("Dealer reveals the hole card.");
            ui.ShowBoard(game);
            Thread.Sleep(MessageDelay);

            while (game.ShouldDealerHit())
            {
                ui.SetDealerMessage("Dealer hits.");

                game.HitDealer();

                ui.ShowBoard(game);
                Thread.Sleep(DealerDelay);
            }

            game.EndDealerTurn();

            if (game.Dealer.Hand.GetValue() > 21)
            {
                ui.SetDealerMessage("Dealer busts.");
            }
            else
            {
                ui.SetDealerMessage(
                    $"Dealer stands on {game.Dealer.Hand.GetValue()}."
                );
            }

            ui.ShowBoard(game);
            Thread.Sleep(MessageDelay);
        }

        // -------------------------
        // RESULTS
        // -------------------------

        for (int i = 0; i < game.Player.Hands.Count; i++)
        {
            Hand hand = game.Player.Hands[i];

            RoundResult result =
                game.GetHandResult(hand);

            if (result == RoundResult.Win)
            {
                ui.SetDealerMessage($"Hand {i + 1} wins.");
            }
            else if (result == RoundResult.Lose)
            {
                ui.SetDealerMessage($"Hand {i + 1} loses.");
            }
            else if (result == RoundResult.Push)
            {
                ui.SetDealerMessage($"Hand {i + 1}: Push.");
            }
            else if (result == RoundResult.Blackjack)
            {
                ui.SetDealerMessage($"Hand {i + 1}: Blackjack.");
            }

            game.Payout(result, hand);

            ui.ShowBoard(game);
            Thread.Sleep(ResultDelay);
        }

        finishedRoundOnTable = true;
    }
}

ui.Shutdown();
