using Dalamud.Game.Chat;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Windowing;
using POPTickets.Models;
using POPTickets.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace POPTickets;

public sealed class Plugin : IDalamudPlugin
{
    public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string CommandMain       = "/poptickets";
    private const string CommandShort      = "/pop";
    private const string CommandCheck      = "/popcheck";
    private const string CommandCheckShort = "/pct";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("POPTickets");
    private readonly MainWindow   _mainWindow;
    private readonly ConfigWindow _configWindow;
    private Regex? _triggerRegex;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        PluginInterface = pluginInterface;
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        _mainWindow   = new MainWindow(this);
        _configWindow = new ConfigWindow(this);

        WindowSystem.AddWindow(_mainWindow);
        WindowSystem.AddWindow(_configWindow);

        CommandManager.AddHandler(CommandMain, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the POP Tickets host window."
        });
        CommandManager.AddHandler(CommandShort, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the POP Tickets host window. (alias)"
        });
        CommandManager.AddHandler(CommandCheck, new CommandInfo(OnCheckCommand)
        {
            HelpMessage = "Look up a player's ticket by name. Usage: /popcheck <name>"
        });
        CommandManager.AddHandler(CommandCheckShort, new CommandInfo(OnCheckCommand)
        {
            HelpMessage = "Look up a player's ticket by name. (alias for /popcheck)"
        });

        PluginInterface.UiBuilder.Draw        += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfig;
        PluginInterface.UiBuilder.OpenMainUi   += ToggleMain;

        ChatGui.ChatMessage += OnChatMessage;

        RebuildTriggerRegex();

        Log.Information("POP Tickets plugin loaded.");
    }

    /// <summary>
    /// Recompile the trigger regex after the pattern changes in config.
    /// Falls back to the default pattern on any parse error.
    /// </summary>
    public void RebuildTriggerRegex()
    {
        const string defaultPattern = @"[Ww]inning\s+number[:\s]+(\d{3})";
        try
        {
            _triggerRegex = new Regex(
                string.IsNullOrWhiteSpace(Configuration.TriggerPattern)
                    ? defaultPattern
                    : Configuration.TriggerPattern,
                RegexOptions.IgnoreCase | RegexOptions.Compiled,
                TimeSpan.FromSeconds(1));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Invalid trigger pattern — reverting to default.");
            _triggerRegex = new Regex(defaultPattern, RegexOptions.Compiled);
        }
    }

    private void OnChatMessage(IHandleableChatMessage message)
    {
        if (message.LogKind != Configuration.MonitoredChatType) return;
        if (_triggerRegex is null) return;

        var text  = message.Message.TextValue;
        var match = _triggerRegex.Match(text);
        if (!match.Success || match.Groups.Count < 2) return;

        var digits = match.Groups[1].Value;
        if (digits.Length != 3) return;

        Log.Debug("Winning number detected from chat: {0}", digits);
        _mainWindow.PreFillWinningNumber(digits);
        ChatGui.Print($"[POP Tickets] Winning number detected: {digits[0]}-{digits[1]}-{digits[2]}. Confirm in the host window.");
    }

    public void SaveConfiguration() => PluginInterface.SavePluginConfig(Configuration);

    // ── Persistent history file ────────────────────────────────────────────

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    /// <summary>
    /// Appends a resolved <see cref="DrawResult"/> to <c>history.json</c> in the
    /// plugin config directory so the record survives uninstall/reinstall.
    /// </summary>
    public void AppendDrawResultToHistoryFile(DrawResult result)
    {
        try
        {
            var dir  = PluginInterface.GetPluginConfigDirectory();
            var path = Path.Combine(dir, "history.json");

            List<DrawResult> existing = new();
            if (File.Exists(path))
            {
                try
                {
                    var text = File.ReadAllText(path);
                    existing = JsonSerializer.Deserialize<List<DrawResult>>(text, JsonOpts) ?? new();
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Could not parse existing history.json — starting fresh.");
                }
            }

            existing.Add(result);
            File.WriteAllText(path, JsonSerializer.Serialize(existing, JsonOpts));
            Log.Debug("Appended draw result to history.json ({0} total entries).", existing.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to write draw result to history.json.");
        }
    }

    /// <summary>
    /// Writes a human-readable CSV of all resolved draws to the plugin config
    /// directory and returns the full file path, or an empty string on failure.
    /// Columns: Date, WinningNumber, TotalPaid, PlayerName, Ticket, Matches, Payout.
    /// </summary>
    public string ExportHistoryCsv(List<DrawResult> history)
    {
        try
        {
            var dir  = PluginInterface.GetPluginConfigDirectory();
            var ts   = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var path = Path.Combine(dir, $"history-export-{ts}.csv");

            var sb = new StringBuilder();
            sb.AppendLine("Date,WinningNumber,TotalPaid,PlayerName,Ticket,Matches,Payout");

            foreach (var draw in history)
            {
                var date    = draw.ResolvedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                var winning = draw.WinningDisplay;
                var total   = draw.TotalPaidOut;

                if (draw.Payouts.Count == 0)
                {
                    sb.AppendLine($"\"{date}\",\"{winning}\",{total},,,,");
                }
                else
                {
                    foreach (var p in draw.Payouts)
                    {
                        sb.AppendLine(
                            $"\"{date}\",\"{winning}\",{total}," +
                            $"\"{EscapeCsv(p.PlayerName)}\",\"{p.TicketDisplay}\"," +
                            $"{p.Matches},{p.Payout}");
                    }
                }
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            Log.Information("Exported history CSV to {0}.", path);
            return path;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to export history CSV.");
            return string.Empty;
        }
    }

    private static string EscapeCsv(string value) => value.Replace("\"", "\"\"");

    private void OnCommand(string command, string args) => _mainWindow.Toggle();
    private void ToggleConfig()                          => _configWindow.Toggle();
    private void ToggleMain()                            => _mainWindow.Toggle();
    private void DrawUI()                                => WindowSystem.Draw();

    private void OnCheckCommand(string command, string args)
    {
        var query = args.Trim();
        if (string.IsNullOrEmpty(query))
        {
            ChatGui.Print("[POP Tickets] Usage: /popcheck <player name>");
            return;
        }

        var tickets = Configuration.CurrentGame.ActiveTickets;
        var matches = tickets.FindAll(t =>
            t.PlayerName.Contains(query, StringComparison.OrdinalIgnoreCase));

        if (matches.Count == 0)
        {
            ChatGui.Print($"[POP Tickets] No ticket found matching \"{query}\".");
            return;
        }

        foreach (var t in matches)
        {
            ChatGui.Print(
                $"[POP Tickets] {t.PlayerName} — Ticket: {t.TicketDisplay} — Bet: {t.BetAmount:N0} gil");
        }
    }

    public void Dispose()
    {
        ChatGui.ChatMessage -= OnChatMessage;
        PluginInterface.UiBuilder.Draw        -= DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfig;
        PluginInterface.UiBuilder.OpenMainUi   -= ToggleMain;

        CommandManager.RemoveHandler(CommandMain);
        CommandManager.RemoveHandler(CommandShort);
        CommandManager.RemoveHandler(CommandCheck);
        CommandManager.RemoveHandler(CommandCheckShort);

        WindowSystem.RemoveAllWindows();
        _configWindow.Dispose();
        _mainWindow.Dispose();

        Log.Information("POP Tickets plugin unloaded.");
    }
}
