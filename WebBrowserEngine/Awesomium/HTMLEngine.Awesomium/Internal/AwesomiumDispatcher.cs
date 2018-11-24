﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Awesomium.Core;
using Neutronium.Core.WebBrowserEngine.Window;

namespace Neutronium.WebBrowserEngine.Awesomium.Internal
{
    internal class AwesomiumDispatcher : IDispatcher
    {

        private void RunOnContext(Action act)
        {
            if (Thread.CurrentThread == AwesomiumWPFWebWindowFactory.WebCoreThread)
                act();
            else
                WebCore.QueueWork(act);
        }

        public Task RunAsync(Action act)
        {
            var tcs = new TaskCompletionSource<object>();

            void Nact()
            {
                try
                {
                    act();
                    tcs.SetResult(null);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            }

            RunOnContext(Nact);
            return tcs.Task;
        }

        public void Dispatch(Action act)
        {
            void Nact()
            {
                try
                {
                    act();
                }
                catch
                {
                }
            }

            RunOnContext(Nact);
        }

        public void Run(Action act)
        {
            RunAsync(act).Wait();
        }

        public Task<T> EvaluateAsync<T>(Func<T> compute)
        {
            var tcs = new TaskCompletionSource<T>();

            void Nact()
            {
                try
                {
                    tcs.TrySetResult(compute());
                }
                catch (Exception e)
                {
                    tcs.TrySetException(e);
                }
            }

            RunOnContext(Nact);
            return tcs.Task;
        }

        public T Evaluate<T>(Func<T> compute)
        {
            return EvaluateAsync(compute).Result;
        }

        public bool IsInContext() 
        {
            return Thread.CurrentThread == AwesomiumWPFWebWindowFactory.WebCoreThread;
        }
    }
}
