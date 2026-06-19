using System;
using System.Collections.Generic;

namespace POPTickets.Models;

/// <summary>
/// Full mutable game state — active tickets for the current round plus
/// an immutable history of every past draw.  Serialised into Configuration
/// so everything survives a plugin reload.
/// </summary>
[Serializable]
public class GameState
{
    /// <summary>Unique ID for the round currently accepting tickets.</summary>
    public Guid CurrentRoundId { get; set; } = Guid.NewGuid();

    /// <summary>Tickets registered for the current (not yet drawn) round.</summary>
    public List<TicketEntry> ActiveTickets { get; set; } = new();

    /// <summary>
    /// Result of the most recently resolved draw, held here until the host
    /// clicks "New Round".  Persisted so the results panel survives a plugin
    /// reload before the host starts the next round.
    /// <c>null</c> when no draw has been resolved yet this round.
    /// </summary>
    public DrawResult? PendingResult { get; set; }

    /// <summary>
    /// Results from all previously resolved rounds, newest first.
    /// Populated by <see cref="ArchiveAndStartNewRound"/> when the host
    /// clicks "New Round" after reviewing the results.
    /// </summary>
    public List<DrawResult> History { get; set; } = new();

    /// <summary>
    /// Moves <see cref="PendingResult"/> into <see cref="History"/>, clears
    /// the active ticket list, and generates a new round ID so the next round
    /// can begin accepting tickets.  Call this when the host clicks "New Round".
    /// </summary>
    public void ArchiveAndStartNewRound()
    {
        if (PendingResult is not null)
            History.Insert(0, PendingResult);
        PendingResult  = null;
        ActiveTickets.Clear();
        CurrentRoundId = Guid.NewGuid();
    }
}
