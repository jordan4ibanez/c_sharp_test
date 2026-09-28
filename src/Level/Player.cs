using System.Numerics;
using FishGame.Graphics;
using FishGame.Utility;
using Raylib_cs;

namespace FishGame.Level;

public enum PlayerState {
    // First person.
    Aiming,
    // To the right. This one also includes the lure flying through the air in CastingArc.
    Casting,
    CastingArc,
    // Further back to the left.
    Menu,
    // Underwater lure cam.
    Water
}

public static class Player {

    static Vector3 position;
    static Vector3 rotation;
    static Vector2 oldPoleTipPosition;
    static Vector3 poleTipRealtimePosition;

    static PlayerState state = PlayerState.Aiming;
    static int playerHandBoneIndex = -1;

    static float animationFrame = 0f;

    // Casting variables.
    static bool firstCastFrame = true;
    static float castTimer = 0.0f;
    readonly static int castFrameMax = 230;
    readonly static int castFrameMiddle = 230 / 2;
    readonly static float castingDistanceMin = 10;
    readonly static float castingDistanceMax = 30;
    // This is how wide of a triangulation you can cast.
    readonly static float maxAngle = 40 * Raylib.DEG2RAD;

    static float castTumblePitch = 0;
    static float castTumbleYaw = 0;

    static float castProgressDistance = 0;
    static float castProgress = 0;

    static float lineCreationProgress = 0;
    static Vector3[] lineData = [];
    static float lineFallRestTimer = 0;
    static bool lureSplashPlayed = false;

    //! Note: these need to be reset when the player changes spots.
    static float castingYaw = 0.0f;
    static float castingDistance = castingDistanceMin;

    //!! NOTE:
    // Animation seems to be float the blender keyframes. So frame 30 is 60-ish. 

    static bool inittrigger = true;



    public static void Update() {
        float delta = Delta.Get();

        UpdateFloating();
        DoControls();
        DoLogic(delta);
        DoAnimation();

        //? This is for debugging in freecam. So you don't have to fly across the map.
        // if (inittrigger) {
        //     CameraHandler.setPosition(position);
        //     inittrigger = false;
        // }
    }

    public static void CameraUpdate() {
        DoCameraPositioning();
    }

    public static void SetPosition(float x, float y, float z) {
        position = new Vector3(x, y, z);
    }

    public static Vector3 GetPosition() {
        return position;
    }

    public static Vector3 GetPoleTipPosition() {
        return poleTipRealtimePosition;
    }

    public static void TriggerEmptyReelCompletion() {
        state = PlayerState.Aiming;
        castTimer = 0;
        // This instantly triggers a frame update.
        animationFrame = 0;
        firstCastFrame = true;
        Lure.setOutOfWater();
    }

    public static unsafe void SetDefaultPosition() {
        Vector2 groundSize = Ground.GetSizeFloating();
        position.X = groundSize.X / 2.0f;
        position.Z = groundSize.Y / 2.0f;
        ModelManager.PlayAnimation("person.glb", 0, 0);
        rotation.Y = (float)Math.PI / 2.0f;

        Model personModel = ModelManager.GetModel("person.glb");

        for (int i = 0; i < personModel.BoneCount; i++) {
            ReadOnlySpan<byte> nameSpan = new(personModel.Bones[i].Name, 9);
            if (nameSpan.StartsWith("MiddleI.R"u8)) {
                // writeln("index ", i);
                playerHandBoneIndex = i;
                break;
            }
        }
    }

    public static void UpdateFloating() {
        position.Y = Water.GetCollisionPoint(position.X, position.Z);
        position.Y -= 0.1f;

        // rotation.Y += Delta.getDelta();
    }

