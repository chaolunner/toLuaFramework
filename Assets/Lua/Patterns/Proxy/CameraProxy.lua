CameraProxy = class(Proxy)

function CameraProxy:OnRegister()
    self:super("OnRegister")
    self.camera = Camera.main
    self.cameraRelativePosition = Vector3(-1.295596, 1.46617, -2.4779)
end

function CameraProxy:OnRemove()
    self:super("OnRemove")
end

return CameraProxy
