using Common;
using QuickFix;
using QuickFix.Fields;
using QuickFix.Transport;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfSample.Models;

namespace WpfSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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

        public MainWindow()
        {
            InitializeComponent();

            _tradeApp = new(_username, _password, _senderCompId, _senderSubId, _tradeTargetCompId);
            _quoteApp = new(_username, _password, _senderCompId, _senderSubId, _quoteTargetCompId);

            var incomingMessagesProcessingBlock = new ActionBlock<Message>(ProcessIncomingMessage);
            var outgoingMessagesProcessingBlock = new ActionBlock<Message>(ProcessOutgoingMessage);

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

            DataContext = MainModel;
        }

        public MainModel MainModel { get; } = new MainModel();

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _tradeApp.Dispose();

            _tradeInitiator.Stop();

            _quoteApp.Dispose();

            _quoteInitiator.Stop();
        }

        private void ProcessOutgoingMessage(Message message)
        {
            if (message is QuickFix.FIX44.MarketDataRequest) return;

            AddLog(new LogModel("Sent", DateTime.Now, message.ToString('|')));
        }

        private async void ProcessIncomingMessage(Message message) => await Dispatcher.InvokeAsync(() =>
        {
            if (message is not QuickFix.FIX44.MarketDataSnapshotFullRefresh)
            {
                AddLog(new LogModel("Received", DateTime.Now, message.ToString('|')));
            }

            if (message is QuickFix.FIX44.Logon && message.Header.IsSetField(50) && message.Header.GetString(50).Equals("TRADE", StringComparison.OrdinalIgnoreCase) && _tradeInitiator.IsLoggedOn)
            {
                SendSecurityListRequest();

                return;
            }

            switch (message)
            {
                case QuickFix.FIX44.SecurityList securityList:
                    OnSecurityList(securityList);
                    break;

                case QuickFix.FIX44.MarketDataSnapshotFullRefresh marketDataSnapshotFullRefresh:
                    OnMarketDataSnapshotFullRefresh(marketDataSnapshotFullRefresh);
                    break;

                case QuickFix.FIX44.PositionReport positionReport:
                    OnPositionReport(positionReport);
                    break;

                case QuickFix.FIX44.ExecutionReport executionReport:
                    OnExecutionReport(executionReport);
                    break;
            }
        });

        private void OnExecutionReport(QuickFix.FIX44.ExecutionReport executionReport)
        {
            var order = executionReport.GetOrder();

            if (order.Type.Equals("Market", StringComparison.OrdinalIgnoreCase) && executionReport.CumQty.getValue() > 0)
            {
                SendPositionsRequest();
            }
            else if (order.Type.Equals("Market", StringComparison.OrdinalIgnoreCase) is false)
            {
                order.SymbolName = MainModel.Symbols.FirstOrDefault(symbol => symbol.Id == order.SymbolId)?.Name;

                var previousOrder = MainModel.Orders.FirstOrDefault(iOrder => iOrder.Id == order.Id);

                if (previousOrder is not null)
                {
                    MainModel.Orders.Remove(previousOrder);
                }

                var executionType = executionReport.ExecType.getValue();

                if (executionType != '4' && executionType != '8' && executionType != 'C' && executionType != 'F')
                {
                    MainModel.Orders.Add(order);
                }
            }
        }

        private void OnPositionReport(QuickFix.FIX44.PositionReport positionReport)
        {
            if (positionReport.TotalNumPosReports.getValue() == 0) return;

            var position = positionReport.GetPosition();

            position.SymbolName = MainModel.Symbols.FirstOrDefault(symbol => symbol.Id == position.SymbolId)?.Name;

            MainModel.Positions.Add(position);
        }

        private void OnSecurityList(QuickFix.FIX44.SecurityList securityList)
        {
            var symbols = securityList.GetSymbols().OrderBy(symbol => symbol.Id);

            foreach (var symbol in symbols)
            {
                MainModel.Symbols.Add(new SymbolModel { Name = symbol.Name, Id = symbol.Id, Digits = symbol.Digits });

                SendMarketDataRequest(true, symbol.Id);
            }

            SendPositionsRequest();
            SendOrderMassStatusRequest();
        }

        private void OnMarketDataSnapshotFullRefresh(QuickFix.FIX44.MarketDataSnapshotFullRefresh marketDataSnapshotFullRefresh)
        {
            var symbolQuote = marketDataSnapshotFullRefresh.GetSymbolQuote();

            var symbol = MainModel.Symbols.FirstOrDefault(symbol => symbol.Id == symbolQuote.SymbolId);

            if (symbol is not null)
            {
                symbol.Bid = symbolQuote.Bid;
                symbol.Ask = symbolQuote.Ask;
            }
        }

        private void SendOrderMassStatusRequest()
        {
            QuickFix.FIX44.OrderMassStatusRequest message = new(new MassStatusReqID("Orders"), new MassStatusReqType(7));

            _tradeApp.SendMessage(message);
        }

        private void SendMarketDataRequest(bool subscribe, int symbolId)
        {
            QuickFix.FIX44.MarketDataRequest message = new(new("MARKETDATAID"), new(subscribe ? '1' : '2'), new(1));

            QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup bidMarketDataEntryGroup = new() { MDEntryType = new MDEntryType('0') };
            QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup offerMarketDataEntryGroup = new() { MDEntryType = new MDEntryType('1') };
            message.AddGroup(bidMarketDataEntryGroup);
            message.AddGroup(offerMarketDataEntryGroup);

            QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup symbolGroup = new() { Symbol = new QuickFix.Fields.Symbol(symbolId.ToString(CultureInfo.InvariantCulture)) };
            message.AddGroup(symbolGroup);

            _quoteApp.SendMessage(message);
        }

        private void SendNewOrderButton_Click(object sender, RoutedEventArgs e)
        {
            var ordType = new OrdType(MainModel.NewOrderModel.SelectedOrderType?.ToLowerInvariant() switch
            {
                "market" => OrdType.MARKET,
                "limit" => OrdType.LIMIT,
                "stop" => OrdType.STOP,
                _ => throw new Exception("unsupported input"),
            });

            var message = new QuickFix.FIX44.NewOrderSingle(
                new ClOrdID(MainModel.NewOrderModel.ClOrdId),
                new QuickFix.Fields.Symbol(MainModel.NewOrderModel.SelectedSymbol.Id.ToString()),
                new Side(MainModel.NewOrderModel.SelectedTradeSide.ToLowerInvariant().Equals("buy", StringComparison.OrdinalIgnoreCase) ? '1' : '2'),
                new TransactTime(DateTime.Now),
                ordType);

            message.Set(new OrderQty(Convert.ToDecimal(MainModel.NewOrderModel.Quantity)));

            if (ordType.getValue() != OrdType.MARKET)
            {
                message.Set(new TimeInForce('1'));

                if (ordType.getValue() == OrdType.LIMIT)
                {
                    message.Set(new Price(Convert.ToDecimal(MainModel.NewOrderModel.TargetPrice)));
                }
                else
                {
                    message.Set(new StopPx(Convert.ToDecimal(MainModel.NewOrderModel.TargetPrice)));
                }

                if (MainModel.NewOrderModel.Expiry.HasValue)
                {
                    message.Set(new ExpireTime(MainModel.NewOrderModel.Expiry.Value));
                }
            }
            else
            {
                message.Set(new TimeInForce('3'));

                if (MainModel.NewOrderModel.PositionId != default)
                {
                    message.SetField(new StringField(721, MainModel.NewOrderModel.PositionId.ToString()));
                }
            }

            if (string.IsNullOrWhiteSpace(MainModel.NewOrderModel.Designation) is false)
            {
                message.Set(new Designation(MainModel.NewOrderModel.Designation));
            }

            message.Header.GetString(Tags.BeginString);

            _tradeApp.SendMessage(message);
        }

        private void SendPositionsRequest()
        {
            QuickFix.FIX44.RequestForPositions message = new();

            message.PosReqID = new PosReqID("Positions");

            MainModel.Positions.Clear();

            _tradeApp.SendMessage(message);
        }

        private void SendSecurityListRequest()
        {
            QuickFix.FIX44.SecurityListRequest securityListRequest = new(new SecurityReqID("symbols"), new SecurityListRequestType(0));

            _tradeApp.SendMessage(securityListRequest);
        }

        private async void AddLog(LogModel log) => await Dispatcher.InvokeAsync(() =>
        {
            MainModel.Logs.Add(log);
        });
    }
}