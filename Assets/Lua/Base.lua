Base = class()

local function findAllProperties(t)
    local get_props = {}
    local set_props = {}
    for k, v in pairs(t) do
        if k == nil or v == nil then
        elseif "function" == type(v) and k:sub(1, 3) == "get" then
            get_props[k:sub(4):gsub("^%u", string.lower)] = v
        elseif "function" == type(v) and k:sub(1, 3) == "set" then
            set_props[k:sub(4):gsub("^%u", string.lower)] = v
        end
    end
    return get_props, set_props
end

function Base:init()
    local t = getmetatable(self).__index
    local get_props, set_props = findAllProperties(t)
    setmetatable(
        self,
        {
            __index = function(self, k)
                if t[k] then
                    return t[k]
                elseif "number" == type(k) and self.elements then
                    return self.elements[k]
                elseif get_props[k] then
                    return get_props[k](self)
                else
                    return nil
                end
            end,
            __newindex = function(self, k, v)
                if get_props[k] then
                    if set_props[k] then
                        set_props[k](self, v)
                    else
                        Debugger.LogError("For the <b>" .. k .. "</b> property, you have implemented get" .. k:gsub("^%l", string.upper) .. "() function. If you want to assign it, please implement <b>set" .. k:gsub("^%l", string.upper) .. "()</b> function.")
                    end
                elseif "number" == type(k) and self.elements then
                    self.elements[k] = v
                else
                    t[k] = v
                end
            end
        }
    )
end

return Base