    public static unsafe void Draw() {
        ModelManager.Draw("boat.glb", position, rotation);

        Vector3 playerOnBoat = position;
        playerOnBoat.Y += 0.6f;

        ModelManager.PlayAnimation("person.glb", 0, (int)Math.Floor(animationFrame));

        // Make the player turn with the casting angle if they're in an interaction state.
        // Also, do not render the player if aiming. (first person mode)
        if (state != PlayerState.Aiming) {
            switch (state) {
                case PlayerState.Casting:
                case PlayerState.CastingArc:
                case PlayerState.Water: {
                        Vector3 combinedRotation = rotation;
                        combinedRotation.Y -= castingYaw;
                        ModelManager.Draw("person.glb", playerOnBoat, combinedRotation);
                    }
                    break;
                default: {
                        ModelManager.Draw("person.glb", playerOnBoat, rotation);
                    }
                    break;
            }
        }

        //? The song and dance you see below is to put the fishing pole in the player's hand.
        //? Thankfully modern x86_64 cpus do this trivialy, but it's a pain in the butt.

        Model model = ModelManager.GetModel("person.glb");
        AnimationContainer personAnimationContainer = ModelManager.GetAnimationContainer("person.glb");
        ModelAnimation[] animation = personAnimationContainer.animationData;

        Transform transform = animation[0].FramePoses[(int)Math.Floor(animationFrame)][playerHandBoneIndex];

        Quaternion inRotation = model.bindPose[playerHandBoneIndex].rotation;

        Quaternion outRotation = transform.rotation;

        // Calculate socket rotation (angle between bone in initial pose and same bone in current animation frame)
        Quaternion matrixRotate = QuaternionMultiply(outRotation, QuaternionInvert(inRotation));

        Matrix matrixTransform = QuaternionToMatrix(matrixRotate);

        // Translate socket to its position in the current animation
        matrixTransform = MatrixMultiply(matrixTransform, MatrixTranslate(transform.translation.X, transform
                .translation.Y, transform.translation.Z));

        // If the player is in an interactive state, we want the animation components to rotate with their
        // aiming yaw. So we shall do that.
        switch (state) {
            case PlayerState.Aiming, PlayerState.Casting, PlayerState.CastingArc, PlayerState.Water: {
                    matrixTransform = MatrixMultiply(matrixTransform, MatrixRotateY(
                            rotation.Y - castingYaw));
                }
                break;
            default: {
                    matrixTransform = MatrixMultiply(matrixTransform, MatrixRotateY(rotation.Y));
                }
        }

        // Transform the socket using the transform of the character (angle and translate)
        matrixTransform = MatrixMultiply(matrixTransform, model.transform);

        Vector3 translationSpace;
        Quaternion quaternionRotation;
        Vector3 scaleSpace;
        MatrixDecompose(matrixTransform, &translationSpace, &quaternionRotation, &scaleSpace);

        Vector3 rotationSpace = QuaternionToEuler(quaternionRotation);

        translationSpace = Vector3Add(translationSpace, playerOnBoat);

        ModelManager.draw("fishing_rod.glb", translationSpace, rotationSpace);

        //? The lure gets kind of complicated lol.

        Vector3 lureTranslation = translationSpace;

        readonly float poleSize = 1.635;
        Vector3 directionOfPole = Vector3Multiply(Vector3Normalize(Vector3(matrixTransform.m8, matrixTransform.m9,
                matrixTransform.m10)), Vector3(poleSize, poleSize, poleSize));

        // todo: fix these variable names, this is a mess.
        // todo: this is supposed to be the pole tip position.
        lureTranslation = Vector3Add(lureTranslation, directionOfPole);
        poleTipRealtimePosition = lureTranslation;

        // This is a trick to simulate the lure swinging during a cast.
        Vector2 poleTipPosition = Vector2(lureTranslation.X, lureTranslation.Z);
        float poleTipDeltaDistance = Vector2Distance(poleTipPosition, oldPoleTipPosition);

        // Only draw the target when aiming.
        if (state == PlayerState.Aiming) {
            DrawSphere(GetCastTarget(), 0.1, Colors.RED);
        }

        switch (state) {
            case PlayerState.Aiming, PlayerState.Menu: {
                    lureTranslation.Y -= 0.1;
                    Lure.setPosition(lureTranslation);
                    Lure.setRotation(Vector3(0, rotation.Y + -castingYaw, 0));
                }
                break;
            case PlayerState.Casting: {

                    // If this is the first cast tick, save and abort.
                    if (firstCastFrame) {
                        oldPoleTipPosition = Vector2(lureTranslation.X, lureTranslation.Z);
                        firstCastFrame = false;
                        SoundManager.play("reel_open_bail.ogg");
                        break;
                    }

                    if (poleTipDeltaDistance > 0) {

                        Vector2 poleTipSwingDirection = Vector2Normalize(Vector2Subtract(oldPoleTipPosition,
                                poleTipPosition));

                        float dx = oldPoleTipPosition.X - poleTipPosition.X;
                        float dy = oldPoleTipPosition.Y - poleTipPosition.Y;
                        float yaw = (-atan2(dy, dx)) - (PI / 2);

                        oldPoleTipPosition = Vector2(lureTranslation.X, lureTranslation.Z);

                        lureTranslation.Y -= 0.1;

                        float swingX = poleTipSwingDirection.X * poleTipDeltaDistance;
                        float swingZ = poleTipSwingDirection.Y * poleTipDeltaDistance;

                        lureTranslation.X += swingX;
                        lureTranslation.Z += swingZ;

                        Lure.setPosition(lureTranslation);

                        Lure.setRotation(Vector3(0, yaw, 0));
                    }
                }
                break;
            case PlayerState.CastingArc: {

                    float currentProgressModified = (castProgress * PI);
                    float arcHeight = (sin(currentProgressModified));

                    if (abs(arcHeight) < 0.001) {
                        arcHeight = 0;
                    }

                    arcHeight -= Lerp(0.1, 0.0, castProgress);

                    Vector3 progress = Vector3Lerp(lureTranslation, GetCastTarget(), castProgress);
                    progress.Y += arcHeight;

                    Lure.setPosition(progress);

                    // Draw the line.

                    if (lineData.length > 0) {
                        DrawLine3D(lureTranslation, lineData[0], Colors.BLACK);
                        foreach (i; 0..(lineData.length) - 1) {
                            Vector3 current = lineData[i];
                            Vector3 next = lineData[i + 1];

                            DrawLine3D(current, next, Colors.BLACK);
                        }
                        DrawLine3D(lineData[(lineData.length) - 1], progress, Colors.BLACK);
                    } else {
                        DrawLine3D(lureTranslation, progress, Colors.BLACK);
                    }

                    // DrawSphere(progress, 0.1, Colors.ORANGE);
                }
                break;
            case PlayerState.Water: {
                    DrawLine3D(lureTranslation, Lure.getPosition(), Colors.BLACK);
                }
                break;
            default: {
                    throw new Error("Oops");
                }
        }
    }

