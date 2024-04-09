using SusanHelper.Entities.Paint;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using Celeste;
using static SusanHelper.Entities.Paint.PaintChunk;
using SusanHelper.Entities;
using Celeste.Mod.SusanHelper;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using static Celeste.Spring;
using static SusanHelper.Entities.Paint.PaintSource;
using System.Collections;
using Celeste.Mod;
using Celeste.Mod.Entities;

namespace SusanHelper.Entities.Paint;

[Tracked]
[CustomEntity("SusanHelper/PaintLiquid")]
public class PaintLiquid : Entity
{
    private static MTexture[] liquidTextures = GFX.Game.GetAtlasSubtextures("objects/paint/juice/").ToArray();

    public readonly Vector2 offset;

    private float extend;

    private readonly MTexture fullTexture, texture;

    private Vector2 scale = new(1);
    private readonly float rotation;

    public readonly Color color;

    public Platform platform;

    private float lifeTimer = 3f, alpha = 1f;

    private readonly LiquidSetting mode;

    public bool evil;

    private float length;
    private Vector2 initialVel;
    public bool colliding = false;
    public float lastLength = 0f;
    public PaintSource orig;
    public Color c;

    private bool staticPaint;
    public CollisionTypes type = CollisionTypes.Other;
    private ClimbBlocker climbBlocker;
    private HangingCeiling hangCeiling = null;
    private PlayerCollider pc;
    private Hitbox hp;
    private int depthModifier;
    private readonly StaticMover staticMover;
    private bool premade = false;
    private Vector2 platformOffset;



    //premade paint
    public PaintLiquid(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        premade = staticPaint = true;
        type = dumbCast(data.Attr("orientation", defaultValue: "Floor"));
        evil = data.Bool("evil", defaultValue: true);
        float l = data.Float("width", defaultValue: 8f);
        depthModifier = data.Int("depthModifier", defaultValue: 0);
        Tag = Tags.Global;
        length = (type == CollisionTypes.LeftWall || type == CollisionTypes.RightWall) ? data.Height : data.Width;

        this.platform = null;
        this.orig = null;

        hp = new Hitbox(length, 5f, 0, -3f);

        //below first ink blast
        rotation = -MathHelper.PiOver2 * (int)type;

        if (type == CollisionTypes.Ceiling)
        {
            scale.X = -1;
            this.Collider = new Hitbox(length, 5f, 0, 0);
        }
        else if (type == CollisionTypes.Floor)
        {
            this.Collider = new Hitbox(length, 4f, 0, -3f);
        }
        else
        {
            this.Collider = new Hitbox(3f, length, type == CollisionTypes.RightWall ? -3f : 0f, 0);
            if (type == CollisionTypes.RightWall) scale.X *= -1;
            if (evil) Add(climbBlocker = new ClimbBlocker(false));
        }

        texture = (fullTexture = Calc.Random.Choose(liquidTextures)).GetSubtexture(Calc.Random.Choose(2, 6), 0, (int)length, 5);
        color = SusanModule.PickColor(evil ? SusanModule.Session.badColorsStr : SusanModule.Session.goodColorsStr) * 0.8f;

        this.mode = LiquidSetting.Permanent;
        initialVel = Vector2.Zero;
        Depth = Depths.FGTerrain - SusanModule.Session.inkPaintLayer;
        InitializeCeilings();
        
    }

    private CollisionTypes dumbCast(string o)
    {
        o = o.ToLower();
        return o.Equals("left wall") ? CollisionTypes.LeftWall : o.Equals("floor") ? CollisionTypes.Floor : o.Equals("right wall") ? CollisionTypes.RightWall : o.Equals("ceiling") ? CollisionTypes.Ceiling : CollisionTypes.Other;
    }

    //static paint
    public PaintLiquid(Vector2 begin, Vector2 end, Platform platform, LiquidSetting mode, short orientation, Color c, PaintSource orig) : base(begin)
    {
        staticPaint = true;
        Tag = Tags.Global;

        this.platform = platform;
        this.orig = orig;
        evil = orig.evil;
        hp = new Hitbox(length, 5f, 0, -3f);

        Depth = Depths.FGTerrain - SusanModule.Session.inkPaintLayer;
        rotation = -MathHelper.PiOver2 * orientation;
        type = (CollisionTypes)orientation;
        offset = begin - platform.Position;

        if (type == CollisionTypes.Ceiling)
        {
            length = Math.Abs((begin - end).X);
            scale.X = -1;
            Position.Y -= 2f;
            this.Collider = new Hitbox(length, 5f, 0, 0f);
        }
        else if (type == CollisionTypes.Floor)
        {
            length = Math.Abs((begin - end).X);
            this.Collider = new Hitbox(length, 4f, 0, -3f);
        }
        else
        {
            if (evil) Add(climbBlocker = new ClimbBlocker(false));
        }

        texture = (fullTexture = Calc.Random.Choose(liquidTextures)).GetSubtexture(2, 0, (int)length, 5);
        color = c;

        this.mode = mode;
        initialVel = Vector2.Zero;
        InitializeCeilings();

    }

