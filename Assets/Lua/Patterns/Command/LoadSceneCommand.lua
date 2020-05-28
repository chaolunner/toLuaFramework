LoadSceneCommand = class(Command)

function LoadSceneCommand:ctor()
    self.scenes = {}
    self.scenes[1] = {
        Load = function()
            LuaFacade.RegisterMediator("Patterns.Mediator.GameMediator")
            LuaFacade.RegisterMediator("Patterns.Mediator.CameraMediator")
            LuaFacade.RegisterMediator("Patterns.Mediator.BoxMediator")
            LuaFacade.RegisterMediator("Patterns.Mediator.PlayerMediator")
        end,
        Unload = function()
            LuaFacade.RemoveMediator("Patterns.Mediator.GameMediator")
            LuaFacade.RemoveMediator("Patterns.Mediator.CameraMediator")
            LuaFacade.RemoveMediator("Patterns.Mediator.BoxMediator")
            LuaFacade.RemoveMediator("Patterns.Mediator.PlayerMediator")
        end
    }
end

function LoadSceneCommand:Execute(notification)
    self:super("Execute")
    local level = notification.Body.level

    if self.lastLevel and self.scenes[self.lastLevel] then
        self.scenes[self.lastLevel].Unload()
    end

    if level == 0 and self.lastLevel then
        SceneManager.LoadSceneAsync(self.lastLevel)
    elseif self.scenes[level] then
        self.scenes[level].Load()
    end
    self.lastLevel = level
end

return LoadSceneCommand
