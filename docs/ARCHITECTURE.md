# Architecture

How the code is organised, how a round flows through it, and how the table gets onto the screen.

[← Back to the README](../README.md)

---

## Layers

```
┌──────────────────────────────────────────────────────────┐
│  Program.cs                         FLOW                 │
│  Main menu → betting → deal → insurance → player →       │
│  dealer → payout → repeat                                │
└──────────────┬──────────────────────────────┬────────────┘
               │ asks / shows                 │ changes
               ▼                              ▼
┌─────────────────────────────┐   ┌────────────────────────┐
│  ConsoleUI.cs   PRESENTATION│   │  Game.cs ...   RULES   │
│  Builds the table from the  │──►│  Player, Dealer, Hand, │
│  game state, menus, input   │   │  Shoe, Deck, Card,     │
└──────────────┬──────────────┘   │  RoundResult           │
               │ lines of text    └────────────────────────┘
               ▼                       (read only)
┌─────────────────────────────┐
│  Screen.cs      TERMINAL    │
│  Diffing, ANSI output,      │
│  setup and restore          │
└─────────────────────────────┘
```

Dependencies only point one way:

- The **rules** (`Game` and the model classes) don't use `Console` at all. They don't know there is a
  screen.
- The **presentation** (`ConsoleUI`) reads the game to draw it, and asks it what's allowed
  (`CanSplit`, `CanDoubleDown`), but never changes it.
- The **flow** (`Program.cs`) is the only part that both changes the game and tells the UI what to
  show.

This keeps the rules testable without a terminal, and the interface replaceable without touching the
rules.

---

## The model

```
Game
 ├── Player ── Credits
 │     └── Hands ──► Hand ── Bet, InsuranceBet
 │                     └── Cards ──► Card (Rank, Suit, Value)
 ├── Dealer
 │     └── Hand
 └── Shoe
       ├── Cards        cards still to be dealt
       └── DiscardPile  cards from finished rounds
```

| Class | Responsibility |
|---|---|
| `Card` | Rank, suit and value. An ace is stored as 11. |
| `Deck` | Builds the 52 cards of one deck. |
| `Shoe` | Six decks shuffled together (Fisher–Yates), draws from the end of the list, collects discards, reshuffles at 62 cards or fewer. |
| `Hand` | Its cards and bet. `GetValue()` counts aces as 11 and turns them into 1 one at a time while the hand is over 21. `IsBusted` and `IsFinished` are calculated, never stored. |
| `Player` | Credits (only changed through its own methods) and a list of hands, because a split creates more. |
| `Dealer` | One hand. The dealer's behaviour is a table rule, so it lives in `Game`. |
| `Game` | Every rule: dealing, the blackjack check, insurance, split, double, moving between hands, the dealer's play, results and payouts. |
| `RoundResult` | `Win`, `Lose`, `Push`, `Blackjack`. |

A few patterns used throughout:

- **"Can" + "Do" pairs.** `CanSplit` / `Split`, `CanDoubleDown` / `DoubleDown`, `CanTakeInsurance` /
  `TakeInsurance`. The UI uses the "Can" method to decide what to offer. The "Do" method checks it
  again and returns `false` instead of breaking a rule.
- **Encapsulated state.** `Credits`, `PlayerTurn`, `HideDealerHoleCard` and similar have `private set`,
  so they can only change through the methods meant for it.
- **`decimal` for money,** so amounts like 2.5 are exact.
- **Nullable results.** `GetInitialBlackjackResult()` returns `RoundResult?`, where `null` means
  "nobody has blackjack, keep playing".

---

## A round

`Program.cs` uses top-level statements and runs the game as a loop:

```
Main menu ──► new Game (1000 credits, shuffled shoe)
               │
               ▼
        Betting screen ◄──────────────────────────────┐
        │  PLACE BET / BUY CHIPS / MAIN MENU          │
        ▼                                             │
        Clear the previous round, start a new one     │
        Place the bet                                 │
        Deal: player, dealer, player, dealer          │
        Dealer shows an ace? ──► offer insurance      │
        Anyone has blackjack? ──► settle ─────────────┤
        Player plays each hand                        │
        Any hand not bust? ──► dealer plays           │
        Settle each hand ─────────────────────────────┘
```

After every step the flow calls `ui.ShowBoard(game)` and pauses briefly, so the cards appear one at a
time. The pauses are named constants at the top of `Program.cs` (`DealDelay`, `DealerDelay`,
`ResultDelay` ...), so the game's pace is set in one place.

The finished round stays on the table until the next bet is placed. Only then are the cards moved to
the discard pile, which is also when the shoe may be reshuffled.

---

## Rendering

### Build the frame, then show it

The interface works like a small game engine. Every time something changes, `ConsoleUI` rebuilds the
**whole** table as a list of strings, straight from the game state:

```csharp
StartFrame();                  // check the window size, clear the list
ShowTableArea(game, false);    // dealer, cards, player hands, message box
ShowPlayerStatus(game);        // hand, total, bet, credits
PresentFrame();                // hand the list to Screen
```

Because the picture is always rebuilt from `Game`, it can never show stale information.

### Only send what changed

`Screen.Present` compares the new frame with the previous one, row by row. Only changed rows are sent,
each placed with a cursor-position code, and everything is written to the terminal in **one** call,
wrapped in a "synchronized update" so supporting terminals show it all at once. Moving a menu selection
repaints one row instead of the whole table.

`Screen` also:

- switches to the terminal's **alternate screen**, hides the cursor and turns off line wrapping at start;
- restores all of that on exit, including Ctrl+C;
- enables ANSI escape codes on Windows;
- centres the 67-column table in the window, and redraws everything after a resize.

### Fixed layout

Every part of the table has a fixed size: cards are 7 rows tall, each hand panel is 12 rows, the
message box is 2 lines of 45 characters. Empty areas still add their rows. A helper (`Fit`) centres
text and cuts anything too long, so a row is always exactly as wide as it should be. This is what keeps
the layout steady with many cards or four split hands.

When there isn't room for every card, a hand is drawn as overlapping strips, and if there still isn't
room, the oldest cards are shown as `+N`.

Colours are added **after** the layout is done, because the invisible colour codes would otherwise be
counted as characters.

### Input

`ConsoleUI.ReadKey` waits for a key by polling, about 33 times a second. Between checks it watches for
window resizes, so the table re-centres even while a menu is waiting. Keys pressed during animations are
thrown away before each menu, so an impatient Enter doesn't pick an option you haven't seen yet.

If the window is smaller than 68 × 42, the game shows a message and waits until it's big enough.

[← Back to the README](../README.md)
