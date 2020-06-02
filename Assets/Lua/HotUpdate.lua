HotUpdate = class()

local Text = require("TMPro.TMP_Text")

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
    self.downloadSize = int64.tonum2(downloadSize)
    self.unit = 0
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
    local unit = getUnit(self.unit)
    self.content.text = string.format("Do you want to download the update [%.2f%s] ?", self.downloadSize, unit)
    self.text.text = string.format("0%s/%.2f%s", unit, self.downloadSize, unit)
    self.isInitialized = true
end

function HotUpdate:Request()
    return true
end

function HotUpdate:Result()
    return true
end

function HotUpdate:Download(downloadedSize, downloadSize)
end

return HotUpdate
