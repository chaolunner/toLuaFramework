using PureMVC.Patterns.Command;
using PureMVC.Interfaces;

public class LuaCommand : SimpleCommand
{
    public string CommandName { get; protected set; }

    public LuaCommand(string commandName)
    {
        CommandName = commandName;
    }

    public override void Execute(INotification notification)
    {
        LuaFacade.Require(CommandName).Call("Execute", notification);
    }
}
