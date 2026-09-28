using System.Numerics;
using FishGame.Utility;
using Raylib_cs;

namespace FishGame.Level;


public enum FishState {
    Idle,
    Looking,
    RandomTarget,
    Following,
    Fight
}



public abstract class Fish {
    // Vector3 oldPosition = Vector3(0, 0, 0);
    Vector3 position = new(0, 0, 0);
    // Pitch and yaw.
    Vector3 rotation = new(0, 0, 0);

    float scale = 1;
    string uuid;
    float collisionVertical = 0.2f;

    // Behavioral variables.
    FishState oldState = FishState.RandomTarget;
    FishState state = FishState.RandomTarget;
    Vector3 lookTarget;
    // double turnLerpProgress = 0;
    float behaviorTimer = 0;
    // double lookAroundTimer = 0;
    bool retrigger = false;
    float movementSpeed = 0;
    bool recalculateTimer = true;

    // These ones can be adjusted based on how aggressive the fish acts.
    protected float relaxedLookSpeed = 1;
    protected float attackLookSpeed = 0.03f;

    protected float maxSpeedRelaxed = 2;

    protected float accelerationRelaxed = 1;

    protected byte tightTurn = 0;

    protected string __model = "undefined";


    public Fish() {
        uuid = Guid.NewGuid().ToString();
        behaviorTimer = Randy.NextFloat(1.5f, 4.0f);

        // todo: uhhh fix this mess
        // Vector2 mapSize = Ground.getSizeFloating();
        // position.X = mapSize.X / 2.0;
        // position.Z = mapSize.Y / 2.0;
    }

    FishState RandomState() {
        ReadOnlySpan<FishState> states = [FishState.Idle, FishState.Looking, FishState.RandomTarget];
        return states[Randy.NextInt(0, 3)];
    }

    void ResetStateData() {
        behaviorTimer = 0;
        retrigger = false;
        recalculateTimer = true;
        tightTurn = 0;
    }

    string GetUUID() {
        return uuid;
    }

    Vector3 GetPosition() {
        return position;
    }

    Vector3 GetRotation() {
        return rotation;
    }

    // todo: fix this.
    // void BoundsCheck() {
    //     Vector2 mapSize = Ground.getSizeFloating();

    //     if (position.X < 1) {
    //         position.X = 1;
    //     } else if (position.X > mapSize.X - 1) {
    //         position.X = mapSize.X - 1;
    //     }

    //     if (position.Z < 1) {
    //         position.Z = 1;
    //     } else if (position.Z > mapSize.Y - 1) {
    //         position.Z = mapSize.Y - 1;
    //     }
    // }

    void MoveToTarget(float delta) {

        // writeln(delta, " ", movementSpeed, " ", rotation.Y);

        float xVelocity = (float)(Math.Sin(rotation.Y) * delta * movementSpeed);
        float zVelocity = (float)(Math.Cos(rotation.Y) * delta * movementSpeed);

        Vector3 oldPosition = position;

        position = position + new Vector3(xVelocity, 0, zVelocity);

        // Todo: find where this came from.
        // BoundsCheck();

        // todo: fix the rest of this when the ground is created.
        // float minY = Ground.getCollisionPoint(position.X, position.Z) + collisionVertical;
        // float maxY = Water.getCollisionPoint(position.X, position.Z) - collisionVertical;

        // // If the fish is trying to go on land, let the lerp of rotation continue, but stop from moving.
        // if (maxY - minY < (collisionVertical * 2)) {
        //     // Simulate the jank of PS1 physics by pushing it out inverse with a fixed amount.
        //     Vector2 inverseDir = Vector2Normalize(Vector2Subtract(Vector2(
        //             oldPosition.X, oldPosition.Z), Vector2(position.X, position.Z)));

        //     inverseDir = Vector2Multiply(inverseDir, Vector2(0.1, 0.1));

        //     Vector2 ploppedOutPosition = Vector2Add(Vector2(oldPosition.X, oldPosition.Z), inverseDir);

        //     position = Vector3(ploppedOutPosition.X, oldPosition.Y, ploppedOutPosition.Y);

        //     BoundsCheck();
        //     return;
        // }

        // float yVelocity = (sin(-rotation.X) * delta) * movementSpeed;

        // position.Y += yVelocity;

        // if (position.Y < minY) {
        //     position.Y = minY;
        // } else if (position.Y > maxY) {
        //     position.Y = maxY;
        // }

    }

