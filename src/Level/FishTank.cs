using FishGame.Graphics;
using FishGame.Utility;

namespace FishGame.Level;

public static class FishTank {
    //? This stores all the fish in the level.
    // <>< <>< <>< <><

    static readonly Dictionary<string, Fish> database = [];


    public static void Update() {
        if (database.Count == 0) {
            for (int i = 0; i < 100; i++) {
                LargeMouthBass newBass = new();
                Console.WriteLine("I am uuid: " + newBass.GetUUID());
                database[newBass.GetUUID()] = newBass;
                Console.WriteLine("spawned new largemouth");
            }
        }

        float delta = Delta.Get();

        foreach (var (uuid, fish) in database) {
            fish.Update(delta);
        }
    }

    public static void Draw() {
        foreach (var (uuid, fish) in database) {

            ModelManager.Draw(fish.GetModel(), fish.GetPosition(), fish.GetRotation());

            // float groundYHeight = Ground.getCollisionPoint(fish.position.x, fish.position.z);
            // DrawCircle3D(Vector3(fish.position.x, groundYHeight, fish.position.z), 1, Vector3(1, 0, 0), 0, Colors
            //         .RED);
            // DrawSphere(Vector3(fish.position.x, groundYHeight, fish.position.z), 0.1, Colors.RED);

            // float waterYHeight = Water.getCollisionPoint(fish.position.x, fish.position.z);

            // DrawCircle3D(Vector3(fish.position.x, waterYHeight, fish.position.z), 1, Vector3(1, 0, 0), 0, Colors
            //         .GREEN);
            // DrawSphere(Vector3(fish.position.x, waterYHeight, fish.position.z), 0.01, Colors.GREEN);

            // Collision point upper.
            // DrawSphere(Vector3Add(fish.position, Vector3(0, fish.collisionVertical, 0)), 0.01, Colors
            //         .BLUE);
            // Collision point lower.
            // DrawSphere(Vector3Subtract(fish.position, Vector3(0, fish.collisionVertical, 0)), 0.01, Colors
            //         .GREEN);

            // DrawSphere(fish.lookTarget, 1.5, Colors.ORANGE);

            // switch (fish.state) {
            // case (FishState.Idle): {
            //         DrawSphere(Vector3Add(fish.position, Vector3(0, 1, 0)), 0.5, Colors.BLACK);
            //         break;
            //     }
            // case (FishState.Looking): {
            //         DrawSphere(Vector3Add(fish.position, Vector3(0, 1, 0)), 0.5, Colors.WHITE);
            //         break;
            //     }
            // case (FishState.RandomTarget): {
            //         DrawSphere(Vector3Add(fish.position, Vector3(0, 1, 0)), 0.5, Colors.BLUE);
            //         break;
            //     }
            // default: {
            //         DrawSphere(Vector3Add(fish.position, Vector3(0, 1, 0)), 0.5, Colors.YELLOW);
            //     }
            // }

        }
    }

    // public static string GetRandomFishUUID() {
    //     if (database.Count == 0) return string.Empty;

    //     // This is as low performance as you can get.
    //     string[] keys = database.Keys.ToArray();

    //     return keys[Randy.NextInt(0, keys.Length)];
    // }

    public static string GetDebugFishUUID() {
        if (database.Count == 0) return string.Empty;

        foreach (var (uuid, fish) in database) {
            if (fish.debugFishSelectionREMOVETHIS) {
                return uuid;
            }
        }

        throw new Exception("something bad happened");
    }

    public static Fish GetFish(string uuid) {
        if (database.TryGetValue(uuid, out var fish)) {
            return fish;
        } else {
            throw new Exception($"[FishTank]: Tried to get fish uuid {uuid} but it doesn't exist.");
        }
    }
}