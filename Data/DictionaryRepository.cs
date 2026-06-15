using System.Collections.Generic;
using System.Linq;
using JPRaidDictionary.Models;

namespace JPRaidDictionary.Data;

/// <summary>
/// Holds the in-memory dictionary dataset and exposes simple read/search
/// operations. All data is bundled locally - no networking is performed.
///
/// To add more entries in the future (e.g. new raid tier terminology),
/// extend the relevant Build*() method below.
/// </summary>
public class DictionaryRepository
{
    private readonly List<DictionaryEntry> entries;

    public DictionaryRepository()
    {
        entries = new List<DictionaryEntry>();
        entries.AddRange(BuildRaidTerms());
        entries.AddRange(BuildJobs());
        entries.AddRange(BuildPfShorthand());
        entries.AddRange(BuildCommonPfTerms());
    }

    /// <summary>Returns every entry in the dictionary.</summary>
    public IReadOnlyList<DictionaryEntry> GetAllEntries() => entries;

    /// <summary>Returns every entry belonging to the given category.</summary>
    public IReadOnlyList<DictionaryEntry> GetByCategory(DictionaryCategory category)
        => entries.Where(e => e.Category == category).ToList();

    /// <summary>
    /// Returns every entry whose English, Japanese, Romaji, abbreviation,
    /// PF short name, description, or aliases contain the given query
    /// (case-insensitive). An empty/whitespace query returns all entries.
    /// </summary>
    public IReadOnlyList<DictionaryEntry> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return entries;

