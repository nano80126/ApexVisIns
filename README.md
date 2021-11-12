
# Apex Physical Appearance Inspection 

Apex 外觀瑕疵檢測 

### 檢測項目

* 耳朵
	* 毛邊
	* 銑銷不良


* 窗戶
	* 毛邊


* 管件
	* 亮紋
	* 坑洞
	* 黃斑
	* 劃記
	* 壓傷
	* 色差


## 確認相機DELAY問題


### 待新增 API

[x] 光源控制器 (RS232), 保留可呼叫物件
[ ] 


### 待移除

* Thermometer (USE <=> RS485)
*

### Tab

* Main Tab
	* Control Panel
	* Image Panel
	* Record Panel
	* Procedure Panel
* Config Tab
	* 新增相機
	* 相機Config
* Debug Tab
	* For Programming



### TO DO LIST

[ ] Change color of scoll bar
[ ] 移除 UVC / COIL 內容 
[ ] 載入初始 Camera config from .json
	* 需要建立 class for json object
[ ] 建立樹狀圖 (draw.io) 
	* DataContext
[x] 確認 BaslerFunc 是否可以控制 MainWindow.ImageSource
	* 否 => 拆除移至Toolbar.cs
[x] DebugTab 綁定回BaslerCam
	* 一次只會有一台相機
[x] Config 根據型號儲存
[ ] 移除 Thermoter ()
[ ] 移除舊Tab
[ ] Read/Write UserSet
[ ] 組態列表儲存 JSON
	* Model
	* S/N
	* Character
[ ] Hotkey 功能恢復
[ ] 寫入Config前，歸零Offset


### Config Logic

* Camera connected => Updata camera property => update camera config property

* Config Panel open => load json file in directory 

* Config Panel Close => synchronize config with camera
 
* Config Save => Save button click => save json file 

* Config Write => Write config to camera

### Know How

* Popupbox 初始化時不會生成

### Performance Test

* 比較 Task 和 ThreadPool 校能差別

### BUGS

* DebugTab
	 * ConfigPanel 比較邏輯需更改 (Textbox & Config)
* Read UserSet Center X, Center Y 紀錄會失效

###  RELEASE MODE 

* [ ] 程式碼優化過(release mode)，FPS才不會掉張
	* 機率性