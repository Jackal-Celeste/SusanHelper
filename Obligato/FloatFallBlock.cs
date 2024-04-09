// Celeste.FloatFallBlock
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.SusanHelper;
using System.Text.RegularExpressions;

[Tracked(false)]
[CustomEntity("SusanHelper/FloatyFallBlock")]
public class FloatFallBlock : Solid
{
    //Falling Block Fields
    public static ParticleType P_FallDustA;

    public static ParticleType P_FallDustB;

    public static ParticleType P_LandDust;

    public bool Triggered;

    public float FallDelay;

    private char TileType;

    private TileGrid tiles;

    private TileGrid highlight;

    private bool finalBoss;

    private bool climbFall;

    public bool HasStartedFalling { get; private set; }


    //floaty space block fields

    private char tileType;

    private float yLerp;

    private float sinkTimer;

    private float sineWave;

    private float dashEase;

    private Vector2 dashDirection;

    private FloatFallBlock master;

    private bool awake;

    public Dictionary<Platform, Vector2> Moves;

    public Point GroupBoundsMin;

    public Point GroupBoundsMax;

    public bool HasGroup { get; private set; }

    public bool MasterOfGroup { get; private set; }

    private Coroutine sequence;


    [MethodImpl(MethodImplOptions.NoInlining)]
    public FloatFallBlock(EntityData data, Vector2 offset)
        : base(data.Position + offset, data.Width, data.Height, safe: false)
    {


        //set tile, height, width, finalboss, behind, climbFall, disableSpawnOffset

        this.finalBoss = data.Bool("finalBoss", defaultValue: false);
        this.climbFall = data.Bool("climbFall", defaultValue: true);
        int newSeed = Calc.Random.Next();
        Calc.PushRandom(newSeed);
        char t = data.Char("tile", defaultValue: 'Q');
        Add(tiles = GFX.FGAutotiler.GenerateBox(t, data.Width / 8, data.Height / 8).TileGrid);
        Calc.PopRandom();
        if (finalBoss)
        {
            Calc.PushRandom(newSeed);
            Add(highlight = GFX.FGAutotiler.GenerateBox(t, data.Width / 8, data.Height / 8).TileGrid);
            Calc.PopRandom();
            highlight.Alpha = 0f;
        }
        sequence = new Coroutine(Sequence());
        Add(sequence);
        Add(new LightOcclude());
        Add(new TileInterceptor(tiles, highPriority: false));
        base.OnDashCollide = OnDash;
        TileType = t;
        SurfaceSoundIndex = SurfaceIndex.TileToIndex[t];
        if (data.Bool("behind", defaultValue: false))
        {
            base.Depth = 5000;
        }
        else
        {
            base.Depth = -9000;
        }

        Add(new LightOcclude());
        sineWave = 0f;
    }




    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        awake = true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private DashCollisionResults OnDash(Player player, Vector2 direction)
    {
        if (MasterOfGroup && dashEase <= 0.2f)
        {
            dashEase = 1f;
            dashDirection = direction;
        }
        return DashCollisionResults.NormalOverride;
    }



    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void OnShake(Vector2 amount)
    {
        base.OnShake(amount);
        tiles.Position += amount;
        if (highlight != null)
        {
            highlight.Position += amount;
        }
    }

