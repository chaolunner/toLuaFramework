GameMediator = class(Mediator)

require("UnityEngine.EventSystems.BaseEventData")
local GameView = require("Views.GameView")
local LuaUIEventListener = require("LuaUIEventListener")
local EventTriggerType = require("UnityEngine.EventSystems.EventTriggerType")

function GameMediator:ListNotificationInterests()
    self:super("ListNotificationInterests")
    return {"PlayerJumped", "GameOver"}
end

function GameMediator:HandleNotification(notification)
    self:super("HandleNotification")
    if notification.Name == "PlayerJumped" then
        self.score = self.score + 1
    elseif notification.Name == "GameOver" then
        self.gameView:OpenEndMenu(self.score - 2)
        self.score = 0
    end
end

function GameMediator:OnRegister()
    self:super("OnRegister")
    self.score = 0
    self.gameView = GameView.new()
    table.insert(self.gameView.OnInitCompleted, Action.new(self, GameMediator.OnInitCompleted))
end

function GameMediator:OnRemove()
    self:super("OnRemove")
end

function GameMediator:bindButton(btn, evt)
    local listener = GetOrCreateComponent(self.gameView[btn], typeof(LuaUIEventListener))
    listener:AddEntry(EventTriggerType.PointerClick)
    listener:AddListener(self[evt], self)
end

function GameMediator:OnInitCompleted()
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
        SceneManager.LoadSceneAsync(0)
    end
end

function GameMediator:OnPlayAgain(type, evtData)
    if type == EventTriggerType.PointerClick then
        self.gameView:OpenStartMenu()
    end
end
