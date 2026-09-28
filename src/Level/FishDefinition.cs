namespace FishGame.Level;


enum FishState {
    Idle,
    Looking,
    RandomTarget,
    Following,
    Fight
}

FishState randomState() {
    return [FishState.Idle, FishState.Looking, FishState.RandomTarget][giveRandomInt(0, 3)];
}

abstract class Fish {
    private:
    // Vector3 oldPosition = Vector3(0, 0, 0);
    Vector3 position = Vector3(0, 0, 0);
    // Pitch and yaw.
    Vector3 rotation = Vector3(0, 0, 0);

    float scale = 1;
    ulong uuid = 0;
    float collisionVertical = 0.2;

    // Behavioral variables.
    FishState oldState = FishState.RandomTarget;
    FishState state = FishState.RandomTarget;
    Vector3 lookTarget;
    // double turnLerpProgress = 0;
    double behaviorTimer = 0;
    // double lookAroundTimer = 0;
    bool retrigger = false;
    double movementSpeed = 0;
    bool recalculateTimer = true;

    // These ones can be adjusted based on how aggressive the fish acts.
    double relaxedLookSpeed = 1;
    double attackLookSpeed = 0.03;

    double maxSpeedRelaxed = 2;

    double accelerationRelaxed = 1;

    byte tightTurn = 0;

    string __model = "undefined";
    public:

    this() {
        uuid = UUID.next();

        behaviorTimer = giveRandomDouble(1.5, 4.0);

    Vector2 mapSize = Ground.getSizeFloating();
    position.x = mapSize.x / 2.0;
        position.z = mapSize.y / 2.0;
    }

    void resetStateData() {
        behaviorTimer = 0;
        retrigger = false;
        recalculateTimer = true;
        tightTurn = 0;
    }

    ulong getUUID() {
        return this.uuid;
    }

    Vector3 getPosition() {
        return this.position;
    }

    Vector3 getRotation() {
        return this.rotation;
    }

    void boundsCheck() {
        Vector2 mapSize = Ground.getSizeFloating();

        if (position.x < 1) {
            position.x = 1;
        } else if (position.x > mapSize.x - 1) {
            position.x = mapSize.x - 1;
        }

        if (position.z < 1) {
            position.z = 1;
        } else if (position.z > mapSize.y - 1) {
            position.z = mapSize.y - 1;
        }
    }

    void moveToTarget(double delta) {

        // writeln(delta, " ", movementSpeed, " ", rotation.y);

        float xVelocity = (sin(rotation.y) * delta) * movementSpeed;
        float zVelocity = (cos(rotation.y) * delta) * movementSpeed;

        Vector3 oldPosition = position;

        position = Vector3Add(position, Vector3(xVelocity, 0, zVelocity));

        boundsCheck();

        float minY = Ground.getCollisionPoint(position.x, position.z) + collisionVertical;
        float maxY = Water.getCollisionPoint(position.x, position.z) - collisionVertical;

        // If the fish is trying to go on land, let the lerp of rotation continue, but stop from moving.
        if (maxY - minY < (collisionVertical * 2)) {
            // Simulate the jank of PS1 physics by pushing it out inverse with a fixed amount.
            Vector2 inverseDir = Vector2Normalize(Vector2Subtract(Vector2(
                    oldPosition.x, oldPosition.z), Vector2(position.x, position.z)));

            inverseDir = Vector2Multiply(inverseDir, Vector2(0.1, 0.1));

            Vector2 ploppedOutPosition = Vector2Add(Vector2(oldPosition.x, oldPosition.z), inverseDir);

            position = Vector3(ploppedOutPosition.x, oldPosition.y, ploppedOutPosition.y);

            boundsCheck();
            return;
        }

        float yVelocity = (sin(-rotation.x) * delta) * movementSpeed;

        position.y += yVelocity;

        if (position.y < minY) {
            position.y = minY;
        } else if (position.y > maxY) {
            position.y = maxY;
        }

    }

    void turnToTarget(double delta) {
        // Calculating yaw.
        Vector2 goalDir = Vector2Normalize(Vector2Subtract(Vector2(lookTarget.x, lookTarget.z), Vector2(
                position.x, position.z)));

        float targetYaw = atan2(goalDir.x, goalDir.y);
        float currentYaw = rotation.y;
        float diff = targetYaw - currentYaw;

        if (diff > PI) {
            targetYaw -= PI * 2;
        } else if (diff < -PI) {
            targetYaw += PI * 2;
        }

        float lookSpeed = relaxedLookSpeed;
        if (tightTurn == 1) {
            lookSpeed *= 3;
        }
        double oldTargetYaw = targetYaw;
        targetYaw = Lerp(currentYaw, targetYaw, delta * lookSpeed);

        // TightTurn 2 basically just looks straight at it.
        if (tightTurn == 2) {
            targetYaw = oldTargetYaw;
        }

        // Raymath can cause Lerp to go into negative or positive infinity.
        // NaN check is because I want to make sure it doesn't crash.
        if (abs(targetYaw) == float.infinity || abs(targetYaw) == float.nan) {
            // writeln("Caught nan yaw.");
            targetYaw = currentYaw;
        }

        // Calculating pitch.
        float distance = Vector2Distance(Vector2(position.x, position.z), Vector2(lookTarget.x, lookTarget
                .z));
        Vector2 pitchNormalized = Vector2Normalize(Vector2Subtract(Vector2(distance, lookTarget.y), Vector2(0, position
                .y)));
        float targetPitch = asin(-pitchNormalized.y);
        float currentPitch = rotation.x;

        targetPitch = Lerp(currentPitch, targetPitch, delta * lookSpeed);
        // Raymath can cause Lerp to go into negative or positive infinity.
        // NaN check is because I want to make sure it doesn't crash.
        if (abs(targetPitch) == float.infinity || abs(targetPitch) == float.nan) {
            // writeln("Caught nan pitch.");
            targetPitch = currentPitch;
        }

        rotation.x = targetPitch;
        rotation.y = targetYaw;
    }

