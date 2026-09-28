namespace FishGame.Level;

public static class FishTank {
    //? This stores all the fish in the level.
    // <>< <>< <>< <><

    static Dictionary<string, Fish> database = [];


    static void Update() {
        if (database.length == 0) {
            // foreach (i; 0 .. 100) {
            LargeMouthBass newBass = new LargeMouthBass();
            writeln("I am uuid: ", newBass.getUUID());
            database[newBass.getUUID()] = newBass;
            writeln("spawned new largemouth");
            // }
        }

        double delta = Delta.getDelta();

        foreach (uuid, fish; database) {
            fish.update(delta);
        }
    }

    static void Draw() {
        foreach (uuid, fish; database) {

            ModelHandler.draw(fish.model(), fish.getPosition(), fish.getRotation());

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

    static Fish GetFish(string uuid) {
        if (database.TryGetValue(uuid, out var fish)) {
            return fish;
        } else {
            throw new Exception($"[FishTank]: Tried to get fish uuid {uuid} but it doesn't exist.");
        }
    }
}