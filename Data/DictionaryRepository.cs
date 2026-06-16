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
        entries.AddRange(BuildJobs());
        entries.AddRange(BuildPfShorthand());
        entries.AddRange(BuildCommonPfTerms());
        entries.AddRange(BuildCommunication());
        entries.AddRange(BuildMechanics());
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

    private static IEnumerable<DictionaryEntry> BuildCommunication()
    {
        static DictionaryEntry Phrase(string english, string japanese, string romaji, string description, params string[] extraAliases)
        {
            var aliases = new List<string> { english.ToLowerInvariant() };
            if (!string.IsNullOrEmpty(japanese)) aliases.Add(japanese);
            aliases.AddRange(extraAliases);
            return new DictionaryEntry
            {
                Category = DictionaryCategory.Communication,
                EnglishName = english,
                JapaneseName = japanese,
                Romaji = romaji,
                Description = description,
                Aliases = aliases.Distinct().ToList(),
            };
        }

        return new List<DictionaryEntry>
        {
            Phrase("Hello / Nice to meet you", "よろしくお願いします", "Yoroshiku onegai shimasu",
                "Standard opening greeting in a party or raid, meaning 'I'm in your care'. Casually shortened to よろしく.",
                "hello", "hi", "yoroshiku", "よろしく", "greet"),

            Phrase("Good game", "お疲れ様でした", "Otsukaresama deshita",
                "Standard end-of-raid phrase meaning 'good work' or 'good game'. Casual forms: お疲れ様 or おつ (otsu).",
                "gg", "gj", "good work", "otsu", "おつ", "otsukare"),

            Phrase("Nice", "ナイス", "Naisu",
                "Nice! Borrowed from English, used to praise a good play or clutch save.",
                "nice", "good"),

            Phrase("Thank you", "ありがとうございます", "Arigatou gozaimasu",
                "Standard thank you. Casual form: ありがとう (arigatou). Commonly said after a clear or a clutch heal.",
                "thank you", "thanks", "ty", "thx", "arigatou", "ありがとう"),

            Phrase("Sorry / Excuse me", "すみません", "Sumimasen",
                "General apology and excuse me. ごめんなさい (gomen nasai) is more personal. 申し訳ない (moushiwake nai) is stronger and more formal.",
                "sorry", "excuse me", "gomen", "ごめんなさい", "申し訳ない"),

            Phrase("My bad", "ミスりました", "Misu rimashita",
                "Quick apology for a mistake. Casual form: ミスった (misutta). Often typed as 'mb' in English chat.",
                "mb", "my bad", "ミスった", "misutta"),

            Phrase("Ready", "マルです", "Maru desu",
                "I'm ready. The ⭕ symbol (maru) signals readiness before a pull or mechanic check.",
                "ready", "rdy", "マル", "⭕️", "⭕"),

            Phrase("Understood", "了解", "Ryoukai",
                "OK / roger that. はい (hai) is a simpler 'yes'. りょ (ryo) is a very casual shortening often typed in-chat.",
                "ok", "okay", "understood", "roger", "はい", "りょ", "ryo"),

            Phrase("Please wait", "少々お待ちください", "Shoushou omachi kudasai",
                "Polite 'please wait a moment'. Shorter form: おまちを (omachi wo).",
                "wait", "please wait", "omachi", "おまちを"),

            Phrase("BRB / Bio", "ちょっとトイレ", "Chotto toire",
                "Be right back / bathroom break. Also ちょっと離席 (chotto riseki) for 'stepping away briefly'.",
                "brb", "bio", "afk", "toilet", "ちょっと離席", "riseki"),

            Phrase("Back", "戻りました", "Modori mashita",
                "I'm back. Note: the in-game directional cue 'Back' refers to the rear of a boss, not this phrase.",
                "back", "im back", "i'm back", "modori"),

            Phrase("Go / Pull", "いきます", "Ikimasu",
                "Going in / pulling now. Often said by the tank just before engaging the boss.",
                "go", "pull", "let's go"),

            Phrase("One more", "もう一回", "Mou ikkai",
                "Let's go one more time. Also もう一度 (mou ichido).",
                "one more", "again", "retry", "もう一度", "mou ichido"),

            Phrase("Last run", "ラスト", "Rasuto",
                "This is the last attempt for this session.",
                "last", "last run", "final"),

            Phrase("Quick Chat", "クイックチャット", "Kuikku Chatto",
                "FFXIV's in-game Quick Chat feature. Preset phrases sent via the chat window or radial menu — useful for fast communication without typing.",
                "qc", "quickchat"),
        };
    }

    private static IEnumerable<DictionaryEntry> BuildMechanics()
    {
        static DictionaryEntry Mech(string english, string japanese, string romaji, string description, params string[] extraAliases)
        {
            var aliases = new List<string> { english.ToLowerInvariant() };
            if (!string.IsNullOrEmpty(japanese)) aliases.Add(japanese);
            aliases.AddRange(extraAliases);
            return new DictionaryEntry
            {
                Category = DictionaryCategory.Mechanics,
                EnglishName = english,
                JapaneseName = japanese,
                Romaji = romaji,
                Description = description,
                Aliases = aliases.Distinct().ToList(),
            };
        }

        return new List<DictionaryEntry>
        {
            // Core mechanics
            Mech("Stack", "頭割り", "Atamawari",
                "Shared damage — the party stacks together to split incoming damage.",
                "stack"),
            Mech("Spread", "散開", "Sankai",
                "Players separate from each other to avoid sharing damage.",
                "spread"),
            Mech("Partners", "ペア", "Pea",
                "Players pair up with a specific partner to share targeted damage.",
                "partner", "pair"),
            Mech("Bait", "誘導", "Yuudou",
                "Intentionally drawing an attack, AoE, or marker onto yourself or away from others.",
                "bait"),
            Mech("Channeling / Casting", "詠唱", "Eichou",
                "The boss is casting an ability. The cast bar fills until the attack fires.",
                "cast", "casting", "channel", "channeling"),
            Mech("Aggro", "ヘイト", "Heito",
                "Threat / aggro. The boss targets whoever has the highest ヘイト (hate). Tanks manage this with tank stances.",
                "aggro", "hate", "threat"),
            Mech("Mitigation", "軽減", "Keigen",
                "Damage-reduction cooldowns (shields, reduced damage%). Healers and tanks coordinate 軽減 to survive raidwides.",
                "mit", "mitigations", "cooldown"),
            Mech("Tank Invuln", "無敵", "Muteki",
                "Tank invulnerability skill — makes the tank immune to damage for a short window. Used to survive lethal tank busters.",
                "invuln", "invulnerability"),
            Mech("Debuff", "デバフ", "Debafu",
                "A negative status effect applied to a player. Often the key mechanic determining positioning or tether assignments.",
                "debuff", "status"),
            Mech("Knockback", "ノクバ", "Nokuba",
                "An attack that launches players away. Shortened from ノックバック (nokkubakku). Use knockback-resist to stay in place.",
                "kb", "knock back", "ノックバック"),

            // Attack types
            Mech("Tank Buster", "タンク強攻撃", "Tanku kyou kougeki",
                "A heavy single-target attack on the tank. JP short form: タン強 (tan kyou). Requires a cooldown, invuln, or tank swap.",
                "tb", "タン強", "tan kyou"),
            Mech("Raidwide", "全体攻撃", "Zentai kougeki",
                "An AoE attack that hits the entire party. Requires raid mitigation and healing to survive.",
                "raid wide", "全体", "aoe"),
            Mech("Conal/Cone", "扇", "Ougi",
                "A fan/cone-shaped AoE fired in front of the boss or a player. Stand to the sides to avoid it.",
                "cone", "fan"),
            Mech("Defam (Circular AoE)", "円範囲", "En han'i",
                "A large circular AoE centered on a player that must be isolated from others to avoid spreading damage or a debuff.",
                "defam", "defamation", "circular aoe", "circle"),
            Mech("Beam (Line AoE)", "直線AoE", "Chokusen AoE",
                "A straight-line AoE. Stand perpendicular to avoid it.",
                "beam", "line aoe", "laser", "chokusen"),
            Mech("Donut", "ドーナツ範囲", "Doonatsu han'i",
                "A ring-shaped AoE — the ring deals damage but the center and far outside are safe.",
                "ring", "doonatsu"),
            Mech("Player Marker", "頭上マーカー", "Zuijou maaka",
                "A marker appearing above a player's head indicating they are targeted by a mechanic. Also simply マーカー.",
                "marker", "head marker", "マーカー"),
            Mech("Waymark", "マーカー", "Maaka",
                "Ground waymarks placed by the party leader (A/B/C/D and 1/2/3/4) used for positional callouts. Same word as player markers.",
                "field marker", "ground marker", "waymarks"),
            Mech("Impact", "着弾", "Chakudan",
                "The moment an attack lands / resolves. Callouts often reference 着弾のタイミング (timing of impact).",
                "land", "resolve"),
            Mech("Drop AoE", "AoE捨て", "AoE sute",
                "Moving to a spot to 'drop' a ground AoE away from the group or into a safe corner. 捨て (sute) means to discard/place.",
                "drop aoe", "aoe drop", "捨て"),
            Mech("Tether", "線", "Sen",
                "A visible line connecting two players or a player to an enemy. Can deal damage, require breaking, or mark a mechanic partner.",
                "tether", "line"),
            Mech("Tower", "塔", "Tou",
                "A ground mechanic that must be stood in ('soaked') by the correct number of players to prevent an explosion.",
                "tower"),
            Mech("Take Tower", "塔踏み", "Tou fumi",
                "Stepping into and soaking a tower. 踏み (fumi) means 'stepping on'.",
                "soak tower", "tower soak"),

            // Positioning
            Mech("Clock Spots", "基本散開 / 8方向散開", "Kihon sankai / Hachi houkou sankai",
                "Standard spread positions at 8 fixed clock directions around the arena (N/NE/E/SE/S/SW/W/NW). JP terms: 基本散開 (kihon sankai) or 8方向散開 (hachi houkou sankai).",
                "clock", "8 direction", "8方向"),
            Mech("Light Party", "MT組 / ST組", "MT gumi / ST gumi",
                "The raid splits into two light parties: MT's group (MT組) and ST's group (ST組), each with one tank, one healer, two DPS. Game term: ライトパーティ.",
                "light party", "lp", "mt組", "st組", "ライトパーティ"),
            Mech("Light Party (MT)", "MT組", "MT gumi",
                "The MT's light party group. Typically contains MT, H1, D1, D3.",
                "mt group", "mt party"),
            Mech("Light Party (ST)", "ST組", "ST gumi",
                "The ST's light party group. Typically contains ST, H2, D2, D4.",
                "st group", "st party"),
            Mech("Clockwise", "時計回り", "Tokei mawari",
                "Move or rotate in a clockwise direction.",
                "cw"),
            Mech("Counterclockwise", "反時計回り", "Han tokei mawari",
                "Move or rotate in a counterclockwise direction.",
                "ccw"),
            Mech("Safe Spot", "安地", "Aji",
                "The safe area during an AoE pattern. Find and move to the 安地 to avoid taking damage.",
                "safe", "safe zone"),
            Mech("Middle", "中央", "Chuuou",
                "The center of the arena.",
                "center", "mid"),
            Mech("Cardinal", "十字", "Juuji",
                "The four cardinal directions: N / S / E / W, forming a cross (十字) pattern.",
                "cardinal directions", "cross"),
            Mech("Intercardinal", "X字", "Ekkusuji",
                "The four intercardinal directions: NE / NW / SE / SW, forming an X pattern.",
                "diagonal"),
            Mech("North", "北", "Kita", "North. The fixed arena north used for positional callouts.", "n"),
            Mech("South", "南", "Minami", "South.", "s"),
            Mech("East", "東", "Higashi", "East.", "e"),
            Mech("West", "西", "Nishi", "West.", "w"),
            Mech("Northeast", "北東", "Hokutou", "Northeast.", "ne"),
            Mech("Northwest", "北西", "Hokusei", "Northwest.", "nw"),
            Mech("Southeast", "南東", "Nantou", "Southeast.", "se"),
            Mech("Southwest", "南西", "Nansei", "Southwest.", "sw"),
            Mech("Waymark Side", "~側", "~ gawa",
                "The side of the arena near a specific waymark. e.g. A側 (A-gawa) = the A-waymark side. Used in callouts like 'A側に集合' (gather at A side).",
                "side", "a side", "b side", "c side", "d side", "1 side", "2 side", "3 side", "4 side", "gawa", "側"),

            // Positional reference
            Mech("North Relative", "北基準", "Kita Kijun",
                "Positioning based on the arena's fixed north, regardless of camera direction.",
                "north relative", "north based"),
            Mech("Waymark Relative", "マーカー基準", "Maaka Kijun",
                "Positioning based on placed waymarks (A/B/C/D, 1/2/3/4) instead of fixed directions.",
                "waymark relative", "marker relative"),

            // Limit Breaks
            Mech("Tank LB", "タンクLB", string.Empty,
                "Tank limit break, used to mitigate or survive heavy raidwide damage.",
                "tank lb"),
            Mech("Healer LB", "ヒラLB", string.Empty,
                "Healer limit break, used as emergency raid-wide healing after a wipe-level hit.",
                "healer lb"),
            Mech("Melee LB", "近接LB", string.Empty,
                "Melee DPS limit break, high-damage burst typically used on enrage timers.",
                "melee lb"),
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
            Term("Clear Party", "クリア目的", "Kuria Mokuteki", "PF tag indicating the party's goal is to clear the duty. Short form seen in PF listings: クリ目 (kuri me).", "クリ目", "kuri me", "cp"),
            Term("Duty Complete", "コン済", "Konzumi", "Indicates the duty has already been completed/unlocked this week."),
            Term("First Time", "初見", "Shoken", "Indicates this is the player's first time attempting the fight."),
            Term("Farm", "周回", "Shuukai", "Indicates the party is farming the duty for loot after clearing."),
            Term("Mistake", "ミス", "Misu", "Used to call out a mistake made during a pull."),
            Term("Wipe", "ワイプ", "Waipu", "The whole party has died and the encounter has reset."),
            Term("Learning Party", "練習PT", "Renshuu PT", "PF tag for a party focused on learning mechanics rather than clearing."),
            Term("Reclear", "消化", "Shouka", "Indicates the party is doing a routine reclear of an already-cleared duty."),

            Term("Merc", "傭兵", "Youhei",
                "A veteran player hired to help carry a party to a clear. Common when a group only needs one experienced player to fill a gap.",
                "mercenary", "carry", "youhei"),
            Term("Slot Taken", "〆", "",
                "Indicates a party slot is reserved or taken. e.g. MT〆 in a PF listing means the MT role is filled. Also used in-chat: MT〆でお願いできますか？ (Can I take the MT slot?).",
                "taken", "reserved", "shimekiri", "slot taken"),
            Term("Food Ready Check", "1飯RC", "",
                "Ready check after one round of food buffs. The number prefix tracks how many food rotations the party plans to do (1飯RC, 2飯RC, etc.).",
                "1 food rc", "food rc", "飯rc", "food ready check", "2飯rc"),

            // Loot
            Term("Free Loot", "フリロ", "Furiro", "All drops can be Need-rolled freely by anyone in the party.", "free", "フリーロット", "ロット自由"),
            Term("Left to Right", "左取り抜け", "Hidari Dori Nuke", "Loot is distributed in turn order based on party list position, from left to right.", "ltr", "loot order", "上から順番", "順番"),
            Term("Need", "ニード", "Niido", "Roll Need on an item you intend to use yourself."),
            Term("Greed", "グリード", "Guriido", "Roll Greed on an item you don't need but want to sell or desynth."),
            Term("Pass", "パス", "Pasu", "Decline to roll on an item, letting others have priority."),
        };
    }
}
