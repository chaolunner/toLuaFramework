GameMediator = class(Mediator)

function GameMediator:ListNotificationInterests()
    self:super("ListNotificationInterests")
    return {"PlayerSpawn", "GameOver"}
end

function GameMediator:HandleNotification(notification)
    self:super("HandleNotification")
    if notification.Name == "PlayerSpawn" then
    elseif notification.Name == "GameOver" then
    end
end

function GameMediator:OnRegister()
    self:super("OnRegister")
    LuaFacade.RegisterProxy("Patterns.Proxy.GameProxy")
    self.gameProxy = LuaFacade.RetrieveProxy("Patterns.Proxy.GameProxy")
end

function GameMediator:OnRemove()
    self:super("OnRemove")
end
