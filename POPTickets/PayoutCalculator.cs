namespace POPTickets;

/// <summary>
/// Pure, stateless payout engine for the POP Tickets lottery.
///
/// Matching rules (positional — each digit compared at the same index):
/// <list type="table">
///   <listheader><term>Matches</term><description>Multiplier</description></listheader>
///   <item><term>0</term><description>×0  — no win</description></item>
///   <item><term>1</term><description>×2  — e.g. player 4-7-2 vs winning 4-1-9 → 1 match → bet × 2</description></item>
///   <item><term>2</term><description>×5  — e.g. player 4-7-2 vs winning 4-7-9 → 2 matches → bet × 5</description></item>
///   <item><term>3</term><description>×50 — jackpot; all three digits match exactly</description></item>
/// </list>
/// </summary>
public static class PayoutCalculator
{
    private static readonly int[] Multipliers = { 0, 2, 5, 50 };

    /// <summary>
    /// Count the number of positional digit matches between a player ticket
    /// and the winning digits.
    /// </summary>
    /// <param name="playerDigits">Three digits (0–9) from the player's ticket.</param>
    /// <param name="winningDigits">Three winning digits drawn this round.</param>
    /// <returns>Number of positions (0–3) where the digits are equal.</returns>
    public static int CountMatches(int[] playerDigits, int[] winningDigits)
    {
        int matches = 0;
        for (int i = 0; i < 3; i++)
            if (playerDigits[i] == winningDigits[i])
                matches++;
        return matches;
    }

    /// <summary>
    /// Return the payout multiplier for a given number of positional matches.
    /// </summary>
    /// <param name="matches">Result of <see cref="CountMatches"/>.</param>
    /// <returns>0, 2, 5, or 50.</returns>
    public static int GetMultiplier(int matches) =>
        (matches is >= 0 and <= 3) ? Multipliers[matches] : 0;

    /// <summary>
    /// Calculate the gil payout for a single ticket.
    /// </summary>
    /// <param name="betAmount">The player's wager in gil.</param>
    /// <param name="playerDigits">Three digits from the player's ticket.</param>
    /// <param name="winningDigits">Three winning digits drawn this round.</param>
    /// <param name="matches">Output: how many digits matched.</param>
    /// <param name="multiplier">Output: the applied multiplier.</param>
    /// <returns>Total gil to pay the player (0 if no match).</returns>
    public static long Calculate(
        long betAmount,
        int[] playerDigits,
        int[] winningDigits,
        out int matches,
        out int multiplier)
    {
        matches    = CountMatches(playerDigits, winningDigits);
        multiplier = GetMultiplier(matches);
        return betAmount * multiplier;
    }
}
