CameraProxy = class(Proxy)

function CameraProxy:OnRegister()
    self:super("OnRegister")
    self.camera = Camera.main
    self.cameraRelativePosition = Vector3.zero
end

function CameraProxy:OnRemove()
    self:super("OnRemove")
end

return CameraProxy