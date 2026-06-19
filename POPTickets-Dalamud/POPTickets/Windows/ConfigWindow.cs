using Dalamud.Game.Text;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;

namespace POPTickets.Windows;

public sealed class ConfigWindow : Window, IDisposable
{
    private readonly Plugin _plugin;
    private string _patternBuf = string.Empty;

    private static readonly XivChatType[] MonitorableChannels =
    {
        XivChatType.Say,
        XivChatType.Yell,
        XivChatType.Shout,
        XivChatType.Party,
        XivChatType.Alliance,
        XivChatType.FreeCompany,
        XivChatType.Linkshell1,
        XivChatType.Linkshell2,
        XivChatType.CrossWorldLinkshell1,
        XivChatType.CustomEmote,
        XivChatType.Echo,
    };

    public ConfigWindow(Plugin plugin) : base(
        "POP Tickets — Configuration",
        ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)
    {
        _plugin     = plugin;
        _patternBuf = plugin.Configuration.TriggerPattern;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(400, 200),
            MaximumSize = new Vector2(800, 600),
        };
    }

    public override void Draw()
    {
        var cfg = _plugin.Configuration;
        bool changed = false;

        ImGui.TextColored(cfg.AccentColor, "Chat Listener");
        ImGui.Separator();

        // Channel dropdown
        ImGui.Text("Monitor Channel:");
        ImGui.SameLine();
        if (ImGui.BeginCombo("##chan", cfg.MonitoredChatType.ToString()))
        {
            foreach (var ct in MonitorableChannels)
            {
                bool sel = cfg.MonitoredChatType == ct;
                if (ImGui.Selectable(ct.ToString(), sel))
                {
                    cfg.MonitoredChatType = ct;
                    changed = true;
                }
                if (sel) ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }

        // Trigger phrase
        ImGui.Text("Trigger Regex:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(300);
        if (ImGui.InputText("##trigger", ref _patternBuf, 256))
        {
            cfg.TriggerPattern = _patternBuf;
            changed = true;
        }
        ImGui.SameLine();
        if (ImGui.Button("Reset##trigger"))
        {
            cfg.TriggerPattern = @"[Ww]inning\s+number[:\s]+(\d{3})";
            _patternBuf = cfg.TriggerPattern;
            changed = true;
        }
        ImGui.TextDisabled("  Capture group 1 must contain exactly 3 digits.");

        ImGui.Spacing();
        ImGui.TextColored(cfg.AccentColor, "Appearance");
        ImGui.Separator();

        // Accent colour
        var col = cfg.AccentColor;
        if (ImGui.ColorEdit4("Accent Colour##ac", ref col,
            ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
        {
            cfg.AccentColor = col;
            changed = true;
        }

        ImGui.Spacing();
        if (ImGui.Button("Close"))
            IsOpen = false;

        if (changed)
        {
            _plugin.RebuildTriggerRegex();
            _plugin.SaveConfiguration();
        }
    }

    public void Dispose() { }
}
