BoxMediator = class(Mediator)
local minDistance = 1.5
local maxDistance = 3

function BoxMediator:ListNotificationInterests()
    self:super("ListNotificationInterests")
    return {"PlayerJumped", "GameOver"}
end

function BoxMediator:HandleNotification(notification)
    self:super("HandleNotification")
    if notification.Name == "PlayerJumped" then
        if notification.Body.hit:CompareTag("Box") then
            local currentBox = self.boxProxy.currentBox
            self.boxProxy.currentBox = notification.Body.hit
            if currentBox == nil then
                self:UpdateBox()
            elseif currentBox == notification.Body.hit then
                LuaFacade.SendNotification("GameOver")
            else
                self:UpdateBox()
            end
        else
            LuaFacade.SendNotification("GameOver")
        end
    elseif notification.Name == "GameOver" then
        self.boxProxy.pool:Clear()
    end
end

function BoxMediator:OnRegister()
    self:super("OnRegister")
    LuaFacade.RegisterProxy("Patterns.Proxy.BoxProxy")
    self.boxProxy = LuaFacade.RetrieveProxy("Patterns.Proxy.BoxProxy")
    table.insert(self.boxProxy.OnGenerateBox, Action.new(self, BoxMediator.GenerateBox))
end

function BoxMediator:GenerateBox()
    table.insert(self.boxProxy.pool.OnClear, Action.new(self, BoxMediator.ClearBox))
    self.boxProxy.pool:Clear()
    self:UpdateBox()
end

function BoxMediator:ClearBox(boxs)
    for k, v in pairs(boxs) do
        v:SetActive(false)
    end
end

function BoxMediator:UpdateBox()
    --盒子随机位置、大小、颜色
    local randomScale = Random.Range(0.5, 1)
    local box = self.boxProxy.pool:Pop()
    if self.boxProxy.currentBox == nil then
        box.transform.position = Vector3(0, 0, 0)
    else
        box.transform.position =
            self.boxProxy.currentBox.transform.position + Vector3(Random.Range(minDistance, maxDistance), 0, 0)
    end
    box.transform.localScale = Vector3(randomScale, 0.5, randomScale)
    local renderer = box:GetComponent("Renderer")
    local propertyBlock = MaterialPropertyBlock()
    renderer:GetPropertyBlock(propertyBlock)
    propertyBlock:SetColor("_BaseColor", Color(Random.Range(0.0, 1.0), Random.Range(0.0, 1.0), Random.Range(0.0, 1.0)))
    renderer:SetPropertyBlock(propertyBlock)
    box:SetActive(true)
end
