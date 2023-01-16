using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.Noise;
using HotSwap;
using UnityEngine;

namespace EnhancedResourceTransportation
{
    [HotSwappable]
    public class ConveyorSegment
    {
        public List<ConveyorBelt> belts;
        public ConveyorBelt startBelt;
        public ConveyorBelt endBelt;
        public List<ConveyorHolder> movingItems = new List<ConveyorHolder>();
        public ThingOwner<Thing> holder;

        public bool blockedEnd = false;

        public ConveyorSegment(List<ConveyorBelt> belts)
        {
            this.belts = belts;
            this.startBelt = belts[0];
            this.endBelt = belts[belts.Count - 1];
            holder = new ThingOwner<Thing>();

            foreach (ConveyorBelt belt in belts)
            {
                belt.segment = this;
            }

            startBelt.Map.GetComponent<MapComponent_ConveyorHandler>().segments.Add(this);
        }

        public virtual bool EmptyTile(Thing thing, IntVec3 tile, Map map)
        {
            if (!tile.IsValid || !tile.InBounds(map) || !tile.Standable(map))
            {
                return false;
            }

            List<Thing> thingsAt = map.thingGrid.ThingsListAt(tile);

            for (int i = thingsAt.Count - 1; i >= 0; i--)
            {
                Thing thingAt = thingsAt[i];

                if (thing.IsSaveCompressible() && thingAt.IsSaveCompressible())
                {
                    return false;
                }

                if (thingAt.def.category == ThingCategory.Item)
                {
                    if (!thingAt.CanStackWith(thing))
                    {
                        return false;
                    }

                    if (thingAt.stackCount + thing.stackCount > thingAt.def.stackLimit)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public virtual void Tick()
        {
            /// ====================
            /// Temporary code start
            /// ====================

            foreach (Thing grabbies in startBelt.Map.thingGrid.ThingsListAt(startBelt.Position - startBelt.Rotation.FacingCell))
            {
                if (grabbies.def.category != ThingCategory.Item)
                {
                    continue;
                }

                startBelt.Accept(grabbies);

                break;
            }

            /// ====================
            /// Temporary code end
            /// ====================

            for (int i = movingItems.Count - 1; i >= 0; i--)
            {
                ConveyorHolder itemHolder = movingItems[i];

                Thing thing = itemHolder.thing;
                float currentPoint = itemHolder.currentPoint;

                if (currentPoint >= belts.Count + 0.5f)
                {
                    endBelt.Dropoff(itemHolder);
                    continue;
                }

                int conveyorID = (int)Math.Floor(currentPoint);
                conveyorID = Math.Min(Math.Max(conveyorID, 0), belts.Count - 1);

                ConveyorBelt belt = belts[conveyorID];

                float moveSpeed = 1 / (float)belt.extension.travelTicks;

                if (i > 0)
                {
                    ConveyorHolder nextItemHolder = movingItems[i - 1];

                    Thing nextThing = nextItemHolder.thing;
                    float nextPoint = nextItemHolder.currentPoint;

                    if (nextPoint - currentPoint < belt.extension.stackDistance)
                    {
                        continue;
                    }

                    if (nextPoint - currentPoint < belt.extension.stackDistance + moveSpeed)
                    {
                        moveSpeed = nextPoint - currentPoint - belt.extension.stackDistance;
                    }
                }

                if (belt == endBelt && currentPoint >= belts.Count + 0.5f - belt.extension.stackDistance)
                {
                    if (!endBelt.CanProceed(itemHolder))
                    {
                        continue;
                    }
                }

                itemHolder.currentPoint = currentPoint + moveSpeed;

                if (currentPoint >= belts.Count + 0.5f)
                {
                    itemHolder.currentPoint = belts.Count + 0.5f;
                }
            }
        }

        public virtual void Draw()
        {
            for (int i = movingItems.Count - 1; i >= 0; i--)
            {
                ConveyorHolder itemHolder = movingItems[i];

                Thing thing = itemHolder.thing;
                float currentPoint = itemHolder.currentPoint;

                int conveyorID = (int)Math.Floor(currentPoint);
                conveyorID = Math.Min(Math.Max(conveyorID, 0), belts.Count - 1);

                ConveyorBelt belt = belts[conveyorID];

                if (currentPoint >= belts.Count)
                {
                    IntVec3 posDiff = belt.Rotation.FacingCell;

                    if (belt.childBelt != null)
                    {
                        posDiff = belt.Child.Position - belt.Position;
                    }

                    thing.Rotation = Rot4.FromIntVec3(posDiff * -1);
                    thing.DrawAt(belt.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverhead) + posDiff.ToVector3() * (currentPoint % 1 + 0.5f));
                }
                else if (currentPoint % 1 <= 0.5)
                {
                    IntVec3 posDiff = belt.Rotation.FacingCell * -1;

                    if (belt.Parent != null)
                    {
                        posDiff = belt.Parent.Position - belt.Position;
                    }

                    thing.Rotation = Rot4.FromIntVec3(posDiff);
                    thing.DrawAt(belt.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverhead) - posDiff.ToVector3() * (currentPoint % 1 - 0.5f));
                }
                else
                {
                    IntVec3 posDiff = belt.Rotation.FacingCell;

                    if (belt.childBelt != null)
                    {
                        posDiff = belt.Child.Position - belt.Position;
                    }

                    thing.Rotation = Rot4.FromIntVec3(posDiff * -1);
                    thing.DrawAt(belt.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverhead) + posDiff.ToVector3() * (currentPoint % 1 - 0.5f));
                }
            }
        }