    //? Begin private section of class.

    static void DoLogic(float delta) {
        switch (state) {
            case PlayerState.Aiming: {

                }
                break;
            case PlayerState.Casting: {
                    if (animationFrame == castFrameMax) {
                        state = PlayerState.CastingArc;
                        castProgress = 0;

                        auto rnd = Random(unpredictableSeed());
                        castTumblePitch = uniform(0.1, 10.0, rnd);
                        castTumbleYaw = uniform(0.1, 10.0, rnd);

                        lineCreationProgress = 0;
                        lineData = new Vector3[](0);
                        lineCreationProgress = 0;
                    }
                }
                break;
            case PlayerState.CastingArc: {

                    readonly float waterLevel = Water.getWaterLevel();

                    // readonly float max = cast(double)(cast(int) lineData.length);

                    // Try to interpolate to a line that's falling onto the water.
                    foreach (i, ref v; lineData) {
                        // todo: test out messing with the max to make a cool looking falling line.
                        float current = cast(double) i + 1;
                        float application = current * 0.1;

                        v.Y -= delta * application;
                        if (v.Y <= waterLevel) {
                            v.Y = waterLevel;
                        }
                    }

                    if (castProgressDistance >= castingDistance) {
                        castProgressDistance = castingDistance;
                        lineFallRestTimer += delta;

                        if (!lureSplashPlayed) {
                            SoundManager.play("lure_hit_water.ogg", 0.1);
                            lureSplashPlayed = true;
                        }

                        // I worked hard on these line physics so you get to watch them. >:)
                        if (lineFallRestTimer >= 1.5) {
                            state = PlayerState.Water;
                            // todo: don't delete the line data.
                            lineData = null;

                            // When the state changes into the water state, we do some "magic" to snap everything into place.

                            Vector3 lurePosition = Lure.getPosition();
                            float x = position.X - lurePosition.X;
                            float z = position.Z - lurePosition.Z;
                            float lureYaw = atan2(x, z);
                            Lure.setRotation(Vector3(0, lureYaw, 0));

                            Lure.setInWater();
                        }
                    } else {

                        float increase = delta * 12.0;
                        castProgressDistance += increase;
                        lineCreationProgress += increase;

                        if (lineCreationProgress >= 1.0) {
                            lineData ~= Lure.getPosition();
                            lineCreationProgress = 0;
                        }

                        Vector3 currentRotation = Lure.getRotation();

                        currentRotation.Y += Delta.getDelta() * castTumbleYaw;
                        currentRotation.X += Delta.getDelta() * castTumblePitch;

                        Lure.setRotation(currentRotation);

                        lineFallRestTimer = 0;
                    }

                    castProgress = castProgressDistance / castingDistance;
                }
                break;
            case PlayerState.Menu: {

                }
                break;
            case PlayerState.Water: {
                    if (Mouse.isButtonDown(MouseButton.MOUSE_BUTTON_LEFT)) {
                        Lure.reel();
                    }
                }
                break;
            default: {
                    throw new Error("Oops");
                }
        }
    }

