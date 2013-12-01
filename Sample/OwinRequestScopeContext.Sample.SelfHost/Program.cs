using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Sample.SelfHost
{
    class Program
    {
        static void Main(string[] args)
        {
            using (WebApp.Start<Startup>("http://localhost:12345"))
            {
                Console.ReadLine();
            }
        }
    }

    public class Startup
    {
        public void Configuration(Owin.IAppBuilder app)
        {
            // using Owin; you can use UseRequestScopeContext extension method.
            // enabled timing is according to Pipeline.
            // so I recommend enable as far in advance as possible.
            app.UseRequestScopeContext();

            app.UseErrorPage();
            app.Run(async _ =>
            {
                // get global context like HttpContext.Current.
                var context = OwinRequestScopeContext.Current;

                // Environment is raw Owin Environment as IDictionary<string, object>.
                var __ = context.Environment;

                // optional:If you want to change Microsoft.Owin.OwinContext, you can wrap.
                new Microsoft.Owin.OwinContext(context.Environment);

                // Timestamp is request started(correctly called RequestScopeContextMiddleware timing).
                var ___ = context.Timestamp;

                // Items is IDictionary<string, object> like HttpContext#Items.
                // Items is threadsafe(as ConcurrentDictionary) by default.
                var ____ = context.Items;

                // DisposeOnPipelineCompleted can register dispose when request finished(correctly RequestScopeContextMiddleware underling Middlewares finished)
                // return value is cancelToken. If call token.Dispose() then canceled register.
                var cancelToken = context.DisposeOnPipelineCompleted(new TraceDisposable());

                // OwinRequestScopeContext over async/await also ConfigureAwait(false)
                context.Items["test"] = "foo";
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                var _____ = OwinRequestScopeContext.Current.Items["test"]; // foo

                await Task.Run(() =>
                {
                    // OwinRequestScopeContext over new thread/threadpool.
                    var ______ = OwinRequestScopeContext.Current.Items["test"]; // foo
                });

                _.Response.ContentType = "text/plain";
                await _.Response.WriteAsync("Hello OwinRequestScopeContext! => ");
                await _.Response.WriteAsync(OwinRequestScopeContext.Current.Items["test"] as string); // render foo
            });
        }
    }

    public class TraceDisposable : IDisposable
    {
        public void Dispose()
        {
            Debug.WriteLine("Disposed");
        }
    }
}