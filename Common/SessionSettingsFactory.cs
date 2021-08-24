using QuickFix;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class SessionSettingsFactory
    {
        public static SessionSettings GetSessionSettings(string host, int port, string senderCompId, string senderSubId, string targetSubId, string targetCompId)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("[DEFAULT]");
            stringBuilder.AppendLine("ConnectionType=initiator");
            stringBuilder.AppendLine("ReconnectInterval=2");
            stringBuilder.AppendLine("FileStorePath=store");
            stringBuilder.AppendLine("FileLogPath=log");
            stringBuilder.AppendLine("StartTime=00:00:00");
            stringBuilder.AppendLine("EndTime=00:00:00");
            stringBuilder.AppendLine("UseDataDictionary=Y");
            stringBuilder.AppendLine("DataDictionary=./FIX44-CSERVER.xml");
            stringBuilder.AppendLine($"SocketConnectHost={host}");
            stringBuilder.AppendLine($"SocketConnectPort={port}");
            stringBuilder.AppendLine("LogoutTimeout=100");
            stringBuilder.AppendLine("ResetOnLogon=Y");
            stringBuilder.AppendLine("ResetOnDisconnect=Y");
            stringBuilder.AppendLine("[SESSION]");
            stringBuilder.AppendLine("BeginString=FIX.4.4");
            stringBuilder.AppendLine($"SenderCompID={senderCompId}");
            stringBuilder.AppendLine($"SenderSubID={senderSubId}");
            stringBuilder.AppendLine($"TargetSubID={targetSubId}");
            stringBuilder.AppendLine($"TargetCompID={targetCompId}");
            stringBuilder.AppendLine("HeartBtInt=30");

            var stringReader = new StringReader(stringBuilder.ToString());

            return new SessionSettings(stringReader);
        }
    }
}