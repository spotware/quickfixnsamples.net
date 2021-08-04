using System;
using System.IO;
using System.Linq;

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
                QuickFix.SessionSettings settings = new QuickFix.SessionSettings(_configFile);

                var defaultSettings = settings.Get();

                var sessionId = settings.GetSessions().First();

                var username = defaultSettings.GetString("Username");
                var password = defaultSettings.GetString("Password");

                QuickFixNApp application = new(username, password, sessionId);
                QuickFix.IMessageStoreFactory storeFactory = new QuickFix.FileStoreFactory(settings);
                QuickFix.ILogFactory logFactory = new QuickFix.ScreenLogFactory(settings);
                QuickFix.Transport.SocketInitiator initiator = new(application, storeFactory, settings, logFactory);

                // this is a developer-test kludge.  do not emulate.
                application.MyInitiator = initiator;

                initiator.Start();
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
    }
}