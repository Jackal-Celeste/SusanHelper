using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.SusanHelper
{
    [TrackedAs(typeof(Water))]
    [CustomEntity("SusanHelper/InkWater")]
    public class InkWater : Water
    {

        private Color baseColor;

        private Color surfaceColor;

        private Color fillColor;

        private Color rayTopColor;

        private bool hasRays;

        private bool fixedSurfaces;

        private bool visibleOnCamera;

        private List<Surface> emptySurfaces;

        private List<Surface> actualSurfaces;

        private Surface actualTopSurface;

        private Surface dummyTopSurface;

        private Surface actualBottomSurface;

        private Surface dummyBottomSurface;

        private static int horizontalVisiblityBuffer = 48;

        private static int verticalVisiblityBuffer = 48;

        public static FieldInfo fillColorField = typeof(Water).GetField("FillColor", BindingFlags.Static | BindingFlags.Public);

        public static FieldInfo surfaceColorField = typeof(Water).GetField("SurfaceColor", BindingFlags.Static | BindingFlags.Public);

        public static FieldInfo rayTopColorField = typeof(Water).GetField("RayTopColor", BindingFlags.Static | BindingFlags.Public);

        protected PlayerCollider playerCollider;

        protected Hitbox hitbox;

        protected float currentHeight;

        public float height;

        public float width;

        public Rectangle rect;

        private bool lastCollisionState = false;

        private DynData<Player> pData;

        public InkWater(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            baseColor = Color.Black;
            surfaceColor = Color.Gray * 0.4f ;
            fillColor = baseColor*0.95f;
            rayTopColor = baseColor * 0.8f;
            hasRays = data.Bool("hasRays", false);
            fixedSurfaces = false;
            currentHeight = data.Height;
            data.Height = (int)currentHeight;
            hitbox = new Hitbox(data.Width, currentHeight);
            Add(playerCollider = new PlayerCollider(OnPlayer, hitbox));
            SusanModule.Session.inInkWater = false;
            SusanModule.Session.inkDeathTimer = 0f;
            SusanModule.Session.inkDashesNow = -1;

        }
        public override void Awake(Scene scene)
        {
            base.Awake(scene);
        }

        private void OnPlayer(Player p)
        {
            SusanModule.Session.inInkWater = true;
        }

        private void fixSurfaces()
        {
            if (!fixedSurfaces)
            {
                Color origFill = Water.FillColor;
                Color origSurface = Water.SurfaceColor;
                changeColor(fillColorField, origFill, fillColor);
                changeColor(surfaceColorField, origSurface, surfaceColor);
                bool hasTop = Surfaces.Contains(TopSurface);
                bool hasBottom = Surfaces.Contains(BottomSurface);
                Surfaces.Clear();
                if (hasTop)
                {
                    TopSurface = new Surface(Position + new Vector2(Width / 2f, 8f), new Vector2(0f, -1f), Width, Height);
                    Surfaces.Add(TopSurface);
                    actualTopSurface = TopSurface;
                    dummyTopSurface = new Surface(Position + new Vector2(Width / 2f, 8f), new Vector2(0f, -1f), Width, Height);
                    if (!hasRays)
                    {
                        actualTopSurface.Rays.Clear();
                        dummyTopSurface.Rays.Clear();
                    }
                }
                if (hasBottom)
                {
                    BottomSurface = new Surface(Position + new Vector2(Width / 2f, Height - 8f), new Vector2(0f, 1f), Width, Height);
                    Surfaces.Add(BottomSurface);
                    actualBottomSurface = BottomSurface;
                    dummyBottomSurface = new Surface(Position + new Vector2(Width / 2f, Height - 8f), new Vector2(0f, 1f), Width, Height);
                    if (!hasRays)
                    {
                        actualBottomSurface.Rays.Clear();
                        dummyBottomSurface.Rays.Clear();
                    }
                }
                fixedSurfaces = true;
                actualSurfaces = Surfaces;
                emptySurfaces = new List<Surface>();
                changeColor(fillColorField, fillColor, origFill);
                changeColor(surfaceColorField, surfaceColor, origSurface);
            }
        }

        private void updateSurfaces()
        {
            Surfaces = (visibleOnCamera ? actualSurfaces : emptySurfaces);
            TopSurface = (visibleOnCamera ? actualTopSurface : dummyTopSurface);
            BottomSurface = (visibleOnCamera ? actualBottomSurface : dummyBottomSurface);

                dummyTopSurface?.Ripples?.Clear();
                dummyBottomSurface?.Ripples?.Clear();
            
        }

        private void updateVisibility(Level level)
        {
            Camera camera = level.Camera;
            bool horizontalCheck = X < camera.Right + horizontalVisiblityBuffer && X + Width > camera.Left - horizontalVisiblityBuffer;
            bool verticalCheck = Y < camera.Bottom + verticalVisiblityBuffer && Y + Height > camera.Top - verticalVisiblityBuffer;
            visibleOnCamera = horizontalCheck && verticalCheck;
        }

        private void changeColor(FieldInfo fieldInfo, Color from, Color to)
        {
            if (from != to)
            {
                fieldInfo.SetValue(null, to);
            }
        }

        public override void Render()
        {
            if (visibleOnCamera)
            {
                Color origFill = Water.FillColor;
                Color origSurface = Water.SurfaceColor;
                changeColor(fillColorField, origFill, fillColor);
                changeColor(surfaceColorField, origSurface, surfaceColor);
                base.Render();
                changeColor(fillColorField, fillColor, origFill);
                changeColor(surfaceColorField, surfaceColor, origSurface);
            }
        }



        public override void Update()
        {
            Level level = base.Scene as Level;
            Color origRayTop = Water.RayTopColor;
            updateVisibility(level);
            updateSurfaces();
            changeColor(rayTopColorField, origRayTop, rayTopColor);
            base.Update();
            changeColor(rayTopColorField, rayTopColor, origRayTop);
            if (SusanModule.TryGetPlayer(out Player p)){ 
                if (CollideCheck(p))
                {
                    if (!lastCollisionState) //first frame of entry
                    {
                        SusanModule.Session.inkDashesNow = (p.DashAttacking || p.Dashes > 0) ?1 :  0;
                    }
                    SusanModule.Session.inInkWater = true;
                    SusanModule.Session.inkDeathTimer += p.StateMachine.State == 2 ? 0.035f : 0.1f;
                    float frac = SusanModule.Session.inkDeathTimer / SusanModule.Session.timeToInkDeath;
                    p.OverrideHairColor = Color.Lerp(p.Hair.Color, Color.Black, frac);
                    if (SusanModule.Session.inkDeathTimer > SusanModule.Session.timeToInkDeath)
                    {
                        if (!p.Dead)
                            p.Die(Vector2.Zero, false, true);
                        p.OverrideHairColor = null;
                        SusanModule.Session.inkDeathTimer = 0f;
                    }
                    
                }
                else if (!AnyCheck(p))
                {
                    SusanModule.Session.inInkWater = false;
                    SusanModule.Session.inkDeathTimer = 0f;
                    SusanModule.Session.inkDashesNow = -1;
                    p.OverrideHairColor = null;
                }
                SusanModule.Session.inkDashesNow = -1;
                lastCollisionState = AnyCheck(p);
            }
        }


        private bool AnyCheck(Player p)
        {
            foreach (Water w in (p.Scene as Level).Tracker.GetEntities<Water>())
            {
                if (w is InkWater && w.CollideCheck(p)) return true;
            }
            return false;
        }

        public override void Added(Scene scene)
        {
            fixSurfaces();
            base.Added(scene);
        }
    }


}
