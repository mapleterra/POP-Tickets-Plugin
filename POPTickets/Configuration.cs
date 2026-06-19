using Dalamud.Configuration;
using Dalamud.Game.Text;
using POPTickets.Models;
using System;
using System.Numerics;

namespace POPTickets;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    // ── Chat listener settings ─────────────────────────────────────────────
    /// <summary>Which chat channel to monitor for auto-detected winning numbers.</summary>
    public XivChatType MonitoredChatType { get; set; } = XivChatType.Say;

    /// <summary>
    /// Regex pattern used to extract a 3-digit winning number from chat.
    /// Capture group 1 must contain exactly 3 digits.
    /// Default: matches "Winning number: 123" case-insensitively.
    /// </summary>
    public string TriggerPattern { get; set; } = @"[Ww]inning\s+number[:\s]+(\d{3})";

    // ── UI settings ────────────────────────────────────────────────────────
    /// <summary>Accent colour used for highlights and win indicators.</summary>
    public Vector4 AccentColor { get; set; } = new Vector4(1.00f, 0.84f, 0.00f, 1.00f);

    // ── Persistent game data ───────────────────────────────────────────────
    /// <summary>Full game state: active tickets + draw history.</summary>
    public GameState CurrentGame { get; set; } = new GameState();
}