    public override void OnStaticMoverTrigger(StaticMover sm)
    {
        if (!finalBoss)
        {
            Triggered = true;
        }
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool PlayerFallCheck()
    {
        if (climbFall &&SusanModule.Session.canTriggerFloatFallBlock)
        {
            return HasPlayerRider();
        }
        return HasPlayerOnTop();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool PlayerWaitCheck()
    {
        if (Triggered)
        {
            return true;
        }
        if (PlayerFallCheck())
        {
            return true;
        }
        if (climbFall)
        {
            if (!CollideCheck<Player>(Position - Vector2.UnitX))
            {
                return CollideCheck<Player>(Position + Vector2.UnitX);
            }
            return true;
        }
        return false;
    }

    private IEnumerator Sequence()
    {
        while (!Triggered && (finalBoss || !PlayerFallCheck()))
        {
            yield return null;
        }
        while (FallDelay > 0f)
        {
            FallDelay -= Engine.DeltaTime;
            yield return null;
        }
        HasStartedFalling = true;
        while (true)
        {
            ShakeSfx();
            StartShaking();
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            if (finalBoss)
            {
                Add(new Coroutine(HighlightFade(1f)));
            }
            yield return 0.2f;
            float timer = 0.4f;
            if (finalBoss)
            {
                timer = 0.2f;
            }
            while (timer > 0f && PlayerWaitCheck())
            {
                yield return null;
                timer -= Engine.DeltaTime;
            }
            StopShaking();
            for (int i = 2; (float)i < Width; i += 4)
            {
                if (Scene.CollideCheck<Solid>(TopLeft + new Vector2(i, -2f)))
                {
                    //SceneAs<Level>().Particles.Emit(P_FallDustA, 2, new Vector2(X + (float)i, Y), Vector2.One * 4f, (float)Math.PI / 2f);
                }
                //SceneAs<Level>().Particles.Emit(P_FallDustB, 2, new Vector2(X + (float)i, Y), Vector2.One * 4f);
            }
            float speed = 0f;
            float maxSpeed = (finalBoss ? 130f : 160f);
            while (true)
            {
                Level level = SceneAs<Level>();
                speed = Calc.Approach(speed, maxSpeed, 500f * Engine.DeltaTime);
                if (CollideCheck<Water>(Position-Height*Vector2.UnitY/2f))
                {
                    Water w = CollideFirst<Water>(Position - Height * Vector2.UnitY/2);
                    // going down to approach zero
                    speed = Calc.Approach(speed, -50f, 800f * Engine.DeltaTime);
                    if(speed < 0)
                    { 
                      speed *= (float)Math.Pow(2 * Math.Abs(CenterY-w.Top) / Height,0.2f);
                        if (speed > -0.25)
                        {
                            Remove(sequence);
                        }
                     }


                }
                MoveV(speed * Engine.DeltaTime);
                /*
                if (MoveVCollideSolids(speed * Engine.DeltaTime, thruDashBlocks: true))
                {
                    break;
                }
                */
                if (Top > (float)(level.Bounds.Bottom + 16) || (Top > (float)(level.Bounds.Bottom - 1) && CollideCheck<Solid>(Position + new Vector2(0f, 1f))))
                {
                    Collidable = (Visible = false);
                    yield return 0.2f;
                    if (level.Session.MapData.CanTransitionTo(level, new Vector2(Center.X, Bottom + 12f)))
                    {
                        yield return 0.2f;
                        SceneAs<Level>().Shake();
                        Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                    }
                    RemoveSelf();
                    DestroyStaticMovers();
                    yield break;
                }
                yield return null;
            }
            ImpactSfx();
            Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
            SceneAs<Level>().DirectionalShake(Vector2.UnitY, finalBoss ? 0.2f : 0.3f);
            if (finalBoss)
            {
                Add(new Coroutine(HighlightFade(0f)));
            }
            StartShaking();
            LandParticles();
            yield return 0.2f;
            StopShaking();
            if (CollideCheck<SolidTiles>(Position + new Vector2(0f, 1f)))
            {
                break;
            }
            while (CollideCheck<Platform>(Position + new Vector2(0f, 1f)))
            {
                yield return 0.1f;
            }

        }
        Safe = true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private IEnumerator HighlightFade(float to)
    {
        float from = highlight.Alpha;
        for (float p = 0f; p < 1f; p += Engine.DeltaTime / 0.5f)
        {
            highlight.Alpha = MathHelper.Lerp(from, to, Ease.CubeInOut(p));
            tiles.Alpha = 1f - highlight.Alpha;
            yield return null;
        }
        highlight.Alpha = to;
        tiles.Alpha = 1f - to;
    }




    [MethodImpl(MethodImplOptions.NoInlining)]
    private void LandParticles()
    {
        for (int i = 2; (float)i <= base.Width; i += 4)
        {
            if (base.Scene.CollideCheck<Solid>(base.BottomLeft + new Vector2(i, 3f)))
            {
                //SceneAs<Level>().ParticlesFG.Emit(P_FallDustA, 1, new Vector2(base.X + (float)i, base.Bottom), Vector2.One * 4f, -(float)Math.PI / 2f);
                float direction = ((!((float)i < base.Width / 2f)) ? 0f : ((float)Math.PI));
                //SceneAs<Level>().ParticlesFG.Emit(P_LandDust, 1, new Vector2(base.X + (float)i, base.Bottom), Vector2.One * 4f, direction);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ShakeSfx()
    {
        if (TileType == '3')
        {
            Audio.Play("event:/game/01_forsaken_city/fallblock_ice_shake", base.Center);
        }
        else if (TileType == '9')
        {
            Audio.Play("event:/game/03_resort/fallblock_wood_shake", base.Center);
        }
        else if (TileType == 'g')
        {
            Audio.Play("event:/game/06_reflection/fallblock_boss_shake", base.Center);
        }
        else
        {
            Audio.Play("event:/game/general/fallblock_shake", base.Center);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ImpactSfx()
    {
        if (TileType == '3')
        {
            Audio.Play("event:/game/01_forsaken_city/fallblock_ice_impact", base.BottomCenter);
        }
        else if (TileType == '9')
        {
            Audio.Play("event:/game/03_resort/fallblock_wood_impact", base.BottomCenter);
        }
        else if (TileType == 'g')
        {
            Audio.Play("event:/game/06_reflection/fallblock_boss_impact", base.BottomCenter);
        }
        else
        {
            Audio.Play("event:/game/general/fallblock_impact", base.BottomCenter);
        }
    }
}

