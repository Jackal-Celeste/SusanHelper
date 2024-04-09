// SusanHelper.Entities.CurrentDreamBlock
using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

[CustomEntity(new string[] { "SusanHelper/CurrentDreamBlock" })]
[Tracked(false)]
[TrackedAs(typeof(DreamBlock))]
public class CurrentDreamBlock : DreamBlock
{
    private struct DreamParticle
    {
        public Vector2 Position;

        public int Layer;

        public Color Color;

        public float TimeOffset;
    }

    public enum CurrentDirections
    {
        None, Up, UpLeft, UpRight, Left, Right, DownLeft, DownRight, Down
    }

    private DreamParticle[] particles;

    private MTexture[] particleTextures;

    public Vector2 Velocity, particleVelocity = Vector2.Zero;

    public bool twoDashes, noJump;

    public float particleSpeed;

    private CurrentDirections direction;

    private CurrentDirections dumbCast(string str)
    {
        CurrentDirections dir = CurrentDirections.None;
        switch (str){
            case "Up":
                dir = CurrentDirections.Up;
                Velocity = -Vector2.UnitY;
                particleTextures = new MTexture[3]
                {
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_up_particles"].GetSubtexture(13, 0, 9, 10),
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_up_particles"].GetSubtexture(7, 0, 5, 3),
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_up_particles"].GetSubtexture(2, 0, 3, 2)
                };
                break;
            case "UpLeft":
                dir = CurrentDirections.UpLeft;
                Velocity = (-Vector2.UnitX - Vector2.UnitY);
                Velocity.Normalize();
                particleTextures = new MTexture[3]
                {
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_upleft_particles"].GetSubtexture(16, 0, 8, 8),
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_upleft_particles"].GetSubtexture(10, 2, 4, 4),
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_upleft_particles"].GetSubtexture(2, 2, 2, 2)
                };
                break;
            case "UpRight":
                dir = CurrentDirections.UpRight;
                Velocity = (Vector2.UnitX - Vector2.UnitY);
                Velocity.Normalize();
                particleTextures = new MTexture[3]
                {
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_upright_particles"].GetSubtexture(16, 0, 8, 8),
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_upright_particles"].GetSubtexture(10, 2, 4, 4),
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_upright_particles"].GetSubtexture(2, 2, 2, 2)
                };
                break;
            case "Left":
                dir = CurrentDirections.Left;
                Velocity = -Vector2.UnitX;
                particleTextures = new MTexture[3]
                {
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_left_particles"].GetSubtexture(13, 0, 10, 9),
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_left_particles"].GetSubtexture(9, 0, 3, 5),
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_left_particles"].GetSubtexture(3, 0, 2, 3)
                };
                break;
            case "Right":
                dir = CurrentDirections.Right;
                Velocity = Vector2.UnitX;
                particleTextures = new MTexture[3]
                {
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_right_particles"].GetSubtexture(13, 0, 10, 9),
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_right_particles"].GetSubtexture(9, 0, 3, 5),
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_right_particles"].GetSubtexture(3, 0, 2, 3)
                };
                break;
            case "Down":
                dir = CurrentDirections.Down;
                Velocity = Vector2.UnitY;
                particleTextures = new MTexture[3]
                {
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_down_particles"].GetSubtexture(13, 0, 9, 10),
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_down_particles"].GetSubtexture(7, 0, 5, 3),
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_down_particles"].GetSubtexture(2, 0, 3, 2)
                };
                break;
            case "DownLeft":
                dir = CurrentDirections.DownLeft;
                Velocity = (Vector2.UnitY - Vector2.UnitX);
                Velocity.Normalize();
                particleTextures = new MTexture[3]
                {
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_downleft_particles"].GetSubtexture(16, 0, 8, 8),
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_downleft_particles"].GetSubtexture(10, 2, 4, 4),
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_downleft_particles"].GetSubtexture(2, 2, 2, 2)
                };
                break;
            case "DownRight":
                dir = CurrentDirections.DownRight;
                Velocity = (Vector2.UnitX + Vector2.UnitY);
                Velocity.Normalize();
                particleTextures = new MTexture[3]
                {
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_downright_particles"].GetSubtexture(16, 0, 8, 8),
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_downright_particles"].GetSubtexture(10, 2, 4, 4),
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_downright_particles"].GetSubtexture(2, 2, 2, 2)
                };
                break;
            default:
                dir = CurrentDirections.None;
                particleTextures = new MTexture[3]
                {
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_none_particles"].GetSubtexture(13, 0, 7, 7),
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_none_particles"].GetSubtexture(7, 0, 5, 5),
                GFX.Game["objects/currentParticles/Obligato/dreamblock/jkl_none_particles"].GetSubtexture(2, 0, 3, 3)
                };
                break;
        }
        return dir;
    }

    private DynData<DreamBlock> val;

    public CurrentDreamBlock(Vector2 position, float width, float height, Vector2? node, bool fastMoving, bool oneUse, bool below, string direction, int Depth, float particleSpeed, bool twoDashes, bool noJump)
        : base(position, width, height, node, fastMoving, oneUse, below)
    {
        base.Depth = Depth;
        this.particleSpeed = particleSpeed;
        this.twoDashes = false;
        this.noJump = noJump;
        this.direction = dumbCast(direction);
        this.particleSpeed = particleSpeed;
        this.noJump = noJump;
        base.Depth = -99;
    }

