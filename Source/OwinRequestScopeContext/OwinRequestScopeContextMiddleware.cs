using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Owin
{
    using AppFunc = Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

    public class OwinRequestScopeContextMiddleware
    {
        readonly AppFunc next;
        readonly bool threadSafeItem;

        public OwinRequestScopeContextMiddleware(AppFunc next)
            : this(next, threadSafeItem: false)
        {

        }

        public OwinRequestScopeContextMiddleware(AppFunc next, bool threadSafeItem)
        {
            this.next = next;
            this.threadSafeItem = threadSafeItem;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            var scopeContext = new OwinRequestScopeContext(environment, threadSafeItem);
            OwinRequestScopeContext.Current = scopeContext;

            try
            {
                await next(environment);
            }
            finally
            {
                try
                {
                    scopeContext.Complete();
                }
                finally
                {
                    OwinRequestScopeContext.FreeContextSlot();
                }
            }
        }
    }

    public static class AppBuilderOwinRequestScopeContextMiddlewareExtensions
    {
        /// <summary>
        /// Use OwinRequestScopeContextMiddleware.
        /// </summary>
        /// <param name="app">Owin app.</param>
        /// <param name="isThreadsafeItem">OwinRequestScopeContext.Item is threadsafe or not. Default is threadsafe.</param>
        /// <returns></returns>
        public static IAppBuilder UseRequestScopeContext(this IAppBuilder app, bool isThreadsafeItem = true)
        {
            return app.Use(typeof(OwinRequestScopeContextMiddleware), isThreadsafeItem);
        }
    }
}