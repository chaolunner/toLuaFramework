HotUpdate = class()

local Text = require("TMPro.TMP_Text")
local LuaUIEventListener = require("LuaUIEventListener")
local EventTriggerType = require("UnityEngine.EventSystems.EventTriggerType")

function HotUpdate:ctor(...)
    self.isInitialized = false
end

local function getUnit(unit)
    if unit == 0 then
        return "B"
    elseif unit == 1 then
        return "KB"
    elseif unit == 2 then
        return "MB"
    elseif unit == 3 then
        return "GB"
    else
        return "TB"
    end
end

function HotUpdate:Initialize(downloadSize)
    self.unit = 0
    self.result = nil
    self.isInitialized = false
    self.downloadSize = int64.tonum2(downloadSize)
    while self.downloadSize >= 1024 and self.unit < 4 do
        self.downloadSize = self.downloadSize / 1024
        self.unit = self.unit + 1
    end
    coroutine.start(HotUpdate.InitializeAsync, self)
end

function HotUpdate:InitializeAsync()
    local canvasHandle = LuaAddressables.LoadAssetAsync("Internal/Prefabs/HotUpdateCanvas.prefab")
    local eventSystemHandle = LuaAddressables.LoadAssetAsync("Internal/Prefabs/EventSystem.prefab")
    while not canvasHandle.IsDone or not eventSystemHandle.IsDone do
        coroutine.step()
    end
    self.canvas = GameObject.Instantiate(canvasHandle.Result)
    self.eventSystem = GameObject.Instantiate(eventSystemHandle.Result)
    self.animator = self.canvas:GetComponent("Animator")
    self.content = self.canvas.transform:Find("PopupMenu/Content"):GetComponent(typeof(Text))
    self.text = self.canvas.transform:Find("Progress/Text"):GetComponent(typeof(Text))
    self.okButton = self.canvas.transform:Find("PopupMenu/OkButton").gameObject
    self.cancelButton = self.canvas.transform:Find("PopupMenu/CancelButton").gameObject
    self:bindButton(self.okButton, "Agree")
    self:bindButton(self.cancelButton, "Disagree")
    local unit = getUnit(self.unit)
    self.content.text = string.format("Do you want to download the update [%.2f%s] ?", self.downloadSize, unit)
    self.text.text = string.format("0%s/%.2f%s", unit, self.downloadSize, unit)
    self.isInitialized = true
end

function HotUpdate:bindButton(btn, evt)
    local listener = GetOrCreateComponent(btn, typeof(LuaUIEventListener))
    listener:AddEntry(EventTriggerType.PointerClick)
    listener:AddListener(self[evt], self)
end

function HotUpdate:Agree()
    self.result = true
end

function HotUpdate:Disagree()
    self.result = false
    print("Disagree")
end

function HotUpdate:Response()
    if not self.isInitialized then
        return false
    end
    self.animator:SetInteger("State", 1)
    if self.result == nil then
        return false
    end
    if self.result then
        self.animator:SetInteger("State", 2)
    else
        self.animator:SetInteger("State", 0)
    end
    return true
end

function HotUpdate:Result()
    return self.result
end

function HotUpdate:Download(downloadedSize, downloadSize)
    if not self.isInitialized then
        return
    end
    local unit = getUnit(self.unit)
    local size = int64.tonum2(downloadedSize)
    for i = 1, self.unit do
        size = size / 1024
    end
    self.animator:SetFloat("Progress", size / self.downloadSize)
    self.text.text = string.format("%.2f%s/%.2f%s", size, unit, self.downloadSize, unit)
end

function HotUpdate:OnDestroy()
    GameObject.Destroy(self.canvas)
    GameObject.Destroy(self.eventSystem)
end

return HotUpdate
