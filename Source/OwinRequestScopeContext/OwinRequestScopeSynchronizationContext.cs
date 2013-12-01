using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Owin
{
    internal class OwinRequestScopeSynchronizationContext : SynchronizationContext
    {
        IOwinRequestScopeContext capturedContext;
        SynchronizationContext existsContext;

        public OwinRequestScopeSynchronizationContext(IOwinRequestScopeContext context)
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
}