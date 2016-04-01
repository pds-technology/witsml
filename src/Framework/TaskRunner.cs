using System;
using System.Threading;
using System.Threading.Tasks;

namespace PDS.Framework
{
    public class TaskRunner
    {
        private CancellationTokenSource _tokenSource;
        private CancellationToken _token;

        public TaskRunner(int interval = 1000)
        {
            Interval = interval;
            OnExecute = Stop;
        }

        public int Interval { get; private set; }

        public Action OnExecute { get; set; }

        public Action<Exception> OnError { get; set; }

        public bool IsRunning
        {
            get { return _tokenSource != null && !_token.IsCancellationRequested; }
        }

        public void Start()
        {
            _tokenSource = new CancellationTokenSource();
            _token = _tokenSource.Token;

            Task.Run(async () =>
            {
                using (_tokenSource)
                {
                    try
                    {
                        await Start(_token, Interval);
                    }
                    catch (Exception ex)
                    {
                        if (OnError != null)
                            OnError(ex);
                    }
                    finally
                    {
                        _tokenSource = null;
                    }
                }
            },
            _token);
        }

        public void Stop()
        {
            if (_tokenSource != null)
                _tokenSource.Cancel();
        }

        private async Task Start(CancellationToken token, int interval)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                    break;

                if (OnExecute != null)
                    OnExecute();

                await Task.Delay(interval);
            }
        }
    }
}
