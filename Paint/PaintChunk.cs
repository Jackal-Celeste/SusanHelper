using Celeste;
using Celeste.Mod.SusanHelper;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Monocle;
using MonoMod.Utils;
using SusanHelper.Paint;
using System;
using System.Collections.Generic;
using static SusanHelper.Entities.Paint.PaintLiquid;
using static SusanHelper.Entities.Paint.PaintSource;

namespace SusanHelper.Entities.Paint;

[Pooled]
public class PaintChunk : Actor
{
    public PaintSource orig;
    public float LifetimeMin, LifetimeMax, Gravity, MaxFallSpeed, AirFriction, GroundFriction;
    private float initGrav;

    private float lifeTimer;

    private Vector2 speed;
    private Vector2 previousLiftSpeed;

    private float rotation = Calc.Random.NextFloat(MathHelper.TwoPi);
    private float rotationVel = Calc.Random.Range(-6f, 6f);

    private bool hitGround;

    private readonly SoundSource sfx = new();
    private EventInstance eventInstance;

    private Level level;

    public MTexture Texture { get; private set; }
    private float alpha = 1f;
    private bool outline;
    private MTexture streaks;

    public List<PaintLiquid> PaintLiquid = new List<PaintLiquid>();

    public enum LiquidSetting
    {
        None, Fading, Permanent,
    }
    

    private LiquidSetting liquidMode;
    public CollisionTypes collision;
    private bool SpreadsLiquid => liquidMode != LiquidSetting.None;
    private PaintLiquid groundLiquid, leftWallLiquid, rightWallLiquid, ceilingLiquid;
    private Platform groundPlatform, leftWallPlatform, rightWallPlatform, ceilingPlatform;
    public Color LiquidColor;
    public Color c;
    private bool canMakeStreaks = true;
    private float scale =1.5f;
    private float chunkScale = 1.5f;
    private float lifeTimerInit;
    private bool hitCeiling = false;

    private float radiusMin = 48f;
    private float radiusMax = 80f;
    private float thisRad;


    // this entity is handled in a pool, it must have a parameterless ctor
    // so this is only called once per debris, and when they are removed from the scene, they can be reused,
    // in which case, we have to rely on StrawberryDebris.Init (down below)
    public PaintChunk()
        : base(Vector2.Zero)
    {
        Collider = new Hitbox(4, 4, -2, -2);

        Add(sfx);
        sfx.Pause();
    }

    // we initialize our entity, knowing that it might've previously been instantiated.
    public PaintChunk Init(Vector2 position, MTexture texture, LiquidSetting liquidMode, Color liquidColor, PaintSource orig, int ceilingDir = 0)
    {
        Tag = Tags.Global;
        this.orig = orig;
        LifetimeMin = orig.lifetimeMin;
        LifetimeMax = orig.lifetimeMax;
        MaxFallSpeed = orig.maxFallSpeed;
        Gravity = orig.gravity;
        AirFriction = orig.airFriction;
        GroundFriction = orig.groundFriction;
        Position = position;
        previousLiftSpeed = Vector2.Zero;
        speed = Calc.Random.Range(orig.launchMagMin, orig.launchMagMax) * explodeRanges(orig.type) * 1.5f;
        Collidable = false;
        Texture = texture;
        this.liquidMode = liquidMode;
        LiquidColor = liquidColor;
        lifeTimer =lifeTimerInit= Calc.Random.Range(LifetimeMin, LifetimeMax);
        if(ceilingDir != 0) speed = 1.5f * orig.launchMagMin * new Vector2(ceilingDir * (float)Math.Sqrt(2) / 2f, -(float)Math.Sqrt(2) / 2f);
        DismissLiquid();
        c = Color.Lerp(Color.White, SusanModule.PickColor(orig.evil ? SusanModule.Session.badColorsStr : SusanModule.Session.goodColorsStr), orig.saturation) * orig.alpha;
        initGrav = Gravity;
        thisRad = Calc.Random.Range(radiusMin, radiusMax);
        lifeTimerInit = lifeTimer;
        return this;
    }

