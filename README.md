﻿
# MIM 鎖片外觀 & 尺寸檢驗

MIM 大鎖片 外觀瑕疵檢測 & 尺寸檢驗


## 整理資料夾結構
* 一個產品一個命名空間

## Conveter 類型分割

## Card 更換為TitleCard

## Controls properties 順序

HorizontalAlignment, VerticalAlignment

Margin, Padding

Width, Height

FontWeight, FontSize

else...

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
	* 測試動態效果
	* 初測正常
	* 區分灰階與 RGB 影像

* [ ] ~測試 CameraEnumer 更改為 static~

* [x] LightEumer 改 SerialPortEumer 
	* 待測試功能正常

* [x] LightPanel 內建 LightControl
	* 測試 LightControl 移到 LightPanel 後的功能

* [ ] SpecListView 直接用固定 Height

* [x] DeviceTab 改 CameraTab
	* 待測試功能正常

* [ ] Enumer 的優先度往前拉

* [ ] 測試所有功能是否正常

* [ ] 回 APEX 時，TargetFeature 要變更

* [x] 預設不要啟動 Trigger Mode
	* 由初始化完成後啟動

* [x] CameraTab 按鈕 UX 新增

* [ ] 確認 UserSet 可不可以寫入 CenterX & CenterY

* [x] MCA Jaw 自動 / 編輯模式區分

* [x] 光源 Panel UX 優化
	* 未連線 Disable slider
	* 按鈕增加按下的視覺效果

* [x] DatabaseTab 新增批號查詢

* [x] DatabaseTab 日期改範圍

* [x] Appbar click evnet 改 command

* [x] 移除 無用Tab
	* MainTab.xaml
	* MontionTab.xaml

* [x] 批號新增確認按鈕
	* 待確認

* [x] ModeWindow 改 command 

* [x] 移除 MontionTab
	* 隱藏Tab
	* 不新增 ItemTab.Content

* [x] ~DatebaseTab 不須 admin~

* [x] 初始化完成前不允許切換 Tab
	* 或避免重複進入 InitHardWare function
	* 新增 Initlizing Flag

* [x] 修正 DatabaseTab Height 不正常 BUG 

* [x] 新增權限 Level功能
	* 0 基本
	* 1 作業員
	* 2 品管員
	* 5 工程師
	* 9 開發者

* [x] 新增登出
	* 加在關閉程式的 Menu 上方

* [x] 測試WISE4050連線Timeout錯誤處理
	* 需要處理 (使用 connectAsync)
	
* [x] 確認 DatabaseTab DataGrid 有 0.088合

* [ ] 新增 SerialPortBase 驅動

* [ ] 新增 TCPIPBase驅動

* [ ] CameraTab 新增搭配鏡頭
	* 廠商
	* 型號
	* 放大倍率

* [x] 新增開發者校正值

* [ ] 測試各種彎曲

* [ ] 處理平面度重複性問題
	* 初步測試完成
* [ ] 0.088 左右差異問題
	* 初步測試完成

* [ ] 測試 光源控制器 Read 和發光時間差

* [ ] 確認 Try Catch 層數
	* 少用 Try catch，用 if 判斷

* [ ] 馬達資訊同步?

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
