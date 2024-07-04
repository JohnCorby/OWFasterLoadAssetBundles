using OWML.Common;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace OWFasterLoadAssetBundles.Helpers;
internal static class AsyncHelper
{
    private static SynchronizationContext s_SynchronizationContext = null!;
    private static int s_MainThreadId = -1;

    public static void InitUnitySynchronizationContext()
    {
        s_SynchronizationContext = SynchronizationContext.Current;
        s_MainThreadId = Thread.CurrentThread.ManagedThreadId;
    }

    public static void Schedule(Func<Task> func)
    {
        Task.Run(async () =>
        {
            try
            {
                await func();
            }
            catch (Exception ex)
            {
                OWFasterLoadAssetBundles.Instance.ModHelper.Console.WriteLine(ex.ToString(), MessageType.Error);
            }
        }).Wait();
    }

    public static SwitchToMainThreadAwaiter SwitchToMainThread() => new();
    public static SwitchToThreadPoolAwaiter SwitchToThreadPool() => new();

    public readonly struct SwitchToMainThreadAwaiter : ICriticalNotifyCompletion
    {
        private static readonly SendOrPostCallback s_OnPostAction = OnPost;

        public readonly SwitchToMainThreadAwaiter GetAwaiter() => this;
        public readonly bool IsCompleted => Thread.CurrentThread.ManagedThreadId == s_MainThreadId;
        public void GetResult()
        { }

        public void OnCompleted(Action continuation)
        {
            UnsafeOnCompleted(continuation);
        }

        public readonly void UnsafeOnCompleted(Action continuation)
        {
            s_SynchronizationContext.Post(s_OnPostAction, continuation);
        }

        private static void OnPost(object state)
        {
            var action = state as Action;
            action?.Invoke();
        }
    }

    public readonly struct SwitchToThreadPoolAwaiter : ICriticalNotifyCompletion
    {
        private static readonly WaitCallback s_OnPostAction = OnPost;

        public readonly SwitchToThreadPoolAwaiter GetAwaiter() => this;
        public readonly bool IsCompleted => false;

        public void GetResult()
        { }

        public readonly void OnCompleted(Action continuation)
        {
            ThreadPool.QueueUserWorkItem(s_OnPostAction, continuation);
        }

        public readonly void UnsafeOnCompleted(Action continuation)
        {
            ThreadPool.UnsafeQueueUserWorkItem(s_OnPostAction, continuation);
        }

        private static void OnPost(object state)
        {
            var action = state as Action;
            action?.Invoke();
        }
    }
}
