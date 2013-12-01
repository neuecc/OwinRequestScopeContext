using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;

namespace Owin
{
    /// <summary>
    /// RequestScopeContext for Owin.
    /// </summary>
    public interface IOwinRequestScopeContext
    {
        /// <summary>
        /// <para>Enables an object's Dispose method to be called when the request completed.</para>
        /// <para>Return value is subscription token. If calle token.Dispose() then canceled register.</para>
        /// </summary>
        /// <param name="target">IDisposable item.</param>
        IDisposable DisposeOnPipelineCompleted(IDisposable target);

        /// <summary>
        /// Raw Owin Environment dictionary.
        /// </summary>
        IDictionary<string, object> Environment { get; }

        /// <summary>
        /// Gets a key/value collection that can be used to organize and share data during an HTTP request.
        /// </summary>
        IDictionary<string, object> Items { get; }

        /// <summary>
        /// Gets the initial timestamp of the current HTTP request.
        /// </summary>
        DateTime Timestamp { get; }
    }

    public class OwinRequestScopeContext : IOwinRequestScopeContext
    {
        const string CallContextKey = "owin.rscopectx";

        /// <summary>
        /// Gets or sets the IOwinRequestScopeContext object for the current HTTP request.
        /// </summary>
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