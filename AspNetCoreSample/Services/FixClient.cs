using Common;
using QuickFix;
using QuickFix.Fields;
using QuickFix.Transport;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace AspNetCoreSample.Services
{
    public class FixClient : IDisposable
    {
        private readonly SocketInitiator _quoteInitiator;
        private readonly SocketInitiator _tradeInitiator;

        private readonly QuickFixNApp _quoteApp;
        private readonly QuickFixNApp _tradeApp;

        private Common.Symbol[] _symbols;

        public FixClient(ApiCredentials credentials)
        {
            _tradeApp = new(credentials.Username, credentials.Password, credentials.TradeSenderCompId, credentials.TradeSenderSubId, credentials.TradeTargetCompId);
            _quoteApp = new(credentials.Username, credentials.Password, credentials.QuoteSenderCompId, credentials.QuoteSenderSubId, credentials.QuoteTargetCompId);

            var incomingMessagesProcessingBlock = new ActionBlock<Message>(ProcessIncomingMessage);
            var outgoingMessagesProcessingBlock = new ActionBlock<Message>(ProcessOutgoingMessage);

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            _tradeApp.IncomingMessagesBuffer.LinkTo(incomingMessagesProcessingBlock, linkOptions);
            _quoteApp.IncomingMessagesBuffer.LinkTo(incomingMessagesProcessingBlock, linkOptions);

            _tradeApp.OutgoingMessagesBuffer.LinkTo(outgoingMessagesProcessingBlock, linkOptions);
            _quoteApp.OutgoingMessagesBuffer.LinkTo(outgoingMessagesProcessingBlock, linkOptions);

            var tradeSettings = SessionSettingsFactory.GetSessionSettings(credentials.TradeHost, credentials.TradePort, credentials.TradeSenderCompId, credentials.TradeSenderSubId, credentials.TradeTargetSubId, credentials.TradeTargetCompId);
            var quoteSettings = SessionSettingsFactory.GetSessionSettings(credentials.QuoteHost, credentials.QuotePort, credentials.QuoteSenderCompId, credentials.QuoteSenderSubId, credentials.QuoteTargetSubId, credentials.QuoteTargetCompId);

            var tradeStoreFactory = new FileStoreFactory(tradeSettings);
            var quoteStoreFactory = new FileStoreFactory(quoteSettings);

            _tradeInitiator = new(_tradeApp, tradeStoreFactory, tradeSettings);
            _quoteInitiator = new(_quoteApp, quoteStoreFactory, quoteSettings);
        }

        public Channel<Log> LogsChannel { get; } = Channel.CreateUnbounded<Log>();

        public Channel<ExecutionReport> ExecutionReportChannel { get; } = Channel.CreateUnbounded<ExecutionReport>();

        public Channel<Position> PositionReportChannel { get; } = Channel.CreateUnbounded<Position>();

        public Channel<SymbolQuote> MarketDataSnapshotFullRefreshChannel { get; } = Channel.CreateUnbounded<SymbolQuote>();

        public Channel<Common.Symbol> SecurityChannel { get; } = Channel.CreateUnbounded<Common.Symbol>();

        public void Connect()
        {
            _tradeInitiator.Start();
            _quoteInitiator.Start();
        }

        public void Dispose()
        {
            _tradeInitiator?.Dispose();
            _quoteInitiator?.Dispose();
        }

        private async Task ProcessOutgoingMessage(Message message)
        {
            if (message is QuickFix.FIX44.MarketDataRequest) return;

            await LogsChannel.Writer.WriteAsync(new("Sent", DateTimeOffset.UtcNow, message.ToString('|')));
        }

        private async Task ProcessIncomingMessage(Message message)
        {
            if (message is not QuickFix.FIX44.MarketDataSnapshotFullRefresh)
            {
                await LogsChannel.Writer.WriteAsync(new("Received", DateTimeOffset.UtcNow, message.ToString('|')));
            }

            if (message is QuickFix.FIX44.Logon && message.Header.IsSetField(50) && message.Header.GetString(50).Equals("TRADE", StringComparison.OrdinalIgnoreCase) && _tradeInitiator.IsLoggedOn)
            {
                SendSecurityListRequest();

                return;
            }

            switch (message)
            {
                case QuickFix.FIX44.SecurityList securityList:
                    await OnSecurityList(securityList);
                    break;

                case QuickFix.FIX44.MarketDataSnapshotFullRefresh marketDataSnapshotFullRefresh:
                    await OnMarketDataSnapshotFullRefresh(marketDataSnapshotFullRefresh);
                    break;

                case QuickFix.FIX44.PositionReport positionReport:
                    await OnPositionReport(positionReport);
                    break;

                case QuickFix.FIX44.ExecutionReport executionReport:
                    await OnExecutionReport(executionReport);
                    break;
            }
        }

        private async Task OnExecutionReport(QuickFix.FIX44.ExecutionReport executionReport)
        {
            var order = executionReport.GetOrder();

            if (order.Type.Equals("Market", StringComparison.OrdinalIgnoreCase) && executionReport.CumQty.getValue() > 0)
            {
                await PositionReportChannel.Writer.WriteAsync(null);

                SendPositionsRequest();
            }
            else if (order.Type.Equals("Market", StringComparison.OrdinalIgnoreCase) is false)
            {
                order.SymbolName = _symbols.FirstOrDefault(symbol => symbol.Id == order.SymbolId)?.Name;

                var executionType = executionReport.ExecType.getValue();

                await ExecutionReportChannel.Writer.WriteAsync(new(executionType, order));
            }
        }

        private async Task OnPositionReport(QuickFix.FIX44.PositionReport positionReport)
        {
            if (positionReport.TotalNumPosReports.getValue() == 0) return;

            var position = positionReport.GetPosition();

            if (_symbols is not null)
            {
                position.SymbolName = _symbols.FirstOrDefault(symbol => symbol.Id == position.SymbolId)?.Name;
            }

            await PositionReportChannel.Writer.WriteAsync(position);
        }

        private async Task OnMarketDataSnapshotFullRefresh(QuickFix.FIX44.MarketDataSnapshotFullRefresh marketDataSnapshotFullRefresh)
        {
            var symbolQuote = marketDataSnapshotFullRefresh.GetSymbolQuote();

            await MarketDataSnapshotFullRefreshChannel.Writer.WriteAsync(symbolQuote);
        }

        private async Task OnSecurityList(QuickFix.FIX44.SecurityList securityList)
        {
            _symbols = securityList.GetSymbols().OrderBy(symbol => symbol.Id).ToArray();

            SendPositionsRequest();
            SendOrderMassStatusRequest();

            foreach (var symbol in _symbols)
            {
                await SecurityChannel.Writer.WriteAsync(symbol);

                SendMarketDataRequest(true, symbol.Id);
            }

            SecurityChannel.Writer.TryComplete();
        }

        public void SendNewOrderRequest(NewOrderRequestParameters parameters)
        {
            var ordType = new OrdType(parameters.Type.ToLowerInvariant() switch
            {
                "market" => OrdType.MARKET,
                "limit" => OrdType.LIMIT,
                "stop" => OrdType.STOP,
                _ => throw new Exception("unsupported input"),
            });

            var message = new QuickFix.FIX44.NewOrderSingle(
                new ClOrdID(parameters.ClOrdId),
                new QuickFix.Fields.Symbol(parameters.SymbolId.ToString(CultureInfo.InvariantCulture)),
                new Side(parameters.TradeSide.ToLowerInvariant().Equals("buy", StringComparison.OrdinalIgnoreCase) ? '1' : '2'),
                new TransactTime(DateTime.Now),
                ordType);

            message.Set(new OrderQty(Convert.ToDecimal(parameters.Quantity)));

            if (ordType.getValue() != OrdType.MARKET)
            {
                message.Set(new TimeInForce('1'));

                if (parameters.TargetPrice.HasValue)
                {
                    if (ordType.getValue() == OrdType.LIMIT)
                    {
                        message.Set(new Price(Convert.ToDecimal(parameters.TargetPrice)));
                    }
                    else
                    {
                        message.Set(new StopPx(Convert.ToDecimal(parameters.TargetPrice)));
                    }
                }

                if (parameters.Expiry.HasValue)
                {
                    message.Set(new ExpireTime(parameters.Expiry.Value));
                }
            }
            else
            {
                message.Set(new TimeInForce('3'));

                if (parameters.PositionId.HasValue)
                {
                    message.SetField(new StringField(721, parameters.PositionId.Value.ToString(CultureInfo.InvariantCulture)));
                }
            }

            if (string.IsNullOrWhiteSpace(parameters.Designation) is false)
            {
                message.Set(new Designation(parameters.Designation));
            }

            message.Header.GetString(Tags.BeginString);

            _tradeApp.SendMessage(message);
        }

        private void SendSecurityListRequest()
        {
            QuickFix.FIX44.SecurityListRequest securityListRequest = new(new SecurityReqID("symbols"), new SecurityListRequestType(0));

            _tradeApp.SendMessage(securityListRequest);
        }

        private void SendPositionsRequest()
        {
            QuickFix.FIX44.RequestForPositions message = new();

            message.PosReqID = new PosReqID("Positions");

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

        private void SendOrderMassStatusRequest()
        {
            QuickFix.FIX44.OrderMassStatusRequest message = new(new MassStatusReqID("Orders"), new MassStatusReqType(7));

            _tradeApp.SendMessage(message);
        }
    }

    public record ApiCredentials(string QuoteHost, string TradeHost, int QuotePort, int TradePort, string QuoteSenderCompId, string TradeSenderCompId, string QuoteSenderSubId, string TradeSenderSubId, string QuoteTargetCompId, string TradeTargetCompId, string QuoteTargetSubId, string TradeTargetSubId, string Username, string Password);

    public record Log(string Type, DateTimeOffset Time, string Message);

    public record ExecutionReport(char Type, Order Order);

    public record NewOrderRequestParameters(string Type, string ClOrdId, int SymbolId, string TradeSide, decimal Quantity)
    {
        public double? TargetPrice { get; init; }

        public DateTime? Expiry { get; init; }

        public long? PositionId { get; init; }

        public string Designation { get; init; }
    }
}