    public Vector2 explodeRanges(CollisionTypes types)
    {
        Vector2 range = -Vector2.UnitY;
        switch (types)
        {
            case CollisionTypes.LeftWall:
                range = new Vector2(Calc.Random.Range(-0.2f, 1f), Calc.Random.Range(-1f, 1f));
                break;
            case CollisionTypes.RightWall:
                range = new Vector2(Calc.Random.Range(-1f, 0.2f), Calc.Random.Range(-1f, 1f));
                break;

            case CollisionTypes.Floor:
                range = new Vector2(Calc.Random.Range(-1f, 1f), Calc.Random.Range(-1f, 0.2f));
                break;
            case CollisionTypes.Ceiling:
                range = new Vector2(Calc.Random.Range(-1f, 1f), Calc.Random.Range(-0.2f, 1f));
                break;
            case CollisionTypes.Other:
                range = new Vector2(Calc.Random.Range(orig.launchMinX, orig.launchMaxX), Calc.Random.Range(orig.launchMinY, orig.launchMaxY));
                break;
            default:
                break;
        }
        range.Normalize();
        return range;
    }

    public float checkDistance(float radius)
    {
        return Math.Max(1 - (Position - orig.Position).Length() / radius, 0f);
    }

    private void OnCollideH(CollisionData data)
    {
        if (speed.X != 0)
        {
            int sign = Math.Sign(speed.X);
            if (sign > 0)
                TryCreateRightWallSpreadLiquid();
            else
                TryCreateLeftWallSpreadLiquid();
        }

        speed.X = 0f;
        rotationVel = 0f;
    }

    private void OnCollideV(CollisionData data)
    {
        if (speed.Y > 0)
        {
            //ImpactSfx(speed.Y);
            hitGround = true;

            TryCreateGroundSpreadLiquid();
        }
        else
        {
            TryCreateCeilingSpreadLiquid();
        }

        speed.Y = 0f;
        rotationVel = 0f;
    }

    protected override void OnSquish(CollisionData data)
    {
        base.OnSquish(data);
        RemoveSelf();
    }

    private void TryCreateGroundSpreadLiquid()
    {
        if (SpreadsLiquid)
        {
            Platform platform = CollideFirstOutside<Platform>(Position + Vector2.UnitY);
            if (platform != null && groundPlatform != platform)
            {
                groundLiquid?.Dismiss();
                Scene.Add(groundLiquid = new PaintLiquid(this, platform, liquidMode, 0,speed,c,orig));
                PaintLiquid.Add(groundLiquid);
                groundLiquid.type = CollisionTypes.Floor;
                groundPlatform = platform;
                canMakeStreaks = false;
            }
        }
    }

    private void TryCreateCeilingSpreadLiquid()
    {
        if (SpreadsLiquid)
        {
            Platform platform = CollideFirstOutside<Platform>(Position - Vector2.UnitY);
            if (platform != null && ceilingPlatform != platform)
            {
                Gravity = 0f;
                speed.X *= 0.75f;
                AirFriction = GroundFriction;
                hitCeiling = true;
                ceilingLiquid?.Dismiss();
                Scene.Add(ceilingLiquid = new PaintLiquid(this, platform, liquidMode, 2, speed, c, orig));
                PaintLiquid.Add(ceilingLiquid);
                ceilingPlatform = platform;
                ceilingLiquid.type = CollisionTypes.Ceiling;
                canMakeStreaks = false;
            }
        }
    }

