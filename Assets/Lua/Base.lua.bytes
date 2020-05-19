Base = class()

local function findAllProperties(t)
    local props = {}
    for k, v in pairs(t) do
        if k == nil or v == nil then
        elseif "function" == type(v) and k:sub(1, 3) == "get" then
            props[k:sub(4):gsub("^%u", string.lower)] = v
        end
    end
    return props
end

function Base:init()
    local t = getmetatable(self).__index
    local props = findAllProperties(t)
    setmetatable(
        self,
        {
            __index = function(self, k)
                if t[k] then
                    return t[k]
                elseif "number" == type(k) and self.elements then
                    return self.elements[k]
                elseif props[k] then
                    return props[k](self)
                else
                    return nil
                end
            end
        }
    )
end

return Base
