LoadSceneMediator = class(Mediator)

local LoadSceneView = require("Views.LoadSceneView")

function LoadSceneMediator:ListNotificationInterests()
    self:super("ListNotificationInterests")
    return {}
end

function LoadSceneMediator:HandleNotification(notification)
    self:super("HandleNotification")
end

function LoadSceneMediator:OnRegister()
    self:super("OnRegister")
    self.loadSceneView = LoadSceneView.new()
    self.loadSceneView:Initialize()
    coroutine.start(LoadSceneMediator.InitializeAsync, self)
end

function LoadSceneMediator:OnRemove()
    self:super("OnRemove")
end

function LoadSceneMediator:InitializeAsync()
    while not self.loadSceneView.isInitialized do
        coroutine.step()
    end
end

return LoadSceneMediator
