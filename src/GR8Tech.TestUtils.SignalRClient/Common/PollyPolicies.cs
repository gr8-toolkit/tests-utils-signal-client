using System;
using System.Collections.Generic;
using Polly;

namespace GR8Tech.TestUtils.SignalRClient.Common
{
    internal static class PollyPolicies
    {
        internal static AsyncPolicy<T> AsyncRetryPolicyWithExceptionAndResult<T>(int retryCount, TimeSpan sleepDuration)
        {
            return Policy
                .Handle<Exception>()
                .OrResult<T>(result => EqualityComparer<T>.Default.Equals(result, default!))
                .WaitAndRetryAsync(retryCount, i => sleepDuration);
        }
    }
}