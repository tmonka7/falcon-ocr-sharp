using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace FalconOcr.Localization
{
    /// <summary>A user-interface language with its gettext catalog file (lang\&lt;code&gt;.po).</summary>
    public sealed class UiLanguage
    {
        public string Code { get; set; }
        public string NativeName { get; set; }
        public override string ToString() => NativeName;
    }

    /// <summary>
    /// Interface translation with gettext PO catalogs. Source strings are English; <see cref="T"/> returns the
    /// translation of the active catalog, or the English text when there is none. Catalogs live in the
    /// "lang" folder next to the executable (lang\zh_CN.po, lang\ja.po) and can be edited with Poedit or any text editor.
    /// </summary>
    public static class L
    {
        public static readonly IReadOnlyList<UiLanguage> Languages = new[]
        {
            new UiLanguage { Code = "en", NativeName = "English" },
            new UiLanguage { Code = "zh_CN", NativeName = "简体中文 (Chinese)" },
            new UiLanguage { Code = "ja", NativeName = "日本語 (Japanese)" },
        };

        private static Dictionary<string, string> _catalog = new Dictionary<string, string>();

        /// <summary>Active language code ("en", "zh_CN", "ja").</summary>
        public static string Current { get; private set; } = "en";

        public static string LangDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lang");

        /// <summary>Language code matching the Windows display language (used on first start).</summary>
        public static string FromSystem()
        {
            var c = CultureInfo.CurrentUICulture;
            if (c.TwoLetterISOLanguageName == "zh") return "zh_CN";
            if (c.TwoLetterISOLanguageName == "ja") return "ja";
            return "en";
        }

        /// <summary>Loads the catalog of <paramref name="code"/>; unknown codes or missing files fall back to English.</summary>
        public static void Load(string code)
        {
            Current = Languages.Any(l => l.Code == code) ? code : "en";
            _catalog = new Dictionary<string, string>();
            if (Current == "en") return;
            var path = Path.Combine(LangDirectory, Current + ".po");
            if (!File.Exists(path))
            {
                Current = "en";
                return;
            }
            _catalog = PoFile.Read(path);
        }

        /// <summary>Translates an English source string.</summary>
        public static string T(string english)
        {
            if (string.IsNullOrEmpty(english)) return english;
            return _catalog.TryGetValue(english, out var s) && s.Length > 0 ? s : english;
        }

        /// <summary>Translates a composite format string, then formats it.</summary>
        public static string F(string englishFormat, params object[] args) => string.Format(T(englishFormat), args);

        public static bool IsCjk => Current == "zh_CN" || Current == "ja";
    }

    /// <summary>Minimal gettext PO reader: msgid/msgstr pairs, multi-line strings, C escapes; comments and msgctxt ignored.</summary>
    public static class PoFile
    {
        public static Dictionary<string, string> Read(string path)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            string id = null, str = null, current = null;
            var sb = new StringBuilder();

            void FlushField()
            {
                if (current == "msgid") id = sb.ToString();
                else if (current == "msgstr") str = sb.ToString();
                sb.Clear();
                current = null;
            }

            void FlushEntry()
            {
                FlushField();
                if (!string.IsNullOrEmpty(id) && str != null) result[id] = str;
                id = str = null;
            }

            foreach (var raw in File.ReadAllLines(path, Encoding.UTF8))
            {
                var line = raw.Trim();
                if (line.Length == 0)
                {
                    FlushEntry();
                    continue;
                }
                if (line.StartsWith("#")) continue;
                if (line.StartsWith("msgctxt")) { FlushField(); current = "msgctxt"; continue; }
                if (line.StartsWith("msgid "))
                {
                    if (current == "msgstr") FlushEntry();
                    FlushField();
                    current = "msgid";
                    sb.Append(Unquote(line.Substring(6)));
                }
                else if (line.StartsWith("msgstr"))
                {
                    FlushField();
                    current = "msgstr";
                    int q = line.IndexOf('"');
                    if (q >= 0) sb.Append(Unquote(line.Substring(q)));
                }
                else if (line.StartsWith("\"") && current != null)
                {
                    sb.Append(Unquote(line));
                }
            }
            FlushEntry();
            return result;
        }

        private static string Unquote(string s)
        {
            s = s.Trim();
            if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"') s = s.Substring(1, s.Length - 2);
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c != '\\' || i + 1 >= s.Length) { sb.Append(c); continue; }
                char n = s[++i];
                switch (n)
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    default: sb.Append('\\').Append(n); break;
                }
            }
            return sb.ToString();
        }
    }
}
