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
	[CustomEntity("SusanHelper/BigFungus")]
	public class BigFungus : Actor
	{
		private Entity outline;
		private enum States
		{
			Idle,
			Hit,
			Gone
		}

		private const float RespawnTime = 2.5f;

		private const float RespawnMoveTime = 0.5f;

		private const float BounceSpeed = 200f;

		private const float DetectRadius = 32f;

		private const float StunnedAccel = 320f;

		private const float AlertedRadius = 60f;

		private States state;

		private Vector2 startPosition;

		private Vector2 anchorPosition;

		private Vector2 lastSpeedPosition;

		private Vector2 lastSinePosition;

		private Circle pushRadius;

		private Circle breakWallsRadius;

		private Circle detectRadius;

		private SineWave idleSine;

		private Vector2 hitSpeed;

		private float goneTimer;

		private float cannotHitTimer;

		private Collision onCollideV;

		private Collision onCollideH;

		private float alertTimer;

		private Wiggler inflateWiggler;

		private Vector2 scale;
		private float scalar;


		private SimpleCurve returnCurve;

		private Vector2 lastPlayerPos;

		private float playerAliveFade;

		private Vector2 facing = Vector2.One;

		private float eyeSpin;
		private float width;
		private float hitSpd;
		private string key;
		private List<Image> stem = new List<Image>();
		private bool longStem;

		public BigFungus(Vector2 position, float width, float hitSpd, string type)
			: base(position)
		{
			base.Collider = new Hitbox(width, 6f);
			this.hitSpd = hitSpd;
			key = type.Substring(0, 1).ToLower();
			Add(new PlayerCollider(OnPlayer, base.Collider));
			//Add(stem = new Image(GFX.Game["objects/bigFungus/stem"]));
			this.width = width;
			scalar = width / 12f;
			idleSine = new SineWave(0.5f, 0f);
			idleSine.Randomize();
			Add(idleSine);
			anchorPosition = Position;
			Position += new Vector2(idleSine.Value * 3f, idleSine.ValueOverTwo * 2f);
			state = States.Idle;
			startPosition = (lastSinePosition = (lastSpeedPosition = Position));
			pushRadius = new Circle(40f);
			detectRadius = new Circle(32f);
			breakWallsRadius = new Circle(16f);
			onCollideV = OnCollideV;
			onCollideH = OnCollideH;
			scale = new Vector2(scalar, 1);
			inflateWiggler = Wiggler.Create(0.6f, 2f);
			Add(inflateWiggler);
			Console.WriteLine(width);
		}

		public BigFungus(EntityData data, Vector2 offset)
			: this(data.Position + offset, data.Width, data.Float("hitSpd", defaultValue: 200f), data.Attr("type", defaultValue: "Green"))
		{
		}



		public override void Awake(Scene scene)
        {
            base.Awake(scene);
			if (false)
			{
				
			}
            else
            {
				Image i;
				Add(i = new Image(GFX.Game["objects/bigFungus/stem"]));
				i.X += (width / 2) - 12;
            }
			MTexture mTexture = GFX.Game["objects/bigFungus/"+key+"mush"];
			int num = mTexture.Width / 8;
			for (int i = 0; i < width/8; i++)
			{
				int x;
				int y;
				if (i == 0)
				{
					x = 0;
					y = ((!CollideCheck<Solid>(Position + new Vector2(-1f, 0f))) ? 1 : 0);
				}
				else if (i == width/8 - 1)
				{
					x = num - 1;
					y = ((!CollideCheck<Solid>(Position + new Vector2(1f, 0f))) ? 1 : 0);
				}
				else
				{
					x = 1 + Calc.Random.Next(num - 2);
					y = Calc.Random.Choose(0, 1);
				}
				Image image = new Image(mTexture.GetSubtexture(x * 8, y * 8, 8, 8))
				{
					X = i * 8
				};
				Add(image);
			}
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
			GotoGone();
		}

		private void OnCollideH(CollisionData data)
		{
			hitSpeed.X *= -0.8f;
		}

		private void OnCollideV(CollisionData data)
		{
			if (!(data.Direction.Y > 0f))
			{
				return;
			}
			for (int i = -1; i <= 1; i += 2)
			{
				for (int j = 1; j <= 2; j++)
				{
					Vector2 vector = Position + Vector2.UnitX * j * i;
					if (!CollideCheck<Solid>(vector) && !OnGround(vector))
					{
						Position = vector;
						return;
					}
				}
			}
			hitSpeed.Y *= -0.2f;
		}

		private void GotoIdle()
		{
			if (state == States.Gone)
			{
				Position = startPosition;
				Audio.Play("event:/new_content/game/10_farewell/puffer_reform", Position);
			}
			lastSinePosition = (lastSpeedPosition = (anchorPosition = Position));
			hitSpeed = Vector2.Zero;
			idleSine.Reset();
			state = States.Idle;
		}

		private void GotoHit(Vector2 from)
		{
			scale = new Vector2(1.2f*scalar, 0.8f);
			hitSpeed = Vector2.UnitY * hitSpd;
			state = States.Hit;
			Audio.Play("event:/new_content/game/10_farewell/puffer_boop", Position);
		}


		private void GotoGone()
		{
			Vector2 control = Position + (startPosition - Position) * 0.5f;
			returnCurve = new SimpleCurve(Position, startPosition, control);
			Collidable = false;
			goneTimer = 2.5f;
			state = States.Gone;
		}


		public override void Update()
		{
			base.Update();
			eyeSpin = Calc.Approach(eyeSpin, 0f, Engine.DeltaTime * 1.5f);
			scale = Calc.Approach(scale, new Vector2(scalar, 1), 1f * Engine.DeltaTime);
			if (cannotHitTimer > 0f)
			{
				cannotHitTimer -= Engine.DeltaTime;
			}
			if (alertTimer > 0f)
			{
				alertTimer -= Engine.DeltaTime;
			}
			Player entity = base.Scene.Tracker.GetEntity<Player>();
			if (entity == null)
			{
				playerAliveFade = Calc.Approach(playerAliveFade, 0f, 1f * Engine.DeltaTime);
			}
			else
			{
				playerAliveFade = Calc.Approach(playerAliveFade, 1f, 1f * Engine.DeltaTime);
				lastPlayerPos = entity.Center;
			}
			switch (state)
			{
				case States.Idle:
					{
						if (Position != lastSinePosition)
						{
							anchorPosition += Position - lastSinePosition;
						}
						Vector2 vector = anchorPosition + new Vector2(idleSine.Value * 3f, idleSine.ValueOverTwo * 2f);
						MoveToX(vector.X);
						MoveToY(vector.Y);
						lastSinePosition = Position;
						break;
					}
				case States.Hit:
					lastSpeedPosition = Position;
					MoveH(hitSpeed.X * Engine.DeltaTime, onCollideH);
					MoveV(hitSpeed.Y * Engine.DeltaTime, OnCollideV);
					anchorPosition = Position;
					hitSpeed.X = Calc.Approach(hitSpeed.X, 0f, 150f * Engine.DeltaTime);
					hitSpeed = Calc.Approach(hitSpeed, Vector2.Zero, 320f * Engine.DeltaTime);
					if (base.Top >= (float)(SceneAs<Level>().Bounds.Bottom + 5))
					{
						GotoGone();
						break;
					}
					if (hitSpeed == Vector2.Zero)
					{
						ZeroRemainderX();
						ZeroRemainderY();
						GotoIdle();
					}
					break;
				case States.Gone:
					{
						float num = goneTimer;
						goneTimer -= Engine.DeltaTime;
						if (goneTimer <= 0.5f)
						{
							if (num > 0.5f && returnCurve.GetLengthParametric(8) > 8f)
							{
								Audio.Play("event:/new_content/game/10_farewell/puffer_return", Position);
							}
							Position = returnCurve.GetPoint(Ease.CubeInOut(Calc.ClampedMap(goneTimer, 0.5f, 0f)));
						}
						if (goneTimer <= 0f)
						{
							Visible = (Collidable = true);
							GotoIdle();
						}
						break;
					}
			}
		}





		private void OnPlayer(Player player)
		{
			if (state == States.Gone)
			{
				return;
			}
			if (player.Speed.Y >= 0)
			{
				player.Bounce(base.Top);
				GotoHit(player.Center);
				MoveToX(anchorPosition.X);
				idleSine.Reset();
				anchorPosition = (lastSinePosition = Position);
				eyeSpin = 1f;
			}
		}
	}
}
