using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

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
    }
}
