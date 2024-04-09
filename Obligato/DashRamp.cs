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

namespace Celeste.Mod.SusanHelper
{
    [CustomEntity(new string[] { "SusanHelper/DashRamp" })]
    [Tracked]
    public class DashRamp : Entity
    {
        public enum DashRampFacings
        {
            Up, SteepUpDiag, UpDiag, MildUpDiag, Horizonal, MildDownDiag, DownDiag, SteepDownDiag, Down
        }
        private Sprite sprite;
        private float cooldown =0f;
        private DashRampFacings facing;
        private bool left, carryXSpd, refillDash;
        private StaticMover staticMover;
        private float launchSpeed, orthogonalSpd;
        private Vector2 launch, adjustment;
        private readonly float dashReset = 0.5f;
        private float ticker = 0f;
        private DynData<Player> pData;
        public static ParticleType P_Dissipate = new ParticleType
        {
            Color = Color.DarkSlateGray,
            Size = 1f,
            FadeMode = ParticleType.FadeModes.Late,
            SpeedMin = 30f,
            SpeedMax = 60f,
            DirectionRange = (float)Math.PI / 3f,
            LifeMin = 0.3f,
            LifeMax = 0.6f
        };

        private DashRampFacings dumbCast(string str)
        {
            if (str.Contains("Diag"))
            {
                if (str.Contains("Steep")) return str.Contains("Up") ? DashRampFacings.SteepUpDiag : DashRampFacings.SteepDownDiag;
                if (str.Contains("Mild")) return str.Contains("Up") ? DashRampFacings.MildUpDiag : DashRampFacings.MildDownDiag;
                return str.Contains("Up") ? DashRampFacings.UpDiag : DashRampFacings.DownDiag;
            }
            
            return str == "Up" ?DashRampFacings.Up : str == "Down" ? DashRampFacings.Down : DashRampFacings.Horizonal;
        }
        private void spriteFlip(Sprite s, bool h, bool v)
        {
            s.FlipX = h;
            s.FlipY = v;
        }