    void turnToTarget(float delta) {
        // Calculating yaw.
        Vector2 goalDir = Raymath.Vector2Normalize(Raymath.Vector2Subtract(new Vector2(lookTarget.X, lookTarget.Z), new Vector2(position.X, position.Z)));

        float targetYaw = (float)Math.Atan2(goalDir.X, goalDir.Y);
        float currentYaw = rotation.Y;
        float diff = targetYaw - currentYaw;

        if (diff > Math.PI) {
            targetYaw -= (float)(Math.PI * 2);
        } else if (diff < -Math.PI) {
            targetYaw += (float)(Math.PI * 2);
        }

        float lookSpeed = relaxedLookSpeed;
        if (tightTurn == 1) {
            lookSpeed *= 3;
        }
        float oldTargetYaw = targetYaw;
        targetYaw = Raymath.Lerp(currentYaw, targetYaw, (float)(delta * lookSpeed));

        // TightTurn 2 basically just looks straight at it.
        if (tightTurn == 2) {
            targetYaw = oldTargetYaw;
        }

        // Raymath can cause Lerp to go into negative or positive infinity.
        // NaN check is because I want to make sure it doesn't crash.
        if (Math.Abs(targetYaw) == float.PositiveInfinity || float.IsNaN(Math.Abs(targetYaw))) {
            // writeln("Caught nan yaw.");
            targetYaw = currentYaw;
        }

        // Calculating pitch.
        float distance = Raymath.Vector2Distance(new Vector2(position.X, position.Z), new Vector2(lookTarget.X, lookTarget
                .Z));
        Vector2 pitchNormalized = Raymath.Vector2Normalize(Raymath.Vector2Subtract(new Vector2(distance, lookTarget.Y), new Vector2(0, position
                .Y)));
        float targetPitch = (float)Math.Asin(-pitchNormalized.Y);
        float currentPitch = rotation.X;

        targetPitch = Raymath.Lerp(currentPitch, targetPitch, (float)(delta * lookSpeed));
        // Raymath can cause Lerp to go into negative or positive infinity.
        // NaN check is because I want to make sure it doesn't crash.
        if (Math.Abs(targetPitch) == float.PositiveInfinity || float.IsNaN(Math.Abs(targetPitch))) {
            // writeln("Caught nan pitch.");
            targetPitch = currentPitch;
        }

        rotation.X = targetPitch;
        rotation.Y = targetYaw;
    }

    void selectRandomTargetPosition() {
        // todo: fix this when the ground is added.
        // Vector2 map2dRange = Ground.getSizeFloating();

        // // Limit the range.
        // map2dRange.X -= 1;
        // map2dRange.Y -= 1;

        // float selectedX;
        // float selectedZ;
        // float minY;
        // float maxY;

        // // Reroll until the fish can fit in the spot.
        // while (true) {
        //     selectedX = giveRandomFloat(1.0, map2dRange.X);
        //     selectedZ = giveRandomFloat(1.0, map2dRange.Y);
        //     minY = Ground.getCollisionPoint(selectedX, selectedZ) + collisionVertical;
        //     maxY = Water.getCollisionPoint(selectedX, selectedZ) - collisionVertical;
        //     if (minY <= maxY) {
        //         break;
        //     }
        // }

        // //? Useful for debugging.
        // // selectedX = giveRandomFloat(position.X - 3, position.X + 3);
        // // selectedZ = giveRandomFloat(position.X - 3, position.X + 3);
        // // minY = Ground.getCollisionPoint(selectedX, selectedZ) + collisionVertical;
        // // maxY = Water.getCollisionPoint(selectedX, selectedZ) - collisionVertical;

        // float selectedY = giveRandomFloat(minY, maxY);

        // lookTarget = Vector3(selectedX, selectedY, selectedZ);

        // // turnLerpProgress = 0;

    }

    public string GetModel() {
        return __model;
    }

