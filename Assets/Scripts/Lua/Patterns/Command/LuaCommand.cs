using PureMVC.Patterns.Command;
using PureMVC.Interfaces;
using LuaInterface;

public class LuaCommand : SimpleCommand
{
    private LuaTable commandClass;
    private LuaTable commandObject;

    public LuaTable Command
    {
        get { return commandObject; }
    }

    public string CommandName { get; protected set; }

    public LuaCommand(string commandName)
    {
        CommandName = commandName;
        commandClass = LuaFacade.GetTable(commandName);
        commandObject = commandClass.Invoke<LuaTable>("new");
    }

    public override void Execute(INotification notification)
    {
        commandClass.Call("Execute", commandObject, notification);
    }
}
