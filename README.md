OwinRequestScopeContext
=======================

Owin Middleware it is possible to RequestScopeContext like HttpContext.Current.

Install
---
using with NuGet, [OwinRequestScopeContext](https://nuget.org/packages/OwinRequestScopeContext/)
```
PM> Install-Package OwinRequestScopeContext
```

Usage
---
```csharp
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
```

Full code is avaliable on this Repositry, [a](https://github.com/neuecc/OwinRequestScopeContext/tree/master/OwinRequestScopeContext.Sample.SelfHost).

History
---
2013-12-01 ver 1.0.0
* first release.

License
---
under [MIT License](http://opensource.org/licenses/MIT)