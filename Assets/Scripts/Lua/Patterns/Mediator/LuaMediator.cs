using PureMVC.Patterns.Mediator;
using PureMVC.Interfaces;
using LuaInterface;

public class LuaMediator : Mediator
{
    private LuaTable mediatorClass;
    private LuaTable mediatorObject;

    public LuaTable Mediator
    {
        get { return mediatorObject; }
    }

    public LuaMediator(string mediatorName, object viewComponent = null) : base(mediatorName, viewComponent)
    {
        mediatorClass = LuaFacade.GetTable(mediatorName);
        mediatorObject = LuaFacade.New(mediatorName);
    }

    public override string[] ListNotificationInterests()
    {
        return mediatorClass.Invoke<LuaTable, string[]>("ListNotificationInterests", mediatorObject);
    }

    public override void HandleNotification(INotification notification)
    {
        mediatorClass.Call("HandleNotification", mediatorObject, notification);
    }

    public override void OnRegister()
    {
        mediatorClass.Call("OnRegister", mediatorObject);
    }

    public override void OnRemove()
    {
        mediatorClass.Call("OnRemove", mediatorObject);
        mediatorClass.Dispose();
        mediatorObject.Dispose();
    }
}
