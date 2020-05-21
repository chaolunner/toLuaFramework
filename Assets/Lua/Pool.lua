Pool = class()

function Pool:ctor()
    self.OnClear = {}
    self.OnLack = {}
    self.active = Array.new()
    self.inactive = Array.new()
end

function Pool:getFirst()
    return self.active[1]
end

function Pool:getLast()
    return self.active[self.active.length]
end

function Pool:Push(item)
    self.active:Remove(item)
    self.inactive:Add(item)
end

function Pool:Pop()
    local item = self.inactive[1]
    if item then
        self.active:Add(item)
        self.inactive:RemoveAt(1)
    else
        for k, v in ipairs(self.OnLack) do
            item = v:Invoke()
        end
        if item == nil then
            self:Push(self:getFirst())
            item = self:Pop()
        end
    end
    return item
end

function Pool:Clear()
    while self.active.length > 0 do
        self:Push(self.active[1])
    end
    if self.OnClear then
        for k, v in ipairs(self.OnClear) do
            v:Invoke(self.inactive.elements)
        end
    end
end

return Pool
