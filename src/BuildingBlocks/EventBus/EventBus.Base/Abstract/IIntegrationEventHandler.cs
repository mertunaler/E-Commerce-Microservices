using EventBus.Base.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Services will implement this interface
namespace EventBus.Base.Abstract
{
    public interface IIntegrationEventHandler <T>: IntegrationEventHandler where T : IntegrationEvent
    {
        Task Handle(T @event);
    }

    //Marker interface indicates whoever implements that interface, shows that it's is an handler of an event.
    public interface IntegrationEventHandler { }
}
