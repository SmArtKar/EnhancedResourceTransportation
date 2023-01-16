using HotSwap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EnhancedResourceTransportation
{
    [HotSwappable]
    public class MapComponent_ConveyorHandler : MapComponent
    {
        public List<ConveyorSegment> segments;

        public MapComponent_ConveyorHandler(Map map) : base(map)
        {
            segments = new List<ConveyorSegment>();
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            foreach(ConveyorSegment segment in segments)
            {
                segment.Tick();
            }
        }

        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();

            foreach (ConveyorSegment segment in segments)
            {
                segment.Draw();
            }
        }
    }
}