using System;
using System.Text.Json;
using TextEdit.Core.SpellChecking;

namespace TextEdit.UI.Services
{
    public static class SpellCheckSuggestionMapper
    {
        /// <summary>
        /// Parses a suggestion object coming from JS interop or reflection and converts to a core SpellCheckSuggestion.
        /// Supports: string, JsonElement (string or object), anonymous object with properties Word/IsPrimary/Confidence.
        /// </summary>
        public static SpellCheckSuggestion? Parse(object? raw)
        {
            if (raw == null) return null;

            string word = string.Empty;
            var isPrimary = false;
            var confidence = 50;

            // Handle JsonElement (JS interop payloads)
            if (raw is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.String)
                {
                    word = je.GetString() ?? string.Empty;
                }
                else if (je.ValueKind == JsonValueKind.Object)
                {
                    if (je.TryGetProperty("Word", out var wprop) || je.TryGetProperty("word", out wprop))
                        word = wprop.GetString() ?? string.Empty;
                    else if (je.TryGetProperty("Suggestion", out var sprop) || je.TryGetProperty("suggestion", out sprop))
                        word = sprop.GetString() ?? string.Empty;

                    if (je.TryGetProperty("IsPrimary", out var ip)) isPrimary = ip.GetBoolean();
                    if (je.TryGetProperty("Confidence", out var cp) && cp.TryGetInt32(out var ci)) confidence = ci;
                }
            }
            else
            {
                // reflection-friendly types (anonymous, POCOs)
                var type = raw.GetType();
                var prop = type.GetProperty("Word") ?? type.GetProperty("word") ?? type.GetProperty("Suggestion") ?? type.GetProperty("suggestion");
                if (prop != null)
                {
                    try { word = Convert.ToString(prop.GetValue(raw)) ?? string.Empty; } catch { word = string.Empty; }
                    var isPrimaryProp = type.GetProperty("IsPrimary") ?? type.GetProperty("isPrimary");
                    if (isPrimaryProp != null)
                    {
                        try { isPrimary = Convert.ToBoolean(isPrimaryProp.GetValue(raw)); } catch { isPrimary = false; }
                    }
                    var confProp = type.GetProperty("Confidence") ?? type.GetProperty("confidence");
                    if (confProp != null)
                    {
                        try { confidence = Convert.ToInt32(confProp.GetValue(raw)); } catch { }
                    }
                }
                else
                {
                    // plain string or fallback
                    try { word = Convert.ToString(raw) ?? string.Empty; } catch { word = string.Empty; }
                }
            }

            if (string.IsNullOrEmpty(word)) return null;
            return new SpellCheckSuggestion { Word = word, IsPrimary = isPrimary, Confidence = confidence };
        }
    }
}
