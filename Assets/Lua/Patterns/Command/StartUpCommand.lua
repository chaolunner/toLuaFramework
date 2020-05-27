StartUpCommand = class(Command)

function StartUpCommand:Execute(notification)
    self:super("Execute")
    LuaFacade.RegisterMediator("Patterns.Mediator.GameMediator")
    LuaFacade.RegisterMediator("Patterns.Mediator.CameraMediator")
    LuaFacade.RegisterMediator("Patterns.Mediator.BoxMediator")
    LuaFacade.RegisterMediator("Patterns.Mediator.PlayerMediator")
end

return StartUpCommand
