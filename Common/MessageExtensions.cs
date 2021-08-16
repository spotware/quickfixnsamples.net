using QuickFix;
using System;
using System.Linq;
using System.Text;

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
            stringBuilder.AppendLine($"    Raw: \"{message.ToString().Replace('', '|')}\"");

            stringBuilder.AppendLine("}");

            return stringBuilder.ToString();
        }
    }
}