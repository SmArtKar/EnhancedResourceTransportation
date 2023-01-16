using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EnhancedResourceTransportation
{
    public class Blueprint_Conveyor : Blueprint_Build
    {
        public override Graphic Graphic
        {
            get
            {
                return base.DefaultGraphic;
            }
        }

        public override void Draw()
        {
            base.Draw();
        }

        public Blueprint_Conveyor()
        {
        }
    }
}
