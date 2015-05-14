using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace VSO.OAuth.UI
{
    public static class Login
    {
        // ReSharper disable once InconsistentNaming
        public static Task<T> StartSTATask<T>(Func<OAuthOptions, T> func, OAuthOptions options)
        {
            var tcs = new TaskCompletionSource<T>();
            var thread = new Thread(() =>
            {
                try
                {
                    tcs.SetResult(func(options));
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            }) { IsBackground = true };

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return tcs.Task;
        }

        private static OAuthTokenInfo GetTokenFromUi(OAuthOptions options)
        {
            // Create our context, and install it:
            SynchronizationContext.SetSynchronizationContext(
                new DispatcherSynchronizationContext(
                    Dispatcher.CurrentDispatcher));

            OAuthTokenInfo token = null;

            var loginWindow = new LoginWindow();

            // When the window closes, shut down the dispatcher
            loginWindow.Closed += (s, e) => Dispatcher.CurrentDispatcher.BeginInvokeShutdown(
                DispatcherPriority.Background);

            loginWindow.LoginCompleted += (sender, info) =>
            {
                token = info;
                loginWindow.Close();
            };

            loginWindow.Show();
            loginWindow.Login(options);

            //Start the dispatcher
            Dispatcher.Run();

            return token;
        }

        public static Task<OAuthTokenInfo> Start(OAuthOptions options)
        {
            return StartSTATask(GetTokenFromUi, options);
        }
    }
}