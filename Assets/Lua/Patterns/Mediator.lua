Mediator = class(Base)

function Mediator:ListNotificationInterests()
    return {}
end

function Mediator:HandleNotification(notification)
end

function Mediator:OnRegister()
end

function Mediator:OnRemove()
end

return Mediator
