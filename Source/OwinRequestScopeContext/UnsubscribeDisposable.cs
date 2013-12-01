using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Owin
{
    internal class UnsubscribeDisposable : IDisposable
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
}