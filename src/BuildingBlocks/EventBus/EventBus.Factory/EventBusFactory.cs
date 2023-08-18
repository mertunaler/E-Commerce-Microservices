﻿using EventBus.Base;
using EventBus.Base.Abstract;
using EventBus.RabbitMQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Factory
{
    public static class EventBusFactory
    {

        public static IEventBus Create(EventBusConfig config, IServiceProvider provider)
        {
            return config.EventBusType switch
            {
                EventBusType.RabbitMQ => new EventBusRabbitMQ(config, provider)

            }; 


        }
    }
}
