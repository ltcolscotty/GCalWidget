using System;
using System.Threading;
using System.Threading.Tasks;

namespace GCaLink.Services
{
    internal sealed class RefreshSchedService: IAsyncDisposable
    {
        private readonly PeriodicTimer _timer;
        private readonly CancellationTokenSource _cts = new();
        private Task? _runTask;

        public RefreshSchedService(TimeSpan interval)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);
            _timer = new PeriodicTimer(interval);
        }

        public Task RunAsync()
        {
            if (_runTask != null)
            {
                return _runTask;
            }

            _runTask = RunTimerAsync();
            return _runTask;
        }

        private async Task RunTimerAsync()
        {
            try
            {
                while (await _timer.WaitForNextTickAsync(_cts.Token))
                {
                    try
                    {
                        await EventAggService.WriteUpcomingEventsMessagePackAsync(SettingsRetriever.GetMainDataPath());
                    }
                    catch (Exception exception)
                    {
                        LoggerService.LogException("RefreshSchedService: Scheduled refresh failed.", exception);
                    }
                }
            }
            catch (OperationCanceledException) when (_cts.IsCancellationRequested)
            {
                LoggerService.Log("RefreshSchedService: Refresh operation canceled", LoggerStatusEnum.INFO);
            }
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            _timer.Dispose();

            if (_runTask != null)
            {
                await _runTask.ConfigureAwait(false);
            }

            _cts.Dispose();
        }

    }
}