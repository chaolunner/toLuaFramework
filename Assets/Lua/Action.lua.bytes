Action = class()

function Action:ctor(obj, func)
    self.obj = obj
    self.func = func
end

function Action:Invoke(...)
    if self.obj and self.func then
        self.func(self.obj, ...)
    end
end

return Action
