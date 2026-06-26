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
/// list, results list, and entry details) and the "Quick Translate" tab
/// (AI-powered EN/JP translation with configurable providers).
/// </summary>
public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly DictionaryRepository repository;

    // --- Dictionary tab state ---
    private string searchQuery = string.Empty;
    private DictionaryCategory? selectedCategory;
    private DictionaryEntry? selectedEntry;

    // --- Provider settings state ---
    private static readonly TranslationProviderType[] AiProviders = new[]
    {
        TranslationProviderType.OpenAi,
        TranslationProviderType.Claude,
        TranslationProviderType.OpenRouter,
        TranslationProviderType.Groq,
        TranslationProviderType.TogetherAi,
        TranslationProviderType.CustomOpenAi,
    };

    private static readonly string[] AiProviderNames = new[]
    {
        "OpenAI",
        "Claude (Anthropic)",
        "OpenRouter",
        "Groq",
        "Together AI",
        "Custom (OpenAI-Compatible)",
    };

    private int selectedAiProviderIndex;
    private string apiKeyInput = string.Empty;
    private string modelInput = string.Empty;
    private string customBaseUrlInput = string.Empty;
    private bool isTestingConnection;

    // --- Model fetch state ---
    private List<string> fetchedModels = new();
    private bool isFetchingModels;
    private string? fetchModelsError;
    private string modelFilter = string.Empty;

    // --- Quick Translate tab state ---
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

        var currentProvider = plugin.Configuration.TranslationProvider;
        selectedAiProviderIndex = Array.IndexOf(AiProviders, currentProvider);
        if (selectedAiProviderIndex < 0)
            selectedAiProviderIndex = 0;

        LoadProviderInputs(AiProviders[selectedAiProviderIndex]);

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
        DrawStatusText();

        ImGui.SameLine();
        ImGui.TextDisabled("|");
        ImGui.SameLine();

        ImGui.TextDisabled("Tip: use /jpt <message> to translate & send to chat");
    }

    private void DrawStatusText()
    {
        var (text, color) = GetStatusDisplay(plugin.TranslationService.Status);
        ImGui.TextColored(color, text);
    }

    private static (string Text, Vector4 Color) GetStatusDisplay(ApiConnectionStatus status) => status switch
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

        if (ImGui.BeginTabBar("##ProviderTabs"))
        {
            if (ImGui.BeginTabItem("API Keys"))
            {
                if (plugin.Configuration.TranslationProvider == TranslationProviderType.GoogleTranslate)
                {
                    plugin.Configuration.TranslationProvider = AiProviders[selectedAiProviderIndex];
                    plugin.Configuration.Save();
                    plugin.TranslationService.ResetStatus();
                }

                DrawAiProviderSettings();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Google"))
            {
                if (plugin.Configuration.TranslationProvider != TranslationProviderType.GoogleTranslate)
                {
                    plugin.Configuration.TranslationProvider = TranslationProviderType.GoogleTranslate;
                    plugin.Configuration.Save();
                    plugin.TranslationService.ResetStatus();
                }

                DrawGoogleSettings();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.Spacing();

        if (ImGui.Button(isTestingConnection ? "Testing..." : "Test Connection"))
        {
            if (!isTestingConnection)
                _ = TestConnectionAsync();
        }

        ImGui.SameLine();
        ImGui.Text("Status:");
        ImGui.SameLine();
        DrawStatusText();

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

    // ------------------------------------------------------------------
    // AI Provider settings (API Keys tab)
    // ------------------------------------------------------------------

    private void DrawAiProviderSettings()
    {
        ImGui.Text("Provider");
        ImGui.SetNextItemWidth(-1);

        if (ImGui.BeginCombo("##ProviderCombo", AiProviderNames[selectedAiProviderIndex]))
        {
            for (var i = 0; i < AiProviderNames.Length; i++)
            {
                var isSelected = i == selectedAiProviderIndex;
                if (ImGui.Selectable(AiProviderNames[i], isSelected))
                {
                    selectedAiProviderIndex = i;
                    var newProvider = AiProviders[i];
                    plugin.Configuration.TranslationProvider = newProvider;
                    plugin.Configuration.Save();
                    plugin.TranslationService.ResetStatus();
                    LoadProviderInputs(newProvider);
                }

                if (isSelected)
                    ImGui.SetItemDefaultFocus();
            }

            ImGui.EndCombo();
        }

        var currentProvider = AiProviders[selectedAiProviderIndex];

        if (currentProvider == TranslationProviderType.CustomOpenAi)
        {
            ImGui.Text("Base URL");
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputTextWithHint("##BaseUrl", "https://example.com/v1", ref customBaseUrlInput, 512))
            {
                plugin.Configuration.CustomOpenAiBaseUrl = customBaseUrlInput;
                plugin.Configuration.Save();
            }
        }

        ImGui.Text("API Key");
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputText("##ApiKey", ref apiKeyInput, 256, ImGuiInputTextFlags.Password))
        {
            plugin.Configuration.SetApiKey(currentProvider, apiKeyInput);
            plugin.Configuration.Save();
        }

        ImGui.Text("Model");

        ImGui.SetNextItemWidth(-110);
        if (ImGui.InputTextWithHint("##Model", GetModelHint(currentProvider), ref modelInput, 256))
        {
            plugin.Configuration.SetModel(currentProvider, modelInput);
            plugin.Configuration.Save();
        }

        ImGui.SameLine();

        if (isFetchingModels)
            ImGui.BeginDisabled();

        if (ImGui.Button(isFetchingModels ? "Fetching..." : "Fetch Models"))
        {
            if (!isFetchingModels)
                _ = FetchModelsAsync();
        }

        if (isFetchingModels)
            ImGui.EndDisabled();

        if (fetchedModels.Count > 0)
        {
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("##ModelFilter", "Filter models...", ref modelFilter, 256);

            var filtered = string.IsNullOrEmpty(modelFilter)
                ? fetchedModels
                : fetchedModels.Where(m => m.Contains(modelFilter, StringComparison.OrdinalIgnoreCase)).ToList();

            var listHeight = Math.Min(120, filtered.Count * ImGui.GetTextLineHeightWithSpacing() + 8);
            ImGui.BeginChild("##ModelList", new Vector2(-1, listHeight), true);

            foreach (var model in filtered)
            {
                if (ImGui.Selectable(model, model == modelInput))
                {
                    modelInput = model;
                    plugin.Configuration.SetModel(currentProvider, model);
                    plugin.Configuration.Save();
                }
            }

            ImGui.EndChild();
        }

        if (!string.IsNullOrEmpty(fetchModelsError))
        {
            ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), fetchModelsError);
        }
    }

    // ------------------------------------------------------------------
    // Google Translate settings (Google tab)
    // ------------------------------------------------------------------

    private void DrawGoogleSettings()
    {
        ImGui.TextWrapped("No API key required. Uses a free, unofficial Google Translate endpoint - " +
            "plain literal translation only, without FFXIV-specific terminology.");
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private void LoadProviderInputs(TranslationProviderType provider)
    {
        apiKeyInput = plugin.Configuration.GetApiKey(provider);
        modelInput = plugin.Configuration.GetModel(provider);
        customBaseUrlInput = plugin.Configuration.CustomOpenAiBaseUrl;
        fetchedModels.Clear();
        modelFilter = string.Empty;
        fetchModelsError = null;
    }

    private static string GetModelHint(TranslationProviderType provider) => provider switch
    {
        TranslationProviderType.OpenAi => "e.g. gpt-4o-mini",
        TranslationProviderType.Claude => "e.g. claude-haiku-4-5-20251001",
        TranslationProviderType.OpenRouter => "e.g. google/gemini-2.0-flash-001",
        TranslationProviderType.Groq => "e.g. llama-3.3-70b-versatile",
        TranslationProviderType.TogetherAi => "e.g. meta-llama/Llama-3-8b-chat-hf",
        _ => "model identifier",
    };

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

    private async Task FetchModelsAsync()
    {
        var currentProvider = AiProviders[selectedAiProviderIndex];

        if (string.IsNullOrWhiteSpace(plugin.Configuration.GetApiKey(currentProvider)))
        {
            fetchModelsError = "Please enter an API key first.";
            return;
        }

        if (currentProvider == TranslationProviderType.CustomOpenAi &&
            string.IsNullOrWhiteSpace(plugin.Configuration.CustomOpenAiBaseUrl))
        {
            fetchModelsError = "Please enter a Base URL first.";
            return;
        }

        isFetchingModels = true;
        fetchModelsError = null;
        fetchedModels.Clear();

        try
        {
            fetchedModels = await plugin.TranslationService.FetchModelsAsync();

            if (fetchedModels.Count == 0)
                fetchModelsError = "No models returned. The provider may not support model listing, or the API key may be invalid.";
        }
        catch (Exception ex)
        {
            fetchModelsError = $"Failed to fetch models: {ex.Message}";
        }
        finally
        {
            isFetchingModels = false;
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