    static void DoAnimation() {
        switch (state) {
            case PlayerState.Aiming: {

                }
                break;
            case PlayerState.Casting: {
                    DoCastAnimation();
                }
                break;
            case PlayerState.CastingArc: {

                    break;
                }
                break;
            case PlayerState.Menu: {

                }
                break;
            case PlayerState.Water: {

                }
                break;
            default: {
                    throw new Error("Oops");
                }
        }
    }

    static void DoControls() {

        float delta = Delta.getDelta();

        switch (state) {
            case PlayerState.Aiming: {

                    Vector2 mouseDelta = Mouse.getDelta();

                    // Begin forwards/backwards lure aiming control.

                    float oldCastingDistance = castingDistance;

                    castingDistance -= mouseDelta.Y / 100.0;

                    // Keep the distance within range.
                    if (castingDistance < castingDistanceMin) {
                        castingDistance = castingDistanceMin;
                    } else if (castingDistance > castingDistanceMax) {
                        castingDistance = castingDistanceMax;
                    }

                    // Don't let it go into the shore.
                    if (LureCollidesWithShore()) {
                        castingDistance = oldCastingDistance;
                    }

                    // writeln(castingDistance);

                    // Begin side/side radial lure aiming control.

                    float oldCastingYaw = castingYaw;

                    castingYaw += mouseDelta.X / 1500.0;

                    if (castingYaw < -maxAngle) {
                        castingYaw = -maxAngle;
                    } else if (castingYaw > maxAngle) {
                        castingYaw = maxAngle;
                    }

                    // Don't let it go into the shore.
                    if (LureCollidesWithShore()) {
                        // First, try to bump the distance back.
                        // This hardcode also creates a jolty effect.
                        castingDistance -= 0.7;

                        castingYaw = oldCastingYaw;

                        if (LureCollidesWithShore()) {
                            // Welp that failed, move everything back.  
                            castingYaw = oldCastingYaw;
                            castingDistance += 0.7;
                        }
                    }

                    if (Mouse.isButtonPressed(MouseButton.MOUSE_BUTTON_LEFT)) {
                        state = PlayerState.Casting;
                        castTimer = 0;
                        castProgressDistance = 0;
                        lureSplashPlayed = false;
                    }
                }
                break;
            case PlayerState.Casting: {

                    // This is a weird player animation/state reset thing.
                    // if (Mouse.isButtonPressed(MouseButton.MOUSE_BUTTON_LEFT)) {
                    //     state = PlayerState.Aiming;
                    //     castTimer = 0;
                    //     // This instantly triggers a frame update.
                    //     frameTimer = (1 / 60) + 0.001;
                    //     animationFrame = 0;
                    //     firstCastFrame = true;
                    //     break;
                    // }

                    castTimer += delta;

                }
                break;
            case PlayerState.CastingArc: {

                    // if (Mouse.isButtonPressed(MouseButton.MOUSE_BUTTON_LEFT)) {
                    //     state = PlayerState.Aiming;
                    //     castTimer = 0;
                    //     // This instantly triggers a frame update.
                    //     frameTimer = (1 / 60) + 0.001;
                    //     animationFrame = 0;
                    //     firstCastFrame = true;
                    //     break;
                    // }

                }
                break;
            case PlayerState.Menu: {

                }
                break;
            case PlayerState.Water: {
                    if (Mouse.isButtonDown(MouseButton.MOUSE_BUTTON_LEFT)) {
                        Lure.reel();
                    }
                }
                break;
            default: {
                    throw new Error("Oops");
                }
        }
    }

    const targetFrameTime = 1.0 / 60.0;

    static void DoCastAnimation() {

        float delta = Delta.getDelta();

        auto oldState = animationFrame;

        if (animationFrame < castFrameMiddle) {
            animationFrame += 100 * delta;
        } else {
            animationFrame += 300 * delta;
        }

        if (oldState < castFrameMiddle && animationFrame >= castFrameMiddle) {
            SoundManager.play("casting_woosh.ogg");
        }

        if (animationFrame >= castFrameMax) {
            animationFrame = castFrameMax;
        }
    }

