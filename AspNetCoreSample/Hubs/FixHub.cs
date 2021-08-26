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
            _apiService.Connect(apiCredentials);

            await Clients.Caller.SendAsync("Connected");
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            _apiService.Disconnect();

            return base.OnDisconnectedAsync(exception);
        }

        public async IAsyncEnumerable<Log> Logs([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (await _apiService.LogsChannel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (_apiService.LogsChannel.Reader.TryRead(out var log))
                {
                    yield return log;
                }
            }
        }

        public async IAsyncEnumerable<Symbol> Symbols([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (await _apiService.SecurityChannel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (_apiService.SecurityChannel.Reader.TryRead(out var symbol))
                {
                    yield return symbol;
                }
            }
        }

        public async IAsyncEnumerable<SymbolQuote> SymbolQuotes([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (await _apiService.MarketDataSnapshotFullRefreshChannel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (_apiService.MarketDataSnapshotFullRefreshChannel.Reader.TryRead(out var symbolQuote))
                {
                    yield return symbolQuote;
                }
            }
        }

        public async IAsyncEnumerable<Position> Positions([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (await _apiService.PositionReportChannel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (_apiService.PositionReportChannel.Reader.TryRead(out var position))
                {
                    yield return position;
                }
            }
        }

        public async IAsyncEnumerable<ExecutionReport> ExecutionReport([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (await _apiService.ExecutionReportChannel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (_apiService.ExecutionReportChannel.Reader.TryRead(out var executionReport))
                {
                    yield return executionReport;
                }
            }
        }
    }
}