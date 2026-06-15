using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using JPRaidDictionary.Data;
using JPRaidDictionary.Models;

namespace JPRaidDictionary.Windows;

/// <summary>
/// The main dictionary window: a search box across the top, a category list
/// on the left, a results list in the middle, and the selected entry's
/// details (including a "Copy Japanese" button) on the right.
/// </summary>
public class MainWindow : Window, IDisposable
{
    private readonly DictionaryRepository repository;

    private string searchQuery = string.Empty;
    private DictionaryCategory? selectedCategory;
    private DictionaryEntry? selectedEntry;

    public MainWindow(Plugin plugin, DictionaryRepository repository)
        : base("JP Raid Dictionary##JPRaidDictionaryMainWindow")
    {
        this.repository = repository;

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
        DictionaryCategory.RaidTerms => "Raid Terms",
        DictionaryCategory.Jobs => "Jobs",
        DictionaryCategory.PFShorthand => "PF Shorthand",
        DictionaryCategory.CommonPFTerms => "Common PF Terms",
        _ => category.ToString(),
    };
}
