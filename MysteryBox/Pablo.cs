using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.SusanHelper
{
    [CustomEntity("SusanHelper/Pablo")]
    public class Pablo : Actor
    {
        public float speed;
        public Sprite sprite;
        public Hitbox hitbox;
        private float cannotHitTimer;
        private float vertSpd = 0;
        private bool god;
        private Circle head;
        private Hitbox body;
        private ColliderList list = new ColliderList();
        private float currentSpeed = 0;
        private Vector2 lastPos;
        private Collision onCollideH;
        private int dashes;
        private bool noGrav;
        private bool goat;
        public Pablo(Vector2 position, float speed, int dashes, bool god, bool noGrav, bool goat) : base(position)
        {
            this.noGrav = noGrav;
            Position.Y -= 1;
            Add(goat ? sprite = SusanModule.SpriteBank.Create("pabloSaiph") : sprite = SusanModule.SpriteBank.Create("pablo"));
            this.god = god;
            this.dashes = dashes;
            this.speed = speed/10;
            this.goat = false;
            head = new Circle(6, 4);
            body = new Hitbox(20f, 6f, -10f,2f);
            base.Collider = list;
            list.Add(head, body);
            Add(new PlayerCollider(OnPlayer, list));

        }
        public Pablo(EntityData data, Vector2 offset) : this(data.Position + offset, data.Float("speed", 2f), data.Int("dashes", 1), data.Bool("god", defaultValue: false), data.Bool("noGrav", defaultValue: false), data.Bool("goat", defaultValue: false))
        {
        }


        public override bool IsRiding(JumpThru jumpThru)
        {
            return false;
        }

        public override bool IsRiding(Solid solid)
        {
            return false;
        }

        protected override void OnSquish(CollisionData data)
        {
            RemoveSelf();
        }

        public override void Update()
        {
            base.Update();
            sprite.Visible = true;
            if (cannotHitTimer > 0f)
            {
                cannotHitTimer -= Engine.DeltaTime;
            }
            currentSpeed = Calc.Approach(speed, currentSpeed, Engine.DeltaTime);
            MoveH(currentSpeed);
            if (!base.OnGround())
            {
                vertSpd = Calc.Approach(0.75f, vertSpd, Engine.DeltaTime);
            }
            else
            {
                vertSpd = 0f;
            }
            if (!noGrav)
            {
                MoveV(god ? -vertSpd : vertSpd);
            }
            if(speed < 0)
            {
                sprite.Scale.X = -1;
            }
            else
            {
                sprite.Scale.X = 1;
            }
            Solid solid = CollideFirst<Solid>(Position + (currentSpeed < 0f ? -2 : 2) * Vector2.UnitX);
            if (solid != null)
            {
                speed *= -1;
                head.Position.X -= speed < 0 ? 8 :-8;
            }
            lastPos = Position;
        }
        private void OnPlayer(Player player)
        {

            if (player.Speed.Y >= 0)
            {
                player.Bounce(base.Top);
                player.Speed *= 1.2f;
                Audio.Play("event:/new_content/game/10_farewell/puffer_boop", Position);
                player.Dashes = dashes;
                
            }
        }

    }
}
