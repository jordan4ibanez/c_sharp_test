using System.Numerics;
using FishGame.Audio;
using FishGame.Graphics;
using FishGame.Utility;
using Raylib_cs;

namespace FishGame.Level;

public static class Lure {

    static bool inWater = false;
    static bool reeling = false;

    static Vector3 position;
    static Vector3 rotation;
    // The actual rotation is stored in rotation.
    // The modified animation rotation is in rotation animated.
    static Vector3 rotationAnimated;

    // Lure reeling behavioral logic.
    static float swimAnimation = 0;
    static float reelSpeed = 0;
    static float lureFloatVelocity = 0;
    static float oldSwimAngle = 0;

    // If the lure hits something,I don't want to explode the player's ears.
    // So I set it to only be allowed to trigger the "thunk" noise every 0.25 seconds.
    static float hitThingSoundTimer = 0;
    readonly static float frequencySoundHitThings = 0.3f;

    static void LoadLureData() {
        ModelManager.LoadModelFromFile("models/lures/deep_c_110.glb");
        TextureManager.LoadTexture("models/lures/deep_c_110.png");
        ModelManager.SetModelTexture("deep_c_110.glb", "deep_c_110.png");
        ModelManager.SetModelShader("deep_c_110.glb", "normal");
    }

    static void Update() {
        if (!inWater) {
            return;
        }

        float delta = Delta.Get();

        if (hitThingSoundTimer < 1) {
            hitThingSoundTimer += delta;
        }

        float restingAngle = 0;
        float targetAngle = Raylib.DEG2RAD * 25;

        float reelTargetSpeed = 1;
        float reelAcceleration = 7;

        // Firstly, the lure needs to face the direction of the player's pole tip internally.
        Vector3 poleTipPosition = Player.GetPoleTipPosition();
        float x = poleTipPosition.X - position.X;
        float z = poleTipPosition.Z - position.Z;
        float lureYaw = (float)Math.Atan2(x, z);
        rotation.Y = lureYaw;

        //? This is the prototype logic for the deep-c 110 and deep-c 220 lures.
        if (reeling) {
            float newAngle = Raymath.Lerp(rotationAnimated.X, targetAngle, delta * reelAcceleration);
            if (float.IsNaN(newAngle)) {
                newAngle = targetAngle;
            }
            rotationAnimated.X = newAngle;

            reelSpeed += delta * reelAcceleration;

            if (reelSpeed >= reelTargetSpeed) {
                reelSpeed = reelTargetSpeed;
            }
        } else {
            float newAngle = Raymath.Lerp(rotationAnimated.X, restingAngle, delta * reelAcceleration);
            if (float.IsNaN(newAngle)) {
                newAngle = targetAngle;
            }
            rotationAnimated.X = newAngle;

            reelSpeed -= delta * reelAcceleration;

            if (reelSpeed <= 0) {
                reelSpeed = 0;
            }
        }

        // The steeper the lure gets the faster it swims.
        float swimSpeed = rotationAnimated.X / targetAngle;
        float swimSpeedMultiplier = 40;
        swimAnimation += delta * swimSpeedMultiplier * swimSpeed;
        if (swimAnimation >= Math.PI * 2.0f) {
            swimAnimation -= (float)Math.PI * 2.0f;
        }
        float swimAngle = (float)Math.Cos(swimAnimation) / 3.0f;
        rotationAnimated.Y = rotation.Y + swimAngle;

        if ((swimAngle > 0 && oldSwimAngle < 0) || (swimAngle < 0 && oldSwimAngle > 0)) {
            SoundManager.Play("diver_lure_rattle_" + Randy.NextInt(1, 4) + ".ogg", 0.3f, 0.9f);
        }

        oldSwimAngle = swimAngle;

        //? The lure uses a combination of animated rotation along with static rotation to not make the player motion sick.

        // The horizontal movement of the Deep-C 110 and Deep-C 220.
        Vector3 velocity = new();
        float lureInternalYaw = (float)(rotation.Y - (Math.PI / 2f));
        velocity.X = (float)Math.Cos(lureInternalYaw);
        velocity.Z = (float)Math.Sin(-lureInternalYaw);

        // Lure dives down when reeled.
        if (reeling) {
            float diveAmount = (rotationAnimated.X / targetAngle);
            velocity.Y -= diveAmount;
            lureFloatVelocity = 0;

        }

        float reelSpeedInTime = reelSpeed * delta;
        velocity = Raymath.Vector3Multiply(velocity, new Vector3(reelSpeedInTime, reelSpeedInTime, reelSpeedInTime));
        float lureMaxFloatVelocity = 0.5f;

        // Lure floats back up smoothly when not reeling.
        if (!reeling) {
            lureFloatVelocity += delta * 0.5f;

            if (lureFloatVelocity >= lureMaxFloatVelocity) {
                lureFloatVelocity = lureMaxFloatVelocity;
            }
            float diveAmount = (1 - (rotationAnimated.X / targetAngle)) * lureFloatVelocity;
            velocity.Y += diveAmount * delta;
        }

        // When you get within 5 units of the pole, you start to reel straight up towards the water.
        Vector2 lurePosition2d = new(position.X, position.Z);
        Vector2 poleTipPosition2d = new(poleTipPosition.X, poleTipPosition.Z);
        float distanceFromTip2d = Raymath.Vector2Distance(lurePosition2d, poleTipPosition2d);
        if (reeling && distanceFromTip2d < 5) {

            // This calculation is not even remotely accurate but it works.
            float pitch = (poleTipPosition.Y - position.Y) / (distanceFromTip2d * 2);
            pitch *= Raymath.Vector2Length(new Vector2(velocity.X, velocity.Z));
            velocity.Y = pitch;
        }

        position += velocity;

        // Do not let the lure fly (literally) out of the water.
        float waterHeighAtPosition = Water.GetCollisionPoint(position.X, position.Z);

        if (position.Y >= waterHeighAtPosition) {
            position.Y = waterHeighAtPosition;
            lureFloatVelocity = 0;
        }

        // Do not let the lure sink through the ground.
        float groundHeightAtPosition = Ground.GetCollisionPoint(position.X, position.Z);
        if (position.Y <= groundHeightAtPosition) {
            // + 0.1 to simulate a "bounce"
            position.Y = groundHeightAtPosition;

            if (hitThingSoundTimer > frequencySoundHitThings) {
                hitThingSoundTimer = 0;

                SoundManager.PlayPitched("lure_scrape_ground_" + Randy.NextInt(1, 4) + ".ogg", 0.5f);

            }
            lureFloatVelocity = 0;
        }

        if (distanceFromTip2d < 0.5) {
            Player.TriggerEmptyReelCompletion();
        }

        reeling = false;
    }

    static void Reel() {
        reeling = true;
    }

    static void Draw() {
        ModelManager.Draw("deep_c_110.glb", position, rotationAnimated);
    }

    static void SetPosition(Vector3 newPosition) {
        position = newPosition;
    }

    static void SetRotation(Vector3 newRotation) {
        rotation = newRotation;
        rotationAnimated = newRotation;
    }

    static Vector3 GetRotation() {
        return rotation;
    }

    static Vector3 GetPosition() {
        return position;
    }

    static void SetInWater() {
        inWater = true;
    }

    static bool IsInWater() {
        return inWater;
    }

    static void SetOutOfWater() {
        inWater = false;
    }
}