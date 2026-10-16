# Rules and Payouts

How this table plays. If you know Blackjack, the short version is: **6 decks, dealer hits soft 17,
blackjack pays 3:2, double after split, split up to 4 hands, no surrender.**

[← Back to the README](../README.md)

---

## The goal

Get closer to **21** than the dealer without going over. Going over 21 is a **bust**, and you lose
straight away, even if the dealer busts later.

## Card values

| Card | Value |
|---|---|
| 2 – 10 | Face value |
| J, Q, K | 10 |
| A | 11, or 1 if 11 would bust the hand |

A hand where an ace still counts as 11 is **soft** (A-6 is "soft 17"). It can't bust on the next card,
because the ace can drop to 1. A hand without such an ace is **hard**.

A **blackjack** is an ace plus a 10-value card as your first two cards.

---

## The table

| Rule | This game |
|---|---|
| Decks | 6 (312 cards) in a shoe |
| Reshuffle | Between rounds, when 62 or fewer cards are left (about 80 % dealt) |
| Starting credits | 1000 |
| Chips | 2.5, 5, 10, 25, 50, 100, 500 |
| Dealer on soft 17 | **Hits** |
| Dealer checks for blackjack | Yes, before you play |
| Blackjack pays | **3:2** |
| Double down | Any first two cards, also after a split |
| Split | Two cards of the **same rank**, up to 4 hands |
| Insurance | Offered when the dealer shows an ace, pays 2:1 |
| Surrender | Not offered |

---

## A round

1. **Bet.** Choose chips with the arrow keys and Space, confirm with Enter. You can't bet more than you
   have.
2. **Deal.** You, the dealer, you, the dealer. The dealer's second card (the **hole card**) is face
   down.
3. **Insurance.** If the dealer shows an ace, you're offered insurance (see below).
4. **Blackjack check.** If you or the dealer has blackjack, the round ends here:

   | You | Dealer | Result |
   |---|---|---|
   | Blackjack | Blackjack | Push |
   | Blackjack | – | You win 3:2 |
   | – | Blackjack | You lose (only your main bet) |

5. **Your turn.** For each hand: **Hit**, **Stand**, **Double** or **Split** (when allowed).
6. **Dealer's turn.** The dealer turns over the hole card and draws until 17 or more, and also draws on
   a soft 17. If all your hands busted, the dealer doesn't need to play.
7. **Payout.** Each hand is compared with the dealer's hand on its own.

The cards stay on the table until you place your next bet, so you can see what happened.

---

## Your options

| Option | When | What happens |
|---|---|---|
| **Hit** | Always | Take one card |
| **Stand** | Always | Keep your total, move to the next hand |
| **Double** | First two cards of a hand, and you have enough credits | Your bet is doubled, you get **exactly one** card, and the hand is finished |
| **Split** | Two cards of the same rank, fewer than 4 hands, and you have enough credits | The cards become two hands, each with the same bet as the original, and each gets a new card |

Some details:

- **Same rank, not same value:** K-K can be split, K-Q can't.
- **Split aces** play like any other hand: you can hit them, and split them again.
- **21 after a split** is a normal 21, not a blackjack. It pays 1:1.
- **Double after split** is allowed.

---

## Payouts

Your bet leaves your credits when you place it. A payout gives back your bet **plus** your winnings.

| Result | You get back | Profit |
|---|---|---|
| Win | 2 × bet | +1 × bet |
| Blackjack | 2.5 × bet | +1.5 × bet (3:2) |
| Push (tie) | 1 × bet | 0 |
| Lose / bust | 0 | −1 × bet |

Example with a bet of 10: a win gives back 20, a blackjack 25, a push 10.

---

## Insurance

When the dealer shows an ace, you can bet that the hole card is a 10-value card (so the dealer has
blackjack).

- It costs **half your bet**.
- If the dealer has blackjack, it pays **2:1**: you get back 3 × the insurance.
- If not, the insurance is lost and the round goes on.

Example with a bet of 10: insurance costs 5. If the dealer has blackjack, you lose your 10 but get 15
back from the insurance, so you break even.

> Insurance is usually a bad bet. Fewer than a third of the cards are worth 10, but it would need more
> than a third to pay off.

---

## Credits

- **PLAY** in the main menu always starts a new game with **1000 credits** and a fresh shoe.
- **BUY CHIPS** on the betting screen adds credits whenever you want more.
- **MAIN MENU** on the betting screen ends the game.

[← Back to the README](../README.md)