        public DashRamp(EntityData data, Vector2 offset)
        : base(data.Position + offset)
        {
            facing = dumbCast(data.Attr("facing"));
            left = data.Bool("left", defaultValue: false);
            refillDash = data.Bool("refillDash", defaultValue: false);
            orthogonalSpd = data.Int("orthogonalSpeed", defaultValue: 100);
            launchSpeed = (float)data.Int("launchSpeed", defaultValue: 300);
            carryXSpd = data.Bool("carryXSpd", defaultValue: false);
            Add(sprite = SusanModule.SpriteBank.Create("dashRamp"));
            staticMover = new StaticMover();
            staticMover.OnAttach = delegate (Platform p)
            {
                base.Depth = p.Depth + 1;
            };
            switch (facing)
            {
                case DashRampFacings.Horizonal:
                    Vector2 adjustment = (left ? -1f : 1f) * Vector2.UnitX;
                    launch = new Vector2(launchSpeed * (left ? -1f : 1f), -orthogonalSpd);

                    staticMover.SolidChecker = (Solid s) => CollideCheck(s, Position + adjustment);
                    staticMover.JumpThruChecker = (JumpThru jt) => CollideCheck(jt, Position + adjustment);
                    spriteFlip(sprite, left, false);
                    Collider = new Hitbox(32f, 8f, -16f, 0f);
                    break;
                case DashRampFacings.Up:

                    launch = new Vector2(orthogonalSpd * (left ? -1f : 1f), -launchSpeed);

                    staticMover.SolidChecker = (Solid s) => CollideCheck(s, Position + Vector2.UnitY);
                    staticMover.JumpThruChecker = (JumpThru jt) => CollideCheck(jt, Position + Vector2.UnitY);

                    sprite.Rotation = (float)Math.PI / 2f;
                    spriteFlip(sprite, true, left);
                    Collider = new Hitbox(8f, 32f, left ? 0f : -8f, -16f);
                    break;
                case DashRampFacings.Down:

                    launch = new Vector2(orthogonalSpd * (left ? -1f : 1f), launchSpeed);

                    staticMover.SolidChecker = (Solid s) => CollideCheck(s, Position - Vector2.UnitY);
                    staticMover.JumpThruChecker = (JumpThru jt) => CollideCheck(jt, Position - Vector2.UnitY);

                    sprite.Rotation = (float)Math.PI / 2f;
                    spriteFlip(sprite, false, left);
                    Collider = new Hitbox(8f, 32f, left ? 0f : -8f, -16f);
                    break;
                case DashRampFacings.UpDiag:
                    launch = new Vector2(launchSpeed * (left ? -1f : 1f) * (float)Math.Cos(Math.PI/4), -launchSpeed * (float)Math.Sin(Math.PI / 4));
                    Vector2 _off = new Vector2((left ? -1f : 1f), 1f);
                    staticMover.SolidChecker = (Solid s) => CollideCheck(s, Position - _off);
                    staticMover.JumpThruChecker = (JumpThru jt) => CollideCheck(jt, Position - _off);

                    sprite.Rotation = left ? (float)Math.PI / 4f : -(float)Math.PI/4f;
                    spriteFlip(sprite, left, false);
                    Collider = new ColliderList(
                        new Hitbox(4f, 4f, left ? 8f : -12f, 8f),
                        new Hitbox(4f, 12f, left ? 4f : -8f, 4f),
                        new Hitbox(4f, 12f, left ? 0f : -4f, 0f),
                        new Hitbox(4f, 12f, left ? -4f : 0f, -4f),
                        new Hitbox(4f, 12f, left ? -8f : 4f, -8f),
                        new Hitbox(4f, 12f, left ? -12f : 8f, -12f));
                    break;
                case DashRampFacings.DownDiag:
                    launch = new Vector2(launchSpeed * (left ? -1f : 1f) * (float)Math.Cos(Math.PI / 4), launchSpeed * (float)Math.Sin(Math.PI / 4));
                    Vector2 _off2 = new Vector2((left ? -1f : 1f), 1f);
                    staticMover.SolidChecker = (Solid s) => CollideCheck(s, Position - _off2);
                    staticMover.JumpThruChecker = (JumpThru jt) => CollideCheck(jt, Position - _off2);

                    sprite.Rotation = left ? -(float)Math.PI / 4f : (float)Math.PI / 4f;
                    spriteFlip(sprite, left, true);
                    Collider = new ColliderList(
                        new Hitbox(4f, 4f, left ? 8f : -12f, -12f),
                        new Hitbox(4f, 12f, left ? 4f : -8f, -16f),
                        new Hitbox(4f, 12f, left ? 0f : -4f, -12f),
                        new Hitbox(4f, 12f, left ? -4f : 0f, -8f),
                        new Hitbox(4f, 12f, left ? -8f : 4f, -4f),
                        new Hitbox(4f, 12f, left ? -12f : 8f, 0f));
                    break;
                case DashRampFacings.MildUpDiag:
                    launch = new Vector2(launchSpeed * (left ? -1f : 1f) * (float)Math.Cos(Math.PI / 8), -launchSpeed * (float)Math.Sin(Math.PI / 8));
                    Vector2 _off3 = new Vector2((left ? -1f : 1f), 1f);
                    staticMover.SolidChecker = (Solid s) => CollideCheck(s, Position - _off3);
                    staticMover.JumpThruChecker = (JumpThru jt) => CollideCheck(jt, Position - _off3);

                    sprite.Rotation = left ? (float)Math.PI / 8f : -(float)Math.PI / 8f;
                    spriteFlip(sprite, left, false);
                    Collider = new ColliderList(
                        new Hitbox(4f, 12f, left ? 8f : -12f, 4f),
                        new Hitbox(4f, 12f, left ? 4f : -8f, 2f),
                        new Hitbox(4f, 12f, left ? 0f : -4f, 0f),
                        new Hitbox(4f, 12f, left ? -4f : 0f, -2f),
                        new Hitbox(4f, 12f, left ? -8f : 4f, -4f),
                        new Hitbox(4f, 12f, left ? -12f : 8f, -6f));
                    break;
                case DashRampFacings.MildDownDiag:
                    launch = new Vector2(launchSpeed * (left ? -1f : 1f) * (float)Math.Cos(Math.PI / 8), launchSpeed * (float)Math.Sin(Math.PI / 8));
                    Vector2 _off4 = new Vector2((left ? -1f : 1f), 1f);
                    staticMover.SolidChecker = (Solid s) => CollideCheck(s, Position - _off4);
                    staticMover.JumpThruChecker = (JumpThru jt) => CollideCheck(jt, Position - _off4);

                    sprite.Rotation = -1f*(left ? (float)Math.PI / 8f : -(float)Math.PI / 8f);
                    spriteFlip(sprite, left, true);
                    Collider = new ColliderList(
                        new Hitbox(4f, 12f, !left ? 8f : -12f, -6f),
                        new Hitbox(4f, 12f, !left ? 4f : -8f, -8f),
                        new Hitbox(4f, 12f, !left ? 0f : -4f, -10f),
                        new Hitbox(4f, 12f, !left ? -4f : 0f, -12f),
                        new Hitbox(4f, 12f, !left ? -8f : 4f, -14f),
                        new Hitbox(4f, 12f, !left ? -12f : 8f, -16f));
                    break;
                case DashRampFacings.SteepUpDiag:
                    launch = new Vector2(launchSpeed * (left ? -1f : 1f) * (float)Math.Cos(3*Math.PI / 8), -launchSpeed * (float)Math.Sin(3*Math.PI / 8));
                    Vector2 _off6 = new Vector2((left ? -1f : 1f), 1f);
                    staticMover.SolidChecker = (Solid s) => CollideCheck(s, Position - _off6);
                    staticMover.JumpThruChecker = (JumpThru jt) => CollideCheck(jt, Position - _off6);

                    sprite.Rotation = 3f* (left ? (float)Math.PI / 8f : -(float)Math.PI / 8f);
                    spriteFlip(sprite, left, false);
                    Collider = new ColliderList(
                        new Hitbox(4f, 4f, left ? -2f : 0f, 14f),
                        new Hitbox(12f, 4f, left ? -4f : -8f, 10f),
                        new Hitbox(12f, 4f, left ? -6f : -6f, 6f),
                        new Hitbox(12f, 4f, left ? -8f : -4f, 2f),
                        new Hitbox(12f, 4f, left ? -10f : -2f, -2f),
                        new Hitbox(12f, 4f, left ? -12f : 0f, -6f),
                        new Hitbox(4f, 4f, left ? -6f : 2f, -10f));
                    break;
                case DashRampFacings.SteepDownDiag:
                    launch = new Vector2(launchSpeed * (left ? -1f : 1f) * (float)Math.Cos(3 * Math.PI / 8), launchSpeed * (float)Math.Sin(3 * Math.PI / 8));
                    Vector2 _off7 = new Vector2((left ? -1f : 1f), 1f);
                    staticMover.SolidChecker = (Solid s) => CollideCheck(s, Position - _off7);
                    staticMover.JumpThruChecker = (JumpThru jt) => CollideCheck(jt, Position - _off7);

                    sprite.Rotation = 5f* (left ? (float)Math.PI / 8f : -(float)Math.PI / 8f);
                    spriteFlip(sprite, !left, true);
                    Collider = new ColliderList(
                        new Hitbox(4f, 4f, !left ? -6f : 2f, 10f),
                        new Hitbox(12f, 4f, !left ? -8f : -4f, 6f),
                        new Hitbox(12f, 4f, !left ? -10f : -2f, 2f),
                        new Hitbox(12f, 4f, !left ? -12f : 0f, -2f),
                        new Hitbox(12f, 4f, !left ? -14f : 2f, -6f),
                        new Hitbox(12f, 4f, !left ? -16f : 4f, -10f),
                        new Hitbox(4f, 4f, !left ? -10f : 6f, -14f));
                    break;
            }
            Depth = -8501;
            Add(new PlayerCollider(OnPlayer));

            Add(staticMover);
        }

