using QuickFix;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks.Dataflow;

namespace ConsoleSample
{
    internal class Program
    {
        private static readonly string _configFile = "Config-dev.cfg";

        private static void Main(string[] args)
        {
            if (File.Exists(_configFile) is false)
            {
                Console.WriteLine("Error: Config file not found");
                Environment.Exit(2);
            }

            try
            {
                SessionSettings settings = new(_configFile);

                var defaultSettings = settings.Get();

                var sessionId = settings.GetSessions().First();

                var username = defaultSettings.GetString("Username");
                var password = defaultSettings.GetString("Password");

                QuickFixNApp application = new(username, password, sessionId);
                IMessageStoreFactory storeFactory = new FileStoreFactory(settings);

                QuickFix.Transport.SocketInitiator initiator = new(application, storeFactory, settings);

                // this is a developer-test kludge.  do not emulate.
                application.MyInitiator = initiator;

                initiator.Start();

                application.MessagesBuffer.LinkTo(new ActionBlock<Message>(message => ShowMessageData(message)), new DataflowLinkOptions { PropagateCompletion = true });

                application.Run();

                initiator.Stop();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }

            Environment.Exit(1);
        }

        private static void ShowMessageData<TMessage>(TMessage message) where TMessage : Message
        {
            var messageType = message.Header.GetString(35);

            if (messageType.Equals("0", StringComparison.OrdinalIgnoreCase) || messageType.Equals("1", StringComparison.OrdinalIgnoreCase) || messageType.Equals("A", StringComparison.OrdinalIgnoreCase)) return;

            var properties = message.GetType().GetProperties();

            var ignoredPropertyNames = new string[] { "Header", "Trailer", "RepeatedTags", "FieldOrder" };

            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("Response: ");

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

            Console.WriteLine();
            Console.WriteLine(stringBuilder);
            Console.WriteLine();
        }
    }
}