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
        public class ComplexType
        {
            public int Hoge { get; set; }
            public string Huga { get; set; }
        }

        [TestMethod]
        public async Task Current()
        {
            var blankEnvironment = new Dictionary<string, object>();

            OwinRequestScopeContext.Current = new OwinRequestScopeContext(blankEnvironment, true);

            OwinRequestScopeContext.Current.Items["test"] = 10;
            OwinRequestScopeContext.Current.Items["test2"] = 100;
            OwinRequestScopeContext.Current.Items["test3"] = new ComplexType { Hoge = 10, Huga = "aaa" };

            await Task.Delay(TimeSpan.FromMilliseconds(10));

            // transparent to await
            OwinRequestScopeContext.Current.IsNotNull();
            OwinRequestScopeContext.Current.Items["test"].Is(10);
            OwinRequestScopeContext.Current.Items["test2"].Is(100);
            (OwinRequestScopeContext.Current.Items["test3"] as ComplexType).Hoge.Is(10);
            (OwinRequestScopeContext.Current.Items["test3"] as ComplexType).Huga.Is("aaa");

            await Task.Delay(TimeSpan.FromMilliseconds(10)).ConfigureAwait(false);

            OwinRequestScopeContext.Current.IsNotNull();
            OwinRequestScopeContext.Current.Items["test"].Is(10);
            OwinRequestScopeContext.Current.Items["test2"].Is(100);
            (OwinRequestScopeContext.Current.Items["test3"] as ComplexType).Hoge.Is(10);
            (OwinRequestScopeContext.Current.Items["test3"] as ComplexType).Huga.Is("aaa");

            var threadId = Thread.CurrentThread.ManagedThreadId;

            // run another thread
            var t = Task.Factory.StartNew(() =>
            {
                Thread.CurrentThread.ManagedThreadId.IsNot(threadId);

                OwinRequestScopeContext.Current.IsNotNull();
                OwinRequestScopeContext.Current.Items["test"].Is(10);
                OwinRequestScopeContext.Current.Items["test2"].Is(100);
                (OwinRequestScopeContext.Current.Items["test3"] as ComplexType).Hoge.Is(10);
                (OwinRequestScopeContext.Current.Items["test3"] as ComplexType).Huga.Is("aaa");
            }, TaskCreationOptions.LongRunning);

            await t;

            var semaphore = new SemaphoreSlim(1);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.CurrentThread.ManagedThreadId.IsNot(threadId);

                OwinRequestScopeContext.Current.IsNotNull();
                OwinRequestScopeContext.Current.Items["test"].Is(10);
                OwinRequestScopeContext.Current.Items["test2"].Is(100);
                (OwinRequestScopeContext.Current.Items["test3"] as ComplexType).Hoge.Is(10);
                (OwinRequestScopeContext.Current.Items["test3"] as ComplexType).Huga.Is("aaa");
                semaphore.Release();
            });

            await semaphore.WaitAsync();

            new Thread(_ =>
            {
                Thread.CurrentThread.ManagedThreadId.IsNot(threadId);

                OwinRequestScopeContext.Current.IsNotNull();
                OwinRequestScopeContext.Current.Items["test"].Is(10);
                OwinRequestScopeContext.Current.Items["test2"].Is(100);
                (OwinRequestScopeContext.Current.Items["test3"] as ComplexType).Hoge.Is(10);
                (OwinRequestScopeContext.Current.Items["test3"] as ComplexType).Huga.Is("aaa");
                semaphore.Release();
            }).Start();

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

        [TestMethod]
        public void DisposeOnPipelineCompleted()
        {
            foreach (var threadSafe in new[] { true, false })
            {
                var blankEnvironment = new Dictionary<string, object>();
                var context = new OwinRequestScopeContext(blankEnvironment, threadSafe);

                var disp = new MonitorDisposable();
                disp.IsDisposeCalled.IsFalse();
                context.DisposeOnPipelineCompleted(disp);
                disp.IsDisposeCalled.IsFalse();

                context.AsDynamic().Complete(); // internal complete method

                disp.IsDisposeCalled.IsTrue();
            }
        }

        [TestMethod]
        public void DisposeOnPipelineCompleted_Cancel()
        {
            foreach (var threadSafe in new[] { true, false })
            {
                var blankEnvironment = new Dictionary<string, object>();
                var context = new OwinRequestScopeContext(blankEnvironment, threadSafe);

                var disp = new MonitorDisposable();
                disp.IsDisposeCalled.IsFalse();
                var token = context.DisposeOnPipelineCompleted(disp);
                disp.IsDisposeCalled.IsFalse();

                token.Dispose();

                context.AsDynamic().Complete(); // internal complete method

                disp.IsDisposeCalled.IsFalse();
            }
        }
    }

    public class MonitorDisposable : IDisposable
    {
        public bool IsDisposeCalled { get; set; }
        public void Dispose()
        {
            IsDisposeCalled = true;
        }
    }
}