    void update(float delta) {

        // todo: implement this when the Lure is added.

        // // if (state != oldState) {
        // // writeln("in state: ", state);
        // // }

        // turnToTarget(delta);
        // MoveToTarget(delta);

        // oldState = state;

        // // This is just a prototype game after all. The fish doesn't even think, it just goes to the lure.

        // if (Lure.isInWater()) {
        //     lookTarget = Lure.getPosition();
        //     state = FishState.Following;
        // } else if (state == FishState.Following) {
        //     if (giveRandomDouble(0.0, 1.0) > 0.5) {
        //         state = FishState.Idle;
        //     } else {
        //         state = FishState.RandomTarget;
        //     }
        // }

        // switch (state) {
        //     case FishState.Idle: {
        //             idle(delta);
        //             break;
        //         }
        //     case FishState.Looking: {
        //             looking(delta);
        //             break;
        //         }
        //     case FishState.RandomTarget: {
        //             randomTarget(delta);
        //             break;
        //         }
        //     case FishState.Following: {
        //             following(delta);
        //             break;
        //         }
        //     case FishState.Fight: {
        //             fight(delta);
        //             break;
        //         }
        //     default: {
        //             throw new Error("I don't know how this got to here.");
        //         }
        // }
    }

    void idle(float delta) {
        // todo: idle animation.

        if (recalculateTimer) {
            recalculateTimer = false;
            behaviorTimer = Randy.NextFloat(5.0f, 15.0f);
        }

        if (movementSpeed > 0) {
            movementSpeed -= (float)delta * accelerationRelaxed;
            if (movementSpeed <= 0) {
                movementSpeed = 0;
            }
        }

        behaviorTimer -= delta;

        if (behaviorTimer <= 0.0) {
            state = RandomState();
            ResetStateData();
        }
    }

    void looking(float delta) {
        // todo: tail turning animation.

        if (behaviorTimer <= 0) {
            if (retrigger) {
                // The fish can keep looking around.
                if (Randy.NextFloat(0.0f, 1.0f) > 0.5) {
                    ResetStateData();
                    state = RandomState();
                }
            } else {
                // If the fish was idling, let it enjoy looking around.
                if (oldState == FishState.Idle) {
                    behaviorTimer = Randy.NextFloat(6, 17);
                } else {
                    behaviorTimer = Randy.NextFloat(5, 12);
                }
                selectRandomTargetPosition();
                retrigger = true;
            }
        }

        if (movementSpeed > 0) {
            movementSpeed -= delta * accelerationRelaxed;
            if (movementSpeed <= 0) {
                movementSpeed = 0;
            }
        }

        behaviorTimer -= delta;
    }

    void randomTarget(float delta) {

        // todo: Use swimming animation.

        behaviorTimer -= delta;

        if (recalculateTimer) {
            tightTurn = 0;
            recalculateTimer = false;
            behaviorTimer = Randy.NextFloat(8.0f, 15.0f);
            selectRandomTargetPosition();
        }

        if (movementSpeed < maxSpeedRelaxed) {
            movementSpeed += delta * accelerationRelaxed;
        }

        float distance = Raymath.Vector3Distance(position, lookTarget);

        if (distance <= 1.5) {
            selectRandomTargetPosition();
            ResetStateData();
        } else if (distance < 3.0) {
            tightTurn = 1;
        }

        if (behaviorTimer <= 0.0) {
            state = RandomState();
            selectRandomTargetPosition();
            ResetStateData();
        }
    }

    void following(float delta) {

        // todo: implement this when the lure is implemented.

        // tightTurn = 2;

        // var distance = Raymath.Vector3Distance(position, Lure.getPosition());

        // if (distance < 0.5) {
        //     movementSpeed -= delta * 30;
        //     if (movementSpeed < 0) {
        //         movementSpeed = 0;
        //     }
        //     // writeln("stage 3");
        // } else if (distance < 1) {
        //     if (movementSpeed > 2) {
        //         movementSpeed -= delta * 10;
        //     } else if (movementSpeed > 0) {
        //         movementSpeed -= delta * 5;

        //     }
        //     if (movementSpeed < 0) {
        //         movementSpeed = 0;
        //     }
        //     // writeln("stage 2");

        // } else if (distance < 3) {
        //     if (movementSpeed > 2) {
        //         movementSpeed -= delta * 10;
        //     } else if (movementSpeed <= 1) {
        //         movementSpeed += delta * 2;
        //     }
        //     if (movementSpeed < 0) {
        //         movementSpeed = 0;
        //     }
        //     // writeln("stage 1");

        // } else {
        //     if (movementSpeed < 4) {
        //         movementSpeed += delta * 2;
        //     }
        // }
    }

    void fight(float delta) {
        // todo: something something here
    }
}

class LargeMouthBass : Fish {
    public LargeMouthBass() {
        __model = "largemouth.glb";
        accelerationRelaxed = 10;
        maxSpeedRelaxed = 5;
    }
}