using System;
using System.Net.Sockets;
using System.Threading;

namespace PythonCommunication
{
    // Source: https://stackoverflow.com/questions/21468137/async-network-operations-never-finish/21468138#21468138
    
    /// <summary>
    /// Provides a disposable scope that executes a specified action when disposed.
    /// </summary>
    public sealed class DisposableScope : IDisposable
    {
        /// <summary>
        /// Action to be executed when the scope is closed/disposed.
        /// </summary>
        private readonly Action _closeScopeAction;

        /// <summary>
        /// Initializes a new instance of the DisposableScope.
        /// </summary>
        /// <param name="closeScopeAction">Action to execute during disposal</param>
        public DisposableScope(Action closeScopeAction)
        {
            _closeScopeAction = closeScopeAction ?? throw new ArgumentNullException(nameof(closeScopeAction));
        }

        /// <summary>
        /// Executes the close scope action when disposed.
        /// </summary>
        public void Dispose()
        {
            _closeScopeAction();
        }
    }

    /// <summary>
    /// Provides extension methods for creating disposable scopes with timeout functionality.
    /// </summary>
    public static class DisposableExtensions
    {
        /// <summary>
        /// Creates a disposable scope with a specified timeout.
        /// </summary>
        /// <param name="disposable">The disposable object to manage</param>
        /// <param name="timeSpan">The timeout duration</param>
        /// <returns>A disposable scope that handles timeout and disposal</returns>
        public static IDisposable CreateTimeoutScope(this IDisposable disposable, TimeSpan timeSpan)
        {
            if (disposable == null)
                throw new ArgumentNullException(nameof(disposable));
        
            if (timeSpan <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeSpan), "Timeout must be a positive timespan");

            var cancellationTokenSource = new CancellationTokenSource(timeSpan);
            var cancellationTokenRegistration = cancellationTokenSource.Token.Register(disposable.Dispose);
    
            return new DisposableScope(
                () =>
                {
                    cancellationTokenRegistration.Dispose();
                    cancellationTokenSource.Dispose();

                    // Additional disposal logic for TcpClient
                    if (disposable is TcpClient tcpClient)
                    {
                        if (tcpClient.Client == null || !tcpClient.Connected)
                            disposable.Dispose();
                    }
                    else
                    {
                        // Dispose of the original disposable object
                        disposable.Dispose();
                    }
                });
        }
    }
}