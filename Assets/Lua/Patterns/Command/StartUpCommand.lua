StartUpCommand = class(Command)

function StartUpCommand:Execute(notification)
    self:super("Execute")
    LuaFacade.RegisterMediator("Patterns.Mediator.LoadSceneMediator")
    LuaFacade.SendNotification(
        "SetupScene",
        {
            name = "Jump_Jump",
            load = function()
                LuaFacade.RegisterMediator("Patterns.Mediator.GameMediator")
                LuaFacade.RegisterMediator("Patterns.Mediator.CameraMediator")
                LuaFacade.RegisterMediator("Patterns.Mediator.BoxMediator")
                LuaFacade.RegisterMediator("Patterns.Mediator.PlayerMediator")
            end,
            unload = function()
                LuaFacade.RemoveMediator("Patterns.Mediator.GameMediator")
                LuaFacade.RemoveMediator("Patterns.Mediator.CameraMediator")
                LuaFacade.RemoveMediator("Patterns.Mediator.BoxMediator")
                LuaFacade.RemoveMediator("Patterns.Mediator.PlayerMediator")
            end
        }
    )
    LuaFacade.SendNotification("LoadScenes", {1})
end

function StartUpCommand:OnRemove()
    self:super("OnRemove")
    LuaFacade.RemoveMediator("Patterns.Mediator.LoadSceneMediator")
end

return StartUpCommand
