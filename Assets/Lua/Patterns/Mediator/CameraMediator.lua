CameraMediator = class(Mediator)

function CameraMediator:ListNotificationInterests()
    self:super("ListNotificationInterests")
    return {"PlayerSpawn", "PlayerJumped"}
end

function CameraMediator:HandleNotification(notification)
    self:super("HandleNotification")
    if
        notification.Name == "PlayerSpawn" or
            (notification.Name == "PlayerJumped" and notification.Body.hit:CompareTag("Box"))
     then
        self:CameraMove(notification.Body.player)
    end
end

function CameraMediator:OnRegister()
    self:super("OnRegister")
    LuaFacade.RegisterProxy("Patterns.Proxy.CameraProxy")
    self.cameraProxy = LuaFacade.RetrieveProxy("Patterns.Proxy.CameraProxy")
end

function CameraMediator:OnRemove()
    self:super("OnRemove")
    LuaFacade.RemoveProxy("Patterns.Proxy.CameraProxy")
end

function CameraMediator:CameraMove(player)
    --DoTween控制摄像机移动效果
    self.cameraProxy.camera.transform:DOMove(player.transform.position + self.cameraProxy.cameraRelativePosition, 1)
end
