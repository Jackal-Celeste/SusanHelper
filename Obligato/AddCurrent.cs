using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace TwigHelper.ARC_Project
{
    [CustomEntity("SusanHelper/AddCurrent")]
    public class AddCurrent : Trigger
    {
        private Vector2 internalAccel;
        private List<AddCurrent> currents;
        public AddCurrent(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            internalAccel = new Vector2(data.Float("xAcceleration", defaultValue: 0f), data.Float("yAcceleration", defaultValue: 0f));
        }
        public override void OnStay(Player player)
        {
            base.OnStay(player);
            player.Speed += internalAccel * Engine.DeltaTime;
            player.Facing = player.Speed.X > 0f ? Facings.Right : Facings.Left;
        }
    }
}
