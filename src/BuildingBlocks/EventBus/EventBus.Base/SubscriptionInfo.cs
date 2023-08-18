using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Base
{
    //Holds infos of the subsriptions 
    public class SubscriptionInfo
    {
        public SubscriptionInfo(Type handler)
        {
            HandlerType = handler;
        }
        public Type HandlerType { get; }

        public static SubscriptionInfo Typed(Type handlerType)
        {
            return new SubscriptionInfo(handlerType);   
        }
    }
}
