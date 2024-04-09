using Celeste;
using Celeste.Mod.Entities;
using Celeste.Mod.SusanHelper;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using static SusanHelper.Entities.Paint.PaintSource;

namespace SusanHelper.Entities.Paint
{

    [Tracked]
    [CustomEntity("SusanHelper/PaintBall")]
    public class PaintBall : Actor
    {
        public static ParticleType P_Infinite = new(Refill.P_Glow);
        public static ParticleType P_Full = new(Refill.P_Glow);
        public static ParticleType P_Half = new(Refill.P_Glow);
        public static ParticleType P_Low = new(Refill.P_Glow);
        public static PaintBall LastHeld;

        public Vector2 Speed;
        public Holdable Hold;
        public bool Running = false;


        private static Vector2 particleOffset = new(0, -8);
        private readonly Sprite sprite;
        private readonly Collision onCollideH, onCollideV;
        private readonly bool ignoreBarriers, oneUse, evil;
        private bool dead, beenHeld;
        private Level Level;
        private float noGravityTimer, swatTimer;
        private Vector2 prevLiftSpeed, previousPosition;
        private HoldableCollider hitSeeker;
        private float hardVerticalHitSoundCooldown = 0f;
        private ParticleType particle;
        private int spinnerHits = 0;
        public bool currentlyHeld = false;
        private bool fragile = true;
        public bool shattered = false;

        public PaintBall(EntityData data, Vector2 offset)
            : base(data.Position + offset)
        {
            previousPosition = data.Position;
            Depth = -600;
            Collider = new Hitbox(8f, 8f, -4f, -8f);
            Add(sprite = SusanModule.SpriteBank.Create("paintBall"));
            evil = data.Bool("evil", defaultValue: true);
            sprite.Play(evil ? "idleEvil" : "idleGood");
            Add(Hold = new Holdable(0.1f));
            Hold.PickupCollider = new Hitbox(16f, 22f, -8f, -16f);
            Hold.SlowFall = false;
            Hold.SlowRun = false;
            Hold.OnPickup = OnPickup;
            Hold.OnRelease = OnRelease;
            Hold.DangerousCheck = Dangerous;
            Hold.OnHitSeeker = HitSeeker;
            Hold.OnSwat = Swat;
            Hold.OnHitSpring = HitSpring;
            Hold.OnHitSpinner = HitSpinner;
            Hold.SpeedGetter = () => Speed;
            onCollideH = OnCollideH;
            onCollideV = OnCollideV;
            LiftSpeedGraceTime = 0.1f;
            Add(new VertexLight(Collider.Center, Color.White, 1f, 32, 64));
            Add(new MirrorReflection());
            particle = P_Full;
            oneUse = data.Bool("oneUse", defaultValue: false);
            ignoreBarriers = data.Bool("ignoreBarriers", defaultValue: false);
            evil = data.Bool("evil", defaultValue: true);
            beenHeld = false;
            LoadParticles();
            Collidable = true;
        }

        public PaintBall(Vector2 position, Vector2 speed, bool evil, bool oneUse, bool ignoreBarriers) : base(position)
        {
            previousPosition = Position;
            Depth = -600;
            Collider = new Hitbox(8f, 8f, -4f, -8f);
            Add(sprite = SusanModule.SpriteBank.Create("paintBall"));
            this.evil = evil;
            sprite.Play(evil ? "idleEvil" : "idleGood");
            Add(Hold = new Holdable(0.1f));
            Hold.PickupCollider = new Hitbox(16f, 22f, -8f, -16f);
            Hold.SlowFall = false;
            Hold.SlowRun = false;
            Hold.OnPickup = OnPickup;
            Hold.OnRelease = OnRelease;
            Hold.DangerousCheck = Dangerous;
            Hold.OnHitSeeker = HitSeeker;
            Hold.OnSwat = Swat;
            Hold.OnHitSpring = HitSpring;
            Hold.OnHitSpinner = HitSpinner;
            Hold.SpeedGetter = () => Speed;
            onCollideH = OnCollideH;
            onCollideV = OnCollideV;
            LiftSpeedGraceTime = 0.1f;
            Add(new VertexLight(Collider.Center, Color.White, 1f, 32, 64));
            Add(new MirrorReflection());
            particle = P_Full;
            this.oneUse = oneUse;
            this.ignoreBarriers = ignoreBarriers;
            beenHeld = false;
            LoadParticles();
            Collidable = true;
            Speed = speed;
        }

