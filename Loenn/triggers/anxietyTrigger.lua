---@class AnxietyTrigger : Trigger

---@type TriggerHandler<AnxietyTrigger>
local anxietyTrigger = {}
anxietyTrigger.name = "FrostHelper/AnxietyTrigger"
anxietyTrigger.category = "visual"
anxietyTrigger.placements = {
    name = "normal",
    data = {
        multiplyer = 1.0,
    }
}

return anxietyTrigger