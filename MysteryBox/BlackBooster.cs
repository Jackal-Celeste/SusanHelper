using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.SusanHelper
{
	[CustomEntity("SusanHelper/FollowBooster")]
	public class BlackBooster : Booster
	{
		private Sprite s;
		private DynData<Booster> dynData;
		private DynData<Player> playerData;
		private Vector2 dir = Vector2.Zero;
		private float retentionRate = 0.96f;
		private float speedBurst = 3f;
		private int rotation;


		public BlackBooster(Vector2 position, float speedBurst, float retentionRate, int rotation)
			: base(position, false)
		{
			this.rotation = (rotation % 360) / 90;
			this.speedBurst = Math.Abs(speedBurst);
			this.retentionRate = Math.Max(0f, Math.Min(10f, retentionRate))/10;
			dynData = new DynData<Booster>(this as Booster);
		}

		public BlackBooster(EntityData data, Vector2 offset)
			: this(data.Position + offset, data.Float("speedBurst", defaultValue: 3f), data.Float("retentionRate", defaultValue: 9.6f), data.Int("rotation", defaultValue: 0))
		{
		}


        public override void Update()
        {
				if (SusanModule.TryGetPlayer(out Player p))
				{
					playerData = new DynData<Player>(p);
					if (p.StateMachine.State != 4)
					{ 
						Position += dir;
						dynData.Get<Entity>("outline").Position = Position;
						dir *= retentionRate;
					if (dir.Length() < 0.1f) dir = Vector2.Zero;
					}
                    else if(p.CurrentBooster is BlackBooster)
                    {
					Vector2 vector = Input.Aim.Value != Vector2.Zero ? Input.Aim.Value : Input.Aim.PreviousValue != Vector2.Zero ? Input.Aim.PreviousValue : Input.LastAim != Vector2.Zero ? Input.LastAim : new Vector2(p.Facing == Facings.Left ? -1f : 1f, 0f) * 3f;
					if (p.StateMachine.State == 2) vector = p.DashDir;
					//dir = vector2;
						dir = vector;
						dir.Normalize();
						dir *= speedBurst;
                    switch (rotation)
                    {
						case 0:
							break;
						case 1:
							dir = new Vector2(dir.Y, -dir.X);
							break;
						case 2:
							dir = -dir;
							break;
						case 3:
							dir = new Vector2(dir.Y, dir.X);
							break;
						default:
							break;
                    }
                    }
				}
			base.Update();
		}

    }
}
