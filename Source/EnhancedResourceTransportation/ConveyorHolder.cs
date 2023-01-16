using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EnhancedResourceTransportation
{
    public class ConveyorHolder
    {
        public Thing thing;
        public float currentPoint;

        public ConveyorHolder(Thing thing, float currentPoint = 0f)
        {
            this.thing = thing;
            this.currentPoint = currentPoint;
        }
    }
}
