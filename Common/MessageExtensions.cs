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

        public static Position GetPosition(this QuickFix.FIX44.PositionReport message)
        {
            var noPositionsGroup = message.GetGroup(1, Tags.NoPositions);

            var longVolume = noPositionsGroup.GetDecimal(704);
            var shortVolume = noPositionsGroup.GetDecimal(705);

            decimal volume;
            string tradeSide;

            if (longVolume > shortVolume)
            {
                volume = longVolume;
                tradeSide = "Buy";
            }
            else
            {
                volume = shortVolume;
                tradeSide = "Sell";
            }

            return new Position
            {
                Id = long.Parse(message.PosMaintRptID.getValue(), NumberStyles.Any, CultureInfo.InvariantCulture),
                SymbolId = int.Parse(message.Symbol.getValue(), NumberStyles.Any, CultureInfo.InvariantCulture),
                EntryPrice = message.SettlPrice.getValue(),
                Volume = volume,
                TradeSide = tradeSide,
                StopLoss = message.IsSetField(1002) ? message.GetDecimal(1002) : 0,
                TakeProfit = message.IsSetField(1000) ? message.GetDecimal(1000) : 0,
                TrailingStopLoss = message.IsSetField(1004) ? message.GetBoolean(1004) : null,
                GuaranteedStopLoss = message.IsSetField(1006) ? message.GetBoolean(1006) : null,
                StopLossTriggerMethod = message.IsSetField(1005) ? message.GetInt(1005) switch
                {
                    1 => "Trade Side",
                    2 => "Opposite Side",
                    3 => "Double Trade Side",
                    4 => "Double Opposite Side",
                    _ => string.Empty
                } : string.Empty
            };
        }

        public static Order GetOrder(this QuickFix.FIX44.ExecutionReport message)
        {
            decimal targetPrice = 0;

            if (message.IsSetField(44))
            {
                targetPrice = message.Price.getValue();
            }
            else if (message.IsSetField(99))
            {
                targetPrice = message.StopPx.getValue();
            }

            return new Order
            {
                Id = long.Parse(message.OrderID.getValue(), NumberStyles.Any, CultureInfo.InvariantCulture),
                Type = message.GetInt(40) switch
                {
                    1 => "Market",
                    2 => "Limit",
                    3 => "Stop",
                    _ => string.Empty
                },
                SymbolId = int.Parse(message.Symbol.getValue(), NumberStyles.Any, CultureInfo.InvariantCulture),
                TargetPrice = targetPrice,
                Volume = message.OrderQty.getValue(),
                Time = message.TransactTime.getValue(),
                TradeSide = message.GetInt(54) == 1 ? "Buy" : "Sell",
                ExpireTime = message.IsSetField(126) ? message.ExpireTime.getValue() : null,
                StopLossInPips = message.IsSetField(1003) ? message.GetDecimal(1003) : 0,
                TakeProfitInPips = message.IsSetField(1001) ? message.GetDecimal(1001) : 0,
                TrailingStopLoss = message.IsSetField(1004) ? message.GetBoolean(1004) : null,
                GuaranteedStopLoss = message.IsSetField(1006) ? message.GetBoolean(1006) : null,
                StopLossTriggerMethod = message.IsSetField(1005) ? message.GetInt(1005) switch
                {
                    1 => "Trade Side",
                    2 => "Opposite Side",
                    3 => "Double Trade Side",
                    4 => "Double Opposite Side",
                    _ => string.Empty
                } : string.Empty
            };
        }
    }

    public record Symbol(int Id, string Name, int Digits);

    public record SymbolQuote(int SymbolId, decimal Bid, decimal Ask);

    public record Position
    {
        public long Id { get; init; }

        public int SymbolId { get; init; }

        public string SymbolName { get; set; }

        public decimal EntryPrice { get; init; }

        public decimal Volume { get; init; }

        public string TradeSide { get; init; }

        public decimal StopLoss { get; init; }

        public decimal TakeProfit { get; init; }

        public bool? TrailingStopLoss { get; init; }

        public string StopLossTriggerMethod { get; init; }

        public bool? GuaranteedStopLoss { get; init; }
    }

    public record Order
    {
        public long Id { get; init; }

        public string Type { get; init; }

        public int SymbolId { get; init; }

        public string SymbolName { get; set; }

        public decimal TargetPrice { get; init; }

        public decimal Volume { get; init; }

        public string TradeSide { get; init; }

        public DateTime Time { get; init; }

        public DateTime? ExpireTime { get; init; }

        public decimal StopLossInPips { get; init; }

        public decimal TakeProfitInPips { get; init; }

        public bool? TrailingStopLoss { get; init; }

        public string StopLossTriggerMethod { get; init; }

        public bool? GuaranteedStopLoss { get; init; }
    }
}