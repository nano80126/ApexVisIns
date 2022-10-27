
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
		* 鏡頭資訊
	* Database Tab
		* 紀錄查詢
	* Engineer Tab
		* Programming and test
	* System Info Tab
		* 系統資訊
		* 系統狀態
		* 網卡資訊

### 每個 Tab 內部最左欄確保 pixel 對齊

### Mongo 移出MCAJaw.xaml.cs


### TO DO LIST

* [ ] MotionTab Textbox UX 糟糕
	* 負號會有問題

* [ ] Enumer 的優先度往前拉

* [ ] 測試所有功能是否正常

* [ ] 回 APEX 時，TargetFeature 要變更

* [ ] 確認 UserSet 可不可以寫入 CenterX & CenterY

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

* [ ] MCA_Jaw 錯誤捕捉

* [ ] DatabaseTab 設定期限

* [ ] cornerSubPixel()
	 * 輪廓度都改掉

* [ ] 平直度subpixel 測試

* [ ] Cameras, Lens, Specification, 建立在資料庫 config 內作為主要資料, 載入失敗才使用.json載入

* [ ] if (spec != null && spec.enable) 改 if (spec?.Enable == true)

* [ ] No matching camera found.
	* API Bub，需要Retry

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
	* 新增UserSet 

* [ ] Engineertab.xaml 修正 CustomCard Padding

* [ ] *MCAJaw.xaml*	=>	MCAJaw.xaml (entry)
					=>	MainUISubTab.xaml
					=>	SizeSpecSubTab.xaml

* [x] 刪除量測用的 TextBlock 和 Binding

* [x] SizeSpecSubTab 調整 UI

* [x] 確認 JawResultGroup.SizeSpecList 已被取代且移除

* [ ] Systeminfo 資訊取得改為內置 
	* 新增 TCP server

* [x] 盡量在命名空間下宣告 enum

* [x] Set MainWindow value when each Tab/Panel initializing

* [ ] 測試 IdleTimer 內置 SystemInfo

* [x] 開始檢測、節批 Padding 調整

* [ ] 主控版更換為 CustomCard

* [ ] SystemInfo 新增 TCP Listener

* [ ] CameraConfigs 變更機制修正

* [ ] EngineerTab.xaml 保留 SubToolbar
	* Panel
		* 一些簡單操作
		* 儲存圖片

 * [ ] TargetFeatureType in BaslerCam.CameraConfigBase move to namespace
	* 待確認功能正常

 * [x] 整理 CameraConfig in BaslerCam.cs

 * [ ] 刪除 ConfigPanel.xmal.cs 內的 BaslerConfig
	* 有標示 temporary
 
 * [ ] 重新釐清 Camera 相關各 Class

 * [ ] 確認變更 TargetFeatureType 功能

 * [ ] CameraConfigBaseWithTarget 新增 Event;
	* 待確認功能正常
	
 * [x] 測試 CameraTab IP、Target 變更後儲存狀態

 * [ ] DatabaseTab.xaml UI 調整未完成.

 * [ ] CameraTab.xaml 新增 LENS 功能
	* CameraConfigBase 置入 Lens 類別一併儲存

* [x] SoundPlayer 音會斷掉
	* HDMI 線問題 ? 

* [x] 音效播放移動到 MCAJaw.xaml.cs
	* 待確認功能正常

* [ ] SystemInfo 測試TCP Listener

* [ ] Migrate to the latest version of MaterialDesign 4.6.1

* [ ] Migrate to the latest version of OpenCVSharp

* [ ] Check why ToolbarTray has default padding 3

* [x] ConfigPanel 新增載入 UserSet 功能

* [ ] 整理 MsgInformer

* [x] 群組化功能
	* 新增不良數量
	* cam1 完成
	* cam2 未完成

* [ ] MCAJawS .Item => .Key
	* 逐行確認

* [ ] DatabaseTab.xaml Header 需要修改

* [x] PopupBox Animation Lag

* [ ] Test Socket in Systeminfo 

* [x] 確認結批、歸零邏輯

* [ ] UpdateProperty Triiger Edit to LostFocus in CameraTab.xaml

* [ ] SystemInfo 改為單例模式
	* Lazy<T>

* [ ] InsResultView 有沒有可能優化?
	* 不用 Clear 而是重複利用現有 Object


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
