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
- PLC: `Service/PLCService.cs` → event `DataReceived(string)` (chạy trên thread pool), `ConnectionChanged(bool)`.
  Mã đọc được: 12 ký tự model + 7 ký tự serial.
- Ca làm việc: bảng `ShiftSettings` (3 ca, cột `IsUse`), cài ở Cài đặt > Calendar Setting. Ca qua đêm (End <= Start) được hỗ trợ.

## Màn hình Home (đã làm lại toàn bộ theo `docs/home_design_target.jpg`)
Ảnh kết quả hiện tại: `docs/home_current.png`.

| Thành phần | File |
|---|---|
| Thanh tiêu đề navy (Panasonic \| SetSupply Monitor + phụ đề, user chip, nút cửa sổ) | `Views/MainWindow.xaml` |
| Điều khiển Home trên thanh tiêu đề: chọn ngày (popup lịch), Cả ngày / Ca N, nút Cài đặt | `UserControls/Home/UCHomeHeader.xaml(.cs)` — gắn vào `MainWindowViewModel.HeaderToolbar` khi `CurrentView is UCHome` |
| Thanh lọc (Vị trí, Nhóm model, Model, cập nhật lần cuối, trạng thái kết nối) + bảng | `UserControls/Home/UCHome.xaml(.cs)` |
| Logic bảng | `ViewModels/UserControlViewModels/Home/UCHomeViewModel.cs` |
| Logic 4 panel dưới (partial class) | `ViewModels/UserControlViewModels/Home/UCHomeViewModel.Charts.cs` |
| Vòng tròn % (donut) | `ComponentUI/RingProgress.cs` |
| Model dòng bảng (nhóm / model / tổng / lựa chọn ca) | `Models/Home/ProductionTableRows.cs` |
| Ảnh thumbnail model | `Helper/ModelImageProvider.cs` |

Chức năng:
- Ngày sản xuất: bắt đầu từ giờ bắt đầu sớm nhất của các ca đang dùng (VD 06:00 → 06:00 hôm sau). `ProductionDataService.GetProductionRangeAsync / GetProductionDateAsync`.
- "Cả ngày" = các cột giờ của cả ngày sản xuất; "Ca N" = cột giờ của ca đó. Nút ca sinh tự động theo ca `IsUse = true`.
- Bảng: dòng nhóm (màu theo thứ tự nhóm, nút +/− mở rộng/thu gọn, trạng thái được nhớ trong phiên), dòng model con (thumbnail + số theo giờ), dòng **Tổng** cố định ở cuối. Giờ chưa tới để trống.
- Cột "Sản lượng (Kế hoạch)": Cả ngày = tổng kế hoạch các ca đang dùng; chọn ca = kế hoạch ca đó.
- Realtime: khi PLC gửi dữ liệu và đang xem ngày sản xuất hiện tại → tải lại; timer 1 phút tự cập nhật / tự sang ngày mới. Chọn ngày khác = xem lịch sử (không tự nhảy về hôm nay; bấm "Hôm nay" trong popup lịch để quay lại).
- 4 panel phía dưới (hàng cao 250px trong `UCHome.xaml`), số liệu theo đúng ngày / ca / bộ lọc đang chọn, khớp dòng Tổng của bảng:
  1. Tổng quan ngày: Kế hoạch, Thực tế, donut Tỷ lệ đạt (màu: >=98% xanh, >=90% cam, còn lại đỏ — `GetRatioColors`).
  2. Sản lượng theo nhóm model: cột ngang vẽ bằng WPF Grid (`GroupBarItem`, tỷ lệ `*`), trục 0..4 bước "đẹp" (`NiceStep`), màu cột = cột thứ 3 của `GroupPalette`.
  3. Sản lượng theo giờ: LiveCharts `LineSeries<double?>` (giờ chưa tới = null). Không dùng `Axis.Labels` vì nó bỏ qua `Labeler` (cần Labeler để thưa nhãn khi 24 giờ). Chữ tiếng Việt có dấu đặt bằng WPF TextBlock, không đưa vào Skia.
  4. Sản lượng theo ca: luôn hiện mọi ca đang dùng của ngày (VM luôn tải dữ liệu cả ngày vào `_dayRecords`, bảng chính lọc theo ca vào `_records`). Ca chưa bắt đầu hiện "--".
