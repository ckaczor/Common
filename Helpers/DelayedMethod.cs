using System.Timers;
using System.Windows.Threading;

namespace Common.Helpers
{
    public class DelayedMethod
    {
        public delegate void DelayedMethodDelegate();

        private readonly Timer _delayTimer;
        private readonly DelayedMethodDelegate _methodDelegate;
        private readonly Dispatcher _dispatcher;

        public DelayedMethod(int interval, DelayedMethodDelegate methodDelegate)
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _methodDelegate = methodDelegate;

            _delayTimer = new Timer(interval) { AutoReset = false };
            _delayTimer.Elapsed += handleDelayTimerElapsed;
        }

        public void Reset()
        {
            _delayTimer.Stop();
            _delayTimer.Start();
        }

        private void handleDelayTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Make sure we're on the right thread
            if (!_dispatcher.CheckAccess())
                _dispatcher.Invoke(_methodDelegate);
        }
    }
}
