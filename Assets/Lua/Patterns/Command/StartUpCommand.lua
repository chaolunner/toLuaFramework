StartUpCommand = class(Command)

function StartUpCommand:Execute(notification)
    self:super("Execute")
    LuaFacade.RegisterMediator("Patterns.Mediator.LoadSceneMediator")
    SceneManager.LoadSceneAsync(1)
end

return StartUpCommand
