using System;
using System.Threading;

// Not used ?

public static class Debounce
{
    public static Action<T> Create<T>(Action<T> action, int milliseconds)
    {
        CancellationTokenSource? cancelToken = null;
        T? lastArg = default;
        object lockObj = new object();

        return arg =>
        {
            lock (lockObj)
            {
                lastArg = arg;
                cancelToken?.Cancel();
                cancelToken = new CancellationTokenSource();
            }
            CancellationToken token = cancelToken.Token;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(milliseconds);

                if (token.IsCancellationRequested)
                    return;

                lock (lockObj)
                {
                    if (token.IsCancellationRequested)
                        return;

                    action(lastArg);
                }
            });
        };
    }

    public static Action Create(Action action, int milliseconds)
    {
        CancellationTokenSource? cancelToken = null;
        object lockObj = new object();

        return () =>
        {
            lock (lockObj)
            {
                cancelToken?.Cancel();
                cancelToken = new CancellationTokenSource();
            }

            CancellationToken token = cancelToken.Token;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(milliseconds);

                if (token.IsCancellationRequested)
                    return;

                lock (lockObj)
                {
                    if (token.IsCancellationRequested)
                        return;

                    action();
                }
            });
        };
    }
}
