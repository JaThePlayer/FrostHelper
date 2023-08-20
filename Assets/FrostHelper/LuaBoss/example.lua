---@diagnostic disable: undefined-global
local vanillaPatterns = loadCelesteAsset("Assets/FrostHelper/LuaBoss/vanillaPatterns")

local function customBeams()
    while true do
        beam()
        beam({
            followTime = 0.5,
            lockTime = 0.,
            --activeTime = 1,
            rotationSpeed = 20000
        })
    end
end

local function customBullets()
    local offset = 0

    while true do
        shoot({
            angle = 0 + offset,
        })
        shoot({
            angle = 90 + offset,
        })
        shoot({
            angle = 180 + offset,
        })
        shoot({
            angle = 270 + offset,
        })

        wait(0.50)

        offset = offset + 45
    end
end

local function coroutines()
    -- the code inside the callback will run in the background without blocking the attacking ai.
    -- the coroutine will automatically end once the boss gets hit.
    startCoroutine(function ()
        while true do
            setFlag("dark", true)
            wait(1)
            setFlag("dark", false)
            wait(1)
        end
    end)

    while true do
        beginCharge()
        wait(0.15)

        shoot({
            angleOffset = 30,
        })
        shoot({
            angleOffset = -30,
        })
        shoot({
            waveStrength = 12,
        })

        wait(1)
    end
end

local ais = {
    customBeams,
    customBullets,
    vanillaPatterns.sequence14,
    coroutines,
}

function ai()
    setFlag("dark", false)
    local aiFunc = ais[nodeIndex + 1]
    if aiFunc then
        aiFunc()
    end
end

function onEnd()
    setFlag("dark", false)
end

function onHit()
    wait(0.25)

    if isFinalNode then
        shatterSpinners()
        shatterSpinners({
            types = { "refill" }
        })
    else
        shatterSpinners({
            types = { "FrostHelper/IceSpinner" },
            filter = function (spinner)
                return spinner.AttachGroup == nodeIndex - 1
            end
        })
    end
end