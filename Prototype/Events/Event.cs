﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Events
{
    public static class Event
    {
        public static IDisposable Subscribe<T>(IHandler<T> handler)
        {
            return Subscribe<T>(handler.HandleAsync);
        }

        public static IDisposable Subscribe<T>(Func<T, Task<bool>> handler)
        {
            return new Subscription<T>(handler);
        }

        public static async Task ExecuteAsync<T>(this T e)
        {
            if (!await RaiseAsync(e))
                throw new NotImplementedException("Required " + typeof(T).Name + " event handler is not registered.");
        }

        public async static Task<bool> RaiseAsync<T>(this T e)
        {
            try
            {
                await Subscription.NotifyAsync(new Broadcast<Before, T>(e));

                if(await Subscription.NotifyAsync(e))
                {
                    await Subscription.NotifyAsync(new Broadcast<Succeeded, T>(e));                    
                    return true;
                }
                else
                {
                    await Subscription.NotifyAsync(new Broadcast<Unhandled, T>(e));                    
                    return false;
                }
            }
            catch(Exception ex)
            {
                await Subscription.NotifyAsync(new Broadcast<Failed, T>(e, ex));
                throw;
            }
        }

        abstract class Subscription : IDisposable
        {
            static ReaderWriterLockSlim Lock { get; } = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            static HashSet<Subscription> Instances { get; } = new HashSet<Subscription>();

            protected Subscription()
            {
                Lock.EnterWriteLock();
                try
                {
                    Instances.Add(this);
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
            }

            public void Dispose()
            {
                Lock.EnterWriteLock();
                try
                {
                    Instances.Remove(this);
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
            }

            public static async Task<bool> NotifyAsync(object e)
            {                
                Task<bool[]> matches;
                Lock.EnterReadLock();
                try
                {
                    matches = Task.WhenAll(from s in Instances
                                           select s.NotifyCoreAsync(e));
                }
                finally
                {
                    Lock.ExitReadLock();
                }

                return (await matches)
                    .Contains(true);
            }

            protected abstract Task<bool> NotifyCoreAsync(object e);
        }

        class Subscription<T> : Subscription
        {
            readonly Func<T, Task<bool>> _handler;

            public Subscription(Func<T, Task<bool>> handler)
            {
                _handler = handler;
            }

            protected override Task<bool> NotifyCoreAsync(object e)
            {
                if (e is T)
                    return _handler((T)e);
                else
                    return Task.FromResult(false);
            }
        }
    }
}
