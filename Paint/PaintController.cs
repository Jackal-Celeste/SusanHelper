using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste;
using SusanHelper.Entities.Paint;
using System.Collections.Generic;
using Celeste.Mod.SusanHelper;
using System.Linq;
using static SusanHelper.Entities.Paint.PaintSource;

namespace SusanHelper.Paint
{
    [CustomEntity("SusanHelper/PaintController")]
    [Tracked]
    public class PaintController : Entity
	{
        public List<PaintLiquid> colliding = new List<PaintLiquid>();
        public Entity bgTiles;
        private bool canRefillDash = true;

        public PaintController(EntityData data, Vector2 offset) : base(data.Position + offset)
		{
		}

        public override void Added(Scene scene)
        {
            base.Added(scene);

            //TODO: room transition refill bug
            (this.Scene as Level).Session.Inventory.NoRefills = false;

            //get grid of bg tiles
            Level level = this.Scene as Level;
            Rectangle rectangle = new Rectangle(level.Bounds.Left / 8, level.Bounds.Y / 8, level.Bounds.Width / 8, level.Bounds.Height / 8);
            Rectangle tileBounds = level.Session.MapData.TileBounds;
            bool[,] array = new bool[rectangle.Width, rectangle.Height];
            for (int i = 0; i < rectangle.Width; i++)
            {
                for (int j = 0; j < rectangle.Height; j++)
                {
                    array[i, j] = level.BgData[i + rectangle.Left - tileBounds.Left, j + rectangle.Top - tileBounds.Top] != '0';
                }
            }
            bgTiles = new Solid(new Vector2(level.Bounds.Left, level.Bounds.Top), 1f, 1f, safe: true);
            bgTiles.Collider = new Grid(8f, 8f, array);
            bgTiles.Collidable = false;
            base.Scene.Add(bgTiles);
        }

        public override void Update()
        {
            if (SusanModule.TryGetPlayer(out Player p))
            {
                Level lvl = this.Scene as Level;
                //binary ink behavior via black/nonblack color
                foreach (PaintLiquid l in lvl.Tracker.GetEntities<PaintLiquid>())
                {
                    if (l.CollideCheck<Player>())
                    {
                        colliding.Add(l);
                    }
                }
                if (colliding.Count == 0)
                {
                    //not colliding with any ink;
                    if (p.OnGround()) { //p.RefillDash();
                        canRefillDash = false; }
                    lvl.Session.Inventory.NoRefills = false;
                    SaveData.Instance.Assists.NoGrabbing = false;
                    SusanModule.Session.hangingToRainbowInk = false;
                    SusanModule.Session.canTriggerFloatFallBlock = true;
                    lvl.Session.SetFlag("SusanHelper_DisableNeutrals", setTo: false);
                    
                }
                else
                {
                    //grabs colliding piece most in foreground
                    if (colliding.Any(a => a.type == CollisionTypes.Floor))
                    {
                        //colliding floor ink, get fg entity
                        PaintLiquid fgPaint = colliding.Where(o => o.type == CollisionTypes.Floor).OrderBy(o => o.Depth).FirstOrDefault();
                        List<PaintLiquid> others = colliding.Where(o => o != fgPaint).ToList();
                        foreach (PaintLiquid l in others) l.Collidable = false;
                        lvl.Session.Inventory.NoRefills = fgPaint.evil;

                        if (!fgPaint.evil && p.OnGround() && canRefillDash)
                        {
                            //p.RefillDash();
                            canRefillDash = false;
                        }
                        else if (fgPaint.evil) canRefillDash = true;
                        SusanModule.Session.canTriggerFloatFallBlock = true;

                        if(colliding.Any(a => a.type == CollisionTypes.LeftWall || a.type == CollisionTypes.RightWall))
                        {
                            PaintLiquid fgP = colliding.Where(o => o.type == CollisionTypes.LeftWall || o.type == CollisionTypes.RightWall).OrderBy(o => o.Depth).FirstOrDefault();
                            lvl.Session.SetFlag("SusanHelper_DisableNeutrals", setTo: fgP.evil);
                        }
                    }
                    //touching ceiling ink
                    else if (colliding.Any(a => a.type == CollisionTypes.Ceiling))
                    {
                        PaintLiquid fgPaint = colliding.Where(o => o.type == CollisionTypes.Ceiling).OrderBy(o => o.Depth).FirstOrDefault();
                        foreach (PaintLiquid l in colliding.Where(o => o != fgPaint).ToList()) l.Collidable = false;
                        SusanModule.Session.hangingToRainbowInk = (Input.GrabCheck && !fgPaint.evil);
                        SusanModule.Session.canTriggerFloatFallBlock = true;

                        if (colliding.Any(a => a.type == CollisionTypes.LeftWall || a.type == CollisionTypes.RightWall))
                        {
                            PaintLiquid fgP = colliding.Where(o => o.type == CollisionTypes.LeftWall || o.type == CollisionTypes.RightWall).OrderBy(o => o.Depth).FirstOrDefault();
                            lvl.Session.SetFlag("SusanHelper_DisableNeutrals", setTo: fgP.evil);
                        }
                    }
                    else
                    {
                        //colliding wall ink
                        PaintLiquid fgPaint = colliding.Where(o => o.type == CollisionTypes.LeftWall || o.type == CollisionTypes.RightWall).OrderBy(o => o.Depth).FirstOrDefault();
                        foreach (PaintLiquid l in colliding.Where(o => o != fgPaint).ToList()) l.Collidable = false;
                        SaveData.Instance.Assists.NoGrabbing = fgPaint.evil;
                        SusanModule.Session.canTriggerFloatFallBlock = !fgPaint.evil;
                        lvl.Session.SetFlag("SusanHelper_DisableNeutrals", setTo: fgPaint.evil);
                    }

                }
                colliding.Clear();
                base.Update();
            }
        }

    }
}

