using QuickFix;
using System;
using System.Text;
using System.Windows.Forms;
using System.IO;
using QuickFix.Transport;
using Common;
using QuickFix.Fields;
using System.Threading.Tasks.Dataflow;

namespace WinFormsSample
{
    public partial class frmFIXAPISample : Form
    {
        // Please set the credentials before running the app
        private const int _quotePort = 5201;

        private const int _tradePort = 5202;

        private const string _host = "h51.p.ctrader.com";
        private const string _username = "3397885";
        private const string _password = "3397885";
        private const string _senderCompId = "demo.ctrader.3397885";
        private const string _senderSubId = "3397885";

        private const string _tradeTargetSubID = "TRADE";
        private const string _quoteTargetSubID = "QUOTE";

        private readonly SocketInitiator _quoteInitiator;
        private readonly SocketInitiator _tradeInitiator;

        private readonly QuickFixNApp _quoteApp;
        private readonly QuickFixNApp _tradeApp;

        public frmFIXAPISample()
        {
            InitializeComponent();

            FormClosing += FrmFIXAPISample_FormClosing;

            _tradeApp = new(_username, _password, _senderCompId, _senderSubId, "cServer");
            _quoteApp = new(_username, _password, _senderCompId, _senderSubId, "cServer");

            var incomingMessagesProcessingBlock = new ActionBlock<QuickFix.Message>(message => ProcessIncomingMessage(message));
            var outgoingMessagesProcessingBlock = new ActionBlock<QuickFix.Message>(message => ProcessOutgoingMessage(message));

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            _tradeApp.IncomingMessagesBuffer.LinkTo(incomingMessagesProcessingBlock, linkOptions);
            _quoteApp.IncomingMessagesBuffer.LinkTo(incomingMessagesProcessingBlock, linkOptions);

            _tradeApp.OutgoingMessagesBuffer.LinkTo(outgoingMessagesProcessingBlock, linkOptions);
            _quoteApp.OutgoingMessagesBuffer.LinkTo(outgoingMessagesProcessingBlock, linkOptions);

            var tradeSettings = GetSessionSettings(_host, _tradePort, _senderCompId, _senderSubId, _tradeTargetSubID);
            var quoteSettings = GetSessionSettings(_host, _quotePort, _senderCompId, _senderSubId, _quoteTargetSubID);

            var tradeStoreFactory = new FileStoreFactory(tradeSettings);
            var quoteStoreFactory = new FileStoreFactory(quoteSettings);

            _tradeInitiator = new(_tradeApp, tradeStoreFactory, tradeSettings);
            _quoteInitiator = new(_quoteApp, quoteStoreFactory, quoteSettings);

            _tradeInitiator.Start();
            _quoteInitiator.Start();
        }

        private void FrmFIXAPISample_FormClosing(object sender, FormClosingEventArgs e)
        {
            _tradeApp.Dispose();

            _tradeInitiator.Stop();

            _quoteApp.Dispose();

            _quoteInitiator.Stop();
        }

        private void ProcessIncomingMessage(QuickFix.Message message)
        {
            var messageType = message.Header.GetString(35);

            if (messageType.Equals("0", StringComparison.OrdinalIgnoreCase) || messageType.Equals("1", StringComparison.OrdinalIgnoreCase) || messageType.Equals("A", StringComparison.OrdinalIgnoreCase)) return;

            Invoke(new Action(() => txtMessageReceived.Text = $"{txtMessageReceived.Text}\n{message.GetMessageText()}"));
        }

        private void ProcessOutgoingMessage(QuickFix.Message message)
        {
            Invoke(new Action(() => txtMessageSend.Text = $"{txtMessageSend.Text}\n{message.GetMessageText()}"));
        }

        private SessionSettings GetSessionSettings(string host, int port, string senderCompId, string senderSubId, string targetSubId)
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
            stringBuilder.AppendLine("TargetCompID=cServer");
            stringBuilder.AppendLine("HeartBtInt=30");

            var stringReader = new StringReader(stringBuilder.ToString());

            return new SessionSettings(stringReader);
        }

        private void clearSentButton_Click(object sender, EventArgs e)
        {
            txtMessageSend.Text = "";
        }