        public static void LoadParticles()
        {
            P_Infinite.Color = Color.Cyan;
            P_Infinite.ColorMode = ParticleType.ColorModes.Static;
            P_Full.Color = Color.Lime;
            P_Full.ColorMode = ParticleType.ColorModes.Static;
            P_Half.Color = Color.LightGoldenrodYellow;
            P_Half.ColorMode = ParticleType.ColorModes.Static;
            P_Low.Color = Color.OrangeRed;
            P_Low.ColorMode = ParticleType.ColorModes.Static;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            Level = SceneAs<Level>();
            //CallPaint(Level);
            currentlyHeld = false;
        }

        public override void Update()
        {
            currentlyHeld = Hold.IsHeld;
            base.Update();
            if (Scene.OnInterval(0.1f))
            {
                bool collided = false;
                foreach (FakeWall wall in Scene.Tracker.GetEntities<FakeWall>())
                {
                    if (wall.Collider.Collide(this))
                    {
                        collided = true;
                    }
                }

                if (!collided)
                {
                    Level.ParticlesFG.Emit(particle, Hold.IsHeld ? 2 : 1, Position + particleOffset, Vector2.One * 5f);
                }
            }

            if (dead)
            {
                return;
            }

            if (swatTimer > 0f)
            {
                swatTimer -= Engine.DeltaTime;
            }

            hardVerticalHitSoundCooldown -= Engine.DeltaTime;
            Depth = 100;

            ParticleType newParticle = P_Low;

            if (Hold.IsHeld)
            {
                if(SusanModule.TryGetPlayer(out Player p))
                {
                    foreach(PaintBall b in (p.Scene as Level).Tracker.GetEntities<PaintBall>())
                    {
                        if (b != this) b.Collidable = false;
                    }
                }
                prevLiftSpeed = Vector2.Zero;
                LastHeld = this;
                if (!Running)
                {
                    Running = true;
                }
            }
            else
            {
                if (SusanModule.TryGetPlayer(out Player p))
                {
                    foreach (PaintBall b in (p.Scene as Level).Tracker.GetEntities<PaintBall>())
                    {
                        b.Collidable = true;
                    }
                }
                if (OnGround())
                {
                    float target = (!OnGround(Position + (Vector2.UnitX * 3f))) ? 20f : (OnGround(Position - (Vector2.UnitX * 3f)) ? 0f : (-20f));
                    Speed.X = Calc.Approach(Speed.X, target, 800f * Engine.DeltaTime);
                    Vector2 liftSpeed = LiftSpeed;
                    if (liftSpeed == Vector2.Zero && prevLiftSpeed != Vector2.Zero)
                    {
                        Speed = prevLiftSpeed;
                        prevLiftSpeed = Vector2.Zero;
                        Speed.Y = Math.Min(Speed.Y * 0.6f, 0f);
                        if (Speed.X != 0f && Speed.Y == 0f)
                        {
                            Speed.Y = -60f;
                        }

                        if (Speed.Y < 0f)
                        {
                            noGravityTimer = 0.15f;
                        }
                    }
                    else
                    {
                        prevLiftSpeed = liftSpeed;
                        if (liftSpeed.Y < 0f && Speed.Y < 0f)
                        {
                            Speed.Y = 0f;
                        }
                    }
                }
                else if (Hold.ShouldHaveGravity)
                {
                    float num = 800f;
                    if (Math.Abs(Speed.Y) <= 30f)
                    {
                        num *= 0.5f;
                    }

                    float num2 = 350f;
                    if (Speed.Y < 0f)
                    {
                        num2 *= 0.5f;
                    }

                    Speed.X = Calc.Approach(Speed.X, 0f, 50 * Engine.DeltaTime);
                    if (noGravityTimer > 0f)
                    {
                        noGravityTimer -= Engine.DeltaTime;
                    }
                    else
                    {
                        Speed.Y = Calc.Approach(Speed.Y, 200f, num * Engine.DeltaTime);
                    }
                }

                previousPosition = ExactPosition;
                MoveH(Speed.X * Engine.DeltaTime, onCollideH);
                MoveV(Speed.Y * Engine.DeltaTime, onCollideV);
                if (Right > Level.Bounds.Right)
                {
                    Right = Level.Bounds.Right;
                    CollisionData data = new()
                    {
                        Direction = Vector2.UnitX
                    };
                    OnCollideH(data);
                }
                else if (Left < Level.Bounds.Left)
                {
                    Left = Level.Bounds.Left;
                    CollisionData data = new()
                    {
                        Direction = -Vector2.UnitX
                    };
                    OnCollideH(data);
                }
                else if (Bottom > Level.Bounds.Bottom && SaveData.Instance.Assists.Invincible)
                {
                    Bottom = Level.Bounds.Bottom;
                    Speed.Y = -300f;
                    Audio.Play("event:/game/general/assist_screenbottom", Position);
                }
                else if (Top > Level.Bounds.Bottom)
                {
                    RemoveSelf();
                }

                Player entity = Scene.Tracker.GetEntity<Player>();
            }

            if (!dead)
            {

                Hold.CheckAgainstColliders();
                if (!ignoreBarriers)
                {
                    foreach (SeekerBarrier entity in Scene.Tracker.GetEntities<SeekerBarrier>())
                    {
                        entity.Collidable = true;
                        bool flag = CollideCheck(entity);
                        entity.Collidable = false;
                        if (flag)
                        {
                            entity.OnReflectSeeker();
                            Die();
                        }
                    }
                }
            }

            if (hitSeeker != null && swatTimer <= 0f && !hitSeeker.Check(Hold))
            {
                hitSeeker = null;
            }

        }

