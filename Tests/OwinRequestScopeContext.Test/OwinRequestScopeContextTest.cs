using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Owin.Test
{
    [TestClass]
    public class OwinRequestScopeContextTest
    {
        [TestMethod]
        public async Task Current()
        {
            var blankEnvironment = new Dictionary<string, object>();

            OwinRequestScopeContext.Current = new OwinRequestScopeContext(blankEnvironment, true);

            OwinRequestScopeContext.Current.Items["test"] = 10;
            OwinRequestScopeContext.Current.Items["test2"] = 100;

            await Task.Delay(TimeSpan.FromMilliseconds(10));

            // transparent to await
            OwinRequestScopeContext.Current.IsNotNull();
            OwinRequestScopeContext.Current.Items["test"].Is(10);
            OwinRequestScopeContext.Current.Items["test2"].Is(100);

            await Task.Delay(TimeSpan.FromMilliseconds(10)).ConfigureAwait(false);

            OwinRequestScopeContext.Current.IsNotNull();
            OwinRequestScopeContext.Current.Items["test"].Is(10);
            OwinRequestScopeContext.Current.Items["test2"].Is(100);

            // run another thread
            var t = Task.Run(() =>
            {
                OwinRequestScopeContext.Current.IsNotNull();
                OwinRequestScopeContext.Current.Items["test"].Is(10);
                OwinRequestScopeContext.Current.Items["test2"].Is(100);
            });

            await t;

            var semaphore = new SemaphoreSlim(1);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                OwinRequestScopeContext.Current.IsNotNull();
                OwinRequestScopeContext.Current.Items["test"].Is(10);
                OwinRequestScopeContext.Current.Items["test2"].Is(100);
                semaphore.Release();
            });

            await semaphore.WaitAsync();
        }

        [TestMethod]
        public async Task IsolateTest()
        {
            var blankEnvironment = new Dictionary<string, object>();

            var aInitialized = new SemaphoreSlim(1);
            var bFinished = new SemaphoreSlim(1);

            var a = Task.Run(async () =>
            {
                OwinRequestScopeContext.Current = new OwinRequestScopeContext(blankEnvironment, true);
                OwinRequestScopeContext.Current.Items["test"] = "foo";
                aInitialized.Release();

                await bFinished.WaitAsync();
                OwinRequestScopeContext.Current.Items["test"].Is("foo");
            });
            
            var b = Task.Run(async () =>
            {
                await aInitialized.WaitAsync();

                OwinRequestScopeContext.Current.IsNull();
                OwinRequestScopeContext.Current = new OwinRequestScopeContext(blankEnvironment, true);
                OwinRequestScopeContext.Current.Items["test"] = "bar";

                bFinished.Release();
            });

            await Task.WhenAll();
        }
    }
}