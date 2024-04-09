// Celeste.Mod.SusanHelper.Entities.HangingCeiling
using System.Collections;
using System.Security.Cryptography;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;
using Celeste.Mod.SusanHelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using SusanHelper.Entities.Paint;
using static On.Celeste.Solid;
using static SusanHelper.Entities.Paint.PaintLiquid;

[Tracked(true)]
[CustomEntity(new string[] { "SusanHelper/HangingCeiling" })]
internal class HangingCeiling : Entity
{

    private Sprite playerSprite;

    private bool PlayerCollideSolid;

    private bool Flashing;

    public bool CanJump = true;

    private bool playSfx;

    private PlayerCollider pc;

    private StaticMover staticMover;

    private Vector2 imageOffset;

    private bool NoStaminaDrain = true;

    public bool JumpGracePeriod;

    public bool playerWasAttached;

    public static Player player;

    private Coroutine JumpGraceTimerRoutine = new Coroutine();

    public Color EnabledColor = Color.White;

    public Color DisabledColor = Color.White;

    public bool VisibleWhenDisabled;

    public Hitbox h,hp;

    private PaintLiquid orig;

    private bool alreadyJumped = false;

    private bool holdingThis = false;

    
    public HangingCeiling(EntityData data, Vector2 offset)
        : base(data.Position + offset)
    {
        base.Tag = Tags.TransitionUpdate;
        h = new Hitbox(data.Width, 3f);
        hp = new Hitbox(data.Width, 6f);
        base.Collider = h;
        CanJump = data.Bool("canJump");
        Add(playerSprite = SusanModule.SpriteBank.Create("SusanHelper_player_ceiling"));
        playerSprite.Visible = false;
        Add(pc = new PlayerCollider(OnCollide, hp));
        Add(staticMover = new StaticMover
        {
            OnShake = OnShake,
            SolidChecker = IsRiding,
            JumpThruChecker = IsRiding,
            OnEnable = OnEnable,
            OnDisable = OnDisable,
            OnMove = OnMove
        });
        base.Depth = -9999;
        playSfx = true;
    }



    public static void Load()
    {
        Everest.Events.Player.OnSpawn += onPlayerSpawn;
        Everest.Events.Player.OnDie += onPlayerDie;
        On.Celeste.Player.IsRiding_Solid += OnPlayerIsRiding;
        On.Celeste.Solid.GetPlayerOnTop += OnSolidGetPlayerOnTop;
    }

    public static void Unload()
    {
        Everest.Events.Player.OnSpawn -= onPlayerSpawn;
        Everest.Events.Player.OnDie -= onPlayerDie;
        On.Celeste.Player.IsRiding_Solid -= OnPlayerIsRiding;
        On.Celeste.Solid.GetPlayerOnTop -= OnSolidGetPlayerOnTop;
    }

    private static void onPlayerSpawn(Player player)
    {
        if (player.SceneAs<Level>().Session.GetFlag("Susan_Helper_Ceiling"))
        {
            player.SceneAs<Level>().Session.SetFlag("Susan_Helper_Ceiling", setTo: false);
        }
    }

    private static void onPlayerDie(Player player)
    {
        foreach (HangingCeiling ceiling in player.SceneAs<Level>().Tracker.GetEntities<HangingCeiling>())
        {
            if (ceiling.playerSprite.Visible)
            {
                ceiling.playerSprite.Visible = false;
                if (!player.Sprite.Visible)
                {
                    player.Sprite.Visible = true;
                }
            }
        }
    }

    private static bool OnPlayerIsRiding(On.Celeste.Player.orig_IsRiding_Solid orig, Player self, Solid solid)
    {
        if (self.SceneAs<Level>().Session.GetFlag("Susan_Helper_Ceiling"))
        {
            return Collide.Check(self, solid, self.Position - new Vector2(0f, 6f));
        }
        return orig.Invoke(self, solid);
    }

    private static Player OnSolidGetPlayerOnTop(On.Celeste.Solid.orig_GetPlayerOnTop orig, Solid self)
    {
        if (self.SceneAs<Level>().Session.GetFlag("Susan_Helper_Ceiling") && player != null)
        {
            return Collide.Check(self, player, self.Position + new Vector2(0f, self.Height) + Vector2.UnitY) ? player : null;
        }
        return orig.Invoke(self);
    }


    public override void Update()
    {
        base.Update();
        if (Input.Jump.Released)
        {
            alreadyJumped = false;
        }
        if (SusanModule.TryGetPlayer(out Player player))
        {

            PlayerCollideSolid = player.CollideCheck<Solid>(player.Position + Vector2.UnitX) || player.CollideCheck<Solid>(player.Position - Vector2.UnitX);
            if (player.Stamina <= 20f && base.Scene.OnInterval(0.06f))
            {
                Flashing = !Flashing;
            }
            if (player.Stamina > 20f)
            {
                Flashing = false;
            }
            playerSprite.Color = Flashing ? Color.Red : Color.White;
            playerSprite.FlipX = player.Facing == Facings.Left;
            if (playerWasAttached && (SceneAs<Level>().Transitioning || !player.CollideCheck<HangingCeiling>(player.Position - Vector2.UnitY)))
            {
                DetachPlayer(player);
            }
            Collidable = SusanModule.Session.hangingToRainbowInk;
        }
    }

