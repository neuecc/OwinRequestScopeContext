using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Threading;
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
            app.UseRequestScopeContext();

            app.Run(async ctx =>
            {
                var _ = OwinRequestScopeContext .Current;
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