        private void clearReceivedButton_Click(object sender, EventArgs e)
        {
            txtMessageReceived.Text = "";
        }

        private void btnMarketDataRequest_Click(object sender, EventArgs e)
        {
            MDReqID mdReqID = new("MARKETDATAID");
            SubscriptionRequestType subType = new('1');
            MarketDepth marketDepth = new(0);

            QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup marketDataEntryGroup = new();
            marketDataEntryGroup.Set(new MDEntryType('1'));

            QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup symbolGroup = new();
            symbolGroup.Set(new Symbol("1"));

            QuickFix.FIX44.MarketDataRequest message = new(mdReqID, subType, marketDepth);
            message.AddGroup(marketDataEntryGroup);
            message.AddGroup(symbolGroup);

            _quoteApp.SendMessage(message);
        }

        private void btnSpotMarketData_Click(object sender, EventArgs e)
        {
            MDReqID mdReqID = new("MARKETDATAID");
            SubscriptionRequestType subType = new('1');
            MarketDepth marketDepth = new(1);

            QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup marketDataEntryGroup = new();
            marketDataEntryGroup.Set(new MDEntryType('1'));

            QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup symbolGroup = new();
            symbolGroup.Set(new Symbol("1"));

            QuickFix.FIX44.MarketDataRequest message = new(mdReqID, subType, marketDepth);
            message.AddGroup(marketDataEntryGroup);
            message.AddGroup(symbolGroup);

            _quoteApp.SendMessage(message);
        }

        private void btnNewOrderSingle_Click(object sender, EventArgs e)
        {
            var ordType = new OrdType(OrdType.MARKET);

            var message = new QuickFix.FIX44.NewOrderSingle(
                new ClOrdID("newOrder"),
                new Symbol("1"),
                new Side('1'),
                new TransactTime(DateTime.Now),
                ordType);

            message.Set(new OrderQty(10000));

            message.Set(new TimeInForce('3'));

            message.Header.GetString(Tags.BeginString);

            _tradeApp.SendMessage(message);
        }

        private void btnLimitOrder_Click(object sender, EventArgs e)
        {
            var ordType = new OrdType(OrdType.LIMIT);

            var message = new QuickFix.FIX44.NewOrderSingle(
                new ClOrdID("newLimitOrder"),
                new Symbol("1"),
                new Side('1'),
                new TransactTime(DateTime.Now),
                ordType);

            message.Set(new OrderQty(10000));

            message.Set(new TimeInForce('1'));

            // Limit Order Target Price
            message.Set(new Price(Convert.ToDecimal(1.08)));

            message.Header.GetString(Tags.BeginString);

            _tradeApp.SendMessage(message);
        }

        private void btnStopOrder_Click(object sender, EventArgs e)
        {
            var ordType = new OrdType(OrdType.STOP);

            var message = new QuickFix.FIX44.NewOrderSingle(
                new ClOrdID("newStopOrder"),
                new Symbol("1"),
                new Side('1'),
                new TransactTime(DateTime.Now),
                ordType);

            message.Set(new OrderQty(10000));

            // Stop Order Target Price
            message.Set(new StopPx(Convert.ToDecimal(1.10)));

            message.Header.GetString(Tags.BeginString);

            _tradeApp.SendMessage(message);
        }

        private void btnOrderStatusRequest_Click(object sender, EventArgs e)
        {
            QuickFix.FIX44.OrderStatusRequest message = new()
            {
                // Unique Order ID set by client
                ClOrdID = new ClOrdID("newOrder"),
            };

            _tradeApp.SendMessage(message);
        }

        private void btnRequestForPositions_Click(object sender, EventArgs e)
        {
            QuickFix.FIX44.RequestForPositions message = new();

            message.PosReqID = new PosReqID("PositionsRequest");

            _tradeApp.SendMessage(message);
        }

        private void btnSecurityListRequest_Click(object sender, EventArgs e)
        {
            QuickFix.FIX44.SecurityListRequest message = new(new SecurityReqID("symbols"), new SecurityListRequestType(0));

            _tradeApp.SendMessage(message);
        }
    }
}