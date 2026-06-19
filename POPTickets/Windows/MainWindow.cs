using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using POPTickets.Models;
using System;
using System.Numerics;

namespace POPTickets.Windows;

public sealed class MainWindow : Window, IDisposable
{
    private readonly Plugin _plugin;

    // ── Add-ticket form state ──────────────────────────────────────────────
    private string _addName   = string.Empty;
    private int    _addD0, _addD1, _addD2;
    private long   _addBet    = 10_000;
    private string _formError = string.Empty;

    // ── Resolve-draw state ─────────────────────────────────────────────────
    private bool _resolvePopupOpen;
    private int  _winD0, _winD1, _winD2;
    private bool _preFilled;

    // ── Active-tickets search filter ───────────────────────────────────────
    private string _ticketFilter = string.Empty;

    // ── History-tab export state ───────────────────────────────────────────
    private string   _exportStatus      = string.Empty;
    private DateTime _exportStatusUntil = DateTime.MinValue;

    private static readonly Random Rng = new();

    public MainWindow(Plugin plugin) : base(
        "POP Tickets — Host",
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        _plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(640, 440),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    /// <summary>
    /// Called by the chat listener when it auto-detects a winning number.
    /// Pre-fills the resolve dialog and brings the window to focus.
    /// </summary>
    public void PreFillWinningNumber(string digits)
    {
        if (digits.Length < 3) return;
        _winD0            = digits[0] - '0';
        _winD1            = digits[1] - '0';
        _winD2            = digits[2] - '0';
        _preFilled        = true;
        _resolvePopupOpen = true;
        IsOpen            = true;
    }

    public override void Draw()
    {
        var cfg   = _plugin.Configuration;
        var state = cfg.CurrentGame;
        var now   = DateTime.Now;

        // ── Header: countdown timer ────────────────────────────────────────
        var nextHour  = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1);
        var remaining = nextHour - now;
        ImGui.TextColored(cfg.AccentColor, $"⏳  Next hourly draw in  {remaining:mm\\:ss}");
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - 130);
        if (ImGui.SmallButton("⚙ Settings"))
            _plugin.WindowSystem.GetWindow("POP Tickets — Configuration")!.IsOpen = true;
        ImGui.Separator();

        // ── Tab bar ────────────────────────────────────────────────────────
        if (!ImGui.BeginTabBar("##tabs")) return;

        if (ImGui.BeginTabItem("🎟 Active Round"))
        {
            DrawActiveRoundTab(state, cfg);
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("📜 History"))
        {
            DrawHistoryTab(state, cfg);
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();

        // ── Modal: resolve draw (rendered outside tab bar) ─────────────────
        DrawResolveModal(state, cfg);
    }

    // ───────────────────────────────────────────────────────────────────────
    //  Active Round tab — shows either the add-form + ticket list,
    //  OR the results panel, depending on whether a draw has been resolved.
    // ───────────────────────────────────────────────────────────────────────
    private void DrawActiveRoundTab(GameState state, Configuration cfg)
    {
        if (state.PendingResult is not null)
        {
            // Results panel — visible until the host clicks "New Round"
            DrawResultsPanel(state, cfg);
        }
        else
        {
            // Normal: add tickets + resolve
            DrawAddTicketForm(cfg, state);
            ImGui.Spacing();
            DrawActiveTicketsTable(state, cfg);
            ImGui.Spacing();
            DrawResolveButton(state);
        }
    }

