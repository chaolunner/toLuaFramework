using PureMVC.Patterns.Mediator;
using PureMVC.Interfaces;

public class LuaMediator : Mediator
{
    public LuaMediator(string mediatorName, object viewComponent = null) : base(mediatorName, viewComponent)
    {
    }

    public override string[] ListNotificationInterests()
    {
        return LuaFacade.Require(MediatorName).Invoke<string[]>("ListNotificationInterests");
    }

    public override void HandleNotification(INotification notification)
    {
        LuaFacade.Require(MediatorName).Call("HandleNotification", notification);
    }

    public override void OnRegister()
    {
        LuaFacade.Require(MediatorName).Call("OnRegister");
    }

    public override void OnRemove()
    {
        LuaFacade.Require(MediatorName).Call("OnRemove");
    }
}
