local compat = {}

-- whether we're currently running in Lönn
---@diagnostic disable-next-line: undefined-global
compat.inLonn = (_MAP_VIEWER == nil) and true or false
-- whether we're currently running in Rysy
---@diagnostic disable-next-line: undefined-global
compat.inRysy = (_MAP_VIEWER and _MAP_VIEWER.name == "rysy") or false

return compat