    //Normal Paint
    public PaintLiquid(PaintChunk debris, Platform platform, LiquidSetting mode, short orientation, Vector2 initVel, Color c, PaintSource orig)
        : base(platform.Position)
    {
        staticPaint = false;
        Tag = Tags.TransitionUpdate;

        this.platform = platform;
        this.orig = orig;
        evil = orig.evil;
        hp = new Hitbox(length, 5f, 0, -3f);

        Depth = Depths.FGTerrain - SusanModule.Session.inkPaintLayer;
        rotation = -MathHelper.PiOver2 * orientation;
        type = (CollisionTypes)orientation;

        if (type == CollisionTypes.Ceiling)
        {
            offset = new Vector2(debris.Center.X, debris.Top);
            this.Collider = new Hitbox(0f, 5f, 0, 0f);
        }
        else if (type == CollisionTypes.Floor)
        {
            offset = new Vector2(debris.Center.X, debris.Bottom);
            this.Collider = new Hitbox(0f, 4f, 0, -3f);
        }
        else
        {
            offset = new Vector2((int)type < 0 ? debris.Left : debris.Right, debris.Center.Y);
            this.Collider = new Hitbox(3f, 0, (int)type > 0 ? -3f : 0f, 0);
            if (evil) Add(climbBlocker = new ClimbBlocker(false));
        }

        offset -= platform.Position;

        texture = (fullTexture = Calc.Random.Choose(liquidTextures)).GetSubtexture(0, 0, 0, 0);
        color = c;

        this.mode = mode;
        initialVel = initVel;
        InitializeCeilings();
    }


    private void InitializeCeilings()
    {
        scale.Y *= Calc.Random.Range(1, 1.5f);
    }



    public void Extend(float amount)
    {
        if (staticPaint) amount = 0;

        extend += amount;

        float sign = Math.Sign(extend);
        scale.X = sign;
        if ((int)type != 0)
            scale.X *= -(int)type;

        length = Calc.Clamp(Math.Abs(extend) - 3, 1, fullTexture.Width);
        fullTexture.GetSubtexture(0, 0, (int)type == 2 ? (int)length / 2 : (int)length, 5, applyTo: texture);

    }


    public void Dismiss()
    {
        if (Math.Abs(extend) <= 3)
            RemoveSelf();
    }



    public override void Update()
    {
        base.Update();
        if (platform != null)
        {
            Position = platform.Position + offset;
            if (platform.Scene == null) ;//RemoveSelf();
        }

        if (!staticPaint)
        {
            if (SusanModule.TryGetPlayer(out Player p) && this.Collider != null)
            {
                Collidable = Visible;
                if (type == CollisionTypes.Floor || type == CollisionTypes.Ceiling)
                {
                    this.Collider.Width = length;
                    if (initialVel.X < 0)
                    {
                        this.Collider.Left = -length;
                    }

                    if (type == CollisionTypes.Ceiling && hp != null)
                    {
                        hp.Left = Collider.Left;
                        hp.Width = Collider.Width;
                    }
                }
                else this.Collider.Height = length;
            }
        }
        else if(premade && platform != null)
        {
            Position = platformOffset + platform.Position;
        }

    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        Level lvl = scene as Level;
        if (premade)
        {
            switch (type)
            {
                case CollisionTypes.RightWall:
                    platform = lvl.CollideFirst<Platform>(Position - Vector2.UnitX);
                    break;
                case CollisionTypes.LeftWall:
                    platform = lvl.CollideFirst<Platform>(Position + Vector2.UnitX);
                    break;
                case CollisionTypes.Floor:
                    platform = lvl.CollideFirst<Platform>(Position + Vector2.UnitY);
                    break;
                case CollisionTypes.Ceiling:
                    platform = lvl.CollideFirst<Platform>(Position - Vector2.UnitY);
                    break;

                    //case
            }
        }
        if(platform != null)
        {
            platformOffset = Position - platform.Position;
        }
    }

    public override void Render()
    {
        texture?.Draw(Position, Vector2.Zero, color * alpha, scale, rotation);
    }






    [Tracked]
    public class PaintStreak : Entity
    {
        private MTexture texture;
        private Color color;
        private PaintChunk orig;
        private float scale;
        private float alpha = 1f;
        private bool drawn = false;
        private float lifetimer;
        private float minAlpha;
        private float fallRate;

        public PaintStreak(Vector2 position, MTexture texture, Color liquidColor, float scale, PaintChunk orig)
        : base(Vector2.Zero)
        {
            Tag = Tags.Global;
            Position = position;
            this.texture = texture;
            this.scale = scale;
            this.orig = orig;
            color = orig.orig.evil ? liquidColor : liquidColor;
            //color = Color.Lerp(liquidColor,Color.Black,0.25f);
            lifetimer = 1.5f * Calc.Random.Range(orig.LifetimeMin, orig.LifetimeMax);
            minAlpha = Math.Max(0f, (float)Calc.Random.Choose(-1.25, 0.75) - 0.5f);
            fallRate = Calc.Random.Range(6, 12);
            Depth += 1;
        }
        public override void Render()
            => texture?.Draw(Position, Vector2.Zero, color * alpha, scale);

        public override void Update()
        {
            base.Update();

            if (lifetimer > 0f)
                lifetimer -= Engine.DeltaTime;
            else if (alpha > 0f && alpha > minAlpha)
            {
                Position += Vector2.UnitY / fallRate;
                alpha -= Engine.DeltaTime;
                if (alpha <= 0f)
                    RemoveSelf();
            }
        }

    }
}