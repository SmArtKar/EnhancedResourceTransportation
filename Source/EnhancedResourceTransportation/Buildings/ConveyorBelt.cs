using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using TMPro;
using Verse;
using Verse.Noise;
using HotSwap;
using System.Runtime.Remoting.Messaging;
using Mono.Unix.Native;

namespace EnhancedResourceTransportation
{
    [HotSwappable]
    public class ConveyorBelt : Building, IConveyor
    {
        public IConveyor parentBelt;
        public IConveyor childBelt;

        public ConveyorExtension extension;
        public ConveyorSegment segment;

        public Building Child
        {
            get
            {
                return childBelt as Building;
            }

            set
            {
                childBelt = value as IConveyor;
            }
        }

        public
            Building Parent
        {
            get
            {
                return parentBelt as Building;
            }

            set
            {
                parentBelt = value as IConveyor;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (respawningAfterLoad)
            {
                return;
            }

            extension = def.GetModExtension<ConveyorExtension>();
            if (extension == null)
            {
                Log.Error("Attempted to spawn a " + def.defName + " conveyor without a ConveyorExtension");
                Destroy();
                return;
            }

            UpdateLinks();
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (segment != null)
            {
                segment.Cut(this);
            }

            base.Destroy(mode);
        }

        public virtual bool Dropoff(ConveyorHolder holder)
        {
            IntVec3 dropTile = Position + Rotation.FacingCell;

            if (segment.EmptyTile(holder.thing, dropTile, Map))
            {
                if (segment.holder.TryDrop(holder.thing, dropTile, MapHeld, ThingPlaceMode.Direct, out Thing outThing))
                {
                    segment.movingItems.Remove(holder);
                    return true;
                }
            }

            return false;
        }

        public virtual bool Accept(Thing thing, IConveyor depositor = null)
        {
            if (thing.def.category != ThingCategory.Item)
            {
                return false;
            }

            if (segment.movingItems.Count > 0)
            {
                ConveyorHolder holder = segment.movingItems[segment.movingItems.Count - 1];
                if (holder.currentPoint <= segment.startBelt.extension.stackDistance - 0.5f)
                {
                    return false;
                }
            }

            if (thing.Spawned)
            {
                thing.DeSpawn(DestroyMode.WillReplace);
            }

            if (!segment.holder.TryAdd(thing, false))
            {
                GenDrop.TryDropSpawn(thing, thing.PositionHeld, thing.MapHeld, ThingPlaceMode.Direct, out Thing dropThing, playDropSound: false);
                return false;
            }

            segment.movingItems.Add(new ConveyorHolder(thing, -0.5f));
            return true;
        }

        public virtual void UpdateLinks()
        {
            if (Parent == null)
            {
                IConveyor newParentBelt = GridsUtility.GetThingList(Position - Rotation.FacingCell, Map).OfType<IConveyor>().FirstOrDefault();

                if (newParentBelt != null)
                {
                    
                    if (CanLink(newParentBelt, false) && newParentBelt.CanLink(this, true))
                    {
                        LinkTo(newParentBelt, false);
                        newParentBelt.LinkTo(this, true);
                    }
                }
            }

            if (Child == null)
            {
                IConveyor newChildBelt = GridsUtility.GetThingList(Position + Rotation.FacingCell, Map).OfType<IConveyor>().FirstOrDefault();

                if (newChildBelt != null)
                {
                    if (CanLink(newChildBelt, true) && newChildBelt.CanLink(this, false))
                    {
                        LinkTo(newChildBelt, true);
                        newChildBelt.LinkTo(this, false);
                    }
                }
            }

            segment = new ConveyorSegment(new List<ConveyorBelt>() { this });

            if (Parent != null && Parent is ConveyorBelt)
            {
                ConveyorBelt parent = Parent as ConveyorBelt;

                if (parent.segment != segment)
                {
                    segment.Merge(parent.segment, false);
                }
            }

            if (Child != null && Child is ConveyorBelt)
            {
                ConveyorBelt child = Child as ConveyorBelt;

                if (child.segment != segment)
                {
                    segment.Merge(child.segment, true);
                }
            }
        }

        public virtual void LinkTo(IConveyor link, bool newChild)
        {
            if (newChild)
            {
                childBelt = link;
                return;
            }

            parentBelt = link;
            Parent.Rotation = new Rot4(Rotation.AsInt);
        }

        public virtual bool CanLink(IConveyor link, bool newChild)
        {
            Building build = link as Building;
            ConveyorBelt newBelt = link as ConveyorBelt;

            if (build.Rotation.Opposite == Rotation)
            {
                return false;
            }

            if (newChild)
            {
                if (newBelt.parentBelt != null)
                {
                    return false;
                }

                if (Parent != null)
                {
                    ConveyorBelt parent = Parent as ConveyorBelt;

                    if (parent.segment == newBelt.segment)
                    {
                        return false;
                    }
                }

                return true;
            }


            if (newBelt != null)
            {   
                if (newBelt.childBelt != null)
                {
                    return false;
                }

                if (Child != null)
                {
                    ConveyorBelt child = Child as ConveyorBelt;

                    if (child.segment == newBelt.segment)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public virtual bool CanProceed(ConveyorHolder holder)
        {


            if (segment.EmptyTile(holder.thing, Position + Rotation.FacingCell, Map))
            {
                return true;
            }

            return false;
        }
    }

    public class ConveyorExtension : DefModExtension
    {
        public int travelTicks = 60;
        public float stackDistance = 1f; //In tiles
    }
}
