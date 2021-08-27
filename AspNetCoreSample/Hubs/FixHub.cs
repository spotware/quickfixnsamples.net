using AspNetCoreSample.Services;
using Common;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace AspNetCoreSample.Hubs
{
    public class FixHub : Hub
    {
        private readonly ApiService _apiService;

        public FixHub(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task Connect(ApiCredentials apiCredentials)
        {
            _apiService.ConnectClient(apiCredentials, Context.ConnectionId);

            await Clients.Caller.SendAsync("Connected");
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var client = _apiService.GetClient(Context.ConnectionId);

            client.Dispose();

            return base.OnDisconnectedAsync(exception);
        }

        public void SendNewOrderRequest(NewOrderRequestParameters parameters)
        {
            var client = _apiService.GetClient(Context.ConnectionId);

            client.SendNewOrderRequest(parameters);
        }

        public async IAsyncEnumerable<Log> Logs([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var client = _apiService.GetClient(Context.ConnectionId);

            while (await client.LogsChannel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (client.LogsChannel.Reader.TryRead(out var log))
                {
                    yield return log;
                }
            }
        }

        public async IAsyncEnumerable<Symbol> Symbols([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var client = _apiService.GetClient(Context.ConnectionId);

            while (await client.SecurityChannel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (client.SecurityChannel.Reader.TryRead(out var symbol))
                {
                    yield return symbol;
                }
            }
        }

        public async IAsyncEnumerable<SymbolQuote> SymbolQuotes([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var client = _apiService.GetClient(Context.ConnectionId);

            while (await client.MarketDataSnapshotFullRefreshChannel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (client.MarketDataSnapshotFullRefreshChannel.Reader.TryRead(out var symbolQuote))
                {
                    yield return symbolQuote;
                }
            }
        }

        public async IAsyncEnumerable<Position> Positions([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var client = _apiService.GetClient(Context.ConnectionId);

            while (await client.PositionReportChannel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (client.PositionReportChannel.Reader.TryRead(out var position))
                {
                    yield return position;
                }
            }
        }

        public async IAsyncEnumerable<ExecutionReport> ExecutionReport([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var client = _apiService.GetClient(Context.ConnectionId);

            while (await client.ExecutionReportChannel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (client.ExecutionReportChannel.Reader.TryRead(out var executionReport))
                {
                    yield return executionReport;
                }
            }
        }
    }
}