    // ───────────────────────────────────────────────────────────────────────
    //  Add-ticket form
    // ───────────────────────────────────────────────────────────────────────
    private void DrawAddTicketForm(Configuration cfg, GameState state)
    {
        ImGui.TextColored(cfg.AccentColor, "Add Ticket");

        ImGui.SetNextItemWidth(180);
        ImGui.InputText("Player##name", ref _addName, 64);
        ImGui.SameLine();

        ImGui.Text("Digits:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(45); ImGui.InputInt("##d0", ref _addD0, 0); _addD0 = Math.Clamp(_addD0, 0, 9);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(45); ImGui.InputInt("##d1", ref _addD1, 0); _addD1 = Math.Clamp(_addD1, 0, 9);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(45); ImGui.InputInt("##d2", ref _addD2, 0); _addD2 = Math.Clamp(_addD2, 0, 9);
        ImGui.SameLine();
        if (ImGui.Button("🎲 Random"))
        {
            _addD0 = Rng.Next(0, 10);
            _addD1 = Rng.Next(0, 10);
            _addD2 = Rng.Next(0, 10);
        }

        ImGui.Text("Bet (gil):");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120);
        var betInt = (int)Math.Min(_addBet, int.MaxValue);
        if (ImGui.InputInt("##bet", ref betInt, 1000))
            _addBet = Math.Clamp(betInt, 1, 500_000);
        ImGui.SameLine();
        ImGui.TextDisabled("max 500,000");

        ImGui.SameLine();
        bool canAdd = !string.IsNullOrWhiteSpace(_addName) && _addBet > 0;
        if (!canAdd) ImGui.BeginDisabled();
        if (ImGui.Button("Add Ticket"))
        {
            state.ActiveTickets.Add(new TicketEntry
            {
                PlayerName = _addName.Trim(),
                Digits     = new[] { _addD0, _addD1, _addD2 },
                BetAmount  = _addBet,
                RoundId    = state.CurrentRoundId,
            });
            _plugin.SaveConfiguration();
            _addName  = string.Empty;
            _addD0 = _addD1 = _addD2 = 0;
            _formError = string.Empty;
        }
        if (!canAdd) ImGui.EndDisabled();

        if (!string.IsNullOrEmpty(_formError))
            ImGui.TextColored(new Vector4(1, 0.3f, 0.3f, 1), _formError);
    }

