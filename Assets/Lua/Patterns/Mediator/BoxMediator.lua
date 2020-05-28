BoxMediator = class(Mediator)

local KeyCode = UnityEngine.KeyCode
local minDistance = 1.5
local maxDistance = 3

function BoxMediator:ListNotificationInterests()
    self:super("ListNotificationInterests")
    return {"PlayerSpawn", "PlayerJumped"}
end

function BoxMediator:HandleNotification(notification)
    self:super("HandleNotification")
    if notification.Name == "PlayerSpawn" then
        self.boxProxy:Clear()
        self.boxProxy.currentBox = nil
        self:UpdateBox()
    elseif notification.Name == "PlayerJumped" then
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
            LuaFacade.SendNotification(
                "ColorChanged",
                {color = self.boxProxy.boxDataMap[self.boxProxy.currentBox].color}
            )
        else
            LuaFacade.SendNotification("GameOver")
        end
    end
end

function BoxMediator:OnRegister()
    self:super("OnRegister")
    LuaFacade.RegisterProxy("Patterns.Proxy.BoxProxy")
    self.boxProxy = LuaFacade.RetrieveProxy("Patterns.Proxy.BoxProxy")
    table.insert(self.boxProxy.OnInitCompleted, Action.new(self, BoxMediator.OnInitCompleted))
end

function BoxMediator:OnRemove()
    self:super("OnRemove")
    LuaFacade.RemoveProxy("Patterns.Proxy.BoxProxy")
end

function BoxMediator:OnInitCompleted()
    self.boxProxy:Clear()
    self.OnUpdate = UpdateBeat:CreateListener(BoxMediator.Update, self)
    UpdateBeat:AddListener(self.OnUpdate)
end

function BoxMediator:Update()
    if Input.GetKeyDown(KeyCode.Space) then
        self.startTime = Time.time
    end
    if Input.GetKeyUp(KeyCode.Space) then
        self.boxProxy.currentBox.transform:DOLocalMoveY(0.25, 0.2)
        self.boxProxy.currentBox.transform:DOScale(self.boxProxy.boxDataMap[self.boxProxy.currentBox].scale, 0.2)
    end
    if Input.GetKey(KeyCode.Space) then
        if
            self.boxProxy.currentBox.transform.localScale.y < 0.51 and
                self.boxProxy.currentBox.transform.localScale.y > 0.3
         then
            self.boxProxy.currentBox.transform.localScale =
                self.boxProxy.currentBox.transform.localScale + Vector3(0, -1, 0) * 0.15 * Time.deltaTime
            self.boxProxy.currentBox.transform.localPosition =
                self.boxProxy.currentBox.transform.localPosition + Vector3(0, -1, 0) * 0.15 * Time.deltaTime
        end
    end
end

function BoxMediator:UpdateBox()
    --盒子随机位置、大小、颜色
    local randomScale = Random.Range(0.5, 1)
    local box = self.boxProxy.pool:Pop()
    if self.boxProxy.currentBox == nil then
        box.transform.position = Vector3(0, 0.25, 0)
    else
        box.transform.position =
            self.boxProxy.currentBox.transform.position + Vector3(Random.Range(minDistance, maxDistance), 0, 0)
    end
    box.transform.localScale = Vector3(randomScale, 0.5, randomScale)
    local renderer = box:GetComponent("Renderer")
    local propertyBlock = MaterialPropertyBlock()
    renderer:GetPropertyBlock(propertyBlock)
    if self.boxProxy.boxDataMap[box] == nil then
        self.boxProxy.boxDataMap[box] = {}
    end
    self.boxProxy.boxDataMap[box].scale = box.transform.localScale
    self.boxProxy.boxDataMap[box].color = Color(Random.Range(0.0, 1.0), Random.Range(0.0, 1.0), Random.Range(0.0, 1.0))
    propertyBlock:SetColor("_BaseColor", self.boxProxy.boxDataMap[box].color)
    renderer:SetPropertyBlock(propertyBlock)
    box:SetActive(true)
end
