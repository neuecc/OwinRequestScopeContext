using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.SelfHost
{
    using System.Collections.Generic;
    using AppFunc = Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

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
            app.UseRequestScopeContext();
            app.UseErrorPage();


            app.Use(typeof(SimpleHandlerMiddleware));

            // app.Use<ExceptionTestMiddleware>();

            //app.Run(async ctx =>
            //{
            //    var _ = OwinRequestScopeContext .Current;
            //    var tid = Thread.CurrentThread.ManagedThreadId;

            //    ctx.Response.ContentType = "text/plain";
            //    await ctx.Response.WriteAsync("hello");
            //    await Task.Delay(TimeSpan.FromSeconds(1));


            //    var tid2 = Thread.CurrentThread.ManagedThreadId;

            //    var __ = OwinRequestScopeContext.Current;

            //    Console.WriteLine(__);
            //});
        }
    }

    public class SimpleHandlerMiddleware
    {
        readonly AppFunc next;

        public SimpleHandlerMiddleware(AppFunc next)
        {
            this.next = next;

        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            var ctx = new Microsoft.Owin.OwinContext(environment);
            ctx.Response.ContentType = "text/plain";

            //var vv4 = System.Runtime.Remoting.Messaging.CallContext.GetData("owin.rscopectx");
            System.Runtime.Remoting.Messaging.CallContext.LogicalSetData("owin.rscopectx", "hogehoge");

            var ______ = OwinRequestScopeContext.Current;

            await ctx.Response.WriteAsync("hello");
            await Task.Delay(TimeSpan.FromSeconds(1));

            var vv = System.Runtime.Remoting.Messaging.CallContext.GetData("owin.rscopectx");

            var t = Task.Run(() =>
            {
                var v = System.Runtime.Remoting.Messaging.CallContext.GetData("owin.rscopectx");
                var ___ = v;
            });

            var ________ = OwinRequestScopeContext.Current;









            await this.next.Invoke(environment);
        }
    }

    public class ExceptionTestMiddleware : Microsoft.Owin.OwinMiddleware
    {
        public ExceptionTestMiddleware(Microsoft.Owin.OwinMiddleware next)
            : base(next)
        {

        }

        public override Task Invoke(Microsoft.Owin.IOwinContext context)
        {
            throw new Exception();
        }
    }
}