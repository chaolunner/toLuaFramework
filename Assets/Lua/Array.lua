Array = class(Base)

function Array:ctor(...)
    self.len = select("#", ...)
    self.elements = {...}
end

function Array:getLength()
    return self.len
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
        self.len = self.len + 1
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
    self.len = self.len - 1
    table.remove(self.elements, pos)
end

function Array:Clear(pos)
    local i = 1
    for k, v in pairs(self.elements) do
        self:RemoveAt(i)
        i = i + 1
    end
    self.len = 0
end

return Array
