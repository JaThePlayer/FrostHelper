using System.Threading;

namespace FrostHelper;
public static class GlobalEntityHelper {
    private static int _hooksLoaded;

    [HookPreload]
    public static void LoadIfNeeded() {
        if (Interlocked.Exchange(ref _hooksLoaded, 1) != 0)
            return;

        On.Celeste.Level.LoadLevel += Level_LoadLevel;
    }

    [OnUnload]
    public static void Unload() {
        if (Interlocked.Exchange(ref _hooksLoaded, 0) == 0)
            return;

        On.Celeste.Level.LoadLevel -= Level_LoadLevel;
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
        if (FrostMapDataProcessor.GlobalEntityMarkers.TryGetValue(self.Session.Area.SID, out var globalEntities)) {
            // prevent global entity duplication
            const string dynDataAddedGlobalsKey = "frostHelper.addedGlobals";
            var mapDataDynData = new DynamicData(self.Session.MapData.Levels);
            if (mapDataDynData.TryGet(dynDataAddedGlobalsKey, out _)) {
                return;
            }
            mapDataDynData.Set(dynDataAddedGlobalsKey, true);

            var globalRooms = globalEntities.Select(p => p.RoomName).ToList();
            foreach (var item in globalEntities) {
                AddFromMarker(self, item, globalRooms);
            }
        }
    }

    private static void AddFromMarker(Level self, FrostMapDataProcessor.EntityMarker globalEntity, List<string> allGlobalRooms) {
        var globalEntityRoomName = globalEntity.RoomName;
        var container = globalEntity.Marker;
        var mapData = self.Session.MapData;

        bool triggerTriggers = container.AttrBool("triggerTriggers", true);
        HashSet<string> targetRooms = mapData.ParseLevelsList(container.Attr("rooms", "*"));

        var globalRoom = mapData.Levels.FirstOrDefault(data => data.Name == globalEntityRoomName);
        if (globalRoom is null)
            return;

        foreach (var room in self.Session.MapData.Levels) {
            if (room == globalRoom || !targetRooms.Contains(room.Name) || allGlobalRooms.Contains(room.Name))
                continue;

            //Console.WriteLine($"[FrostHelper.Globals] Adding from {globalEntityRoomName} to {room.Name}");

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