        public override bool IsRiding(Solid solid)
        {
            return Speed.Y == 0f && base.IsRiding(solid);
        }

        protected override void OnSquish(CollisionData data)
        {
            if (!TrySquishWiggle(data) && !SaveData.Instance.Assists.Invincible)
            {
                Die();
            }
        }

        public void Reset()
        {
        }

        public void ExplodeLaunch(Vector2 from)
        {
            if (!Hold.IsHeld)
            {
                Speed = (Center - from).SafeNormalize(120f);
                SlashFx.Burst(Center, Speed.Angle());
            }
        }

        public void Swat(HoldableCollider hc, int dir)
        {
            if (Hold.IsHeld && hitSeeker == null)
            {
                swatTimer = 0.1f;
                hitSeeker = hc;
                Hold.Holder.Swat(dir);
            }
        }

        public bool Dangerous(HoldableCollider holdableCollider)
        {
            return !Hold.IsHeld && Speed != Vector2.Zero && hitSeeker != holdableCollider;
        }

        public void HitSeeker(Seeker seeker)
        {
            Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_side", Position);
            if (!Hold.IsHeld)
            {
                Speed = (Center - seeker.Center).SafeNormalize(120f);
            }
        }

        public void HitSpinner(Entity spinner)
        {
            if (spinnerHits < 10 && !Hold.IsHeld && Speed.Length() < 0.01f && LiftSpeed.Length() < 0.01f && (previousPosition - ExactPosition).Length() < 0.01f && OnGround())
            {
                spinnerHits++;
                int num = Math.Sign(X - spinner.X);
                if (num == 0)
                {
                    num = 1;
                }

                Speed.X = num * 120f;
                Speed.Y = -30f;
            }
        }

        public bool HitSpring(Spring spring)
        {
            if (!Hold.IsHeld)
            {
                if (spring.Orientation == Spring.Orientations.Floor && Speed.Y >= 0f)
                {
                    Speed.X *= 0.5f;
                    Speed.Y = -160f;
                    noGravityTimer = 0.15f;
                    return true;
                }
                else if (spring.Orientation == Spring.Orientations.WallLeft && Speed.X <= 0f)
                {
                    MoveTowardsY(spring.CenterY + 5f, 4f);
                    Speed.X = 220f;
                    Speed.Y = -80f;
                    noGravityTimer = 0.1f;
                    return true;
                }
                else if (spring.Orientation == Spring.Orientations.WallRight && Speed.X >= 0f)
                {
                    MoveTowardsY(spring.CenterY + 5f, 4f);
                    Speed.X = -220f;
                    Speed.Y = -80f;
                    noGravityTimer = 0.1f;
                    return true;
                }
            }

            return false;
        }

        public void Use()
        {
        }

        public void Die()
        {
            if (!dead)
            {
                if (Hold.IsHeld)
                {
                    Vector2 speed2 = Hold.Holder.Speed;
                    Hold.Holder.Drop();
                    Speed = speed2 * 0.333f;
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                }

                dead = true;
                Audio.Play("event:/char/madeline/death", Position);
                Add(new DeathEffect(Color.ForestGreen, Center - Position));
                sprite.Visible = false;
                Depth = -1000000;
                Collidable = false;
                AllowPushing = false;
            }
        }