    public CurrentDreamBlock(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Width, data.Height, null, data.Bool("fastMoving",defaultValue: false), data.Bool("oneUse"), data.Bool("below"), data.Attr("direction", defaultValue: "None"),data.Int("Depth"), data.Float("particleSpeed"), data.Bool("twoDashes"), data.Bool("noJump"))
    { 
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        val = new DynData<DreamBlock>(this);
    }
    public void WindSetup()
    {
        particles = new DreamParticle[(int)(base.Width / 8f * (base.Height / 8f) * 0.4f)];
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Position = new Vector2(Calc.Random.NextFloat(base.Width), Calc.Random.NextFloat(base.Height));


            particles[i].Layer = Calc.Random.Choose(0,1, 2);
            particles[i].TimeOffset = Calc.Random.NextFloat(2f);
            particles[i].Color = Color.LightGray * (0.5f + (float)particles[i].Layer / 2f * 0.5f);
            switch (particles[i].Layer)
                {
                    case 0:
                        particles[i].Color = Calc.Random.Choose<Color>(Calc.HexToColor("F9A1A1"), Calc.HexToColor("9CB2D8"), Calc.HexToColor("AEDAB4"), Calc.HexToColor("FFF3C1"));
                        break;
                    case 1:
                        Color hue = Calc.Random.Choose<Color>(Calc.HexToColor("F9A1A1"), Calc.HexToColor("9CB2D8"), Calc.HexToColor("AEDAB4"), Calc.HexToColor("FFF3C1"));

                        particles[i].Color = Color.Lerp(Color.Black, hue, 0.66f);
                        break;
                    case 2:
                        Color hue2 = Calc.Random.Choose<Color>(Calc.HexToColor("FBC4C4"), Calc.HexToColor("AEC6E2"), Calc.HexToColor("C0E4CA"), Calc.HexToColor("FFF8D4"));

                        particles[i].Color = Color.Lerp(Color.Black, hue2, 0.33f);
                        break;
                }
            
        }
    }


    public override void Update()
    {
        base.Update();
        particleVelocity += Velocity * particleSpeed * Engine.DeltaTime;
    }

    public void WindRender()
    {
        Color color = Color.Black;
        Color color2 = Color.White;
        float num3 = val.Get<float>("whiteFill");
        Draw.Rect(base.X, base.Y, base.Width, base.Height, color);
        Vector2 position = SceneAs<Level>().Camera.Position;
        for (int i = 0; i < particles.Length; i++)
        {
            int layer = particles[i].Layer;
            Vector2 position2 = particles[i].Position;
            position2 += position * (0.3f + 0.25f * (float)layer) + particleVelocity * (0.3f + 0.25f * (1f - (float)layer));
            position2 = PutInside(position2);
            Color color3 = particles[i].Color;
            MTexture mTexture = particleTextures[layer];
            if (position2.X >= base.X + 2f && position2.Y >= base.Y + 2f && position2.X < base.Right - 2f && position2.Y < base.Bottom - 2f)
            {
                mTexture.DrawCentered(position2, color3);
            }
        }
        WobbleLine(color2,  color, num3, new Vector2(base.X, base.Y), new Vector2(base.X + base.Width, base.Y), 0f);
        WobbleLine(color2, color, num3, new Vector2(base.X + base.Width, base.Y), new Vector2(base.X + base.Width, base.Y + base.Height),0.7f);
        WobbleLine(color2, color, num3, new Vector2(base.X + base.Width, base.Y + base.Height),new Vector2(base.X, base.Y + base.Height),1.5f);
        WobbleLine(color2, color, num3, new Vector2(base.X, base.Y + base.Height),new Vector2(base.X, base.Y),2.5f);
        Draw.Rect(new Vector2(base.X, base.Y), 2f, 2f, color2);
        Draw.Rect(new Vector2(base.X + base.Width - 2f, base.Y), 2f, 2f, color2);
        Draw.Rect(new Vector2(base.X, base.Y + base.Height - 2f), 2f, 2f, color2);
        Draw.Rect(new Vector2(base.X + base.Width - 2f, base.Y + base.Height - 2f), 2f, 2f, color2);
    }


    private Vector2 PutInside(Vector2 pos)
    {
        if (pos.X > base.Right)
        {
            pos.X -= (float)Math.Ceiling((pos.X - base.Right) / base.Width) * base.Width;
        }
        else if (pos.X < base.Left)
        {
            pos.X += (float)Math.Ceiling((base.Left - pos.X) / base.Width) * base.Width;
        }
        if (pos.Y > base.Bottom)
        {
            pos.Y -= (float)Math.Ceiling((pos.Y - base.Bottom) / base.Height) * base.Height;
        }
        else if (pos.Y < base.Top)
        {
            pos.Y += (float)Math.Ceiling((base.Top - pos.Y) / base.Height) * base.Height;
        }
        return pos;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void WobbleLine(Color activeLine, Color activeBack, float whiteFill, Vector2 from, Vector2 to, float offset)
    {

        float num = (to - from).Length();
        Vector2 vector = Vector2.Normalize(to - from);
        Vector2 vector2 = new Vector2(vector.Y, 0f - vector.X);
        Color color = activeLine;
        Color color2 = activeBack;
        if (whiteFill > 0f)
        {
            color = Color.Lerp(color, Color.White, whiteFill);
            color2 = Color.Lerp(color2, Color.White, whiteFill);
        }
        float num2 = 0f;
        int num3 = 16;
        for (int i = 2; (float)i < num - 2f; i += num3)
        {
            float num4 = 2f; //hey buddy make this not zero
            if ((float)(i + num3) >= num)
            {
                num4 = 0f;
            }
            float num5 = Math.Min(num3, num - 2f - (float)i);
            Vector2 vector3 = from + vector * i + vector2 * num2;
            Vector2 vector4 = from + vector * ((float)i + num5) + vector2 * num4;
            Draw.Line(vector3 - vector2, vector4 - vector2, color2);
            Draw.Line(vector3 - vector2 * 2f, vector4 - vector2 * 2f, color2);
            Draw.Line(vector3, vector4, color);
            num2 = num4;
        }
    }
}
