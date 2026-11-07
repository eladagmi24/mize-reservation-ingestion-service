using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using SupplierFeed.Api.Infrastructure;

namespace SupplierFeed.Api.Services;

public sealed class SlidingWindowRateLimiter : IRateLimiter
{
    #region Fields & Dependencies
    private readonly int _requestLimit;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _window;
    private readonly ConcurrentDictionary<string, SupplierWindow> _windowsBySupplier = new(StringComparer.Ordinal);
    #endregion

    #region Constructor
    public SlidingWindowRateLimiter(IOptions<RateLimitOptions> rateLimitOptions, TimeProvider timeProvider)
    {
        _requestLimit = rateLimitOptions.Value.RequestLimit;
        _timeProvider = timeProvider;
        _window = TimeSpan.FromSeconds(rateLimitOptions.Value.WindowSeconds);
    }
    #endregion

    #region Public Methods
    public RateLimitDecision TryAcquire(string supplierId)
    {
        var supplierWindow = _windowsBySupplier.GetOrAdd(supplierId, _ => new SupplierWindow());

        lock (supplierWindow.SyncRoot)
        {
            var now = _timeProvider.GetTimestamp();
            RemoveExpiredAdmissions(supplierWindow.AdmissionTimestamps, now);

            if (supplierWindow.AdmissionTimestamps.Count >= _requestLimit)
            {
                var oldestAdmission = supplierWindow.AdmissionTimestamps.Peek();
                var retryAfter = _window - _timeProvider.GetElapsedTime(oldestAdmission, now);
                return RateLimitDecision.Rejected(retryAfter);
            }

            supplierWindow.AdmissionTimestamps.Enqueue(now);
            return RateLimitDecision.Admitted();
        }
    }
    #endregion

    #region Private Helpers
    private void RemoveExpiredAdmissions(Queue<long> admissionTimestamps, long now)
    {
        while (admissionTimestamps.Count > 0 && _timeProvider.GetElapsedTime(admissionTimestamps.Peek(), now) >= _window)
        {
            admissionTimestamps.Dequeue();
        }
    }
    #endregion

    #region Nested Types
    private sealed class SupplierWindow
    {
        #region Properties
        public Queue<long> AdmissionTimestamps { get; } = new();

        public object SyncRoot { get; } = new();
        #endregion
    }
    #endregion
}