    private void TryCreateLeftWallSpreadLiquid()
    {
        if (SpreadsLiquid)
        {
            Platform platform = CollideFirstOutside<Platform>(Position - Vector2.UnitX);
            if (platform != null && leftWallPlatform != platform)
            {
                leftWallLiquid?.Dismiss();
                Scene.Add(leftWallLiquid = new PaintLiquid(this, platform, liquidMode, -1, speed,c,orig));
                PaintLiquid.Add(leftWallLiquid);
                leftWallPlatform = platform;
                leftWallLiquid.type = CollisionTypes.LeftWall;
                canMakeStreaks = false;
            }
        }
    }

    private void TryCreateRightWallSpreadLiquid()
    {
        if (SpreadsLiquid)
        {
            Platform platform = CollideFirstOutside<Platform>(Position + Vector2.UnitX);
            if (platform != null && rightWallPlatform != platform)
            {
                rightWallLiquid?.Dismiss();
                Scene.Add(rightWallLiquid = new PaintLiquid(this, platform, liquidMode, 1,speed,c,orig));
                PaintLiquid.Add(rightWallLiquid);
                rightWallPlatform = platform;
                rightWallLiquid.type = CollisionTypes.RightWall;
                canMakeStreaks = false;

            }
        }
    }

    private void DismissLiquid()
    {
        groundLiquid?.Dismiss(); leftWallLiquid?.Dismiss(); rightWallLiquid?.Dismiss(); ceilingLiquid?.Dismiss();
        groundLiquid = leftWallLiquid = rightWallLiquid = ceilingLiquid = null;
        groundPlatform = leftWallPlatform = rightWallPlatform = ceilingPlatform = null;
    }

