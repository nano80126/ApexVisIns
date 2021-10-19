﻿# Apex Physical Appearance Inspection 

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

### 待新增 API

* 光源控制器 (RS232)
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

* Change color of scoll bar
* 移除 UVC / COIL 內容 
* 載入初始 Camera config from .json
	* 需要建立 class for json object

### Performance Test

* 比較 Task 和 ThreadPool 校能差別


### BUGS

* [x] 滑鼠中見移動 ROI 有偏移
* [x] Popupbox can't blur
	* Add Keyboard.ClearFocus()
* [ ] Hotkey may be invalid
* [ ] 影像抖動 

###  RELEASE MODE 

* [ ] 程式碼優化過(release mode)，FPS才不會掉張
	* 機率性