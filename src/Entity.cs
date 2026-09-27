using Raylib_cs;
using System.Numerics;

namespace RaylibGame;

public abstract class Entity
{
    public Vector2 Position;
    public Vector2 Velocity;
    public bool IsActive = true;

    protected Entity(Vector2 position)
    {
        Position = position;
    }

    public abstract void Update(float dt);
    public abstract void Draw();
}

public class Ball : Entity
{
    public float Radius;
    public Color Color;

    private readonly int _screenWidth;
    private readonly int _screenHeight;

    public Ball(Vector2 position, Vector2 velocity, float radius, Color color, int screenWidth, int screenHeight)
        : base(position)
    {
        Velocity = velocity;
        Radius = radius;
        Color = color;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
    }

    public override void Update(float dt)
    {
        Position += Velocity * dt;

        System.Console.WriteLine(this.Position);

        if (float.IsNaN(Position.X) || float.IsInfinity(Position.X))
        {
            Position.X = 0;
        }
        if (float.IsNaN(Position.Y) || float.IsInfinity(Position.Y))
        {
            Position.Y = 0;
        }



        // System.Console.WriteLine(Velocity);



        // Wall collisions
        if (Position.X + Radius >= _screenWidth || Position.X - Radius <= 0)
        {
            Velocity.X *= -1;
            // Position.X = _screenWidth / 2;
        }



        if (Position.Y + Radius >= _screenHeight || Position.Y - Radius <= 0)
        {
            Velocity.Y *= -1;
            // Position.Y = _screenHeight / 2;
        }
    }

    public override void Draw()
    {
        Raylib.DrawCircleV(Position, Radius, Color);
    }
}