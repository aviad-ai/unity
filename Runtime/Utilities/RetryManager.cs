using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aviad
{
    [System.Serializable]
    public class RetryConfig
    {
        // = -1: infinite retries are allowed
        public int maxRetries = 3;
        public int delayMs = 1000;
        public float backoffMultiplier = 2f;

        public int GetDelayMs(int attemptNumber)
        {
            return (int)(delayMs * Math.Pow(backoffMultiplier, attemptNumber));
        }
    }

    public class RetryManager
    {
        /// <summary>
        /// Executes an operation with retry support and calls a result callback on the main thread.
        /// </summary>
        /// <param name="operationName">Name of the operation (used for logging).</param>
        /// <param name="operation">The operation to attempt.</param>
        /// <param name="onSuccess">Callback invoked if any attempt succeeds.</param>
        /// <param name="onFailure">Callback invoked if max retries fail.</param>
        /// <param name="config">Retry configuration. Uses default if null.</param>
        public static void ExecuteWithRetry(
            string operationName,
            Action<Action<bool>> operation,
            Action onSuccess,
            Action onFailure,
            RetryConfig config = null)
        {
            if (config == null) config = new RetryConfig();
            Action<bool> retryCallback = null;
            int currentAttempt = 0;
            retryCallback = (bool success) =>
            {
                if (success)
                {
                    onSuccess?.Invoke();
                    return;
                }
                currentAttempt++;
                if (config.maxRetries != -1 && currentAttempt > config.maxRetries)
                {
                    onFailure?.Invoke();
                    return;
                }

                int delayMs = config.GetDelayMs(currentAttempt - 1);
                Task.Delay(delayMs).ContinueWith(_ =>
                {
                    try
                    {
                        operation.Invoke(retryCallback);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception in {operationName} attempt {currentAttempt}: {ex.Message}");
                        retryCallback?.Invoke(false);
                    }
                });
            };

            try
            {
                operation.Invoke(retryCallback);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in {operationName} initial attempt: {ex.Message}");
                retryCallback?.Invoke(false);
            }
        }
    }
}