using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Base.Events
{
    public class IntegrationEvent
    {
        public IntegrationEvent()
        {

        }

        [JsonConstructor]
        public IntegrationEvent(Guid id, DateTime createdDate)
        {
            Id = id;
            CreatedDate = createdDate;
        }
        [JsonProperty]
        public Guid Id { get; set; }

        [JsonProperty]
        public DateTime CreatedDate { get; set; }



    }
}
