namespace FrostHelper;
public static class GlobalEntityHelper {
    [OnLoad]
    public static void Load() {
        On.Celeste.Level.LoadLevel += Level_LoadLevel;
    }

    public static EntityData CloneData(EntityData toClone, LevelData newLevel) {
        return new() {
            Level = newLevel,
            Name = toClone.Name,
            Nodes = toClone.Nodes,
            Width = toClone.Width,
            Height = toClone.Height,
            Position = toClone.Position,
            Values = new(toClone.Values),
        };
    }



    private static void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
        if (isFromLoader) {
            AddGlobals(self);
        }

        orig(self, playerIntro, isFromLoader);


    }

    private static void AddGlobals(Level self) {
        // load global entities
        if (FrostMapDataProcessor.GlobalEntityMarkers.TryGetValue(self.Session.Area.SID, out var globalEntity)) {

            // prevent global entity duplication
            const string dynDataAddedGlobalsKey = "frostHelper.addedGlobals";
            var mapDataDynData = new DynamicData(self.Session.MapData.Levels);
            if (mapDataDynData.TryGet(dynDataAddedGlobalsKey, out _)) {
                Console.WriteLine("preventing dupe");
                return;
            }
            mapDataDynData.Set(dynDataAddedGlobalsKey, true);


            var globalEntityRoomName = globalEntity.Key;
            var container = globalEntity.Value;

            bool triggerTriggers = container.AttrBool("triggerTriggers", true);

            var globalRoom = self.Session.MapData.Levels.FirstOrDefault(data => data.Name == globalEntityRoomName);
            //var room = self.Session.LevelData;
            foreach (var room in self.Session.MapData.Levels) {
                if (room == globalRoom)
                    continue;

                room.Entities.AddRange(globalRoom.Entities
                    .Where(e => e.Name != "FrostHelper/GlobalEntityMarker")
                    .Select(e => CloneData(e, room)));
                room.Triggers.AddRange(globalRoom.Triggers.Select(e => {
                    var data = CloneData(e, room);
                    if (triggerTriggers) {
                        // slight offset to make the trigger edges not visible when viewing hitboxes
                        data.Position.X = -1;
                        data.Position.Y = -1;
                        data.Width = room.Bounds.Width + 2;
                        data.Height = room.Bounds.Height + 2;
                    }

                    return data;
                }));
            }

        }
    }

    [OnUnload]
    public static void Unload() {
        On.Celeste.Level.LoadLevel -= Level_LoadLevel;
    }
}
