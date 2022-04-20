
# Apex Physical Appearance Inspection 

Apex 外觀瑕疵檢測 

### 檢測項目

* 耳朵
	* 毛邊
	* 銑銷不良

* 窗戶
	* 毛邊
	* 撞傷


* 管件
	* 亮紋
	* 坑洞
	* 黃斑
	* 劃記
	* 壓傷
	* 色差
	* 車刀紋

## 確認相機DELAY問題

* 初步判定為舊筆電效能問題

## 為了性能，將可改為 struct 之 class 改為 struct

## 整理資料夾結構
* 一個產品一個命名空間

## 定位有機率失敗

* Bug fixed
	* Methods.GetVertialWindowWidth 內分組後使用 4 捨 5 入


## APEX 檢測要保留項目
	* 單步測試
		1. 單相機單特徵
		2. 單相機連續
	* 整合測試
		1. 多相機同步進行

## 修正初始化 Binding 失敗

## 角度校正 

* 新增插入第一步，判斷初始旋轉方向

### Tab

* Main Tab
	* Control Panel
	* Image Panel
	* Procedure Panel
	* Record Panel
* Config Tab
	* 新增相機
	* 相機Config
* Motion Tab
	* 設定馬達參數
* Database Tab
	* 紀錄查詢

* Debug Tab
	* For Programming

* MCA Jaw Tab
	* For MCA Jaw 檢驗

### TO DO LIST


* [ ] 載入初始 Camera config from .json
	* 需要建立 class for json object
* [ ] 原點復歸
	* 待測試
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


* [ ] Motion Config 載入/儲存
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

* [ ] 相機連線失敗 Retry
	* 預設三次
	* 測試中
	* 有機率連線失敗

* [ ] ~MotionTab 有些 Button 需要綁定 servo on~

* [ ] 原點復歸若在原點上不會觸發重置位置

* [ ] ServoMotion.Axes.Clear() 時會導致 Binding 產生 Error
	* 設置BindingOperation

* [ ] Motion Reset Error 若即停開關按下則會 delay 
	* 重置錯誤失敗會導致 Delay

* [ ] 新增 IO panel 彈出視窗
	* 即時更新
	* 

* [ ] Initializer 需要等待 Enumer

* [ ] 新增 CRC 計算

* [ ] Add _cancellation to camera retry methods 

* [ ] 新增 Database 連線 Block
	* 若未連線也可測量，但是紀錄不會留下 

* [ ] 軟體即停處置

* [ ] Reset 處置

* [ ] 重構 Alogorithm

* [ ] EngineerTab.xaml camera use Task open

* [ ] Config Panel 增加參數
	* 新增 Format
	* 新增 offset、置中按鈕
	* 取消自動置中

* [ ] Camera Close 增加相機就算啟動中也可以 Close 的功能

* [x] Motion Tab 新增速度控制

* [ ] MotionTab Textbox UX 糟糕
	* 負號會有問題

* [ ] I/O Panel
	* 定時刷新 I/O 狀態

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