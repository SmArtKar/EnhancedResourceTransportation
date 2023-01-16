using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;
using HotSwap;

namespace EnhancedResourceTransportation
{
    [HotSwappable]
    public class Graphic_Conveyor : Graphic
    {
        public override Material MatSingle
        {
            get
            {
                return subGraphics[0].MatSingle;
            }
        }

        public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
        {
            return GraphicDatabase.Get<Graphic_Conveyor>(path, newShader, drawSize, newColor, newColorTwo, data, null);
        }

        public override void Init(GraphicRequest req)
        {
            data = req.graphicData;
            path = req.path;
            color = req.color;
            drawSize = req.drawSize;

            subGraphics = new List<Graphic>();
            for (int i = 0; i < 24; i++)
            {
                subGraphics.Add(GraphicDatabase.Get<Graphic_Single>(path + "_" + i, req.shader, drawSize, color));
            }
        }

        public override Material MatAt(Rot4 rot, Thing thing = null)
        {
            return SubGraphicFor(thing).MatAt(rot, thing);
        }

        public override Material MatSingleFor(Thing thing)
        {
            return SubGraphicFor(thing).MatSingleFor(thing);
        }

        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            SubGraphicFor(thing).DrawWorker(loc, rot, thingDef, thing, extraRotation);
        }

        public virtual Graphic SubGraphicFor(Thing thing)
        {
            if (thing == null)
            {
                return subGraphics[0];
            }

            if (thing is not ConveyorBelt)
            {
                return subGraphics[thing.Rotation.AsInt * 4];
            }

            ConveyorBelt belt = thing as ConveyorBelt;

            int num = belt.Rotation.AsInt * 4;

            if (belt.Parent == null)
            {
                num += 2;
            }
            else if (belt.Parent.Position != belt.Position - belt.Rotation.FacingCell)
            {
                num = 16 + belt.Parent.Rotation.AsInt * 2;

                if (belt.Parent.Rotation.AsInt != (belt.Rotation.AsInt + 1) % 4)
                {
                    num += 1;
                }

                return subGraphics[num];
            }

            if (belt.Child == null)
            {
                num += 1;
            }

            return subGraphics[num];
        }

        public List<Graphic> subGraphics;
    }
}