    static void DoCameraPositioning() {
        switch (state) {
            case PlayerState.Aiming: {
                    readonly float waterLevel = Water.getWaterLevel();
                    Vector3 newCameraPosition = Vector3();
                    newCameraPosition.X = position.X;
                    // This is at the level of the player's chest but it looks better.
                    newCameraPosition.Y = waterLevel + 1.6;
                    newCameraPosition.Z = position.Z;

                    CameraHandler.setPosition(newCameraPosition);

                    //! Debugging.
                    // Vector3 target = getCastTarget();
                    // target.X -= 0.5;
                    // target.Y += 0.5;
                    // target.Z -= 0.5;

                    // CameraHandler.setPosition(target);

                    CameraHandler.setTarget(GetCastTarget());

                }
                break;
            case PlayerState.Casting, PlayerState.CastingArc: {
                    readonly float shift = 2.6;
                    readonly float distance = 2;
                    readonly float waterLevel = Water.getWaterLevel();

                    float rotated = (rotation.Y + (PI / shift)) + castingYaw;
                    float x = cos(rotated) * distance;
                    float z = sin(rotated) * distance;

                    Vector3 newCameraPosition = Vector3();
                    newCameraPosition.X = position.X + x;
                    newCameraPosition.Y = waterLevel + 1.6;
                    newCameraPosition.Z = position.Z + z;

                    CameraHandler.setPosition(newCameraPosition);

                    rotated -= PI / 1.25;

                    x = cos(rotated) * distance;
                    z = sin(rotated) * distance;

                    Vector3 newTargetPosition = Vector3();
                    newTargetPosition.X = position.X + x;
                    newTargetPosition.Y = waterLevel + 1.6;
                    newTargetPosition.Z = position.Z + z;

                    CameraHandler.setTarget(newTargetPosition);
                }
                break;
            case PlayerState.Menu: {
                    readonly float shiftFront = 5;
                    readonly float shiftBack = 1.05;
                    readonly float distance = 8;
                    readonly float waterLevel = Water.getWaterLevel();

                    float rotated = (-rotation.Y) - (PI / shiftFront);
                    float x = cos(rotated) * distance;
                    float z = sin(rotated) * distance;

                    Vector3 newCameraPosition = Vector3();
                    newCameraPosition.X = position.X + x;
                    newCameraPosition.Y = waterLevel + 2;
                    newCameraPosition.Z = position.Z + z;

                    CameraHandler.setPosition(newCameraPosition);

                    rotated = (-rotation.Y) + (PI / shiftBack);

                    x = cos(rotated) * distance;
                    z = sin(rotated) * distance;

                    Vector3 newTargetPosition = Vector3();
                    newTargetPosition.X = position.X + x;
                    newTargetPosition.Y = waterLevel + 2;
                    newTargetPosition.Z = position.Z + z;

                    CameraHandler.setTarget(newTargetPosition);
                }
                break;
            case PlayerState.Water: {

                    Vector3 lurePosition = Lure.getPosition();

                    Fish fish = FishTank.getFish(0);

                    auto fishYaw = ((RAD2DEG * fish.getRotation().Y) + 195) * DEG2RAD;

                    auto fishDir = Vector3(sin(fishYaw), 0.0f, cos(fishYaw));

                    CameraHandler.setPosition(fish.getPosition().Vector3Add(fishDir));

                    // Now rotate this 180 degrees.
                    fishYaw += PI;
                    writeln(fishYaw);
                    fishDir = Vector3(sin(fishYaw), 0.0f, cos(fishYaw));

                    CameraHandler.setTarget(fish.getPosition().Vector3Add(fishDir));

                    // CameraHandler.setTarget(lurePosition);
                    // lurePosition.X -= 1;
                    // lurePosition.Y += 1;
                    // lurePosition.Z -= 1;
                    // CameraHandler.setPosition(lurePosition);
                    // CameraHandler.setTarget(FishTank.whereDatFish());

                }
                break;
            default: {
                    throw new Error("Oops");
                }
        }

        if (state == PlayerState.Menu) {

        } else if (state == PlayerState.Casting) {

        }
    }

    // This will compose the imaginary yaw and distance into the real world position.
    static Vector3 GetCastTarget() {
        Vector3 castTarget;

        float totalYaw = (rotation.Y + castingYaw) - (PI / 2);

        castTarget.X = (cos(totalYaw) * castingDistance) + position.X;
        castTarget.Z = (sin(totalYaw) * castingDistance) + position.Z;

        castTarget.Y = Water.getCollisionPoint(castTarget.X, castTarget.Z);

        return castTarget;
    }

    static bool LureCollidesWithShore() {

        float totalYaw = (rotation.Y + castingYaw) - (PI / 2);

        float x = (cos(totalYaw) * castingDistance) + position.X;
        float z = (sin(totalYaw) * castingDistance) + position.Z;

        float waterHeight = Water.getCollisionPoint(x, z);
        float groundHeight = Ground.getCollisionPoint(x, z);

        return (waterHeight - groundHeight) < 0.3;
    }
}