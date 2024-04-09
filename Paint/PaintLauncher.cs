using Celeste;
using Celeste.Mod.Entities;
using Celeste.Mod.SusanHelper;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using static SusanHelper.Entities.Paint.PaintSource;
using static SusanHelper.Entities.Paint.PaintBall;
using SusanHelper.Entities.Paint;

namespace SusanHelper.Paint
{
	[CustomEntity("SusanHelper/PaintLauncher")]
	public class PaintLauncher : Entity
	{
		private Vector2 launch;
		private float frequency1, frequency2, timer;
		private string flag;
		private bool mainFrequency = true;
        private enum LaunchType
        {
            Pulse, Constant, Double
        }
		private LaunchType launchType;
		private PaintBall ball;

		private LaunchType cast(string s)
		{
			s = s.ToLower();
			return s.Contains("pulse") ? LaunchType.Pulse : s.Contains("constant") ? LaunchType.Constant : LaunchType.Double;
		}

        public PaintLauncher(EntityData data, Vector2 offset) : base(data.Position + offset)
		{
			launchType = cast(data.Attr("launchType", defaultValue: "Pulse"));
			launch = new Vector2(data.Float("xVelocity", defaultValue: 0f), data.Float("yVelocity", defaultValue: 0f));
			flag = data.Attr("flag", defaultValue: "");
            frequency1 = data.Float("mainFrequency", defaultValue: 1f);
            switch (launchType)
			{
				case LaunchType.Pulse:
					frequency2 = float.MaxValue;
					break;
				case LaunchType.Constant:
					frequency2 = frequency1;
					break;
				case LaunchType.Double:
					frequency2 = data.Float("altFrequency", defaultValue: 0.1f);
					break;
			}
			timer = 0f;
		}

        public override void Update()
        {
            base.Update();
			Level l = this.Scene as Level;
			if (l != null)
			{
				if (!(launchType == LaunchType.Constant && ball != null && !ball.shattered))
				{
					if (l.Session.GetFlag(flag))
					{
						timer += Engine.DeltaTime;
						float f = mainFrequency ? frequency1 : frequency2;
						if (timer > f)
						{
							LaunchPaintBall(l);
							if (launchType == LaunchType.Pulse) frequency1 = float.MaxValue;
							mainFrequency = !mainFrequency;
							timer = 0f;
						}
					}
					else if (!l.Session.GetFlag(flag))
					{
						timer = 0f;
					}
				}
			}
        }

		private void LaunchPaintBall(Level lvl)
		{
			ball = new PaintBall(Position, launch, true, true, true);
			lvl.Add(ball);
		}

    }
}

