using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EnhancedResourceTransportation
{
    public interface IConveyor
    {
        public abstract Building Child { get; set; }
        public abstract Building Parent { get; set; }

        public abstract bool CanLink(IConveyor link, bool newChild);

        public abstract void LinkTo(IConveyor link, bool newChild);

        public abstract bool Accept(Thing thing, IConveyor depositor = null);

        public abstract void UpdateLinks();

        public abstract bool Dropoff(ConveyorHolder holder);
    }
}
