--[[
    All vanilla patterns, translated to lua.
    Can be included in your boss file via
    local vanillaPatterns = loadCelesteAsset("Assets/FrostHelper/LuaBoss/vanillaPatterns")
    Then you can call them like this:
    vanillaPatterns.sequence02()
]]

local vanillaPatterns = {}

function vanillaPatterns.sequence01()
    beginCharge()
    while true do
        wait(0.5)
        shoot()
        wait(1)
        beginCharge()
        wait(0.15)
        wait(0.3)
    end
end

function vanillaPatterns.sequence02()
    while true do
        wait(0.5)
        beam()
        wait(0.4)
        beginCharge()
        wait(0.3)
        shoot()
        wait(0.5)
        wait(0.3)
    end
end

function vanillaPatterns.sequence03()
    beginCharge()
    wait(0.1)
    while true do
        for i = 0, 4, 1 do
            if player then
                local at = player.Center
                shootAt(at)
                wait(0.15)
                shootAt(at)
                wait(0.15)
            end
            if i < 4 then
                beginCharge()
                wait(0.5)
            end
        end
        wait(2)
        beginCharge()
        wait(0.7)
    end
end

function vanillaPatterns.sequence04()
    beginCharge()
    wait(0.1)
    while true do
        for i = 0, 4, 1 do
            if player then
                local at = player.Center
                shootAt(at)
                wait(0.15)
                shootAt(at)
                wait(0.15)
            end
            if i < 4 then
                beginCharge()
                wait(0.5)
            end
        end
        wait(1.5)
        beam()
        wait(1.5)
        beginCharge()
    end
end

function vanillaPatterns.sequence05()
    wait(0.2)
    while true do
        beam()
        wait(0.6)
        beginCharge()
        wait(0.3)
        for i = 0, 2, 1 do
            if player then
                local at = player.Center
                shootAt(at)
                wait(0.15)
                shootAt(at)
                wait(0.15)
            end
            if i < 2 then
                beginCharge()
                wait(0.5)
            end
        end
        wait(0.8)
    end
end

function vanillaPatterns.sequence06()
    while true do
        beam()
        wait(0.7)
    end
end

function vanillaPatterns.sequence07()
    while true do
        shoot()
        wait(0.8)
        beginCharge()
        wait(0.8)
    end
end

function vanillaPatterns.sequence08()
    while true do
        wait(0.1)
        beam()
        wait(0.8)
    end
end

function vanillaPatterns.sequence09()
    beginCharge()
    while true do
        wait(0.5)
        shoot()
        wait(0.15)
        beginCharge()
        shoot()
        wait(0.4)
        beginCharge()
        wait(0.1)
    end
end

function vanillaPatterns.sequence10()

end

function vanillaPatterns.sequence11()
    if nodeIndex == 0 then
        beginCharge()
        wait(0.6)
    end
    while true do
        shoot()
        wait(1.9)
        beginCharge()
        wait(0.6)
    end
end

function vanillaPatterns.sequence13()
    if nodeIndex == 0 then
        return
    end

    vanillaPatterns.Attack01Sequence()
end

function vanillaPatterns.sequence14()
    while true do
        wait(0.2)
        beam()
        wait(0.3)
    end
end

function vanillaPatterns.sequence15()
    while true do
        wait(0.2)
        beam()
        wait(1.2)
    end
end

return vanillaPatterns