        private void OnPlayer(Player player)
        {
            if (player != null)
            {
                cooldown = 0.3f;
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                Level level = SceneAs<Level>();
                level.Shake();
                level.Displacement.AddBurst(Position, 0.2f, 8f, 48f);
                for (int i = 0; i < 5; ++i)
                {
                    float dir = Calc.Random.NextAngle();
                    level.Particles.Emit(P_Dissipate, 10, Position + Calc.AngleToVector(dir, 8f), Vector2.One * 10f, dir);
                }
                //Audio.Play("event:/ricky06/ZBC2023/DashRamp", Position);
                global::Celeste.Celeste.Freeze(0.05f);
                DynData<Player> playerData = new DynData<Player>(player);
                playerData.Set("jumpGraceTimer", 0f);
                playerData.Set("wallBoostTimer", 0f);
                sprite.Play("launch");
                player.StateMachine.State = 0;
                if (facing == DashRampFacings.Up || facing == DashRampFacings.SteepUpDiag || facing == DashRampFacings.UpDiag || facing == DashRampFacings.MildUpDiag)
                {
                    player.MoveV(Top - player.Bottom);
                }
                else if (facing != DashRampFacings.Horizonal)
                {
                    player.MoveV(Bottom - player.Top);
                }
                else
                {
                    player.MoveV(Bottom - player.Top);
                }

                if (facing != DashRampFacings.Up && facing != DashRampFacings.Down)
                {
                    float padPos = left ? Left : Right;
                    float playerPos = left ? player.Right : player.Left;
                    player.MoveH(padPos - playerPos);
                }
                if (!carryXSpd)
                {
                    player.Speed = launch;
                }
                else
                {
                    player.Speed.X = left ? Math.Min(launch.X, player.Speed.X + launch.X) : Math.Max(launch.X, player.Speed.X + launch.X);
                    player.Speed.Y = launch.Y;
                }
                if (refillDash) player.RefillDash();
                pData = new DynData<Player>(player);
                pData.Set<float>("dashCooldownTimer", 0.15f);
            }
            Collidable = false;
        }

        public override void Update()
        {
            base.Update();
            if (cooldown > 0f)
            {
                cooldown -= Engine.DeltaTime;
            }
            Collidable = cooldown <= 0f;
        }

        public override void Render()
        {
            sprite.DrawOutline();
            base.Render();
        }

    }
}
