
# MCA Jaw 尺寸量測

MCA Jaw CCD 尺寸量測設備 

## 檢測項目

如圖面

## 檢驗標準

依照圖面公差

## 整理資料夾結構

* 一個產品一個命名空間

## Tabs

	* MCA Jaw 
		* MCA Jaw 檢驗主畫面
		* 規格調整畫面
	* Camera Tab
		* 新增相機
		* 相機Config
	* Database Tab
		* 紀錄查詢
	* Engineer Tab
		* Programming and test
	* System Info Tab
		* 系統資訊
		* 系統狀態

### 每個 Tab 內部最左欄確保 pixel 對齊

### Mongo 移出MCAJaw.xaml.cs


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

* [x] 新增權限 Level功能
	* 0 基本
	* 1 作業員
	* 2 品管員
	* 5 工程師
	* 9 開發者

* [ ] CameraTab 新增搭配鏡頭
	* 廠商
	* 型號
	* 放大倍率
	* Pixel SIze

* [ ] 測試各種彎曲

* [ ] 0.088 左右差異問題
	* 初步測試完成

* [ ] MCA_Jaw 錯誤捕捉

* [ ] Databasetab highlight enable element

* [ ] DatabaseTab 設定期限

* [ ] cornerSubPixel()
	 * 輪廓度都改掉

* [ ] 平直度subpixel 測試

* [ ] Cameras, Lens, Specification, 建立在資料庫 config 內作為主要資料, 載入失敗才使用.json載入

* [ ] if (spec != null && spec.enable) 改 if (spec?.Enable == true)

* [ ] No matching camera found.

* [ ] 調整字體排版 (1.5倍、1倍)

* [ ] 測試 Methods.GetHorizontalFlatness

* [ ] 增加系統啟動、關閉紀錄
	* 每次啟動自動模式且初始化完成時插入一筆資料，紀錄啟動時間點，啟動計時
	* 若為自動模式，關閉前插入一筆資料，關閉計時
	* 閒置超過1分鐘，進入閒置狀態 (背景)，暫停計時
	* 一旦有操作，進入運作狀態，繼續計時

* [ ] 切換大、中、小 JAW

* [ ] 新增系統資訊頁面
	* 作業系統 (win10 xx 版)
	* 平台 32位元 or 64位元
	* 程序 ID (PID)
	* .NET 版本
	* 資料庫

	* 系統時間

	* 版本
	* 當前狀態 (XX模式運行中)
	* 自動模式運作時間 (單次)
	* 自動模式運作時間 (累計)
	* 已檢測數量

* [ ] 確認 UserlayoutRounding 影響
	* border, rectangl 使用 SnapToDevicePixels

* [ ] ConfigPanel 新增 Extension config

* [ ] Engineertab.xaml 修正 CustomCard Padding

* [ ] *MCAJaw.xaml*	=>	MCAJaw.xaml (entry)
					=>	MainUISubTab.xaml
					=>	SizeSpecSubTab.xaml

* [ ] 刪除量測用的 TextBlock 和 Binding

* [ ] SizeSpecSubTab 調整 UI

* [x] 確認 JawResultGroup.SizeSpecList 已被取代且移除

* [ ] Systeminfo 資訊取得改為內置 
	* 新增 TCP server

* [ ] 盡量在命名空間下宣告 enum

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
