using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalTTS.G2P
{
    /// <summary>
    /// Expands text into speakable words: currency, percentages, ordinals, decimals,
    /// integers, and common abbreviations. Runs before G2P; pure C#, thread-safe.
    /// </summary>
    public static class TextNormalizer
    {
        private static readonly Dictionary<string, string> Abbreviations =
            new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Dr."] = "Doctor", ["Mr."] = "Mister", ["Mrs."] = "Missus", ["Ms."] = "Miss",
            ["Prof."] = "Professor", ["Capt."] = "Captain", ["Sgt."] = "Sergeant",
            ["Lt."] = "Lieutenant", ["Gen."] = "General", ["St."] = "Saint",
            ["Ave."] = "Avenue", ["Blvd."] = "Boulevard", ["Rd."] = "Road",
            ["etc."] = "et cetera", ["vs."] = "versus", ["approx."] = "approximately",
        };

        private static readonly string[] Ones =
        {
            "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine",
            "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen",
            "seventeen", "eighteen", "nineteen",
        };

        private static readonly string[] Tens =
        {
            "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety",
        };

        private static readonly (long value, string name)[] Scales =
        {
            (1_000_000_000_000, "trillion"), (1_000_000_000, "billion"),
            (1_000_000, "million"), (1_000, "thousand"),
        };

        private static readonly Dictionary<string, string> OrdinalExceptions =
            new Dictionary<string, string>
        {
            ["one"] = "first", ["two"] = "second", ["three"] = "third", ["five"] = "fifth",
            ["eight"] = "eighth", ["nine"] = "ninth", ["twelve"] = "twelfth",
        };

        private static readonly Regex CurrencyRx =
            new Regex(@"([$£€])(\d[\d,]*)(?:\.(\d{1,2}))?", RegexOptions.Compiled);
        private static readonly Regex PercentRx =
            new Regex(@"(\d[\d,]*(?:\.\d+)?)\s*%", RegexOptions.Compiled);
        private static readonly Regex OrdinalRx =
            new Regex(@"\b(\d[\d,]*)(st|nd|rd|th)\b", RegexOptions.Compiled);
        private static readonly Regex DecimalRx =
            new Regex(@"\b(\d[\d,]*)\.(\d+)\b", RegexOptions.Compiled);
        private static readonly Regex IntegerRx =
            new Regex(@"\b\d[\d,]*\b", RegexOptions.Compiled);

        /// <summary>Normalizes text, then splits into sentences (terminators kept).</summary>
        public static List<string> NormalizeAndSplit(string text)
        {
            string normalized = Normalize(text);
            var sentences = new List<string>();
            var current = new StringBuilder();

            foreach (char c in normalized)
            {
                current.Append(c);
                if (c is '.' or '!' or '?' or '…')
                {
                    Flush();
                }
            }

            Flush();
            return sentences;

            void Flush()
            {
                string s = current.ToString().Trim();
                if (s.Length > 0)
                {
                    sentences.Add(s);
                }

                current.Clear();
            }
        }

        public static string Normalize(string text)
        {
            text = Regex.Replace(text, @"\s+", " ").Trim();

            foreach (var kv in Abbreviations)
            {
                text = text.Replace(kv.Key, kv.Value);
            }

            text = CurrencyRx.Replace(text, m =>
            {
                string unit = m.Groups[1].Value switch
                {
                    "$" => "dollar", "£" => "pound", _ => "euro",
                };
                long whole = ParseDigits(m.Groups[2].Value);
                string result = $"{NumberToWords(whole)} {unit}{(whole == 1 ? "" : "s")}";
                if (m.Groups[3].Success)
                {
                    long cents = long.Parse(m.Groups[3].Value.PadRight(2, '0'));
                    if (cents > 0)
                    {
                        string sub = m.Groups[1].Value == "$" ? "cent" : m.Groups[1].Value == "£" ? "pence" : "cent";
                        result += $" and {NumberToWords(cents)} {sub}{(cents == 1 || sub == "pence" ? "" : "s")}";
                    }
                }

                return result;
            });

            text = PercentRx.Replace(text, m => $"{ExpandNumberToken(m.Groups[1].Value)} percent");
            text = OrdinalRx.Replace(text, m => NumberToOrdinalWords(ParseDigits(m.Groups[1].Value)));
            text = DecimalRx.Replace(text, m =>
            {
                var sb = new StringBuilder(NumberToWords(ParseDigits(m.Groups[1].Value)));
                sb.Append(" point");
                foreach (char d in m.Groups[2].Value)
                {
                    sb.Append(' ').Append(Ones[d - '0']);
                }

                return sb.ToString();
            });
            text = IntegerRx.Replace(text, m => NumberToWords(ParseDigits(m.Value)));

            return text;
        }

        public static string NumberToWords(long n)
        {
            if (n < 0)
            {
                return "minus " + NumberToWords(-n);
            }

            if (n < 20)
            {
                return Ones[n];
            }

            if (n < 100)
            {
                return Tens[n / 10] + (n % 10 > 0 ? " " + Ones[n % 10] : "");
            }

            if (n < 1000)
            {
                return Ones[n / 100] + " hundred" + (n % 100 > 0 ? " " + NumberToWords(n % 100) : "");
            }

            foreach (var (value, name) in Scales)
            {
                if (n >= value)
                {
                    return NumberToWords(n / value) + " " + name
                        + (n % value > 0 ? " " + NumberToWords(n % value) : "");
                }
            }

            return Ones[0];
        }

        public static string NumberToOrdinalWords(long n)
        {
            string words = NumberToWords(n);
            int lastSpace = words.LastIndexOf(' ');
            string head = lastSpace < 0 ? "" : words.Substring(0, lastSpace + 1);
            string last = words.Substring(lastSpace + 1);

            if (OrdinalExceptions.TryGetValue(last, out string exception))
            {
                return head + exception;
            }

            if (last.EndsWith("y"))
            {
                return head + last.Substring(0, last.Length - 1) + "ieth";
            }

            return head + last + "th";
        }

        private static long ParseDigits(string s) => long.Parse(s.Replace(",", ""));

        private static string ExpandNumberToken(string token)
        {
            return token.Contains('.')
                ? DecimalRx.Replace(token, m =>
                  {
                      var sb = new StringBuilder(NumberToWords(ParseDigits(m.Groups[1].Value)));
                      sb.Append(" point");
                      foreach (char d in m.Groups[2].Value)
                      {
                          sb.Append(' ').Append(Ones[d - '0']);
                      }

                      return sb.ToString();
                  })
                : NumberToWords(ParseDigits(token));
        }
    }
}