    void selectRandomTargetPosition() {
        Vector2 map2dRange = Ground.getSizeFloating();

        // Limit the range.
        map2dRange.x -= 1;
        map2dRange.y -= 1;

        float selectedX;
        float selectedZ;
        float minY;
        float maxY;

        // Reroll until the fish can fit in the spot.
        while (true) {
            selectedX = giveRandomFloat(1.0, map2dRange.x);
            selectedZ = giveRandomFloat(1.0, map2dRange.y);
            minY = Ground.getCollisionPoint(selectedX, selectedZ) + collisionVertical;
            maxY = Water.getCollisionPoint(selectedX, selectedZ) - collisionVertical;
            if (minY <= maxY) {
                break;
            }
        }

        //? Useful for debugging.
        // selectedX = giveRandomFloat(position.x - 3, position.x + 3);
        // selectedZ = giveRandomFloat(position.x - 3, position.x + 3);
        // minY = Ground.getCollisionPoint(selectedX, selectedZ) + collisionVertical;
        // maxY = Water.getCollisionPoint(selectedX, selectedZ) - collisionVertical;

        float selectedY = giveRandomFloat(minY, maxY);

        lookTarget = Vector3(selectedX, selectedY, selectedZ);

        // turnLerpProgress = 0;

    }

    @property string model() {
    return this.__model;
}

void update(double delta) {

    // if (state != oldState) {
    // writeln("in state: ", state);
    // }

    turnToTarget(delta);
    moveToTarget(delta);

    oldState = state;

    // This is just a prototype game after all. The fish doesn't even think, it just goes to the lure.

    if (Lure.isInWater()) {
        lookTarget = Lure.getPosition();
        state = FishState.Following;
    } else if (state == FishState.Following) {
        if (giveRandomDouble(0.0, 1.0) > 0.5) {
            state = FishState.Idle;
        } else {
            state = FishState.RandomTarget;
        }
    }

    switch (state) {
        case FishState.Idle: {
                idle(delta);
                break;
            }
        case FishState.Looking: {
                looking(delta);
                break;
            }
        case FishState.RandomTarget: {
                randomTarget(delta);
                break;
            }
        case FishState.Following: {
                following(delta);
                break;
            }
        case FishState.Fight: {
                fight(delta);
                break;
            }
        default: {
                throw new Error("I don't know how this got to here.");
            }
    }
}

void idle(double delta) {
    // todo: idle animation.

    if (recalculateTimer) {
        recalculateTimer = false;
        behaviorTimer = giveRandomDouble(5.0, 15.0);
    }

    if (movementSpeed > 0) {
        movementSpeed -= delta * accelerationRelaxed;
        if (movementSpeed <= 0) {
            movementSpeed = 0;
        }
    }

    behaviorTimer -= delta;

    if (behaviorTimer <= 0.0) {
        state = randomState();
        resetStateData();
    }
}

void looking(double delta) {
    // todo: tail turning animation.

    if (behaviorTimer <= 0) {
        if (retrigger) {
            // The fish can keep looking around.
            if (giveRandomDouble(0.0, 1.0) > 0.5) {
                resetStateData();
                state = randomState();
            }
        } else {
            // If the fish was idling, let it enjoy looking around.
            if (oldState == FishState.Idle) {
                behaviorTimer = giveRandomDouble(6, 17);
            } else {
                behaviorTimer = giveRandomDouble(5, 12);
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

void randomTarget(double delta) {

    // todo: Use swimming animation.

    behaviorTimer -= delta;

    if (recalculateTimer) {
        tightTurn = 0;
        recalculateTimer = false;
        behaviorTimer = giveRandomDouble(8.0, 15.0);
        selectRandomTargetPosition();
    }

    if (movementSpeed < maxSpeedRelaxed) {
        movementSpeed += delta * accelerationRelaxed;
    }

    float distance = Vector3Distance(position, lookTarget);

    if (distance <= 1.5) {
        selectRandomTargetPosition();
        resetStateData();
    } else if (distance < 3.0) {
        tightTurn = 1;
    }

    if (behaviorTimer <= 0.0) {
        state = randomState();
        selectRandomTargetPosition();
        resetStateData();
    }
}

void following(double delta) {
    tightTurn = 2;

    auto distance = Vector3Distance(this.position, Lure.getPosition());

    if (distance < 0.5) {
        movementSpeed -= delta * 30;
        if (movementSpeed < 0) {
            movementSpeed = 0;
        }
        // writeln("stage 3");
    } else if (distance < 1) {
        if (movementSpeed > 2) {
            movementSpeed -= delta * 10;
        } else if (movementSpeed > 0) {
            movementSpeed -= delta * 5;

        }
        if (movementSpeed < 0) {
            movementSpeed = 0;
        }
        // writeln("stage 2");

    } else if (distance < 3) {
        if (movementSpeed > 2) {
            movementSpeed -= delta * 10;
        } else if (movementSpeed <= 1) {
            movementSpeed += delta * 2;
        }
        if (movementSpeed < 0) {
            movementSpeed = 0;
        }
        // writeln("stage 1");

    } else {
        if (movementSpeed < 4) {
            movementSpeed += delta * 2;
        }
    }

}

void fight(double delta) {

}
}

class LargeMouthBass : Fish {
    this() {
        __model = "largemouth.glb";
        accelerationRelaxed = 10;
        maxSpeedRelaxed = 5;
    }
}