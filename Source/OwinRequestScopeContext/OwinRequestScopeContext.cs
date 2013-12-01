using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;

namespace Owin
{
    public interface IOwinRequestScopeContext
    {
        IDisposable DisposeOnPipelineCompleted(IDisposable target);
        IDictionary<string, object> Environment { get; }
        IDictionary<string, object> Items { get; }
        DateTime Timestamp { get; }
    }

    public class OwinRequestScopeContext : IOwinRequestScopeContext
    {
        const string CallContextKey = "owin.rscopectx";

        public static IOwinRequestScopeContext Current
        {
            get
            {
                return (IOwinRequestScopeContext)CallContext.LogicalGetData(CallContextKey);
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
}