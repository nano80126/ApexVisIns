
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
	* 鋁戒位置偏移

### 檢驗標準

* 亮紋 ROI需要分區塊
	* 覆蓋區域整圈洗淨治具亮紋要抓(先抓超過半圈)
	* 非覆蓋區域有亮紋就抓
* 耳朵、窗戶
	* 平面部分倒角刮傷不抓，但倒角過大則抓

## 整理資料夾結構
* 一個產品一個命名空間

## 定位有機率失敗

* Bug fixed
	* Methods.GetVertialWindowWidth 內分組後使用 4 捨 5 入

* 精定位時減少搖擺
	* 確認相機和馬達先後順序且確保命令不重複


## APEX 檢測要保留項目
	* 單步測試
		1. 單相機單特徵
		2. 單相機連續
	* 整合測試
		1. 多相機同步進行

## Tabs

	* Main Tab
		* Control Region
		* Image Region
		* Chart Region
		* Procedure Region
		* Record Region
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

* [ ] ServoMotion.Axes.Clear() 會導致 Binding Error 
	* Binding Operation 
	* Try to add New Property for Error handling

* [ ] IO Window 功能
	* Reset 功能
	* 急停  功能
		* 急停重置失敗會造成 UI 卡住

* [ ] DatabaseTab 功能
	* 自動連線
	* 檢驗紀錄查詢

* [ ] 重構 Alogorithm

* [ ] EngineerTab 使用 Task 開啟相機
	* 避免 UI 卡住

* [ ] Camera Close 增加相機就算拍攝中也可以 Close
	* 新增 If 判斷是否拍攝中

* [ ] MotionTab Textbox UX 糟糕
	* 負號會有問題

* [ ] I/O Panel
	* 定時刷新 I/O 狀態

* [x] 修正各 Tab 物件錯誤

* [x] 各 Tab 改動態載入

* [x] ~各 Tab  MsgInformer = MainWindow.MsgInformer~

* [ ] 標示器新增 RGB 

* [ ] ~測試 CameraEnumer 更改為 static~

* [ ] LightEumer 改 SerialPortEumer 

* [ ] LightPanel 內建 LightControl
	* 測試 LightControl 移到 LightPanel 後的功能

* [ ] SpecListView 直接用固定Height

* [ ] DeviceTab 改 CameraTab

* [ ] Enumer 的優先度往前拉

### Config Logic

* Camera connected => Updata camera property => update camera config property

* Config Panel open => load json file in directory 

* Config Panel Close => synchronize config with camera
 
* Config Save => Save button click => save json file 

* Config Write => Write config to camera

### BUGS

* DebugTab
	* ConfigPanel 比較邏輯需更改 (Textbox & Config)
* Read UserSet Center X, Center Y 紀錄會失效
	* UserSet 不會儲存 Center X, Center Y

### Camera Parameters
	* UserSet CenterX & CenterY 確認是否可以儲存
		* 測試所有型號
