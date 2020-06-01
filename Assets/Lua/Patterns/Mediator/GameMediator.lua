GameMediator = class(Mediator)

require("UnityEngine.EventSystems.BaseEventData")
local GameView = require("Views.GameView")
local LuaUIEventListener = require("LuaUIEventListener")
local EventTriggerType = require("UnityEngine.EventSystems.EventTriggerType")

function GameMediator:ListNotificationInterests()
    self:super("ListNotificationInterests")
    return {"GameOver"}
end

function GameMediator:HandleNotification(notification)
    self:super("HandleNotification")
    if notification.Name == "GameOver" then
        self.gameView:OpenEndMenu(self.gameProxy.score)
        self.gameProxy.score = 0
    end
end

function GameMediator:OnRegister()
    self:super("OnRegister")
    LuaFacade.RegisterProxy("Patterns.Proxy.GameProxy")
    self.gameProxy = LuaFacade.RetrieveProxy("Patterns.Proxy.GameProxy")
    self.gameView = GameView.new()
    self.gameView:Initialize()
    coroutine.start(GameMediator.InitializeAsync, self)
end

function GameMediator:OnRemove()
    self:super("OnRemove")
    LuaFacade.RemoveProxy("Patterns.Proxy.GameProxy")
end

function GameMediator:bindButton(btn, evt)
    local listener = GetOrCreateComponent(self.gameView[btn], typeof(LuaUIEventListener))
    listener:AddEntry(EventTriggerType.PointerClick)
    listener:AddListener(self[evt], self)
end

function GameMediator:InitializeAsync()
    while not self.gameView.isInitialized do
        coroutine.step()
    end
    self:bindButton("startButton", "OnGameStart")
    self:bindButton("exitButton", "OnGameExit")
    self:bindButton("restartButton", "OnPlayAgain")
end

function GameMediator:OnGameStart(type, evtData)
    if type == EventTriggerType.PointerClick then
        self.gameView:CloseStartMenu()
        LuaFacade.SendNotification("GameStart")
    end
end

function GameMediator:OnGameExit(type, evtData)
    if type == EventTriggerType.PointerClick then
        UnityEngine.Application.Quit()
    end
end

function GameMediator:OnPlayAgain(type, evtData)
    if type == EventTriggerType.PointerClick then
        self.gameView:OpenStartMenu()
    end
end

return GameMediator
