using QuickFix;
using System;
using System.Text;
using System.Windows.Forms;
using System.IO;
using QuickFix.Transport;
using Common;
using QuickFix.Fields;
using System.Threading.Tasks.Dataflow;
using Symbol = QuickFix.Fields.Symbol;

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

        private const string _tradeTargetSubId = "TRADE";
        private const string _quoteTargetSubId = "QUOTE";

        private const string _tradeTargetCompId = "cServer";
        private const string _quoteTargetCompId = "cServer";

        private readonly SocketInitiator _quoteInitiator;
        private readonly SocketInitiator _tradeInitiator;

        private readonly QuickFixNApp _quoteApp;
        private readonly QuickFixNApp _tradeApp;

        public frmFIXAPISample()
        {
            InitializeComponent();

            FormClosing += FrmFIXAPISample_FormClosing;

            _tradeApp = new(_username, _password, _senderCompId, _senderSubId, _tradeTargetCompId);
            _quoteApp = new(_username, _password, _senderCompId, _senderSubId, _quoteTargetCompId);

            var incomingMessagesProcessingBlock = new ActionBlock<QuickFix.Message>(ProcessIncomingMessage);
            var outgoingMessagesProcessingBlock = new ActionBlock<QuickFix.Message>(ProcessOutgoingMessage);

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            _tradeApp.IncomingMessagesBuffer.LinkTo(incomingMessagesProcessingBlock, linkOptions);
            _quoteApp.IncomingMessagesBuffer.LinkTo(incomingMessagesProcessingBlock, linkOptions);

            _tradeApp.OutgoingMessagesBuffer.LinkTo(outgoingMessagesProcessingBlock, linkOptions);
            _quoteApp.OutgoingMessagesBuffer.LinkTo(outgoingMessagesProcessingBlock, linkOptions);

            var tradeSettings = SessionSettingsFactory.GetSessionSettings(_host, _tradePort, _senderCompId, _senderSubId, _tradeTargetSubId, _tradeTargetCompId);
            var quoteSettings = SessionSettingsFactory.GetSessionSettings(_host, _quotePort, _senderCompId, _senderSubId, _quoteTargetSubId, _quoteTargetCompId);

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

        private void clearSentButton_Click(object sender, EventArgs e)
        {
            txtMessageSend.Text = "";
        }

        private void clearReceivedButton_Click(object sender, EventArgs e)
        {
            txtMessageReceived.Text = "";
        }

        private void btnSubscribeSpotMarketData_Click(object sender, EventArgs e)
        {
            SendMarketDataRequest(true, false);
        }

        private void btnUnsubscribeSpotMarketDataRequest_Click(object sender, EventArgs e)
        {
            SendMarketDataRequest(false, false);
        }

        private void btnSubscribeDepthMarketDataRequest_Click(object sender, EventArgs e)
        {
            SendMarketDataRequest(true, true);
        }

        private void btnUnsubscribeDepthMarketDataRequest_Click(object sender, EventArgs e)
        {
            SendMarketDataRequest(false, true);
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

        private void SendMarketDataRequest(bool subscribe, bool depth)
        {
            MDReqID mdReqID = new("MARKETDATAID");
            SubscriptionRequestType subType = new(subscribe ? '1' : '2');
            MarketDepth marketDepth = new(depth ? 0 : 1);

            QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup bidMarketDataEntryGroup = new() { MDEntryType = new MDEntryType('0') };
            QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup offerMarketDataEntryGroup = new() { MDEntryType = new MDEntryType('1') };

            QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup symbolGroup = new() { Symbol = new Symbol("1") };

            QuickFix.FIX44.MarketDataRequest message = new(mdReqID, subType, marketDepth);

            message.AddGroup(bidMarketDataEntryGroup);
            message.AddGroup(offerMarketDataEntryGroup);
            message.AddGroup(symbolGroup);

            _quoteApp.SendMessage(message);
        }
    }
}