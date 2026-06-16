using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using JPRaidDictionary.Data;
using JPRaidDictionary.Models;
using JPRaidDictionary.Services;

namespace JPRaidDictionary.Windows;

/// <summary>
/// The main plugin window. Hosts the "Dictionary" tab (search box, category
/// list, results list, and entry details - unchanged from the original
/// plugin) and the "Quick Translate" tab (OpenAI-powered EN/JP translation).
/// A small status bar at the top always shows the current chat translation
/// mode and OpenAI connection status.
/// </summary>
public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly DictionaryRepository repository;

    // --- Dictionary tab state ---
    private string searchQuery = string.Empty;
    private DictionaryCategory? selectedCategory;
    private DictionaryEntry? selectedEntry;

    // --- Quick Translate tab state ---
    private string openAiApiKeyInput;
    private string anthropicApiKeyInput;
    private bool isTestingConnection;

    private string translateInput = string.Empty;
    private string translateOutput = string.Empty;
    private TranslationDirection translationDirection = TranslationDirection.EnglishToJapanese;
    private bool isTranslating;
    private string? translateError;

    public MainWindow(Plugin plugin, DictionaryRepository repository)
        : base("JP Raid Dictionary##JPRaidDictionaryMainWindow")
    {
        this.plugin = plugin;
        this.repository = repository;
        openAiApiKeyInput = plugin.Configuration.OpenAiApiKey;
        anthropicApiKeyInput = plugin.Configuration.AnthropicApiKey;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(560, 360),
            MaximumSize = new Vector2(2000, 2000),
        };
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        DrawStatusBar();
        ImGui.Separator();

        if (ImGui.BeginTabBar("##JPRaidDictionaryTabs"))
        {
            if (ImGui.BeginTabItem("Dictionary"))
            {
                DrawDictionaryTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Quick Translate"))
            {
                DrawQuickTranslateTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    // ------------------------------------------------------------------
    // Status bar
    // ------------------------------------------------------------------

    private void DrawStatusBar()
    {
        ImGui.TextDisabled($"{TranslationService.GetProviderDisplayName(plugin.Configuration.TranslationProvider)}:");
        ImGui.SameLine();
        DrawOpenAiStatusText();

        ImGui.SameLine();
        ImGui.TextDisabled("|");
        ImGui.SameLine();

        ImGui.TextDisabled("Tip: use /jpt <message> to translate & send to chat");
    }

    private void DrawOpenAiStatusText()
    {
        var (text, color) = GetOpenAiStatusDisplay(plugin.TranslationService.Status);
        ImGui.TextColored(color, text);
    }

    private static (string Text, Vector4 Color) GetOpenAiStatusDisplay(ApiConnectionStatus status) => status switch
    {
        ApiConnectionStatus.Connected => ("Connected", new Vector4(0.4f, 1f, 0.4f, 1f)),
        ApiConnectionStatus.InvalidApiKey => ("Invalid API Key", new Vector4(1f, 0.4f, 0.4f, 1f)),
        ApiConnectionStatus.Error => ("Error", new Vector4(1f, 0.4f, 0.4f, 1f)),
        _ => ("Not Configured", new Vector4(0.7f, 0.7f, 0.7f, 1f)),
    };

    // ------------------------------------------------------------------
    // Dictionary tab
    // ------------------------------------------------------------------

    private void DrawDictionaryTab()
    {
        DrawSearchBar();

        ImGui.Separator();

        var contentHeight = ImGui.GetContentRegionAvail().Y;

        ImGui.BeginChild("##CategoryList", new Vector2(150, contentHeight), true);
        DrawCategoryList();
        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild("##ResultsList", new Vector2(220, contentHeight), true);
        DrawResultsList();
        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild("##EntryDetails", new Vector2(0, contentHeight), true);
        DrawEntryDetails();
        ImGui.EndChild();
    }

    private void DrawSearchBar()
    {
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint(
            "##SearchBox",
            "Search English, Japanese, Romaji, job/PF abbreviations...",
            ref searchQuery,
            100);
    }

    private void DrawCategoryList()
    {
        ImGui.TextDisabled("Categories");
        ImGui.Separator();

        if (ImGui.Selectable("All", selectedCategory == null))
            selectedCategory = null;

        foreach (var category in Enum.GetValues<DictionaryCategory>())
        {
            if (ImGui.Selectable(GetCategoryDisplayName(category), selectedCategory == category))
                selectedCategory = category;
        }
    }

    private void DrawResultsList()
    {
        ImGui.TextDisabled("Results");
        ImGui.Separator();

        var results = GetFilteredEntries();

        if (results.Count == 0)
        {
            ImGui.TextWrapped("No entries found.");
            return;
        }

        foreach (var entry in results)
        {
            var label = string.IsNullOrEmpty(entry.JapaneseName)
                ? entry.EnglishName
                : $"{entry.EnglishName} ({entry.JapaneseName})";

            if (ImGui.Selectable($"{label}##{entry.GetHashCode()}", selectedEntry == entry))
                selectedEntry = entry;
        }
    }

    private void DrawEntryDetails()
    {
        ImGui.TextDisabled("Details");
        ImGui.Separator();

        if (selectedEntry == null)
        {
            ImGui.TextWrapped("Select an entry from the results list to see its details here.");
            return;
        }

        var entry = selectedEntry;

        DrawDetailRow("English", entry.EnglishName);
        DrawDetailRow("Japanese", string.IsNullOrEmpty(entry.JapaneseName) ? "-" : entry.JapaneseName);

        if (!string.IsNullOrEmpty(entry.Romaji))
            DrawDetailRow("Romaji", entry.Romaji);

        if (!string.IsNullOrEmpty(entry.Role))
            DrawDetailRow("Role", entry.Role);

        if (!string.IsNullOrEmpty(entry.Abbreviation))
            DrawDetailRow("Abbreviation", entry.Abbreviation);

        if (!string.IsNullOrEmpty(entry.PFShortName))
            DrawDetailRow("PF Short", entry.PFShortName);

        if (!string.IsNullOrEmpty(entry.Description))
        {
            ImGui.Spacing();
            ImGui.TextDisabled("Description");
            ImGui.TextWrapped(entry.Description);
        }

        if (entry.Aliases.Count > 0)
        {
            ImGui.Spacing();
            ImGui.TextDisabled("Aliases");
            ImGui.TextWrapped(string.Join(", ", entry.Aliases));
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var hasJapanese = !string.IsNullOrEmpty(entry.JapaneseName);

        if (!hasJapanese)
            ImGui.BeginDisabled();

        if (ImGui.Button("Copy Japanese"))
            ImGui.SetClipboardText(entry.JapaneseName);

        ImGui.SameLine();

        if (ImGui.Button("Say in Chat"))
            Plugin.Framework.RunOnFrameworkThread(() => ChatSendingService.SendMessage(entry.JapaneseName));

        if (!hasJapanese)
            ImGui.EndDisabled();

        var hasPfShort = !string.IsNullOrEmpty(entry.PFShortName);

        if (hasPfShort)
        {
            ImGui.SameLine();

            if (ImGui.Button("Copy PF Shorthand"))
                ImGui.SetClipboardText(entry.PFShortName);
        }
    }

    private static void DrawDetailRow(string label, string value)
    {
        ImGui.TextDisabled($"{label}:");
        ImGui.SameLine();
        ImGui.TextWrapped(value);
    }

    private List<DictionaryEntry> GetFilteredEntries()
    {
        IEnumerable<DictionaryEntry> filtered = string.IsNullOrWhiteSpace(searchQuery)
            ? repository.GetAllEntries()
            : repository.Search(searchQuery);

        if (selectedCategory != null)
            filtered = filtered.Where(e => e.Category == selectedCategory);

        return filtered
            .OrderBy(e => e.Category)
            .ThenBy(e => e.EnglishName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string GetCategoryDisplayName(DictionaryCategory category) => category switch
    {
        DictionaryCategory.Jobs => "Jobs",
        DictionaryCategory.PFShorthand => "PF Shorthand",
        DictionaryCategory.CommonPFTerms => "Common PF Terms",
        DictionaryCategory.Communication => "Communication",
        DictionaryCategory.Mechanics => "Mechanics",
        _ => category.ToString(),
    };

    // ------------------------------------------------------------------
    // Quick Translate tab
    // ------------------------------------------------------------------

    private void DrawQuickTranslateTab()
    {
        ImGui.TextDisabled("Settings");
        ImGui.Separator();

        ImGui.Text("Provider");

        var provider = plugin.Configuration.TranslationProvider;

        if (ImGui.RadioButton("OpenAI", ref provider, TranslationProviderType.OpenAi))
        {
            plugin.Configuration.TranslationProvider = provider;
            plugin.Configuration.Save();
            plugin.TranslationService.ResetStatus();
        }

        ImGui.SameLine();

        if (ImGui.RadioButton("Claude", ref provider, TranslationProviderType.Claude))
        {
            plugin.Configuration.TranslationProvider = provider;
            plugin.Configuration.Save();
            plugin.TranslationService.ResetStatus();
        }

        ImGui.SameLine();

        if (ImGui.RadioButton("Free (Google Translate)", ref provider, TranslationProviderType.GoogleTranslate))
        {
            plugin.Configuration.TranslationProvider = provider;
            plugin.Configuration.Save();
            plugin.TranslationService.ResetStatus();
        }

        if (provider == TranslationProviderType.Claude)
        {
            ImGui.Text("Claude API Key");
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputText("##AnthropicApiKey", ref anthropicApiKeyInput, 256, ImGuiInputTextFlags.Password))
            {
                plugin.Configuration.AnthropicApiKey = anthropicApiKeyInput;
                plugin.Configuration.Save();
            }
        }
        else if (provider == TranslationProviderType.GoogleTranslate)
        {
            ImGui.TextWrapped("No API key required. Uses a free, unofficial Google Translate endpoint - " +
                "plain literal translation only, without FFXIV-specific terminology.");
        }
        else
        {
            ImGui.Text("OpenAI API Key");
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputText("##OpenAiApiKey", ref openAiApiKeyInput, 256, ImGuiInputTextFlags.Password))
            {
                plugin.Configuration.OpenAiApiKey = openAiApiKeyInput;
                plugin.Configuration.Save();
            }
        }

        if (ImGui.Button(isTestingConnection ? "Testing..." : "Test Connection"))
        {
            if (!isTestingConnection)
                _ = TestConnectionAsync();
        }

        ImGui.SameLine();
        ImGui.Text("Status:");
        ImGui.SameLine();
        DrawOpenAiStatusText();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextDisabled("Translate");
        ImGui.Separator();

        ImGui.RadioButton("EN -> JP", ref translationDirection, TranslationDirection.EnglishToJapanese);
        ImGui.SameLine();
        ImGui.RadioButton("JP -> EN", ref translationDirection, TranslationDirection.JapaneseToEnglish);

        ImGui.Text("Input");
        ImGui.InputTextMultiline("##QuickTranslateInput", ref translateInput, 1000, new Vector2(-1, 80));

        if (ImGui.Button(isTranslating ? "Translating..." : "Translate"))
        {
            if (!isTranslating && !string.IsNullOrWhiteSpace(translateInput))
                _ = TranslateAsync();
        }

        ImGui.SameLine();

        if (ImGui.Button("Copy"))
            ImGui.SetClipboardText(translateOutput);

        ImGui.Text("Output");
        ImGui.InputTextMultiline("##QuickTranslateOutput", ref translateOutput, 1000, new Vector2(-1, 80), ImGuiInputTextFlags.ReadOnly);

        if (!string.IsNullOrEmpty(translateError))
        {
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), translateError);
        }
    }

    private async Task TestConnectionAsync()
    {
        isTestingConnection = true;

        try
        {
            await plugin.TranslationService.TestConnectionAsync();
        }
        finally
        {
            isTestingConnection = false;
        }
    }

    private async Task TranslateAsync()
    {
        isTranslating = true;
        translateError = null;

        try
        {
            var result = await plugin.TranslationService.TranslateAsync(translateInput, translationDirection);

            translateOutput = result.Text;

            if (!result.Success)
                translateError = result.ErrorMessage;
        }
        finally
        {
            isTranslating = false;
        }
    }
}
