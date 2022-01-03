﻿
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

* 初步判定為舊筆電效能問題


## 為了性能，將可改為 struct 之 class 改為 struct


### 待新增 API

* [x] 光源控制器 (RS232), 保留可呼叫物件
* [x] I/O 控制
* [ ] 馬達控制 (EtherCAT)

### 待移除

* [ ] Thermometer (USE <=> RS485)
* [ ] class 資料夾內部本專案無用 cs 檔

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

* [x] Change color of scoll bar
* [x] 移除 UVC / COIL 內容 
* [ ] 載入初始 Camera config from .json
	* 需要建立 class for json object
* [ ] 建立樹狀圖 (draw.io) 
	* DataContext
* [ ] Camera Enumer Debug
* [x] DeviceConfig 清除空儲存按鈕不能隱藏
* [ ] DeviceConfig 多台 Camera 測試 (等工業電腦到廠)
* [x] 確認 Device Tab 和 Enginner Tab 不會衝突
	* 
* [x] Add custom event for DI interrupt
* [x] 中斷器有可能啟用失敗
	* 情境 CH0 啟用後再啟用CH1
	* 反之亦然
* [x] Digital IO Debounce
* [x] 組態列表儲存 JSON
	* Model
	* S/N
	* Character
	* IP (之後IP要設定為固定)
* [x] Jog 使用 Popupbox
* [x] Motion Tab Unload 的處置
	* 暫停 Timer
* [x] Device Tab
	* Device List 儲存IP，相機IP需要綁定
* [ ] 原點復歸
	* 待測試
* [x] 測試 MoveAbs & ModeRel
* [x] 簡化控制板 UI
* [x] Handle 遺失問題待處理
* [x] 新增光源控制器 (MainTab)
* [ ] MainTab.xaml
	* 光源控制器 初始化
	* 相機 初始化
		* 尚未錯誤處理
	* 伺服馬達 初始化
* [ ] 啟動速度優化
* [ ] MainTab.xaml 反初始化
* [ ] CameraEnumer 
	* 初始化 Flag
	* 單支相機被移除時的處置 (camsSourc.Remove)
* [ ] Navigation 新增登入按鈕
	* 登入後才可以進入 DeviceTab & MotionTab
* [ ] ProgressBar UI 會卡
* [ ] 確認Motion GetDevices 會不會讓Handle 遺失 
* [ ] Motion Config 載入/儲存
* [ ] ImageSource Array
* [ ] 轉移 DeviceConfig 到物件上
	* 方便直接比對

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

### 