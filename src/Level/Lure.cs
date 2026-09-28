using System.Numerics;
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
    readonly static float frequencySoundHitThings = 0.3;

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
        float lureYaw = Math.Atan2(x, z);
        rotation.Y = lureYaw;

        //? This is the prototype logic for the deep-c 110 and deep-c 220 lures.
        if (reeling) {
            float newAngle = lerp(rotationAnimated.X, targetAngle, delta * reelAcceleration);
            if (newAngle == float.nan) {
                newAngle = targetAngle;
            }
            rotationAnimated.X = newAngle;

            reelSpeed += delta * reelAcceleration;

            if (reelSpeed >= reelTargetSpeed) {
                reelSpeed = reelTargetSpeed;
            }
        } else {
            float newAngle = lerp(rotationAnimated.X, restingAngle, delta * reelAcceleration);
            if (newAngle == float.nan) {
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
        if (swimAnimation >= PI * 2) {
            swimAnimation -= PI * 2;
        }
        float swimAngle = cos(swimAnimation) / 3.0;
        rotationAnimated.Y = rotation.Y + swimAngle;

        if ((swimAngle > 0 && oldSwimAngle < 0) || (swimAngle < 0 && oldSwimAngle > 0)) {
            SoundManager.play("diver_lure_rattle_" ~to!string(uniform(1, 4)) ~".ogg", 0.3, 0.9);
        }

        oldSwimAngle = swimAngle;

        //? The lure uses a combination of animated rotation along with static rotation to not make the player motion sick.

        // The horizontal movement of the Deep-C 110 and Deep-C 220.
        Vector3 velocity = Vector3();
        float lureInternalYaw = rotation.Y - (PI / 2);
        velocity.X = cos(lureInternalYaw);
        velocity.Z = sin(-lureInternalYaw);

        // Lure dives down when reeled.
        if (reeling) {
            float diveAmount = (rotationAnimated.X / targetAngle);
            velocity.Y -= diveAmount;
            lureFloatVelocity = 0;

        }

        float reelSpeedInTime = reelSpeed * delta;
        velocity = Vector3Multiply(velocity, Vector3(reelSpeedInTime, reelSpeedInTime, reelSpeedInTime));
        readonly float lureMaxFloatVelocity = 0.5;

        // Lure floats back up smoothly when not reeling.
        if (!reeling) {
            lureFloatVelocity += delta * 0.5;

            if (lureFloatVelocity >= lureMaxFloatVelocity) {
                lureFloatVelocity = lureMaxFloatVelocity;
            }
            float diveAmount = (1 - (rotationAnimated.X / targetAngle)) * lureFloatVelocity;
            velocity.Y += diveAmount * delta;
        }

        // When you get within 5 units of the pole, you start to reel straight up towards the water.
        Vector2 lurePosition2d = Vector2(position.X, position.Z);
        Vector2 poleTipPosition2d = Vector2(poleTipPosition.X, poleTipPosition.Z);
        float distanceFromTip2d = Vector2Distance(lurePosition2d, poleTipPosition2d);
        if (reeling && distanceFromTip2d < 5) {

            // This calculation is not even remotely accurate but it works.
            float pitch = (poleTipPosition.Y - position.Y) / (distanceFromTip2d * 2);
            pitch = pitch * Vector2Length(Vector2(velocity.X, velocity.Z));
            velocity.Y = pitch;
        }

        position += velocity;

        // Do not let the lure fly (literally) out of the water.
        float waterHeighAtPosition = Water.getCollisionPoint(position.X, position.Z);

        if (position.Y >= waterHeighAtPosition) {
            position.Y = waterHeighAtPosition;
            lureFloatVelocity = 0;
        }

        // Do not let the lure sink through the ground.
        float groundHeightAtPosition = Ground.getCollisionPoint(position.X, position.Z);
        if (position.Y <= groundHeightAtPosition) {
            // + 0.1 to simulate a "bounce"
            position.Y = groundHeightAtPosition;

            if (hitThingSoundTimer > frequencySoundHitThings) {
                hitThingSoundTimer = 0;

                SoundManager.playPitched("lure_scrape_ground_" ~to!string(uniform(1, 4)) ~".ogg", 0.5);

            }
            lureFloatVelocity = 0;
        }

        if (distanceFromTip2d < 0.5) {
            Player.triggerEmptyReelCompletion();
        }

        reeling = false;
    }

    static void reel() {
        reeling = true;
    }

    static void draw() {
        ModelManager.Draw("deep_c_110.glb", position, rotationAnimated);
    }

    static void setPosition(Vector3 newPosition) {
        position = newPosition;
    }

    static void setRotation(Vector3 newRotation) {
        rotation = newRotation;
        rotationAnimated = newRotation;
    }

    static Vector3 getRotation() {
        return rotation;
    }

    static Vector3 getPosition() {
        return position;
    }

    static void setInWater() {
        inWater = true;
    }

    static bool isInWater() {
        return inWater;
    }

    static void setOutOfWater() {
        inWater = false;
    }
}