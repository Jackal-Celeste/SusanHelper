using System;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;
using MonoMod.Utils;
using Celeste.Mod.SusanHelper;
using System.Collections.Generic;
using static SusanHelper.Entities.Paint.PaintChunk;
using System.Linq;

namespace SusanHelper.Entities.Paint;

[CustomEntity("SusanHelper/PaintEmitter")]
[Tracked]
public class PaintSource : Entity
{
    
    private bool canEmit;
    public List<PaintLiquid> colliding = new List<PaintLiquid>();
    public Entity bgTiles;

    private ParticleType P_ExplodeBlack = new ParticleType(Seeker.P_Regen)
    {
        Color = Color.Black,
        Color2 = Color.DarkGray,
    };

    private ParticleType P_ExplodeColor = new ParticleType(Seeker.P_Regen)
    {
        Color = Strawberry.P_Glow.Color2,
        Color2 = Color.Green,
    };

    public enum CollisionTypes
    {
        LeftWall = -1, RightWall = 1, Floor = 0, Ceiling = 2, Other = -2
    }


    //CarryOn Vars
    public bool evil, permanent;
    public float lifetimeMin, amount, lifetimeMax,
        maxFallSpeed, gravity, airFriction, groundFriction, saturation,
        alpha, launchMagMin, launchMagMax;
    public CollisionTypes type;
    private MTexture texture;
    public Platform hit;
    private GrabModes grab;

    //custom launch vars
    private bool custom, hasExploded = false;
    public float launchMinX, launchMaxX, launchMinY, launchMaxY;
    private string flag = "";

    public PaintSource(Vector2 position, float amount, bool evil, float lifetimeMin, float lifetimeMax,
        float maxFallSpeed, float gravity, float airFriction, float groundFriction,
        float saturation, float alpha, float launchMagMin, float launchMagMax, CollisionTypes type, Platform hit) : base(position)
    {
        Tag = Tags.Global;
        this.hit = hit;
        this.type = type;
        permanent = true;
        this.amount = amount;
        canEmit = true;
        Depth = -12000;
        this.evil = evil;
        this.lifetimeMax = lifetimeMax;
        this.lifetimeMin = lifetimeMin;
        this.maxFallSpeed = maxFallSpeed;
        this.gravity = gravity;
        this.airFriction = airFriction;
        this.groundFriction = groundFriction;
        this.saturation = saturation;
        this.alpha = Calc.Clamp(alpha, 0f, 1f);
        this.launchMagMin = launchMagMin;
        this.launchMagMax = Math.Max(launchMagMin, launchMagMax);



    }

    public PaintSource(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        permanent = data.Bool("permanent", defaultValue: false);
        amount = data.Float("amount", defaultValue: 80f);
        canEmit = true;
        Depth = -12000;
        evil = data.Bool("evil", defaultValue: true);
        lifetimeMin = data.Float("lifetimeMin", defaultValue: 2.0f);
        lifetimeMax = data.Float("lifetimeMax", defaultValue: 6.0f);
        maxFallSpeed = data.Float("maxFallSpeed", defaultValue: 280f);
        gravity = data.Float("gravity", defaultValue: 240f);
        airFriction = data.Float("airFriction", defaultValue: 5f);
        groundFriction = data.Float("groundFriction", defaultValue: 75f);
        saturation = Calc.Clamp(data.Float("saturation", defaultValue: 0.5f),0f,1f);
        alpha = Calc.Clamp(data.Float("alpha", defaultValue: 0.5f), 0f, 1f);
        launchMagMin = data.Float("launchMagMin", defaultValue: 160f);
        launchMagMax = Math.Max(launchMagMin, data.Float("launchMagMax", defaultValue: 240f));

        custom = true;
        type = CollisionTypes.Other;
        launchMinX = data.Float("launchMinX", defaultValue: -1);
        launchMinY = data.Float("launchMinY", defaultValue: -1);
        launchMaxX = Math.Max(launchMinX, data.Float("launchMaxX", defaultValue: 1));
        launchMaxY = Math.Max(launchMinY, data.Float("launchMaxY", defaultValue: 1));
        flag = data.Attr("flag", defaultValue: "paint_flag_activate");

    }


    public void OnShatter(Level level)
    {

        Calc.PushRandom();
        SusanModule.Session.inkPaintLayer += 1;

        ParticleType explodeParticle = evil? P_ExplodeBlack : P_ExplodeColor;
        Color color;
        level.Shake();
        level.Displacement.AddBurst(Position, 0.4f, 12f, 36f, 0.5f);

        for (float num = 0f; num < (float)Math.PI * 2f; num += 0.17453292f)
        {
            Vector2 position = Center + Calc.AngleToVector(num + Calc.Random.Range(-(float)Math.PI / 90f, (float)Math.PI / 90f), Calc.Random.Range(4, 10));
            level.Particles.Emit(explodeParticle, position, num);
        }

        LiquidSetting juice = permanent ? LiquidSetting.Permanent : LiquidSetting.Fading;

        if (canEmit && amount > 0)
        {
            Vector2 from = Calc.Floor(Position);

            for (int i = 0; i < amount; ++i)
            {
                color = SusanModule.PickColor(evil ? SusanModule.Session.badColorsStr : SusanModule.Session.goodColorsStr);
                color = Color.Lerp(Color.White, color, saturation) * alpha;
                PaintChunk debris = Engine.Pooler.Create<PaintChunk>();
                level.Add(debris.Init(from, Calc.Random.Choose(GFX.Game.GetAtlasSubtextures("objects/paint/shards/").ToArray()), juice, color, this));
            }
            
            color = SusanModule.PickColor(evil ? SusanModule.Session.badColorsStr : SusanModule.Session.goodColorsStr);
            color = Color.Lerp(Color.White, color, saturation) * alpha;
            PaintLiquid pool;
            float poolRad = 12f;
            Vector2 begin, end;
            switch (type)
            {
                case CollisionTypes.Floor:
                    begin = new Vector2(Math.Max(hit.Left, Position.X - poolRad), Position.Y+2f);
                    end = new Vector2(Math.Min(hit.Right, Position.X + poolRad), Position.Y+2f);
                    pool = new PaintLiquid(begin, end, hit, juice, 0, color, this);
                    pool.type = CollisionTypes.Floor;
                    level.Add(pool);
                    break;
                case CollisionTypes.Ceiling:
                    begin = new Vector2(Math.Max(hit.Left, Position.X - poolRad), Position.Y-2f);
                    end = new Vector2(Math.Min(hit.Right, Position.X + poolRad), Position.Y-2f);
                    pool = new PaintLiquid(begin, end, hit, juice, 2, color, this);
                    pool.type = CollisionTypes.Ceiling;
                    level.Add(pool);
                    break;
            }
        }
        //Audio.Play(SFX.game_10_puffer_splode);
        Calc.PopRandom();

        canEmit = false;
    }

    public override void Update()
    {
        base.Update();
        if(SusanModule.TryGetPlayer(out Player p) && custom && !hasExploded)
        {
            if ((p.Scene as Level).Session.GetFlag(flag)) { OnShatter(p.Scene as Level); hasExploded = true; }
        }
    }
}