    private IEnumerator JumpGraceTimer(Player player)
    {
        float timer = 0.1f;
        JumpGracePeriod = true;
        while (timer > 0f)
        {
            timer -= Engine.DeltaTime;
            yield return null;
            if (Input.Jump.Pressed && CanJump)
            {
                timer = 0f;
            }
        }
        JumpGracePeriod = false;
    }

    private void DetachPlayer(Player player)
    {
        SceneAs<Level>().Session.SetFlag("Susan_Helper_Ceiling", setTo: false);
        SceneAs<Level>().Session.SetFlag("Susan_Helper_Ceiling_Can_Jump", setTo: false);
        player.Sprite.Visible = true;
        playerSprite.Visible = false;
        playSfx = true;
        playerWasAttached = false;
    }

    private void OnCollide(Player player)
    {
        holdingThis = !CollideCheck<Player>(Position);
        if (holdingThis)
        {
            if (player.Holding != null)
            {
                return;
            }
            if (!(holdingThis && player.Speed.Y == -105f) && player.CollideCheck(this, player.Position - Vector2.UnitY))
            {
                if (Input.GrabCheck && (!CanJump || player.Speed.Y >= 0f) && player.Left > base.Left - 6f && player.Right < base.Right + 6f && player.Top > base.Top && player.Stamina > 0f && !player.Dead && !player.CollideCheck<Spikes>())
                {
                    playerWasAttached = true;
                    if (playSfx)
                    {
                        Audio.Play("event:/char/madeline/climb_ledge");
                        playSfx = false;
                    }
                    SceneAs<Level>().Session.SetFlag("Susan_Helper_Ceiling");
                    if (CanJump)
                    {
                        SceneAs<Level>().Session.SetFlag("Susan_Helper_Ceiling_Can_Jump");
                    }
                    player.Sprite.Visible = false;
                    playerSprite.Visible = true;
                    player.Speed.Y = 0f;
                    player.MoveToY(base.Bottom + 11f);
                    staticMover.TriggerPlatform();
                    if (player.Sprite.CurrentAnimationID != "idle")
                    {
                        player.Sprite.Play("idle");
                    }
                    if (Input.Jump.Pressed && !alreadyJumped)
                    {
                        player.Jump();
                        alreadyJumped = true;
                    }
                }
                else if (player.CollideCheck<Spikes>())
                {
                    DetachPlayer(player);
                    player.Die(new Vector2(0f, 1f));
                }
                else if (playerWasAttached)
                {
                    if (CanJump && !JumpGraceTimerRoutine.Active)
                    {
                        Add(JumpGraceTimerRoutine = new Coroutine(JumpGraceTimer(player)));
                        playerWasAttached = false;
                    }
                    DetachPlayer(player);
                }
            }
            else if (playerWasAttached)
            {
                DetachPlayer(player);
            }
        }
    }

    public override void Render()
    {
        base.Render();
        Player player = SceneAs<Level>().Tracker.GetEntity<Player>();
        if (player != null && playerSprite != null)
        {
            playerSprite.RenderPosition = player.Position + new Vector2(-16f, -31f);
            string backpack = (SceneAs<Level>().Session.Inventory.Backpack ? "Backpack" : "NoBackpack");
            if (Input.Aim.Value.SafeNormalize().X != 0f && !PlayerCollideSolid && !SceneAs<Level>().Paused)
            {
                Sprite sprite = playerSprite;
                sprite.RenderPosition += new Vector2(Input.Aim.Value.SafeNormalize().X, 0f);
                playerSprite.Play("move" + backpack);
            }
            else
            {
                playerSprite.Play("idle" + backpack);
            }
        }
    }

    private void OnShake(Vector2 amount)
    {
        imageOffset += amount;
    }

    private bool IsRiding(Solid solid)
    {
        return CollideCheckOutside(solid, Position - Vector2.UnitY);
    }

    private bool IsRiding(JumpThru jumpThru)
    {
        return CollideCheck(jumpThru, Position - Vector2.UnitY);
    }

    public void SetCeilingColor(Color color)
    {
        foreach (Component component in base.Components)
        {
            if (component is Sprite sprite)
            {
                sprite.Color = color;
            }
        }
    }

    private void OnEnable()
    {
        Visible = (Collidable = true);
        SetCeilingColor(EnabledColor);
    }

    private void OnDisable()
    {
        Collidable = false;
        Player player = SceneAs<Level>().Tracker.GetEntity<Player>();
        if (player != null && SceneAs<Level>().Session.GetFlag("Susan_Helper_Ceiling"))
        {
            DetachPlayer(player);
        }
        JumpGracePeriod = false;
        if (VisibleWhenDisabled)
        {
            SetCeilingColor(DisabledColor);
        }
        else
        {
            Visible = false;
        }
    }

    private void OnMove(Vector2 amount)
    {
        Position += amount;
    }
}