        private void CallPaint(Level l, Vector2 pos,CollisionTypes t, Platform hit)
        {
            PaintSource p = new PaintSource(pos, 48f, evil, 2.0f, 6.0f, 280f,
                    360f, 15f, 65f, 0.8f, 0.8f, 120f, 180f,t,hit);
            l.Add(p);
            p.OnShatter(l);
            shattered = true;
        }



        private void OnCollideH(CollisionData data)
        {
            if (data.Hit is DashSwitch)
            {
                (data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitX * Math.Sign(Speed.X));
            }


            Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_side", Position);
            if (Math.Abs(Speed.X) > 100f)
            {
                ImpactParticles(data.Direction);
            }

            //Speed.X *= -0.4f;
            if (beenHeld || (fragile && Math.Abs(Speed.X) > 100f))
            {
                if (Speed.X < 0) CallPaint(Scene as Level, CenterLeft+2*Vector2.UnitX, CollisionTypes.LeftWall, data.Hit);
                else CallPaint(this.Scene as Level, CenterRight - 2*Vector2.UnitX, CollisionTypes.RightWall, data.Hit);
                RemoveSelf();
            }
        }

        private void OnCollideV(CollisionData data)
        {
            if (data.Hit is DashSwitch)
            {
                (data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitY * Math.Sign(Speed.Y));
            }

            if (Speed.Y > 0f)
            {
                if (hardVerticalHitSoundCooldown <= 0f)
                {
                    Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_ground", Position, "crystal_velocity", Calc.ClampedMap(Speed.Y, 0f, 200f));
                    hardVerticalHitSoundCooldown = 0.5f;
                }
                else
                {
                    Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_ground", Position, "crystal_velocity", 0f);
                }
            }
            if (beenHeld || (fragile && Math.Abs(Speed.Y)>120f))
            {
                if (Speed.Y > 0) CallPaint(this.Scene as Level, BottomCenter - 2*Vector2.UnitY,CollisionTypes.Floor, data.Hit);
                else CallPaint(this.Scene as Level, TopCenter + 3*Vector2.UnitY, CollisionTypes.Ceiling, data.Hit);
                RemoveSelf();
            }

            if (Speed.Y > 160f)
            {
                ImpactParticles(data.Direction);
            }

            if (Speed.Y > 140f && data.Hit is not SwapBlock && data.Hit is not DashSwitch)
            {
                Speed.Y *= -0.6f;
            }
            else
            {
                Speed.Y = 0f;
            }

        }

        private void ImpactParticles(Vector2 dir)
        {
            float direction;
            Vector2 position;
            Vector2 positionRange;
            if (dir.X > 0f)
            {
                direction = (float)Math.PI;
                position = new Vector2(Right, Y - 4f);
                positionRange = Vector2.UnitY * 6f;
            }
            else if (dir.X < 0f)
            {
                direction = 0f;
                position = new Vector2(Left, Y - 4f);
                positionRange = Vector2.UnitY * 6f;
            }
            else if (dir.Y > 0f)
            {
                direction = -(float)Math.PI / 2f;
                position = new Vector2(X, Bottom);
                positionRange = Vector2.UnitX * 6f;
            }
            else
            {
                direction = (float)Math.PI / 2f;
                position = new Vector2(X, Top);
                positionRange = Vector2.UnitX * 6f;
            }

            Level.Particles.Emit(TheoCrystal.P_Impact, 12, position, positionRange, direction);
        }

        private void OnPickup()
        {
            spinnerHits = 0;
            Speed = Vector2.Zero;
            beenHeld = true;
            AddTag(Tags.Persistent);
        }

        private void OnRelease(Vector2 force)
        {
            RemoveTag(Tags.Persistent);
            Vector2 dir = Vector2.Zero;
            if (SusanModule.TryGetPlayer(out Player p))
            {
                if (force.X != 0f && force.Y == 0f)
                {
                    dir = new Vector2(Math.Sign(force.X), -0.33f);
                    dir.Normalize();
                }

                if (Input.LastAim.X == 0f)
                {
                    dir = new Vector2(0.33f * (int)p.Facing, Math.Sign(Input.LastAim.Y));
                    dir.Normalize();
                }
                if (Speed != Vector2.Zero)
                {
                    noGravityTimer = 0.1f;
                }
            }
            Speed = 200f * dir;
        }
    }
}