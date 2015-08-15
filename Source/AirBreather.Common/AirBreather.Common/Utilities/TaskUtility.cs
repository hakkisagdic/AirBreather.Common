﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace AirBreather.Common.Utilities
{
    public static class TaskUtility
    {
        public static IEnumerable<Task<T>> InCompletionOrder<T>(this IEnumerable<Task<T>> tasks)
        {
            // Stephen Toub named his version of this method "Interleaved" in the TAP guide.
            // I picked Jon Skeet's name for this method because it's more expressive.
            // ...
            // Rx.NET has IObservable<Task<T>> --> IObservable<T> called "Merge".
            // It does basically the same thing as this, except it takes advantage
            // of the fact that IObservable<T> supports non-success messages natively,
            // so it can return IObservable<T> instead of keeping the Task<T> wrapper.
            tasks.ValidateNotNull(nameof(tasks));
            Task<T>[] taskArray = tasks.ToArray();

            TaskCompletionSource<T>[] outputSources = Array.ConvertAll(taskArray, _ => new TaskCompletionSource<T>());

            int highestCompletedIndex = -1;
            foreach (Task<T> task in taskArray)
            {
                task.ContinueWith(t =>
                {
                    TaskCompletionSource<T> outputSource = outputSources[Interlocked.Increment(ref highestCompletedIndex)];
                    switch (t.Status)
                    {
                        case TaskStatus.Canceled:
                            outputSource.SetCanceled();
                            break;

                        case TaskStatus.Faulted:
                            outputSource.SetException(t.Exception);
                            break;

                        ////case TaskStatus.RanToCompletion:
                        default:
                            outputSource.SetResult(t.Result);
                            break;
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);
            }

            return outputSources.Select(outputSource => outputSource.Task);
        }
    }
}
