using System;
using System.Collections.Generic;

namespace JPRaidDictionary.Models;

/// <summary>
/// A single dictionary entry. Used for raid terminology, job abbreviations,
/// Party Finder shorthand, and common PF phrases alike.
///
/// Not every field is relevant to every category - e.g. <see cref="Romaji"/>
/// is mostly used for Raid Terms, while <see cref="Abbreviation"/>,
/// <see cref="PFShortName"/> and <see cref="Role"/> are mostly used for Jobs.
/// Unused fields are simply left as empty strings.
/// </summary>
public class DictionaryEntry
{
    public DictionaryCategory Category { get; set; }

    public string EnglishName { get; set; } = string.Empty;

    public string JapaneseName { get; set; } = string.Empty;

    public string Romaji { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Extra search terms (e.g. "stack", "lp", "白", "ヒラ"). All searches
    /// are matched against this list in addition to the named fields above.
    /// </summary>
    public List<string> Aliases { get; set; } = new();

    /// <summary>Job abbreviation, e.g. "WHM". Empty for non-job entries.</summary>
    public string Abbreviation { get; set; } = string.Empty;

    /// <summary>Role grouping, e.g. "Tank", "Healer". Empty for non-job entries.</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Single-character PF shorthand for a job, e.g. "白". Empty for non-job entries.</summary>
    public string PFShortName { get; set; } = string.Empty;

    /// <summary>
    /// Returns true if the given search query matches any searchable field
    /// on this entry (English, Japanese, Romaji, abbreviation, role,
    /// PF short name, description, or any alias).
    /// </summary>
    public bool Matches(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return true;

        query = query.Trim();

        if (Contains(EnglishName, query)) return true;
        if (Contains(JapaneseName, query)) return true;
        if (Contains(Romaji, query)) return true;
        if (Contains(Abbreviation, query)) return true;
        if (Contains(Role, query)) return true;
        if (Contains(PFShortName, query)) return true;
        if (Contains(Description, query)) return true;

        foreach (var alias in Aliases)
        {
            if (Contains(alias, query))
                return true;
        }

        return false;
    }

    private static bool Contains(string source, string query)
    {
        return !string.IsNullOrEmpty(source) &&
               source.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
