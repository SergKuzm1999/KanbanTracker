namespace KanbanTracker.Application.Services;

/// <summary>
/// Механізм відновлення з експоненційною затримкою .
/// </summary>
public static class RetryPolicy
{
    public static async Task<T> ExecuteAsync<T>(
        Func<Task<T>> action,
        int maxAttempts = 3,
        int initialDelayMs = 200,
        Action<Exception, int>? onRetry = null)
    {
        Exception? last = null;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                last = ex;
                onRetry?.Invoke(ex, attempt);
                if (attempt == maxAttempts) break;
                var delay = initialDelayMs * (int)Math.Pow(2, attempt - 1);
                await Task.Delay(delay);
            }
        }
        throw last ?? new InvalidOperationException("Retry failed.");
    }

    public static async Task ExecuteAsync(
        Func<Task> action,
        int maxAttempts = 3,
        int initialDelayMs = 200,
        Action<Exception, int>? onRetry = null)
    {
        await ExecuteAsync(async () =>
        {
            await action();
            return true;
        }, maxAttempts, initialDelayMs, onRetry);
    }
}
