StartUpCommand = class(Command)

function StartUpCommand:Execute(notification)
    self:super("Execute")
    SceneManager.LoadSceneAsync(1)
end

return StartUpCommand
