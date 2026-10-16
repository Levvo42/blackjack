# Blackjack

A casino-style Blackjack game for the terminal, written in **C# / .NET 10**.

You play against the dealer with a six-deck shoe and real table rules: splitting up to four hands,
doubling down, insurance, and blackjack paying 3 to 2. The whole table is drawn with text characters,
and the screen updates without flicker.

Made as a school project.

```
═══════════════════════════════════════════════════════════════════
                              DEALER
═══════════════════════════════════════════════════════════════════

                              [ ? ]
                       ┌─────────┐─────────┐
                       │K♠       │ /\_/\   │
                       │         │( o.o )  │
                       │    ♠    │ > ^ <   │
                       │         │         │
                       │       ♠K│         │
                       └─────────┘─────────┘

───────────────────────────────────────────────────────────────────

            ▶ HAND 1 ◀                        HAND 2
            [ Bet 10 ]                      [ Bet 10 ]

          ┌───┌─────────┐                 ┌───┌─────────┐
          │8♥ │3♦       │                 │8♣ │K♥       │
          │   │         │                 │   │         │
          │   │    ♦    │                 │   │    ♥    │
          │   │         │                 │   │         │
          │   │       ♦3│                 │   │       ♥K│
          └───└─────────┘                 └───└─────────┘
              [ 11 ]                          [ 18 ]

          ┌─────────────────────────────────────────────┐
          │               Hand 1 splits.                │
          │                                             │
          └─────────────────────────────────────────────┘
═══════════════════════════════════════════════════════════════════
                              PLAYER
───────────────────────────────────────────────────────────────────
           HAND 1  |  SUM 11  |  BET 10  |  CREDITS 980
───────────────────────────────────────────────────────────────────
                > Hit <     Stand     Double
```

*(Roughly what it looks like. In the terminal the felt is green and the suits are red and grey.)*

---

## Features

- **Six-deck shoe** with a discard pile, reshuffled when it runs low, like at a real table
- **Split** pairs up to four hands, shown side by side
- **Double down** on any first two cards, also after a split
- **Insurance** when the dealer shows an ace
- **Blackjack pays 3:2**, the dealer hits soft 17 and checks for blackjack before you play
- **Chip-based betting** (2.5 to 500), and you can buy more chips between rounds
- **Flicker-free rendering:** only the parts of the screen that changed are redrawn
- **Resize-aware:** the table re-centres itself, and waits for you if the window is too small
- **Clean exit:** your terminal is restored exactly as it was, even on Ctrl+C

The full rules and payouts are in [docs/RULES.md](docs/RULES.md).

---

## Controls

| Key | Action |
|---|---|
| ← → (↑ ↓ in the main menu) | Move the selection |
| Enter | Confirm |
| Space | Add the selected chip to your bet |
| Backspace | Reset the bet |
| Esc | Go back (from the chip selector) |
| Ctrl+C | Quit at any time |

---

## Getting started

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A terminal window of at least **68 × 42** characters
- **Windows 10 or later** (Windows Terminal recommended). It also builds and runs on macOS and Linux.

### Run it

```bash
git clone https://github.com/Levvo42/blackjack.git
cd blackjack
dotnet run
```

### Build a standalone executable

A single file that runs without .NET installed:

```bash
# Windows (x64)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:AssemblyName=Blackjack -o .\export\windows

# macOS (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:AssemblyName="Blackjack(apple-silicon)" -o .\export\mac-apple-silicon

use: chmod +x "Blackjack(apple-silicon)" before running

# macOS (Intel)
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -p:AssemblyName="Blackjack(apple-intel)" -o .\export\mac-intel

use: chmod +x "Blackjack(apple-intel)" before running

# Linux
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:AssemblyName="Blackjack(linux-x64)" -o .\export\linux-x64

use: chmod +x "Blackjack(linux-x64)" before running
```

---

## How it works

The project is split into layers that each have one job:

```
Program.cs   →  runs a round step by step (deal, insurance, player, dealer, payout)
Game.cs ...  →  the rules: cards, hands, shoe, player, dealer, results
ConsoleUI.cs →  draws the table from the game state and reads the keys
Screen.cs    →  gets the picture onto the terminal, redrawing only what changed
```

The game rules don't know the screen exists, and the screen never changes the game.
Read more in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

### Project structure

| File | Contents |
|---|---|
| `Program.cs` | Game flow: main menu, betting, one round from deal to payout, timing |
| `Game.cs` | All the table rules: dealing, insurance, split, double, dealer play, results, payouts |
| `Player.cs` | Credits, bets and the player's hands |
| `Dealer.cs` | The dealer's hand |
| `Hand.cs` | Cards in one hand, its bet, total value (aces as 1 or 11), blackjack and soft checks |
| `Card.cs` | One card: rank, suit and value |
| `Deck.cs` | Creates the 52 cards of a deck |
| `Shoe.cs` | Six decks, shuffling, drawing, the discard pile and reshuffling |
| `RoundResult.cs` | `Win`, `Lose`, `Push`, `Blackjack` |
| `ConsoleUI.cs` | The table, cards, hand panels, message box, menus and input |
| `Screen.cs` | Terminal setup, flicker-free frame output, restoring the terminal |

---

## House rules

> ⚠️ **NOTICE FROM THE MANAGEMENT**
>
> Cheating of any kind (card counting included) is strictly forbidden at this table.
> Our dealers are trained to spot it, and our software is *definitely not* listening for it.
>
> So please don't even *type* the word "cheat". And whatever you do,
> **do not press Enter afterwards.**

---

## License

School project. Feel free to look around and learn from it.
