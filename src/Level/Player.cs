using System.Numerics;
using FishGame.Audio;
using FishGame.Graphics;
using FishGame.Input;
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

// todo: This class is a fucking mess clean this shit hole up
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

    // Todo: Make this a list!!!!!!!
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
        //     CameraManager.setPosition(position);
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
        Lure.SetOutOfWater();
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

        Quaternion inRotation = model.BindPose[playerHandBoneIndex].Rotation;

        Quaternion outRotation = transform.Rotation;

        // Calculate socket rotation (angle between bone in initial pose and same bone in current animation frame)
        Quaternion matrixRotate = Raymath.QuaternionMultiply(outRotation, Raymath.QuaternionInvert(inRotation));

        Matrix4x4 matrixTransform = Raymath.QuaternionToMatrix(matrixRotate);

        // Translate socket to its position in the current animation
        matrixTransform = Raymath.MatrixMultiply(matrixTransform, Raymath.MatrixTranslate(transform.Translation.X, transform.Translation.Y, transform.Translation.Z));

        // If the player is in an interactive state, we want the animation components to rotate with their
        // aiming yaw. So we shall do that.
        switch (state) {
            case PlayerState.Aiming:
            case PlayerState.Casting:
            case PlayerState.CastingArc:
            case PlayerState.Water: {
                    matrixTransform = Raymath.MatrixMultiply(matrixTransform, Raymath.MatrixRotateY(rotation.Y - castingYaw));
                }
                break;
            default: {
                    matrixTransform = Raymath.MatrixMultiply(matrixTransform, Raymath.MatrixRotateY(rotation.Y));
                }
                break;
        }

        // Transform the socket using the transform of the character (angle and translate)
        matrixTransform = Raymath.MatrixMultiply(matrixTransform, model.Transform);

        Vector3 translationSpace;
        Quaternion quaternionRotation;
        Vector3 scaleSpace;
        Raymath.MatrixDecompose(matrixTransform, &translationSpace, &quaternionRotation, &scaleSpace);

        Vector3 rotationSpace = Raymath.QuaternionToEuler(quaternionRotation);

        translationSpace = Raymath.Vector3Add(translationSpace, playerOnBoat);

        ModelManager.Draw("fishing_rod.glb", translationSpace, rotationSpace);

        //? The lure gets kind of complicated lol.

        Vector3 lureTranslation = translationSpace;

        float poleSize = 1.635f;
        Vector3 zVector = new(matrixTransform.M13, matrixTransform.M23, matrixTransform.M33);
        Vector3 directionOfPole = Raymath.Vector3Normalize(zVector) * new Vector3(poleSize);

        // todo: fix these variable names, this is a mess.
        // todo: this is supposed to be the pole tip position.
        lureTranslation = Raymath.Vector3Add(lureTranslation, directionOfPole);
        poleTipRealtimePosition = lureTranslation;

        // This is a trick to simulate the lure swinging during a cast.
        Vector2 poleTipPosition = new(lureTranslation.X, lureTranslation.Z);
        float poleTipDeltaDistance = Raymath.Vector2Distance(poleTipPosition, oldPoleTipPosition);

        // Only draw the target when aiming.
        if (state == PlayerState.Aiming) {
            Raylib.DrawSphere(GetCastTarget(), 0.1f, Color.Red);
        }

        switch (state) {
            case PlayerState.Aiming or PlayerState.Menu: {
                    lureTranslation.Y -= 0.1f;
                    Lure.SetPosition(lureTranslation);
                    Lure.SetRotation(new Vector3(0, rotation.Y + -castingYaw, 0));
                }
                break;
            case PlayerState.Casting: {

                    // If this is the first cast tick, save and abort.
                    if (firstCastFrame) {
                        oldPoleTipPosition = new Vector2(lureTranslation.X, lureTranslation.Z);
                        firstCastFrame = false;
                        SoundManager.Play("reel_open_bail.ogg");
                        break;
                    }

                    if (poleTipDeltaDistance > 0) {

                        Vector2 poleTipSwingDirection = Raymath.Vector2Normalize(Raymath.Vector2Subtract(oldPoleTipPosition,
                                poleTipPosition));

                        float dx = oldPoleTipPosition.X - poleTipPosition.X;
                        float dy = oldPoleTipPosition.Y - poleTipPosition.Y;
                        float yaw = (float)((-Math.Atan2(dy, dx)) - (Math.PI / 2f));

                        oldPoleTipPosition = new Vector2(lureTranslation.X, lureTranslation.Z);

                        lureTranslation.Y -= 0.1f;

                        float swingX = poleTipSwingDirection.X * poleTipDeltaDistance;
                        float swingZ = poleTipSwingDirection.Y * poleTipDeltaDistance;

                        lureTranslation.X += swingX;
                        lureTranslation.Z += swingZ;

                        Lure.SetPosition(lureTranslation);

                        Lure.SetRotation(new Vector3(0, yaw, 0));
                    }
                }
                break;
            case PlayerState.CastingArc: {

                    float currentProgressModified = (float)(castProgress * Math.PI);
                    float arcHeight = (float)(Math.Sin(currentProgressModified));

                    if (Math.Abs(arcHeight) < 0.001) {
                        arcHeight = 0;
                    }

                    arcHeight -= Raymath.Lerp(0.1f, 0.0f, castProgress);

                    Vector3 progress = Raymath.Vector3Lerp(lureTranslation, GetCastTarget(), castProgress);
                    progress.Y += arcHeight;

                    Lure.SetPosition(progress);

                    // Draw the line.

                    if (lineData.Length > 0) {
                        Raylib.DrawLine3D(lureTranslation, lineData[0], Color.Black);

                        for (int i = 0; i < lineData.Length - 1; i++) {
                            Vector3 current = lineData[i];
                            Vector3 next = lineData[i + 1];

                            Raylib.DrawLine3D(current, next, Color.Black);
                        }
                        Raylib.DrawLine3D(lineData[lineData.Length - 1], progress, Color.Black);
                    } else {
                        Raylib.DrawLine3D(lureTranslation, progress, Color.Black);
                    }

                    // DrawSphere(progress, 0.1, Colors.ORANGE);
                }
                break;
            case PlayerState.Water: {
                    Raylib.DrawLine3D(lureTranslation, Lure.GetPosition(), Color.Black);
                }
                break;
            default: {
                    throw new Exception("Oops");
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

                        castTumblePitch = Randy.NextFloat(0.1f, 10.0f);
                        castTumbleYaw = Randy.NextFloat(0.1f, 10.0f);

                        lineCreationProgress = 0;
                        lineData = [];
                        lineCreationProgress = 0;
                    }
                }
                break;
            case PlayerState.CastingArc: {

                    float waterLevel = Water.GetWaterLevel();

                    // readonly float max = cast(double)(cast(int) lineData.length);

                    // Try to interpolate to a line that's falling onto the water.
                    for (int i = 0; i < lineData.Length; i++) {
                        ref var v = ref lineData[i];
                        // todo: test out messing with the max to make a cool looking falling line.
                        float current = (float)i + 1;
                        float application = (float)(current * 0.1f);

                        v.Y -= delta * application;
                        if (v.Y <= waterLevel) {
                            v.Y = waterLevel;
                        }
                    }

                    if (castProgressDistance >= castingDistance) {
                        castProgressDistance = castingDistance;
                        lineFallRestTimer += delta;

                        if (!lureSplashPlayed) {
                            SoundManager.Play("lure_hit_water.ogg", 0.1f);
                            lureSplashPlayed = true;
                        }

                        // I worked hard on these line physics so you get to watch them. >:)
                        if (lineFallRestTimer >= 1.5) {
                            state = PlayerState.Water;
                            // todo: don't delete the line data.
                            lineData = [];

                            // When the state changes into the water state, we do some "magic" to snap everything into place.

                            Vector3 lurePosition = Lure.GetPosition();
                            float x = position.X - lurePosition.X;
                            float z = position.Z - lurePosition.Z;
                            float lureYaw = (float)Math.Atan2(x, z);
                            Lure.SetRotation(new Vector3(0, lureYaw, 0));

                            Lure.SetInWater();
                        }
                    } else {

                        float increase = delta * 12.0f;
                        castProgressDistance += increase;
                        lineCreationProgress += increase;

                        if (lineCreationProgress >= 1.0) {
                            lineData = lineData.Concat([Lure.GetPosition()]).ToArray();
                            lineCreationProgress = 0;
                        }

                        Vector3 currentRotation = Lure.GetRotation();

                        currentRotation.Y += Delta.Get() * castTumbleYaw;
                        currentRotation.X += Delta.Get() * castTumblePitch;

                        Lure.SetRotation(currentRotation);

                        lineFallRestTimer = 0;
                    }

                    castProgress = castProgressDistance / castingDistance;
                }
                break;
            case PlayerState.Menu: {

                }
                break;
            case PlayerState.Water: {
                    if (Mouse.IsButtonDown(MouseButton.MOUSE_BUTTON_LEFT)) {
                        Lure.Reel();
                    }
                }
                break;
            default: {
                    throw new Exception("Oops");
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
            case PlayerState.Menu: {

                }
                break;
            case PlayerState.Water: {

                }
                break;
            default: {
                    throw new Exception("Oops");
                }
        }
    }

    static void DoControls() {

        float delta = Delta.Get();

        switch (state) {
            case PlayerState.Aiming: {

                    Vector2 mouseDelta = Mouse.GetDelta();

                    // Begin forwards/backwards lure aiming control.

                    float oldCastingDistance = castingDistance;

                    castingDistance -= mouseDelta.Y / 100.0f;

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

                    castingYaw += mouseDelta.X / 1500.0f;

                    if (castingYaw < -maxAngle) {
                        castingYaw = -maxAngle;
                    } else if (castingYaw > maxAngle) {
                        castingYaw = maxAngle;
                    }

                    // Don't let it go into the shore.
                    if (LureCollidesWithShore()) {
                        // First, try to bump the distance back.
                        // This hardcode also creates a jolty effect.
                        castingDistance -= 0.7f;

                        castingYaw = oldCastingYaw;

                        if (LureCollidesWithShore()) {
                            // Welp that failed, move everything back.  
                            castingYaw = oldCastingYaw;
                            castingDistance += 0.7f;
                        }
                    }

                    if (Mouse.IsButtonPressed(MouseButton.Left)) {
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
                    if (Mouse.IsButtonDown(MouseButton.Left)) {
                        Lure.Reel();
                    }
                }
                break;
            default: {
                    throw new Exception("Oops");
                }
        }
    }

    const float targetFrameTime = 1.0f / 60.0f;

    static void DoCastAnimation() {

        float delta = Delta.Get();

        var oldState = animationFrame;

        if (animationFrame < castFrameMiddle) {
            animationFrame += 100 * delta;
        } else {
            animationFrame += 300 * delta;
        }

        if (oldState < castFrameMiddle && animationFrame >= castFrameMiddle) {
            SoundManager.Play("casting_woosh.ogg");
        }

        if (animationFrame >= castFrameMax) {
            animationFrame = castFrameMax;
        }
    }

    static void DoCameraPositioning() {
        switch (state) {
            case PlayerState.Aiming: {
                    float waterLevel = Water.GetWaterLevel();
                    Vector3 newCameraPosition = new();
                    newCameraPosition.X = position.X;
                    // This is at the level of the player's chest but it looks better.
                    newCameraPosition.Y = waterLevel + 1.6f;
                    newCameraPosition.Z = position.Z;

                    CameraManager.SetPosition(newCameraPosition);

                    //! Debugging.
                    // Vector3 target = getCastTarget();
                    // target.X -= 0.5;
                    // target.Y += 0.5;
                    // target.Z -= 0.5;

                    // CameraManager.setPosition(target);

                    CameraManager.SetTarget(GetCastTarget());

                }
                break;
            case PlayerState.Casting or PlayerState.CastingArc: {
                    float shift = 2.6f;
                    float distance = 2f;
                    float waterLevel = Water.GetWaterLevel();

                    float rotated = (float)(rotation.Y + (Math.PI / shift)) + castingYaw;
                    float x = (float)Math.Cos(rotated) * distance;
                    float z = (float)Math.Sin(rotated) * distance;

                    Vector3 newCameraPosition = new();
                    newCameraPosition.X = position.X + x;
                    newCameraPosition.Y = waterLevel + 1.6f;
                    newCameraPosition.Z = position.Z + z;

                    CameraManager.SetPosition(newCameraPosition);

                    rotated -= (float)Math.PI / 1.25f;

                    x = (float)Math.Cos(rotated) * distance;
                    z = (float)Math.Sin(rotated) * distance;

                    Vector3 newTargetPosition = new();
                    newTargetPosition.X = position.X + x;
                    newTargetPosition.Y = waterLevel + 1.6f;
                    newTargetPosition.Z = position.Z + z;

                    CameraManager.SetTarget(newTargetPosition);
                }
                break;
            case PlayerState.Menu: {
                    float shiftFront = 5;
                    float shiftBack = 1.05f;
                    float distance = 8;
                    float waterLevel = Water.GetWaterLevel();

                    float rotated = (float)((-rotation.Y) - (Math.PI / shiftFront));
                    float x = (float)Math.Cos(rotated) * distance;
                    float z = (float)Math.Sin(rotated) * distance;

                    Vector3 newCameraPosition = new();
                    newCameraPosition.X = position.X + x;
                    newCameraPosition.Y = waterLevel + 2;
                    newCameraPosition.Z = position.Z + z;

                    CameraManager.SetPosition(newCameraPosition);

                    rotated = (float)((-rotation.Y) + (Math.PI / shiftBack));

                    x = (float)Math.Cos(rotated) * distance;
                    z = (float)Math.Sin(rotated) * distance;

                    Vector3 newTargetPosition = new();
                    newTargetPosition.X = position.X + x;
                    newTargetPosition.Y = waterLevel + 2;
                    newTargetPosition.Z = position.Z + z;

                    CameraManager.SetTarget(newTargetPosition);
                }
                break;
            case PlayerState.Water: {

                    Vector3 lurePosition = Lure.GetPosition();

                    // todo: create debug thing to get any fish.
                    Fish fish = FishTank.GetFish(0);

                    var fishYaw = (float)(((Raylib.RAD2DEG * fish.GetRotation().Y) + 195) * Raylib.DEG2RAD);

                    var fishDir = new Vector3((float)Math.Sin(fishYaw), 0.0f, (float)Math.Cos(fishYaw));

                    CameraManager.SetPosition(Raymath.Vector3Add(fish.GetPosition(), fishDir));

                    // Now rotate this 180 degrees.
                    fishYaw += (float)Math.PI;
                    Console.WriteLine(fishYaw);
                    fishDir = new Vector3((float)Math.Sin(fishYaw), 0.0f, (float)Math.Cos(fishYaw));

                    CameraManager.SetTarget(Raymath.Vector3Add(fish.GetPosition(), fishDir));

                    // CameraManager.setTarget(lurePosition);
                    // lurePosition.X -= 1;
                    // lurePosition.Y += 1;
                    // lurePosition.Z -= 1;
                    // CameraManager.setPosition(lurePosition);
                    // CameraManager.setTarget(FishTank.whereDatFish());

                }
                break;
            default: {
                    throw new Exception("Oops");
                }
        }

        if (state == PlayerState.Menu) {

        } else if (state == PlayerState.Casting) {

        }
    }

    // This will compose the imaginary yaw and distance into the real world position.
    static Vector3 GetCastTarget() {
        Vector3 castTarget;

        float totalYaw = (float)((rotation.Y + castingYaw) - (Math.PI / 2));

        castTarget.X = (float)(Math.Cos(totalYaw) * castingDistance) + position.X;
        castTarget.Z = (float)(Math.Sin(totalYaw) * castingDistance) + position.Z;

        castTarget.Y = Water.GetCollisionPoint(castTarget.X, castTarget.Z);

        return castTarget;
    }

    static bool LureCollidesWithShore() {

        float totalYaw = (float)((rotation.Y + castingYaw) - (Math.PI / 2));

        float x = (float)(Math.Cos(totalYaw) * castingDistance) + position.X;
        float z = (float)(Math.Sin(totalYaw) * castingDistance) + position.Z;

        float waterHeight = Water.GetCollisionPoint(x, z);
        float groundHeight = Ground.GetCollisionPoint(x, z);

        return (waterHeight - groundHeight) < 0.3;
    }
}