    private void ImpactSfx(float speed)
        => Audio.Play(SFX.game_gen_debris_dirt, Position, "debris_velocity", Calc.ClampedMap(speed, 0f, 150f));

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        level = Scene as Level;
    }



    private bool OutsideBounds()
        => Top >= level.Bounds.Bottom + 5
        || Bottom <= level.Bounds.Top - 5
        || Left >= level.Bounds.Right + 5
        || Right <= level.Bounds.Left - 5;

    public override void Update()
    {
        Collidable = true;

        bool onGround = OnGround();

        float oldYSpeed = speed.Y;
        scale = Math.Min(scale, 1.5f * checkDistance(thisRad));
        chunkScale = Math.Min(chunkScale, 1.5f * checkDistance(3*thisRad));

        float friction = onGround ? GroundFriction : AirFriction;
        speed.X = Calc.Approach(speed.X, 0, friction * Engine.DeltaTime);
        if (!onGround)
            speed.Y = Calc.Approach(speed.Y, MaxFallSpeed, Gravity * Engine.DeltaTime);

        if (speed != Vector2.Zero)
        {
            if (speed.Y * oldYSpeed < 0)
                DismissLiquid(); // Our Y speed has changed sign, so let's dismiss the current spread liquid entity

            Vector2 oldPos = Position;

            hitGround = false;
            MoveH(speed.X * Engine.DeltaTime, OnCollideH);
            MoveV(speed.Y * Engine.DeltaTime, OnCollideV);

            bool doParticles = Scene.OnInterval(0.035f) && (Position-orig.Position).Length() < 2*thisRad;

            bool sliding = false;
            float slideAmount = 0f;
            bool isCurrentlyOnGround = hitGround || onGround;

            float dx = X - oldPos.X;
            float dy = Y - oldPos.Y;

            if(Gravity == 0f)
            {
                if (speed.Y == 0 && speed.X != 0)
                {
                    if (doParticles)
                        level.Particles.Emit(ParticleTypes.Dust, new Vector2(CenterX, Top), orig.evil ? Color.Black * 0.5f : c);

                    sliding = true;
                    slideAmount = Math.Abs(speed.X);

                    TryCreateCeilingSpreadLiquid();
                    ceilingLiquid?.Extend(dx);
                }

                Platform platform = CollideFirstOutside<Platform>(Position - Vector2.UnitY);
                if(platform == null)
                {
                    Gravity = initGrav;
                }
            }
            else if (isCurrentlyOnGround)
            {
                if (speed.Y == 0 && speed.X != 0)
                {
                    if (doParticles)
                        level.Particles.Emit(ParticleTypes.Dust, new Vector2(CenterX, Bottom), orig.evil ? Color.Black * 0.5f : c);

                    sliding = true;
                    slideAmount = Math.Abs(speed.X);

                    TryCreateGroundSpreadLiquid();
                    groundLiquid?.Extend(dx);
                }
            }
            else
            {
                if (speed.Y != 0 && speed.X == 0)
                {
                    Platform platform = null;

                    platform = CollideFirstOutside<Platform>(Position - Vector2.UnitX);
                    if (platform != null)
                    {
                        sliding = true;

                        if (doParticles)
                            level.ParticlesFG.Emit(ParticleTypes.Dust, new Vector2(Left, CenterY), orig.evil ? Color.Black * 0.5f : c);

                        TryCreateLeftWallSpreadLiquid();
                        leftWallLiquid?.Extend(dy);
                    }

                    platform = CollideFirstOutside<Platform>(Position + Vector2.UnitX);
                    if (platform != null)
                    {
                        sliding = true;

                        if (doParticles)
                            level.ParticlesFG.Emit(ParticleTypes.Dust, new Vector2(Right, CenterY), orig.evil ? Color.Black * 0.5f : c);

                        TryCreateRightWallSpreadLiquid();
                        rightWallLiquid?.Extend(dy);
                    }

                    slideAmount = Math.Abs(speed.Y);
                }
            }

            rotation += rotationVel * Engine.DeltaTime;

            if (sliding)
            {
                if (!sfx.Playing)
                {
                    if (eventInstance is null)
                    {
                        sfx.Play(SFX.char_mad_wallslide);
                        eventInstance = (EventInstance)new DynData<SoundSource>(sfx)["instance"];
                    }
                    sfx.Resume();
                }

                eventInstance.setVolume(Calc.Clamp(slideAmount / 24f, 0, 2.25f));
            }
            else
            {
                if (sfx.Playing)
                    sfx.Pause();
                DismissLiquid();
            }

            if (OutsideBounds())
                RemoveSelf();
        }
        else
        {
            if (sfx.Playing)
                sfx.Pause();
        }

        if (previousLiftSpeed != Vector2.Zero && LiftSpeed == Vector2.Zero)
            speed += previousLiftSpeed;
        previousLiftSpeed = LiftSpeed;

        if (lifeTimer > 0f)
            lifeTimer -= Engine.DeltaTime;
        else if (alpha > 0f)
        {
            alpha -= Engine.DeltaTime;
            if (alpha <= 0f)
                RemoveSelf();
        }

        if (SusanModule.TryGetPlayer(out Player p))
        {
            PaintController pc = (p.Scene as Level).Tracker.GetEntity<PaintController>();
            if (pc != null)
            {
                Entity e = pc.bgTiles;
                e.Collidable = true;
                if (orig.bgTiles != null && CollideCheck(orig.bgTiles) && checkDistance(thisRad) > 0.1 && canMakeStreaks)
                {
                    Level l = this.Scene as Level;
                    MTexture[] shards = GFX.Game.GetAtlasSubtextures("objects/paint/bgStreaks/").ToArray();
                    MTexture texture = Calc.Random.Choose(shards);
                    PaintStreak s = new PaintStreak(Position - 4 * Vector2.One, texture, c, scale, this);
                    l.Add(s);
                }
                e.Collidable = false;
            }


        }

        Collidable = false;
    }


    public override void Render()
    {
        Texture.DrawCentered(Center, c * alpha, chunkScale*Math.Max(0f,(float)Math.Pow(lifeTimer/lifeTimerInit,2)), rotation);
    }
        


    public override void Removed(Scene scene)
    {
        base.Removed(scene);

        sfx.Stop();
        DismissLiquid();
    }
}