- Định dạng số/%: `InvariantCulture` ("1,016", "98.3%").
- Độ rộng cột cố định khai báo ở 2 nơi phải khớp nhau: hằng số trong `UCHomeViewModel` (GroupColumnWidth...) và `GridLength` trong `UCHome.xaml`.
- Header/footer bảng tự chừa chỗ cho thanh cuộn dọc (`UCHome.xaml.cs` → `AlignHeaderWithBody`, đo `PART_VerticalScrollBar`; KHÔNG dùng `ViewportWidth` vì = 0 khi tắt cuộn ngang → gây vòng lặp layout).

## Nhóm model & kế hoạch
- Bảng mới `ModelGroups` (`Models/Database/ModelGroup.cs`): Name, Prefixes (tiền tố model, cách nhau dấu phẩy), PlanShift1/2/3, SortOrder.
  Prefixes rỗng = nhóm "Khác" (gom model không khớp). Khớp theo tiền tố dài nhất: `ModelGroupService.Match`.
- DB cũ không tự có bảng mới (EnsureCreated không migrate) → `App.EnsureModelGroupsTableAsync` chạy `CREATE TABLE IF NOT EXISTS`. Thêm cột/bảng mới sau này cũng phải làm tương tự.
- Cài đặt: Cài đặt > "Model Group / Plan" (`UserControls/Setting/Sub/UCModelGroupSetting.xaml`, VM `UCModelGroupSettingViewModel`), nút "Cài đặt" trên header Home mở thẳng trang này (cần quyền Admin).
  Có nút "Tạo nhóm" tự sinh nhóm từ model đã chạy 30 ngày theo N ký tự đầu (mặc định 8, VD `NR-C33ES2L-C` → `NR-C33ES`).
- Ảnh model: `<thư mục exe>\Resources\ModelImages\<tên model>.png` (hoặc `<tên nhóm>.png`, hỗ trợ png/jpg/jpeg/bmp). Không có ảnh → icon tủ lạnh.

## Dữ liệu demo
`python tools/seed_demo_data.py <đường dẫn SetSupply.db> [--two-shifts]` — tạo nhóm mẫu + dữ liệu ngẫu nhiên hôm nay & hôm qua. `--two-shifts` đặt Ca1 06–18, Ca2 18–06, tắt Ca3 (giống ảnh thiết kế). **Không chạy trên DB thật** (xóa ProductionData 2 ngày gần nhất).
Cách test đã dùng: copy `bin/Debug/net6.0-windows7.0` ra thư mục tạm, chạy exe với working dir là thư mục đó, seed DB ở đó.

## Thay đổi khác so với bản gốc (commit `version1`)
- Sửa lỗi: `UCHomeViewModel` cũ lưu cả chuỗi mã vào cột `Model`; nay lưu đúng 12 ký tự model (đã Trim).
- `ProductionDataService.AddProductionDataAsync` dùng DbContext riêng (tránh xung đột với truy vấn của Dashboard).
- `UCControlBar`: `Background` của instance áp cho cả ColorZone; `Tag` = độ rộng vùng kéo cửa sổ (mặc định 200).
- Nền vùng nội dung đổi sang `#EEF3F9`; hàng tiêu đề trang (icon + tên trang, đồng hồ, trạng thái PLC) bị ẩn ở Home.
- Không còn dùng ở Home: `ShiftViewModel`, `DataGridColumnHelper`, `BoolToIconConverter` (giữ lại, chưa xóa).

## Việc có thể làm tiếp
- Tỷ lệ đạt đang so thực tế với kế hoạch CẢ ca/ngày (giữa ca sẽ thấp); nếu muốn có thể so với kế hoạch tính theo thời gian đã trôi qua.
- Combobox "Vị trí" hiện chỉ có 1 giá trị cố định "CABI - SetSupply" (`UCHomeViewModel.Locations`).
- Kế hoạch hiện là cố định mỗi ngày theo ca; nếu cần kế hoạch theo từng ngày phải thêm bảng mới.
