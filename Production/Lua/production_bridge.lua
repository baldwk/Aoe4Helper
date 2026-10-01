-- Loaded once through the game's developer Lua debugger.
-- Subsequent requests are consumed by a game rule; no key or mouse input is involved.
return function(mailbox)
    if Aoe4HelperBridge_Tick then Rule_Remove(Aoe4HelperBridge_Tick) end

    local lastId = nil
    local function quote(value)
        local escaped = tostring(value):gsub('[%z\1-\31\\"]', function(c)
            if c == '\\' then return '\\\\' end
            if c == '"' then return '\\"' end
            return string.format('\\u%04x', string.byte(c))
        end)
        return '"' .. escaped .. '"'
    end

    local function buildings()
        local player = Game_GetLocalPlayer()
        local group = Player_GetEntities(player)
        local result = {}
        for i = 1, EGroup_CountSpawned(group) do
            local entity = EGroup_GetSpawnedEntityAt(group, i)
            if Entity_HasProductionQueue(entity) then result[#result + 1] = entity end
        end
        return player, result
    end

    local function catalog()
        local player, entities = buildings()
        local race = Player_GetRace(player)
        local units, rows = {}, {}
        for i = 0, World_GetPossibleSquadsCount(race) - 1 do
            local bp = World_GetPossibleSquadsBlueprint(race, i)
            units[#units + 1] = quote(BP_GetName(bp))
        end
        for _, entity in ipairs(entities) do
            rows[#rows + 1] = '{"EntityId":' .. Entity_GetID(entity)
                .. ',"OwnerId":' .. Player_GetID(player)
                .. ',"BlueprintName":' .. quote(BP_GetName(Entity_GetBlueprint(entity))) .. '}'
        end
        return '{"Race":' .. quote(Player_GetRaceName(player))
            .. ',"UnitNames":[' .. table.concat(units, ',') .. ']'
            .. ',"Entities":[' .. table.concat(rows, ',') .. ']}'
    end

    local function produce(request)
        local accepted = 0
        local bp = BP_GetSquadBlueprint(request.unit)
        for n = 1, request.count do
            local _, entities = buildings()
            local candidates = {}
            for _, entity in ipairs(entities) do
                if BP_GetName(Entity_GetBlueprint(entity)) == request.building
                    and Entity_IsProductionQueueAvailable(entity) then
                    candidates[#candidates + 1] = entity
                end
            end
            table.sort(candidates, function(a, b)
                local aq, bq = Entity_GetProductionQueueSize(a), Entity_GetProductionQueueSize(b)
                if aq == bq then return Entity_GetID(a) < Entity_GetID(b) end
                return aq < bq
            end)
            if #candidates == 0 then break end
            if not Entity_QueueProductionItemByPBG(candidates[1], PITEM_Spawn, bp) then break end
            accepted = accepted + 1
        end
        return accepted
    end

    function Aoe4HelperBridge_Tick()
        local chunk = loadfile(mailbox)
        if not chunk then return end
        local loaded, request = pcall(chunk)
        if not loaded or type(request) ~= 'table' or request.id == lastId then return end
        lastId = request.id
        if request.kind == 'stop' then Rule_Remove(Aoe4HelperBridge_Tick); return end
        if request.kind ~= 'produce' then return end
        local ok, accepted = pcall(produce, request)
        if ok then
            print('AOE4HELPER_RESULT|' .. request.id .. '|OK|' .. accepted .. '|' .. request.count)
        else
            local message = tostring(accepted):gsub('[\r\n|]', ' ')
            print('AOE4HELPER_RESULT|' .. request.id .. '|ERROR|' .. message)
        end
    end

    Rule_AddInterval(Aoe4HelperBridge_Tick, 0.25)
    return catalog()
end
