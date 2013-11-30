using Microsoft.Owin;
using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;

// first concept desgign.

namespace ConsoleApplication23
{
    class Program
    {
        static void Main(string[] args)
        {
            // Timestamp
            //HttpContext.Current.DisposeOnPipelineCompleted(
            // HttpContext.Current.Timestamp

            using (WebApp.Start<Startup>("http://localhost:12345"))
            {
                Console.ReadLine();
            }
        }
    }

    public class OwinRequestScopeSynchronizationContext : SynchronizationContext
    {
        OwinRequestScopeContext capturedContext;
        SynchronizationContext existsContext;

        public OwinRequestScopeSynchronizationContext(OwinRequestScopeContext context)
        {
            this.capturedContext = context;
            this.existsContext = SynchronizationContext.Current;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            if (existsContext != null)
            {
                existsContext.Post(_ =>
                 {
                     OwinRequestScopeContext.Current = capturedContext;
                     SetSynchronizationContext(new OwinRequestScopeSynchronizationContext(capturedContext)); // recapture syncContext
                     d(state);
                 }, null);
            }
            else
            {
                OwinRequestScopeContext.Current = capturedContext;
                SetSynchronizationContext(this);
                d(state);
            }
        }
    }

    public class OwinRequestScopeContext
    {
        const string CallContextKey = "owin.rscopectx";

        public static OwinRequestScopeContext Current
        {
            get
            {
                return (OwinRequestScopeContext)CallContext.LogicalGetData(CallContextKey);
            }
            set
            {
                CallContext.LogicalSetData(CallContextKey, value);
            }
        }

        readonly DateTime utcTimestamp = DateTime.UtcNow;
        readonly List<IDisposable> disposables = new List<IDisposable>();

        public IDictionary<string, object> Environment { get; private set; }
        public IDictionary<string, object> Items { get; private set; }
        public DateTime Timestamp { get { return utcTimestamp.ToLocalTime(); } }

        public OwinRequestScopeContext(IDictionary<string, object> environment, bool threadSafeItem)
        {
            this.utcTimestamp = DateTime.UtcNow;
            this.Environment = environment;
            this.Items = (threadSafeItem)
                ? new ConcurrentDictionary<string, object>()
                : (IDictionary<string, object>)new Dictionary<string, object>();
        }

        public IDisposable DisposeOnPipelineCompleted(IDisposable target)
        {
            if (target == null) throw new ArgumentNullException("target");

            var token = new UnsubscribeDisposable(target);
            disposables.Add(token);
            return token;
        }

        internal void Complete()
        {
            var exceptions = new List<Exception>();
            try
            {
                foreach (var item in disposables)
                {
                    item.Dispose();
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
            finally
            {
                if (exceptions.Any())
                {
                    throw new AggregateException("failed on disposing", exceptions);
                }
            }
        }
    }

    public class UnsubscribeDisposable : IDisposable
    {
        IDisposable target;
        bool unsubscribe = false;

        public UnsubscribeDisposable(IDisposable target)
        {
            this.target = target;
        }

        public void CallTargetDispose()
        {
            if (!unsubscribe)
            {
                target.Dispose();
            }
        }

        public void Dispose()
        {
            unsubscribe = true;
        }
    }

    public class OwinRequestScopeContextMiddleware : OwinMiddleware
    {
        readonly bool threadSafeItem;

        public OwinRequestScopeContextMiddleware(OwinMiddleware next)
            : this(next, threadSafeItem: false)
        {

        }

        public OwinRequestScopeContextMiddleware(OwinMiddleware next, bool threadSafeItem)
            : base(next)
        {
            this.threadSafeItem = threadSafeItem;
        }


        public async override Task Invoke(IOwinContext context)
        {
            var scopeContext = new OwinRequestScopeContext(context.Environment, threadSafeItem);
            OwinRequestScopeContext.Current = scopeContext;
            var syncContext = new OwinRequestScopeSynchronizationContext(scopeContext);
            SynchronizationContext.SetSynchronizationContext(syncContext);

            try
            {
                await this.Next.Invoke(context);
            }
            finally
            {
                scopeContext.Complete();
            }
        }
    }


    public class Startup
    {
        public void Configuration(Owin.IAppBuilder app)
        {
            var v = app.Properties;

            app.Use(typeof(OwinRequestScopeContextMiddleware));

            app.Run(async ctx =>
            {
                var _ = OwinRequestScopeContext.Current;
                var tid = Thread.CurrentThread.ManagedThreadId;

                ctx.Response.ContentType = "text/plain";
                await ctx.Response.WriteAsync("hello");
                await Task.Delay(TimeSpan.FromSeconds(1));


                var tid2 = Thread.CurrentThread.ManagedThreadId;

                var __ = OwinRequestScopeContext.Current;

                Console.WriteLine(__);
            });





        }
    }


}