        public virtual void Merge(ConveyorSegment segment, bool start)
        {
            foreach (ConveyorBelt belt in segment.belts)
            {
                belt.segment = this;
            }

            if (start)
            {
                for (int i = 0; i < segment.movingItems.Count; i++)
                {
                    ConveyorHolder thingHolder = segment.movingItems[i];
                    thingHolder.currentPoint += belts.Count;

                    movingItems.Add(thingHolder);
                }

                belts = belts.Concat(segment.belts).ToList();
            }
            else
            {
                for (int i = 0; i < movingItems.Count; i++)
                {
                    ConveyorHolder thingHolder = movingItems[i];
                    thingHolder.currentPoint += segment.belts.Count;

                    segment.movingItems.Add(thingHolder);
                }

                movingItems = segment.movingItems;
                belts = segment.belts.Concat(belts).ToList();
            }

            startBelt = belts[0];
            endBelt = belts[belts.Count - 1];

            startBelt.Map.GetComponent<MapComponent_ConveyorHandler>().segments.Remove(segment);
        }

        public virtual void Cut(ConveyorBelt cutPoint)
        {
            if (cutPoint.Child != null)
            {
                cutPoint.childBelt.Parent = null;
            }

            if (cutPoint.Parent != null)
            {
                cutPoint.parentBelt.Child = null;
            }

            int cutIndex = belts.IndexOf(cutPoint);

            ConveyorSegment firstSegment = null;
            if (cutIndex > 0)
            {
                firstSegment = new ConveyorSegment(belts.Take(cutIndex).ToList());
            }

            ConveyorSegment secondSegment = null;

            if (cutIndex < belts.Count - 1)
            {
                secondSegment = new ConveyorSegment(belts.Skip(cutIndex + 1).ToList());
            }

            for (int i = movingItems.Count - 1; i >= 0; i--)
            {
                ConveyorHolder itemHolder = movingItems[i];

                Thing thing = itemHolder.thing;
                float currentPoint = itemHolder.currentPoint;

                if (currentPoint >= cutIndex + 1)
                {
                    itemHolder.currentPoint -= cutIndex + 1;
                    secondSegment.movingItems.Add(itemHolder);
                    holder.Remove(thing);
                    secondSegment.holder.TryAdd(thing, false);
                }
                else if (currentPoint < cutIndex)
                {
                    firstSegment.movingItems.Add(itemHolder);
                    holder.Remove(thing);
                    firstSegment.holder.TryAdd(thing, false);
                }
                else
                {
                    holder.TryDrop(thing, cutPoint.Position, cutPoint.MapHeld, ThingPlaceMode.Direct, out Thing outThing);
                }
            }

            startBelt.Map.GetComponent<MapComponent_ConveyorHandler>().segments.Remove(this);
        }
    }
}
