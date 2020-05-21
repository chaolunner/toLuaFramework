Array = class(Base)

function Array:ctor(...)
    self.elements = {...}
end

function Array:getLength()
    local length = 0
    for k, v in pairs(self.elements) do
        length = length + 1
    end
    return length
end

function Array:Contains(obj)
    for k, v in pairs(self.elements) do
        if v == obj then
            return true
        end
    end
    return false
end

function Array:Add(obj)
    if self:Contains(obj) then
    else
        table.insert(self.elements, obj)
    end
end

function Array:Remove(obj)
    local i = 1
    for k, v in pairs(self.elements) do
        if v == obj then
            self:RemoveAt(i)
        end
        i = i + 1
    end
end

function Array:RemoveAt(pos)
    table.remove(self.elements, pos)
end

function Array:Clear(pos)
    local i = 1
    for k, v in pairs(self.elements) do
        self:RemoveAt(i)
        i = i + 1
    end
end

return Array
