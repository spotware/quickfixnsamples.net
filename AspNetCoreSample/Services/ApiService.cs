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
    public class ApiService
    {
        private SocketInitiator _quoteInitiator;
        private SocketInitiator _tradeInitiator;

        private QuickFixNApp _quoteApp;
        private QuickFixNApp _tradeApp;

        private Common.Symbol[] _symbols;

        public Channel<Log> LogsChannel { get; } = Channel.CreateUnbounded<Log>();

        public Channel<ExecutionReport> ExecutionReportChannel { get; } = Channel.CreateUnbounded<ExecutionReport>();

        public Channel<Position> PositionReportChannel { get; } = Channel.CreateUnbounded<Position>();

        public Channel<SymbolQuote> MarketDataSnapshotFullRefreshChannel { get; } = Channel.CreateUnbounded<SymbolQuote>();

        public Channel<Common.Symbol> SecurityChannel { get; } = Channel.CreateUnbounded<Common.Symbol>();

        public void Connect(ApiCredentials apiCredentials)
        {
            _tradeApp = new(apiCredentials.Username, apiCredentials.Password, apiCredentials.TradeSenderCompId, apiCredentials.TradeSenderSubId, apiCredentials.TradeTargetCompId);
            _quoteApp = new(apiCredentials.Username, apiCredentials.Password, apiCredentials.QuoteSenderCompId, apiCredentials.QuoteSenderSubId, apiCredentials.QuoteTargetCompId);

            var incomingMessagesProcessingBlock = new ActionBlock<Message>(ProcessIncomingMessage);
            var outgoingMessagesProcessingBlock = new ActionBlock<Message>(ProcessOutgoingMessage);

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            _tradeApp.IncomingMessagesBuffer.LinkTo(incomingMessagesProcessingBlock, linkOptions);
            _quoteApp.IncomingMessagesBuffer.LinkTo(incomingMessagesProcessingBlock, linkOptions);

            _tradeApp.OutgoingMessagesBuffer.LinkTo(outgoingMessagesProcessingBlock, linkOptions);
            _quoteApp.OutgoingMessagesBuffer.LinkTo(outgoingMessagesProcessingBlock, linkOptions);

            var tradeSettings = SessionSettingsFactory.GetSessionSettings(apiCredentials.TradeHost, apiCredentials.TradePort, apiCredentials.TradeSenderCompId, apiCredentials.TradeSenderSubId, apiCredentials.TradeTargetSubId, apiCredentials.TradeTargetCompId);
            var quoteSettings = SessionSettingsFactory.GetSessionSettings(apiCredentials.QuoteHost, apiCredentials.QuotePort, apiCredentials.QuoteSenderCompId, apiCredentials.QuoteSenderSubId, apiCredentials.QuoteTargetSubId, apiCredentials.QuoteTargetCompId);

            var tradeStoreFactory = new FileStoreFactory(tradeSettings);
            var quoteStoreFactory = new FileStoreFactory(quoteSettings);

            _tradeInitiator = new(_tradeApp, tradeStoreFactory, tradeSettings);
            _quoteInitiator = new(_quoteApp, quoteStoreFactory, quoteSettings);

            _tradeInitiator.Start();
            _quoteInitiator.Start();
        }

        public void Disconnect()
        {
            _tradeInitiator?.Dispose();
            _quoteInitiator?.Dispose();
        }

        private async Task ProcessOutgoingMessage(Message message)
        {
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
                SendPositionsRequest();
            }
            else if (order.Type.Equals("Market", StringComparison.OrdinalIgnoreCase) is false)
            {
                order.SymbolName = _symbols.FirstOrDefault(symbol => symbol.Id == order.SymbolId)?.Name;

                var executionType = executionReport.ExecType.getValue();

                await ExecutionReportChannel.Writer.WriteAsync(new("ExecutionReport", order));
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

            foreach (var symbol in _symbols)
            {
                await SecurityChannel.Writer.WriteAsync(symbol);

                SendMarketDataRequest(true, symbol.Id);
            }

            SecurityChannel.Writer.TryComplete();

            SendPositionsRequest();
            SendOrderMassStatusRequest();
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

    public record ExecutionReport(string Type, Order Order);
}