using QuickFix;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Common
{
    public static class MessageExtensions
    {
        public static string GetMessageText<TMessage>(this TMessage message) where TMessage : Message
        {
            var properties = message.GetType().GetProperties();

            var ignoredPropertyNames = new string[] { "Header", "Trailer", "RepeatedTags", "FieldOrder" };

            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("Message: ");

            stringBuilder.AppendLine("{");

            foreach (var property in properties)
            {
                if (property.CanRead is false || ignoredPropertyNames.Contains(property.Name, StringComparer.OrdinalIgnoreCase)) continue;

                try
                {
                    stringBuilder.AppendLine($"    {property.Name}: \"{property.GetValue(message)}\",");
                }
                catch (ApplicationException)
                {
                }
            }

            stringBuilder.AppendLine("    All Fields: ");
            stringBuilder.AppendLine("    [");

            var fields = message.ToString().Split('').Where(field => string.IsNullOrWhiteSpace(field) is false).ToArray();

            var lastField = fields.Last();

            foreach (var field in fields)
            {
                var tagValue = field.Split('=');

                if (tagValue.Length < 2) continue;

                var comma = field.Equals(lastField, StringComparison.OrdinalIgnoreCase) ? "" : ",";

                stringBuilder.AppendLine($"        {{{tagValue[0]}: \"{tagValue[1]}\"}}{comma}");
            }

            stringBuilder.AppendLine("    ],");
            stringBuilder.AppendLine($"    Raw: \"{message.ToString('|')}\"");

            stringBuilder.AppendLine("}");

            return stringBuilder.ToString();
        }

        public static string ToString<TMessage>(this TMessage message, char separator) where TMessage : Message => message.ToString().Replace('', separator);

        public static IEnumerable<Symbol> GetSymbols(this QuickFix.FIX44.SecurityList message)
        {
            var symbolsFields = message.ToString('|').Split("|55=", StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();

            foreach (var symbolFields in symbolsFields)
            {
                var symbolFieldsSplit = symbolFields.Split('|');

                if (symbolFieldsSplit.Length < 3
                    || long.TryParse(symbolFieldsSplit[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var symbolId) is false
                    || int.TryParse(symbolFieldsSplit[2].Substring(5), NumberStyles.Any, CultureInfo.InvariantCulture, out var symbolDigits) is false)
                {
                    continue;
                }

                var symbolName = symbolFieldsSplit[1].Substring(5);

                yield return new Symbol(symbolId, symbolName, symbolDigits);
            }
        }
    }

    public record Symbol(long Id, string Name, int Digits);
}