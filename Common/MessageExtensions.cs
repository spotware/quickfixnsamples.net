using QuickFix;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using QuickFix.Fields;

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

        public static string ToString<TMessage>(this TMessage message, char separator) where TMessage : Message
        {
            try
            {
                return message.ToString().Replace('', separator);
            }
            catch (InvalidOperationException)
            {
                return string.Empty;
            }
        }

        public static IEnumerable<Symbol> GetSymbols(this QuickFix.FIX44.SecurityList message)
        {
            var numberOfGroups = message.GetInt(Tags.NoRelatedSym);

            var symbolField = new IntField(Tags.Symbol);
            var symbolNameField = new StringField(Tags.SideReasonCd);
            var symbolDigitsField = new IntField(Tags.SideTrdSubTyp);

            for (int groupIndex = 1; groupIndex <= numberOfGroups; groupIndex += 1)
            {
                var group = message.GetGroup(groupIndex, Tags.NoRelatedSym);

                var symbolFieldValue = group.GetField(symbolField).getValue();
                var symbolNameValue = group.GetField(symbolNameField).getValue();
                var symbolDigitsFieldValue = group.GetField(symbolDigitsField).getValue();

                yield return new Symbol(symbolFieldValue, symbolNameValue, symbolDigitsFieldValue);
            }
        }

        public static SymbolQuote GetSymbolQuote(this QuickFix.FIX44.MarketDataSnapshotFullRefresh message)
        {
            var numberOfGroups = message.GetInt(Tags.NoMDEntries);

            decimal bid = 0;
            decimal ask = 0;

            var mdEntryTypeField = new CharField(Tags.MDEntryType);
            var mdEntryPxField = new DecimalField(Tags.MDEntryPx);

            for (int groupIndex = 1; groupIndex <= numberOfGroups; groupIndex += 1)
            {
                var group = message.GetGroup(groupIndex, Tags.NoMDEntries);

                var mdEntryTypeFieldValue = group.GetField(mdEntryTypeField).getValue();
                var mdEntryPxFieldValue = group.GetField(mdEntryPxField).getValue();

                if (mdEntryTypeFieldValue == '0')
                {
                    bid = mdEntryPxFieldValue;
                }
                else if (mdEntryTypeFieldValue == '1')
                {
                    ask = mdEntryPxFieldValue;
                }
            }

            return new SymbolQuote(message.GetField(new IntField(Tags.Symbol)).getValue(), bid, ask);
        }
    }

    public record Symbol(int Id, string Name, int Digits);

    public record SymbolQuote(int SymbolId, decimal Bid, decimal Ask);
}