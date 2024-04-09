using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Celeste;
using Monocle;
using System;

public interface ILight { }

public class LightSource : Entity, ILight
{
    protected readonly VertexLight Vertex;

    protected readonly BloomPoint Bloom;

    public LightSource(Vector2 position, Color color, float alpha, float radius, int startFade, int endFade)
        : base(position)
    {
        Add(Vertex = new VertexLight(color, alpha, startFade, endFade));
        Add(Bloom = new BloomPoint(alpha, radius));
    }

    public LightSource(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.HexColor("color", Color.White), data.Float("alpha", 1f), data.Float("radius", 48f), data.Int("startFade", 24), data.Int("endFade", 48))
    {
    }
}



[CustomEntity(new string[] { "SusanHelper/InstantFlickerLightSource" })]
public class InstantFlicker : LightSource
{
    private Color color;

    private readonly float alphaA, alphaB, radius, startFade, endFade, alphaSpeed;

    private readonly bool inverted;

    private float alpha, time;

    private Random random = new Random();

    public InstantFlicker(EntityData data, Vector2 offset) : base(data, offset)
    {
        color = data.HexColor("color", defaultValue: Calc.HexToColor("faf9f6"));
        alphaA = data.Float("alphaA", defaultValue: 0.93f);
        alphaB = data.Float("alphaA", defaultValue: 0.95f);
        radius = data.Float("radius", defaultValue: 80f);
        startFade = data.Int("startFade", defaultValue: 48);
        endFade = data.Int("endFade", defaultValue: 80);
        inverted = data.Bool("inverted", defaultValue: false);
        alphaSpeed = data.Float("alphaSpeed", defaultValue: 0.2f);

        float alpha = Calc.LerpClamp(inverted ? alphaB : alphaA, inverted ? alphaA : alphaB, Ease.CubeInOut(Calc.YoYo(0f / 2f * alphaSpeed % 1f)));
        Vertex.Alpha = alpha;
        Bloom.Alpha = alpha;


    }

    public override void Update()
    {
        base.Update();
        time += 1f;
        float alpha = Calc.LerpClamp(inverted ? alphaB : alphaA, inverted ? alphaA : alphaB, Ease.CubeInOut(Calc.YoYo(base.Scene.TimeActive / 2f * alphaSpeed % 1f)));
        Vertex.Alpha = alpha;
        Bloom.Alpha = alpha;
        //Bloom.Radius = radius;
        //Vertex.EndRadius = endFade;
        //Vertex.StartRadius = startFade;
    }


    public float GetFlickeringAlpha(float alphaA, float alphaB, float elapsedTime)
    {

        // Determine the base frequency of the flicker, adding a little randomness
        double frequency = 5 + random.NextDouble(); // Base frequency in Hz, with some randomness

        // Calculate the sine wave for smooth periodic changes
        double sineWave = Math.Sin(elapsedTime * frequency);

        // Add randomness to the amplitude of the sine wave to vary the intensity of the flicker
        double amplitude = 0.5 + 0.5 * random.NextDouble(); // Ensure amplitude is between 0.5 and 1 for noticeable but not excessive flickering

        // Use the sine wave and amplitude to interpolate between alphaA and alphaB
        float alpha = alphaA + (alphaB - alphaA) * ((float)sineWave * (float)amplitude);
        alpha = Math.Max(Math.Min(alpha, Math.Max(alphaA, alphaB)), Math.Min(alphaA, alphaB));


        // Clamp the alpha value to ensure it stays within the desired boundaries
        return alpha;
    }
}

