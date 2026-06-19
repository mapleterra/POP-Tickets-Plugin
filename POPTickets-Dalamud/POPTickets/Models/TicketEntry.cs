using System;

namespace POPTickets.Models;

/// <summary>Represents a single player's ticket for the current round.</summary>
[Serializable]
public class TicketEntry
{
    /// <summary>Display name of the player.</summary>
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>Three digits (0–9 each) chosen by the player.</summary>
    public int[] Digits { get; set; } = new int[3];

    /// <summary>Bet amount in gil. Capped at 500,000.</summary>
    public long BetAmount { get; set; }

    /// <summary>Round identifier this ticket belongs to.</summary>
    public Guid RoundId { get; set; } = Guid.NewGuid();

    /// <summary>Human-readable ticket string, e.g. "4-7-2".</summary>
    public string TicketDisplay => $"{Digits[0]}-{Digits[1]}-{Digits[2]}";
}
