using System;
using System.Collections.Generic;

namespace POPTickets.Models;

/// <summary>Stores the outcome of a single draw resolution.</summary>
[Serializable]
public class DrawResult
{
    /// <summary>The three winning digits drawn this round.</summary>
    public int[] WinningDigits { get; set; } = new int[3];

    /// <summary>UTC timestamp when the draw was resolved.</summary>
    public DateTime ResolvedAt { get; set; } = DateTime.UtcNow;

    /// <summary>All per-player payouts for this round.</summary>
    public List<PayoutRecord> Payouts { get; set; } = new();

    /// <summary>Sum of all gil paid out this round.</summary>
    public long TotalPaidOut { get; set; }

    /// <summary>Human-readable winning ticket string, e.g. "3-1-9".</summary>
    public string WinningDisplay => $"{WinningDigits[0]}-{WinningDigits[1]}-{WinningDigits[2]}";
}

/// <summary>A single player's resolved payout record.</summary>
[Serializable]
public class PayoutRecord
{
    public string PlayerName  { get; set; } = string.Empty;
    public string TicketDisplay { get; set; } = string.Empty;
    public int    Matches    { get; set; }
    public int    Multiplier { get; set; }
    public long   Payout     { get; set; }
}
