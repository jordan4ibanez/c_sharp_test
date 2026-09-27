using Raylib_cs;
using System.Numerics;

namespace RaylibGame;

public abstract class Entity(Vector3 position) {
    public Vector3 Position = position;
    public Vector3 Velocity;
    public bool IsActive = true;

    public abstract void Update(float dt);
    public abstract void Draw();
}