    // ───────────────────────────────────────────────────────────────────────
    //  Active tickets table
    // ───────────────────────────────────────────────────────────────────────
    private void DrawActiveTicketsTable(GameState state, Configuration cfg)
    {
        // ── Search / filter input ──────────────────────────────────────────
        ImGui.SetNextItemWidth(200);
        ImGui.InputText("🔍 Filter by name##ticketFilter", ref _ticketFilter, 64);
        if (!string.IsNullOrEmpty(_ticketFilter))
        {
            ImGui.SameLine();
            if (ImGui.SmallButton("✖##clearFilter"))
                _ticketFilter = string.Empty;
        }
        ImGui.SameLine();

        var filter  = _ticketFilter.Trim();
        var visible = string.IsNullOrEmpty(filter)
            ? state.ActiveTickets
            : state.ActiveTickets
                   .FindAll(t => t.PlayerName.Contains(filter, StringComparison.OrdinalIgnoreCase));

        ImGui.TextColored(cfg.AccentColor,
            string.IsNullOrEmpty(filter)
                ? $"Registered Tickets ({state.ActiveTickets.Count})"
                : $"Registered Tickets ({visible.Count} of {state.ActiveTickets.Count})");

        if (state.ActiveTickets.Count == 0)
        {
            ImGui.TextDisabled("No tickets yet — add some above.");
            return;
        }

        if (visible.Count == 0)
        {
            ImGui.TextDisabled($"No tickets match \"{filter}\".");
            return;
        }

        const ImGuiTableFlags flags =
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg |
            ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingStretchProp;

        if (!ImGui.BeginTable("##tickets", 4, flags, new Vector2(0, 150))) return;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("Player",    ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Ticket",    ImGuiTableColumnFlags.WidthFixed, 70);
        ImGui.TableSetupColumn("Bet (gil)", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Remove",    ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableHeadersRow();

        int removeIdx = -1;
        for (int i = 0; i < visible.Count; i++)
        {
            var t = visible[i];
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); ImGui.TextUnformatted(t.PlayerName);
            ImGui.TableNextColumn(); ImGui.TextUnformatted(t.TicketDisplay);
            ImGui.TableNextColumn(); ImGui.TextUnformatted($"{t.BetAmount:N0}");
            ImGui.TableNextColumn();
            // Use the real index in ActiveTickets so removal targets the right entry
            int realIdx = state.ActiveTickets.IndexOf(t);
            ImGui.PushID(realIdx);
            if (ImGui.SmallButton("✖")) removeIdx = realIdx;
            ImGui.PopID();
        }
        ImGui.EndTable();

        if (removeIdx >= 0)
        {
            state.ActiveTickets.RemoveAt(removeIdx);
            _plugin.SaveConfiguration();
        }
    }

    // ───────────────────────────────────────────────────────────────────────
    //  Resolve Draw button (bottom of normal view)
    // ───────────────────────────────────────────────────────────────────────
    private void DrawResolveButton(GameState state)
    {
        bool hasTickets = state.ActiveTickets.Count > 0;
        if (!hasTickets) ImGui.BeginDisabled();
        if (ImGui.Button("🎯 Resolve Draw…"))
        {
            _resolvePopupOpen = true;
            if (!_preFilled) { _winD0 = _winD1 = _winD2 = 0; }
        }
        if (!hasTickets) ImGui.EndDisabled();
        if (!hasTickets) ImGui.TextDisabled("Add at least one ticket before resolving.");
    }

    // ───────────────────────────────────────────────────────────────────────
    //  Results panel (shown after resolve, until "New Round" is pressed)
    // ───────────────────────────────────────────────────────────────────────
    private void DrawResultsPanel(GameState state, Configuration cfg)
    {
        var result = state.PendingResult!;

        ImGui.TextColored(cfg.AccentColor,
            $"✔ Round Results — Winning number: {result.WinningDisplay}");
        ImGui.TextDisabled("Review payouts below, then click New Round to start the next round.");
        ImGui.Separator();

        const ImGuiTableFlags rflags =
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg |
            ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingStretchProp;

        if (ImGui.BeginTable("##results", 5, rflags, new Vector2(0, 160)))
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn("Player",       ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Ticket",       ImGuiTableColumnFlags.WidthFixed, 70);
            ImGui.TableSetupColumn("Matches",      ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn("Multiplier",   ImGuiTableColumnFlags.WidthFixed, 70);
            ImGui.TableSetupColumn("Payout (gil)", ImGuiTableColumnFlags.WidthFixed, 110);
            ImGui.TableHeadersRow();

            foreach (var p in result.Payouts)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn(); ImGui.TextUnformatted(p.PlayerName);
                ImGui.TableNextColumn(); ImGui.TextUnformatted(p.TicketDisplay);
                ImGui.TableNextColumn(); ImGui.TextUnformatted(p.Matches.ToString());
                ImGui.TableNextColumn();
                if (p.Multiplier > 0)
                    ImGui.TextColored(cfg.AccentColor, $"×{p.Multiplier}");
                else
                    ImGui.TextDisabled("×0");
                ImGui.TableNextColumn();
                if (p.Payout > 0)
                    ImGui.TextColored(cfg.AccentColor, $"{p.Payout:N0}");
                else
                    ImGui.TextDisabled("0");
            }
            ImGui.EndTable();
        }

        ImGui.Spacing();
        ImGui.Text("Total paid out:");
        ImGui.SameLine();
        ImGui.TextColored(cfg.AccentColor, $"{result.TotalPaidOut:N0} gil");

        ImGui.SameLine(ImGui.GetContentRegionAvail().X - 115);
        if (ImGui.Button("New Round 🔄", new Vector2(115, 0)))
        {
            // Archive the pending result to history and reset for the next round
            state.ArchiveAndStartNewRound();
            _plugin.SaveConfiguration();
        }
    }

    // ───────────────────────────────────────────────────────────────────────
    //  Resolve modal dialog
    // ───────────────────────────────────────────────────────────────────────
    private void DrawResolveModal(GameState state, Configuration cfg)
    {
        if (_resolvePopupOpen)
        {
            ImGui.OpenPopup("Resolve Draw##popup");
            _resolvePopupOpen = false;
        }

        var viewport = ImGui.GetMainViewport();
        var center   = viewport.Pos + viewport.Size * 0.5f;
        ImGui.SetNextWindowPos(center, ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        bool open = true;

        if (!ImGui.BeginPopupModal("Resolve Draw##popup", ref open,
            ImGuiWindowFlags.AlwaysAutoResize)) return;

        if (_preFilled)
        {
            ImGui.TextColored(cfg.AccentColor, "✔ Winning number auto-detected from chat!");
            _preFilled = false;
        }
        else
        {
            ImGui.Text("Enter the 3-digit winning number:");
        }

        ImGui.Spacing();
        ImGui.Text("Winning digits:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(45); ImGui.InputInt("##w0", ref _winD0, 0); _winD0 = Math.Clamp(_winD0, 0, 9);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(45); ImGui.InputInt("##w1", ref _winD1, 0); _winD1 = Math.Clamp(_winD1, 0, 9);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(45); ImGui.InputInt("##w2", ref _winD2, 0); _winD2 = Math.Clamp(_winD2, 0, 9);

        ImGui.Spacing();
        if (ImGui.Button("✔ Resolve", new Vector2(120, 0)))
        {
            var winning = new[] { _winD0, _winD1, _winD2 };
            var result  = new DrawResult
            {
                WinningDigits = winning,
                ResolvedAt    = DateTime.UtcNow,
            };

            long total = 0;
            foreach (var ticket in state.ActiveTickets)
            {
                long payout = PayoutCalculator.Calculate(
                    ticket.BetAmount, ticket.Digits, winning,
                    out int matches, out int multiplier);

                result.Payouts.Add(new PayoutRecord
                {
                    PlayerName    = ticket.PlayerName,
                    TicketDisplay = ticket.TicketDisplay,
                    Matches       = matches,
                    Multiplier    = multiplier,
                    Payout        = payout,
                });
                total += payout;
            }
            result.TotalPaidOut = total;

            // Persist to history.json immediately so the record survives uninstall/reinstall
            _plugin.AppendDrawResultToHistoryFile(result);

            // Store as pending — tickets stay visible; history is updated on New Round
            state.PendingResult = result;
            _plugin.SaveConfiguration();

            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(80, 0)))
            ImGui.CloseCurrentPopup();

        ImGui.EndPopup();
    }

    // ───────────────────────────────────────────────────────────────────────
    //  History tab
    // ───────────────────────────────────────────────────────────────────────
    private void DrawHistoryTab(GameState state, Configuration cfg)
    {
        // ── Export button ──────────────────────────────────────────────────
        bool hasHistory = state.History.Count > 0;
        if (!hasHistory) ImGui.BeginDisabled();
        if (ImGui.Button("📥 Export CSV…"))
        {
            var path = _plugin.ExportHistoryCsv(state.History);
            if (!string.IsNullOrEmpty(path))
            {
                _exportStatus      = $"Saved: {path}";
                _exportStatusUntil = DateTime.Now.AddSeconds(10);
            }
            else
            {
                _exportStatus      = "Export failed — see plugin log for details.";
                _exportStatusUntil = DateTime.Now.AddSeconds(6);
            }
        }
        if (!hasHistory) ImGui.EndDisabled();

        if (!string.IsNullOrEmpty(_exportStatus) && DateTime.Now < _exportStatusUntil)
        {
            ImGui.SameLine();
            ImGui.TextDisabled(_exportStatus);
        }

        ImGui.Separator();

        if (!hasHistory)
        {
            ImGui.TextDisabled("No draw history yet — complete your first round to see it here.");
            return;
        }

        // 5 columns: the last two (Multiplier, Payout) are empty on summary
        // rows and filled on per-player child rows.
        const ImGuiTableFlags flags =
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg |
            ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingStretchProp;

        if (!ImGui.BeginTable("##history", 5, flags, new Vector2(0, 0))) return;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("Date / Time  ▸  Player", ImGuiTableColumnFlags.WidthFixed,   170);
        ImGui.TableSetupColumn("Winning / Ticket",        ImGuiTableColumnFlags.WidthFixed,    95);
        ImGui.TableSetupColumn("Tickets / Matches",       ImGuiTableColumnFlags.WidthFixed,    70);
        ImGui.TableSetupColumn("Multiplier",              ImGuiTableColumnFlags.WidthFixed,    70);
        ImGui.TableSetupColumn("Total Paid / Payout",     ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        for (int i = 0; i < state.History.Count; i++)
        {
            var draw = state.History[i];

            // ── Summary row (always visible) ───────────────────────────────
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            bool expanded = ImGui.TreeNodeEx(
                $"##draw{i}",
                ImGuiTreeNodeFlags.SpanFullWidth,
                draw.ResolvedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));

            ImGui.TableNextColumn();
            ImGui.TextColored(cfg.AccentColor, draw.WinningDisplay);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(draw.Payouts.Count.ToString());
            ImGui.TableNextColumn();
            // Multiplier column — blank for the summary row
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{draw.TotalPaidOut:N0} gil");

            // ── Per-player child rows (visible when expanded) ──────────────
            if (expanded)
            {
                foreach (var p in draw.Payouts)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    // Indent the player name to visually nest it
                    ImGui.Indent(16f);
                    ImGui.TextUnformatted(p.PlayerName);
                    ImGui.Unindent(16f);

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(p.TicketDisplay);

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(p.Matches.ToString());

                    ImGui.TableNextColumn();
                    if (p.Multiplier > 0)
                        ImGui.TextColored(cfg.AccentColor, $"×{p.Multiplier}");
                    else
                        ImGui.TextDisabled("×0");

                    ImGui.TableNextColumn();
                    if (p.Payout > 0)
                        ImGui.TextColored(cfg.AccentColor, $"{p.Payout:N0}");
                    else
                        ImGui.TextDisabled("0");
                }

                ImGui.TreePop();
            }
        }

        ImGui.EndTable();
    }

    public void Dispose() { }
}
