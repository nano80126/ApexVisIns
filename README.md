
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
* [x] 建立樹狀圖 (draw.io) 
	* DataContext
* [x] Camera Enumer Debug
* [x] DeviceConfig 清除空儲存按鈕不能隱藏
* [x] DeviceConfig 多台 Camera 測試 (等工業電腦到廠)
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
		* DeviceConfigs 狀態變更

* [ ] Navigation 新增登入按鈕
	* 登入後才可以進入 DeviceTab & MotionTab
	* 密碼錯誤警告
	* 自動登出
	* 手動登出

* [ ] ProgressBar UI 會卡
	* 待測試，若無辦法則改 Infinity

* [x] 確認 Motion GetDevices 會不會讓 Handle 遺失 
	※ 要等 220V

* [ ] Motion Config 載入/儲存
* [x] ImageSource Array
* [x] 轉移 DeviceConfig 到物件上
	* 方便直接比對
* [ ] 測試 EngineerTab
* [ ] IO Control 重複初始化
	* DigitalIOPanel 和 MainTab.xaml
	* 執行緒有時會衝突
* [ ] ServoMotion 實作 Dispose
	* 待測試

* [ ] 光源新增 Tab?

* [ ] 隱藏 App Toolbar
	* 上線使用
	* 隱藏 Close Button
	* 關閉程式 = 關機

* [x] IO Control Panel 改為按鈕建立實例
	* 避免和 MainTab 衝突
	
* [x] 除了 MainTab 以外，需要 admin 權限才可操作其他 Tab
	* 新增登出 ?

* [x] 新增 Interface of CustomCam
	* 待測試

* [x] 更新 ServoMotion HomeModes
	* 多測試幾次

* [x] 確認 OOP 命名規則
	* SltMotionAxis (ServoMotion)
	* 待測試

* [ ] 相機連線失敗 Retry
	* 預設三次
	* 測試中
	* 有機率連線失敗

* [x] Test Hotkey
	* Engineer Tab

* [ ] ~MotionTab 有些 Button 需要綁定 servo on~

* [ ] 原點復歸若在原點上不會觸發重置位置

* [x] 確認 ProgressBar 可以 100%

* [x] 初始化 Motion
	* 確認 MotionEnumer 是否使用 => 可移除
	* 確認 ServoMotion.ListAvailableDevices() 觸發時機 (必須避免重複觸發)

* [x] Motion Status Pack Icon
	* ALM 時顯示驚嘆號 (exclamation)

* [ ] ServoMotion.Axes.Clear() 時會導致 Binding 產生 Error
	* 設置BindingOperation

* [x] 測試原點復歸中是否可以進行其他操作
	* Ans: 會發出警報

* [x] MotionTab 尋找軸卡改為按鈕觸發

* [ ] 原點復歸前確認 IO

* [x] 整理初始化流程 methods
	* try... catch
	* MsgInformer Error Type
	* SpinWait Flag

* [x] 初始化改 Task with token

* [ ] Motion Reset Error 若即停開關按下則會 delay 
	* 重置錯誤失敗會導致 Delay

* [ ] 新增 IO panel 彈出視窗
	* 即時更新

* [x] 光源控制器待轉移

* [ ] Initializer 需要等待 Enumer

* [ ] 新增 Hardware Status Block

* [ ] 新增 CRC 計算

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
	* UserSet 不會儲存 Center X, Center Y

###  RELEASE MODE 

* [ ] 程式碼優化過(release mode)，FPS才不會掉張
	* 機率性
	* 效能影響最大

### 