        return entries.Where(e => e.Matches(query)).ToList();
    }

    private static IEnumerable<DictionaryEntry> BuildRaidTerms()
    {
        return new List<DictionaryEntry>
        {
            new()
            {
                Category = DictionaryCategory.RaidTerms,
                EnglishName = "Stack",
                JapaneseName = "頭割り",
                Romaji = "Atamawari",
                Description = "Shared damage mechanic - the party must stack together to split incoming raid-wide damage.",
                Aliases = new List<string> { "stack" },
            },
            new()
            {
                Category = DictionaryCategory.RaidTerms,
                EnglishName = "Spread",
                JapaneseName = "散開",
                Romaji = "Sankai",
                Description = "Mechanic that requires players to separate from each other to avoid sharing damage.",
                Aliases = new List<string> { "spread" },
            },
            new()
            {
                Category = DictionaryCategory.RaidTerms,
                EnglishName = "Partners",
                JapaneseName = "ペア",
                Romaji = "Pea",
                Description = "Mechanic that requires players to pair up with a partner, often to share damage.",
                Aliases = new List<string> { "partner", "partners", "pair" },
            },
            new()
            {
                Category = DictionaryCategory.RaidTerms,
                EnglishName = "Clock Spots",
                JapaneseName = "時計",
                Romaji = "Tokei",
                Description = "Positions around the arena named after clock numbers (12, 3, 6, 9, etc.) used for callouts.",
                Aliases = new List<string> { "clock", "clock spots" },
            },
            new()
            {
                Category = DictionaryCategory.RaidTerms,
                EnglishName = "Light Party",
                JapaneseName = "ライトパーティ",
                Romaji = "Raito Paati",
                Description = "A subgroup of one tank, one healer, and two DPS, used when a mechanic splits the raid.",
                Aliases = new List<string> { "light party", "lp" },
            },
            new()
            {
                Category = DictionaryCategory.RaidTerms,
                EnglishName = "North Relative",
                JapaneseName = "北基準",
                Romaji = "Kita Kijun",
                Description = "A positioning callout based on the arena's fixed north, regardless of camera direction.",
                Aliases = new List<string> { "north relative" },
            },
            new()
            {
                Category = DictionaryCategory.RaidTerms,
                EnglishName = "Waymark Relative",
                JapaneseName = "マーカー基準",
                Romaji = "Maaka Kijun",
                Description = "A positioning callout based on placed waymarks (A/B/C/D, 1/2/3/4) instead of fixed directions.",
                Aliases = new List<string> { "waymark", "marker", "waymark relative" },
            },
            new()
            {
                Category = DictionaryCategory.RaidTerms,
                EnglishName = "Bait",
                JapaneseName = "誘導",
                Romaji = "Yuudou",
                Description = "To intentionally draw an enemy attack, AoE, or marker toward yourself or away from others.",
                Aliases = new List<string> { "bait" },
            },
            new()
            {
                Category = DictionaryCategory.RaidTerms,
                EnglishName = "Tank LB",
                JapaneseName = "タンクLB",
                Romaji = string.Empty,
                Description = "Tank limit break, typically used to mitigate or survive heavy raid-wide damage.",
                Aliases = new List<string> { "tank lb" },
            },
            new()
            {
                Category = DictionaryCategory.RaidTerms,
                EnglishName = "Healer LB",
                JapaneseName = "ヒラLB",
                Romaji = string.Empty,
                Description = "Healer limit break, often used as emergency raid-wide healing.",
                Aliases = new List<string> { "healer lb" },
            },
            new()
            {
                Category = DictionaryCategory.RaidTerms,
                EnglishName = "Melee LB",
                JapaneseName = "近接LB",
                Romaji = string.Empty,
                Description = "Melee DPS limit break, usually a high-damage burst used for enrage timers.",
                Aliases = new List<string> { "melee lb" },
            },
        };
    }

    private static IEnumerable<DictionaryEntry> BuildJobs()
    {
        // Local helper to keep each job declaration to a single line.
        static DictionaryEntry Job(string english, string abbreviation, string japanese, string pfShort, string role)
        {
            return new DictionaryEntry
            {
                Category = DictionaryCategory.Jobs,
                EnglishName = english,
                JapaneseName = japanese,
                Romaji = string.Empty,
                Abbreviation = abbreviation,
                PFShortName = pfShort,
                Role = role,
                Description = $"{role} job.",
                Aliases = new List<string> { english, abbreviation, japanese, pfShort },
            };
        }

        return new List<DictionaryEntry>
        {
            // Tank
            Job("Paladin", "PLD", "ナイト", "ナ", "Tank"),
            Job("Warrior", "WAR", "戦士", "戦", "Tank"),
            Job("Dark Knight", "DRK", "暗黒騎士", "暗", "Tank"),
            Job("Gunbreaker", "GNB", "ガンブレイカー", "ガ", "Tank"),

            // Healer
            Job("White Mage", "WHM", "白魔道士", "白", "Healer"),
            Job("Scholar", "SCH", "学者", "学", "Healer"),
            Job("Astrologian", "AST", "占星術師", "占", "Healer"),
            Job("Sage", "SGE", "賢者", "賢", "Healer"),

            // Melee DPS
            Job("Monk", "MNK", "モンク", "モ", "Melee DPS"),
            Job("Dragoon", "DRG", "竜騎士", "竜", "Melee DPS"),
            Job("Ninja", "NIN", "忍者", "忍", "Melee DPS"),
            Job("Samurai", "SAM", "侍", "侍", "Melee DPS"),
            Job("Reaper", "RPR", "リーパー", "リ", "Melee DPS"),
            Job("Viper", "VPR", "ヴァイパー", "ヴ", "Melee DPS"),

            // Physical Ranged
            Job("Bard", "BRD", "吟遊詩人", "詩", "Physical Ranged"),
            Job("Machinist", "MCH", "機工士", "機", "Physical Ranged"),
            Job("Dancer", "DNC", "踊り子", "踊", "Physical Ranged"),

            // Magical Ranged
            Job("Black Mage", "BLM", "黒魔道士", "黒", "Magical Ranged"),
            Job("Summoner", "SMN", "召喚士", "召", "Magical Ranged"),
            Job("Red Mage", "RDM", "赤魔道士", "赤", "Magical Ranged"),
            Job("Pictomancer", "PCT", "ピクトマンサー", "ピ", "Magical Ranged"),
            Job("Blue Mage", "BLU", "青魔道士", "青", "Magical Ranged"),
        };
    }

    private static IEnumerable<DictionaryEntry> BuildPfShorthand()
    {
        // Local helper for shorthand entries. When the shorthand itself is
        // written in Japanese, it is stored as JapaneseName and the English
        // "meaning" becomes the EnglishName shown as the primary result.
        static DictionaryEntry Shorthand(string term, bool isJapaneseTerm, string meaning, string description, params string[] extraAliases)
        {
            var aliases = new List<string> { term, meaning };
            aliases.AddRange(extraAliases);

            return new DictionaryEntry
            {
                Category = DictionaryCategory.PFShorthand,
                EnglishName = isJapaneseTerm ? meaning : term,
                JapaneseName = isJapaneseTerm ? term : string.Empty,
                Romaji = string.Empty,
                Description = description,
                Aliases = aliases.Distinct().ToList(),
            };
        }

        return new List<DictionaryEntry>
        {
            Shorthand("TH", false, "Tanks + Healers", "PF shorthand requesting tanks and healers only.", "tanks and healers"),
            Shorthand("DPS", false, "DPS only", "PF shorthand requesting DPS roles only.", "dps only"),
            Shorthand("近接", true, "Melee DPS", "PF shorthand for melee DPS jobs.", "melee"),
            Shorthand("遠隔", true, "Physical Ranged", "PF shorthand for physical ranged DPS jobs."),
            Shorthand("キャス", true, "Caster", "PF shorthand for magical ranged (caster) DPS jobs.", "caster"),
            Shorthand("レンジ", true, "Physical Ranged", "PF shorthand for physical ranged DPS jobs (from 'range').", "range"),
            Shorthand("ヒラ", true, "Healer", "PF shorthand for healer (from 'ヒーラー').", "healer"),
            Shorthand("タンク", true, "Tank", "PF shorthand for tank.", "tank"),
            Shorthand("MT", false, "Main Tank", "PF shorthand for main tank.", "main tank"),
            Shorthand("ST", false, "Off Tank", "PF shorthand for off tank / sub tank.", "off tank", "sub tank"),
        };
    }

    private static IEnumerable<DictionaryEntry> BuildCommonPfTerms()
    {
        static DictionaryEntry Term(string english, string japanese, string romaji, string description, params string[] extraAliases)
        {
            var aliases = new List<string> { english.ToLowerInvariant(), japanese };
            aliases.AddRange(extraAliases);

            return new DictionaryEntry
            {
                Category = DictionaryCategory.CommonPFTerms,
                EnglishName = english,
                JapaneseName = japanese,
                Romaji = romaji,
                Description = description,
                Aliases = aliases.Distinct().ToList(),
            };
        }

        return new List<DictionaryEntry>
        {
            Term("Practice", "練習", "Renshuu", "PF tag indicating the party is practicing a fight, not aiming for a clear."),
            Term("Clear Party", "クリア目的", "Kuria Mokuteki", "PF tag indicating the party's goal is to clear the duty."),
            Term("Duty Complete", "コン済", "Konzumi", "Indicates the duty has already been completed/unlocked this week."),
            Term("First Time", "初見", "Shoken", "Indicates this is the player's first time attempting the fight."),
            Term("Farm", "周回", "Shuukai", "Indicates the party is farming the duty for loot after clearing."),
            Term("Mistake", "ミス", "Misu", "Used to call out a mistake made during a pull."),
            Term("Wipe", "ワイプ", "Waipu", "The whole party has died and the encounter has reset."),
            Term("Learning Party", "練習PT", "Renshuu PT", "PF tag for a party focused on learning mechanics rather than clearing."),
            Term("Reclear", "消化", "Shouka", "Indicates the party is doing a routine reclear of an already-cleared duty."),

            // Loot
            Term("Free Loot", "フリロ", "Furiro", "All drops can be Need-rolled freely by anyone in the party.", "free", "フリーロット", "ロット自由"),
            Term("Left to Right", "左取り抜け", "Hidari Dori Nuke", "Loot is distributed in turn order based on party list position, from left to right.", "ltr", "loot order", "上から順番", "順番"),
            Term("Need", "ニード", "Niido", "Roll Need on an item you intend to use yourself."),
            Term("Greed", "グリード", "Guriido", "Roll Greed on an item you don't need but want to sell or desynth."),
            Term("Pass", "パス", "Pasu", "Decline to roll on an item, letting others have priority."),
        };
    }
}
