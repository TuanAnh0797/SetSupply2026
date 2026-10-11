# SetSupply Monitor — ghi chú cho Claude / dev

Ứng dụng WPF giám sát sản lượng line SetSupply (CABI). Đọc mã sản phẩm từ PLC (Mitsubishi MC protocol qua TCP), lưu SQLite, hiển thị sản lượng theo giờ / ca / nhóm model.

## Build & chạy
- Solution: `SetSupply.sln`, project `SetSupply/SetSupply.csproj` (.NET 6 `net6.0-windows7.0`, WPF).
- Build: `dotnet build SetSupply/SetSupply.csproj -c Debug` (warning thiếu `DLL\Interop.bpacx64.dll` là bình thường).
- DB: `<thư mục chạy>\Resources\Database\SetSupply.db` (dùng `Directory.GetCurrentDirectory()`), tạo bằng `EnsureCreated` khi chưa có.
- Tài khoản admin seed: xem `Database/AppDbContext.cs` (User seed).
- `.gitignore` bỏ qua `bin/ obj/ .vs/ *.user`. Riêng `Library/*/bin|obj` vẫn được track vì chứa DLL bên thứ 3 (Library không nằm trong solution).

## Kiến trúc
- MVVM: CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`), UI MaterialDesignThemes 5.2.1, chart LiveChartsCore.
- DI: `App.xaml.cs` đăng ký tất cả service / ViewModel / UserControl dạng **Singleton**. `AppDbContext` cũng là singleton dùng chung → code mới đọc/ghi DB qua `IServiceScopeFactory` (DbContext riêng mỗi lần) để tránh lỗi truy cập song song (thread PLC + UI).
- Điều hướng: `MainWindowViewModel.CurrentView` (Home / Dashboard / Setting / History). Menu trái là `DrawerHost`.
- PLC (Mitsubishi MC protocol 3E binary, TCP) — đã viết lại 2026-10-11:
  - `Helper/PLC.cs`: mỗi lệnh đọc đủ header 9 byte + phần dữ liệu theo length, kiểm tra subheader D000 và end code. Monitoring timer 4s.
  - `Service/PLCService.cs`: giữ 1 kết nối; quét bit trigger M mỗi 300ms, ghi bit Alive M mỗi 1s; mất kết nối → 5s sau kết nối lại.
    Trigger ON → đọc D (TotalRegisterData word, ASCII) → mã 12 ký tự model + 7 serial → `ProductionDataService.TryInsertProductionAsync` → **sau khi lưu xong mới reset trigger**. Lưu lỗi thì không reset (lần quét sau thử lại). Mã < 19 ký tự: ghi WARN rồi reset trigger.
  - Event `DataReceived(string)` chỉ phát khi đã lưu mới (thread nền) → Home/Dashboard chỉ tải lại (marshal về UI thread). `ConnectionChanged(bool)` chỉ phát khi trạng thái đổi; "đã kết nối" = PLC trả lời đúng giao thức.
  - Log: `<exe>\Logs\PLC_yyyyMMdd.log` (`Helper/PlcLogger.cs`), giữ 30 ngày, cùng 1 lỗi tối đa 1 lần/5 phút. Mỗi mã nhận được ghi 1 dòng INFO.
  - Thử không cần PLC thật: `python tools/plc_simulator.py --duration 60 --drop-after 20` rồi cài PLC: IP 127.0.0.1, Port 5000, AddressData 100, TotalRegisterData 10, Trigger 10, Alive 20.
  - Trang PLC Setting kiểm tra IP/port/số word (>= 10)/trigger ≠ alive trước khi lưu. `AddressConnfirmFinalReadData` không dùng.
- Ca làm việc: bảng `ShiftSettings` (3 ca, cột `IsUse`), cài ở Cài đặt > Calendar Setting. Ca qua đêm (End <= Start) được hỗ trợ.

## Màn hình Home (đã làm lại toàn bộ theo `docs/home_design_target.jpg`)
Ảnh kết quả hiện tại: `docs/home_current.png`.

| Thành phần | File |
|---|---|
| Thanh tiêu đề navy (Panasonic \| SetSupply Monitor + phụ đề, user chip, nút cửa sổ) | `Views/MainWindow.xaml` |
| Điều khiển Home trên thanh tiêu đề: chọn ngày (popup lịch), Cả ngày / Ca N, nút Cài đặt | `UserControls/Home/UCHomeHeader.xaml(.cs)` — gắn vào `MainWindowViewModel.HeaderToolbar` khi `CurrentView is UCHome` |
| Thanh lọc (Nhóm model, Model, ô **Codeback** vừa đọc từ PLC, cập nhật lần cuối, trạng thái kết nối) + bảng | `UserControls/Home/UCHome.xaml(.cs)` |
| Logic bảng | `ViewModels/UserControlViewModels/Home/UCHomeViewModel.cs` |
| Logic 4 panel dưới (partial class) | `ViewModels/UserControlViewModels/Home/UCHomeViewModel.Charts.cs` |
| Vòng tròn % (donut) | `ComponentUI/RingProgress.cs` |
| Model dòng bảng (nhóm / model / tổng / lựa chọn ca) | `Models/Home/ProductionTableRows.cs` |
| Ảnh thumbnail model | `Helper/ModelImageProvider.cs` |

Chức năng:
- Ngày sản xuất: bắt đầu từ giờ bắt đầu sớm nhất của các ca đang dùng (VD 06:00 → 06:00 hôm sau). `ProductionDataService.GetProductionRangeAsync / GetProductionDateAsync`.
- "Cả ngày" = các cột giờ của cả ngày sản xuất; "Ca N" = cột giờ của ca đó. Nút ca sinh tự động theo ca `IsUse = true`.
- Bảng: dòng nhóm (màu theo thứ tự nhóm, nút +/− mở rộng/thu gọn, trạng thái được nhớ trong phiên), dòng model con (thumbnail + số theo giờ), dòng **Tổng** cố định ở cuối. Giờ chưa tới để trống.
- Cột "Sản lượng (Kế hoạch)": lấy từ Daily Plan. Cả ngày = tổng kế hoạch các ca đang dùng; chọn ca = kế hoạch ca đó. Ngày chưa có kế hoạch → hiện "-", tỷ lệ "--"; **số thực tế luôn đếm từ ProductionData, không phụ thuộc kế hoạch**.
- Realtime: khi PLC gửi dữ liệu và đang xem ngày sản xuất hiện tại → tải lại; timer 1 phút tự cập nhật / tự sang ngày mới. Chọn ngày khác = xem lịch sử (không tự nhảy về hôm nay; bấm "Hôm nay" trong popup lịch để quay lại).
- 4 panel phía dưới (hàng cao 250px trong `UCHome.xaml`), số liệu theo đúng ngày / ca / bộ lọc đang chọn, khớp dòng Tổng của bảng:
  1. Tổng quan ngày: Kế hoạch, Thực tế, donut Tỷ lệ đạt (màu: >=98% xanh, >=90% cam, còn lại đỏ — `GetRatioColors`).
  2. Sản lượng theo nhóm model: cột ngang vẽ bằng WPF Grid (`GroupBarItem`, tỷ lệ `*`), trục 0..4 bước "đẹp" (`NiceStep`), màu cột = cột thứ 3 của `GroupPalette`.
  3. Sản lượng theo giờ: LiveCharts `LineSeries<double?>` (giờ chưa tới = null). Không dùng `Axis.Labels` vì nó bỏ qua `Labeler` (cần Labeler để thưa nhãn khi 24 giờ). Chữ tiếng Việt có dấu đặt bằng WPF TextBlock, không đưa vào Skia.
  4. Sản lượng theo ca: luôn hiện mọi ca đang dùng của ngày (VM luôn tải dữ liệu cả ngày vào `_dayRecords`, bảng chính lọc theo ca vào `_records`). Ca chưa bắt đầu hiện "--".
- Định dạng số/%: `InvariantCulture` ("1,016", "98.3%").
- Độ rộng cột cố định khai báo ở 2 nơi phải khớp nhau: hằng số trong `UCHomeViewModel` (GroupColumnWidth...) và `GridLength` trong `UCHome.xaml`.
- Header/footer bảng tự chừa chỗ cho thanh cuộn dọc (`UCHome.xaml.cs` → `AlignHeaderWithBody`, đo `PART_VerticalScrollBar`; KHÔNG dùng `ViewportWidth` vì = 0 khi tắt cuộn ngang → gây vòng lặp layout).

## Cài đặt chung (General)
- Bảng `AppSettings` (Key, Value) — `Models/Database/AppSetting.cs`, `Service/AppSettingService.cs` (event `SettingChanged`).
- `ApplicationTitle` (mặc định "SetSupply Monitor") hiển thị trên thanh tiêu đề + tiêu đề cửa sổ; đổi ở Cài đặt > General (`UCGeneralSetting`), áp dụng ngay.
- Thêm cài đặt chung mới: thêm key vào `AppSettingService`, KHÔNG cần đổi schema.

## Ô Codeback trên Home
- `PLCService.CodeRead(PlcReadResult)` phát cho MỌI mã đọc được (Saved / Duplicate / Invalid / SaveError); `PLCService.LastRead` giữ mã gần nhất.
- Home hiện mã + giờ + nhãn trạng thái (xanh Đã lưu / cam Trùng / đỏ Sai mã, Lỗi lưu). Mở chương trình chưa đọc mã nào → hiện bản ghi mới nhất trong DB (nhãn "Gần nhất").
- Ô "Vị trí" đã bỏ.

## Nhóm model (Model Group)
- Bảng `ModelGroups` (`Models/Database/ModelGroup.cs`): Name, Prefixes (tiền tố model, cách nhau dấu phẩy), SortOrder. **Chỉ để gom model, không có kế hoạch** (bản trước có PlanShift1-3, đã bỏ; DB cũ được `DROP COLUMN` khi khởi động).
  Prefixes rỗng = nhóm "Khác" (gom model không khớp). Khớp theo tiền tố dài nhất: `ModelGroupService.Match`.
- DB cũ không tự có bảng mới (EnsureCreated không migrate) → `App.EnsureSchemaUpdatesAsync` chạy `CREATE TABLE/INDEX IF NOT EXISTS`, `DropColumnsIfExistAsync`, sửa dữ liệu cũ. Thêm cột/bảng mới sau này cũng phải thêm vào đây (lệnh phải chạy lại được nhiều lần).
- Trang Cài đặt > "Model Group" (`UCModelGroupSetting`), nút "Cài đặt" trên header Home mở thẳng trang này (cần Admin). Có "Tạo nhóm" (tự sinh từ model 30 ngày theo N ký tự đầu), **Import / Export** (cột Thứ tự | Tên nhóm | Tiền tố model; Import chỉ nạp vào bảng, phải bấm Lưu), ảnh nhóm/model.
- Ảnh model: `<thư mục exe>\Resources\ModelImages\<tên model>.png` (hoặc `<tên nhóm>.png`). Không có ảnh → icon tủ lạnh.

## Kế hoạch theo ngày (Daily Plan) — nguồn kế hoạch DUY NHẤT
- Bảng `ProductionPlans` (`Models/Database/ProductionPlan.cs`): PlanDate, GroupName (theo TÊN nhóm vì ModelGroups bị ghi đè khi lưu → Id đổi), ShiftNumber, Quantity.
- Service `ProductionPlanService` (GetPlansAsync, SavePlansAsync, GetPlansInRangeAsync, SaveDaysAsync trong 1 transaction).
- Trang Cài đặt > "Daily Plan" (`UCDailyPlanSetting`): chọn ngày, sửa theo ca, Lưu / Copy từ ngày trước / Xóa kế hoạch ngày.
  **Import** nhiều ngày (cột Ngày | Nhóm model | Kế hoạch Ca 1 | Ca 2 | Ca 3; ngày có trong file bị ghi đè toàn bộ; nhóm chưa có trong Model Group → cảnh báo; tên nhóm khớp không phân biệt hoa/thường). **Export** khoảng ngày (mỗi ngày × mỗi nhóm 1 dòng, chưa có = 0) — dùng làm file mẫu.
- Đọc/ghi file: `Helper/SpreadsheetHelper.cs` (ClosedXML 0.102.3 cho .xlsx, không cần cài Excel; .csv UTF-8 BOM, tự nhận dấu `,` hoặc `;`).

## Giờ ca khi xem ngày cũ
- `ProductionDataService.GetShiftsForDateAsync(date)`: ngày < hôm nay → lấy giờ ca/IsUse từ `ShiftSettingHistory` của ngày đó (bản ghi mới nhất mỗi ca), ca không có lịch sử dùng cài đặt hiện tại. Hôm nay/tương lai → cài đặt hiện tại.
- Lịch sử được ghi bởi `ShiftSettingService.UpdateShiftSettingAsync` và `ShiftHistoryBackgroundService` (2 tiếng/lần, ghi đè bản ghi của hôm nay).

## Dữ liệu demo
`python tools/seed_demo_data.py <đường dẫn SetSupply.db> [--two-shifts]` — tạo nhóm mẫu + Daily Plan + dữ liệu ngẫu nhiên hôm nay & hôm qua. `--two-shifts` đặt Ca1 06–18, Ca2 18–06, tắt Ca3 (giống ảnh thiết kế). **Không chạy trên DB thật** (xóa ProductionData 2 ngày gần nhất).
Cách test đã dùng: copy `bin/Debug/net6.0-windows7.0` ra thư mục tạm, chạy exe với working dir là thư mục đó, seed DB ở đó.
Lưu ý: app chỉ cho chạy 1 instance (Mutex "APP") — nếu đang mở app thì bản test sẽ tự thoát; khi app đang mở, `bin/Debug/.../SetSupply.exe` bị khóa → build ra thư mục khác (`dotnet build -o <thư mục>`).

## Thay đổi khác so với bản gốc (commit `version1`)
- Sửa lỗi: `UCHomeViewModel` cũ lưu cả chuỗi mã vào cột `Model`; nay lưu đúng 12 ký tự model (đã Trim).
- `ProductionDataService.AddProductionDataAsync` dùng DbContext riêng (tránh xung đột với truy vấn của Dashboard) và kiểm tra trùng theo **Model + Serial** (bản gốc chỉ theo Serial → 2 model trùng serial bị mất bản ghi), có index `IX_ProductionData_Model_Serial`.
- Khi khởi động: dữ liệu cũ có `Model = Codeback` và dài >= 19 ký tự được sửa thành 12 ký tự đầu (model).
- `UCControlBar`: `Background` của instance áp cho cả ColorZone; `Tag` = độ rộng vùng kéo cửa sổ (mặc định 200).
- Nền vùng nội dung đổi sang `#EEF3F9`; hàng tiêu đề trang (icon + tên trang, đồng hồ, trạng thái PLC) bị ẩn ở Home.
- Không còn dùng ở Home: `ShiftViewModel`, `DataGridColumnHelper`, `BoolToIconConverter` (giữ lại, chưa xóa).

## Việc có thể làm tiếp
- Tỷ lệ đạt đang so thực tế với kế hoạch CẢ ca/ngày (giữa ca sẽ thấp); nếu muốn có thể so với kế hoạch tính theo thời gian đã trôi qua.
