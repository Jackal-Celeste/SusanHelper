// Susan usings.
using Celeste.Mod.UI;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Celeste.Mod.Entities;
using MonoMod.RuntimeDetour;
using On.Celeste;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using IL.Celeste;
using MonoMod.Utils;
using SusanHelper.Entities.Paint;
using Mono.Cecil;

namespace Celeste.Mod.SusanHelper
{
    public class SusanModule : EverestModule
    {

        // Only one alive module instance can exist at any given time.
        public static SusanModule Instance;
        public static SpriteBank SpriteBank;
        public SusanModule()
        {
            Instance = this;
        }

        public override Type SessionType => typeof(SusanHelperSession);
        public static SusanHelperSession Session => (SusanHelperSession)Instance._Session;

        // Check the next section for more information about mod settings, save data and session.
        // Those are optional: if you don't need one of those, you can remove it from the module.

        private static FieldInfo playerDreamJump = typeof(Player).GetField("dreamJump", BindingFlags.NonPublic | BindingFlags.Instance);

        public static Player GetPlayer()
        {
            return (Engine.Scene as Level)?.Tracker?.GetEntity<Player>();
        }

        public static bool TryGetPlayer(out Player player)
        {
            player = GetPlayer();
            return player != null;
        }

        // Set up any hooks, event handlers and your mod in general here.
        // Load runs before Celeste itself has initialized properly.
        public override void Load()
        {
            On.Celeste.DreamBlock.Render += DreamBlock_Render;
            On.Celeste.DreamBlock.Setup += DreamBlock_Setup;

            On.Celeste.Player.NormalUpdate += HoldableUpdate;
            //IL.Celeste.Player.NormalUpdate += patchJumpGraceTimer;


            //funny hooks
            //On.Celeste.Puffer.Explode += Paint_Explode;

            On.Celeste.Player.RefillDash += Player_RefillDash;


        }

        private bool Player_RefillDash(On.Celeste.Player.orig_RefillDash orig, Player self)
        {
            Water w = self.CollideFirst<Water>();
            if (w != null && (w is InkWater) &&!self.DashAttacking && self.Dashes == 0)
            {
                return false;
            }

            return orig.Invoke(self);
        }

        // Optional, initialize anything after Celeste has initialized itself properly.
        public override void Initialize()
        {
        }

        private int HoldableUpdate(On.Celeste.Player.orig_NormalUpdate orig, Player self)
        {
            Holdable h;
            bool origGrabData = SaveData.Instance.Assists.NoGrabbing;
            if (self.Holding?.Entity is PaintBall && Input.GrabCheck)
            {
                h = self.Holding;
                PaintBall tc = (self.Holding?.Entity as PaintBall);
                tc.Collidable = false;
                self.Holding = null;
                SaveData.Instance.Assists.NoGrabbing = true;
                int origState = orig.Invoke(self);
                self.Holding = h;
                tc.Collidable = true;
                SaveData.Instance.Assists.NoGrabbing = origGrabData;
                return origState;
            }
            return orig.Invoke(self);
        }


        public void DreamBlock_Render(On.Celeste.DreamBlock.orig_Render orig, DreamBlock dreamblock)
        {
            Session session = ((dreamblock.Scene is Level level) ? level.Session : null);
            if (session != null && dreamblock is CurrentDreamBlock)
            {
                CurrentDreamBlock currentDreamBlock = dreamblock as CurrentDreamBlock;
                currentDreamBlock.WindRender();         
            }
            else
            {
                orig.Invoke(dreamblock);
            }
        }


        public void DreamBlock_Setup(On.Celeste.DreamBlock.orig_Setup orig, DreamBlock dreamblock)
        {
            Session session = ((dreamblock.Scene is Level level) ? level.Session : null);
            if (dreamblock is CurrentDreamBlock)
            {
                CurrentDreamBlock currentDreamBlock = dreamblock as CurrentDreamBlock;
                currentDreamBlock.WindSetup();
            }
            else
            {
                orig.Invoke(dreamblock);
            }
        }


        public static Color PickColor(string str)
        {
            return Calc.HexToColor(Calc.Random.Choose(str.ToUpper().Split(',').ToList()));
        }

        // Optional, do anything requiring either the Celeste or mod content here.
        public override void LoadContent(bool firstLoad)
        {
            base.LoadContent(firstLoad);
            SpriteBank = new SpriteBank(GFX.Game, "Graphics/SusanHelper/Sprites.xml");
        }

        // Unload the entirety of your mod's content. Free up any native resources.
        public override void Unload()
        {
            On.Celeste.DreamBlock.Render -= DreamBlock_Render;
            On.Celeste.DreamBlock.Setup -= DreamBlock_Setup;

            On.Celeste.Player.NormalUpdate -= HoldableUpdate;

            On.Celeste.Player.RefillDash -= Player_RefillDash;
            //IL.Celeste.Player.NormalUpdate -= patchJumpGraceTimer;
            //funny hooks
            //On.Celeste.Puffer.Explode -= Paint_Explode;
        }

    }
}