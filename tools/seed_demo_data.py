"""
Tạo dữ liệu DEMO cho màn hình Home (chỉ dùng để thử giao diện, KHÔNG chạy trên DB thật).

Cách dùng:
    python tools/seed_demo_data.py <đường dẫn SetSupply.db> [--two-shifts]

--two-shifts : đặt Ca 1 = 06:00-18:00, Ca 2 = 18:00-06:00, tắt Ca 3 (giống ảnh thiết kế)
Script sẽ: tạo bảng ModelGroups nếu thiếu, ghi đè nhóm model mẫu, xóa ProductionData của
hôm nay + hôm qua rồi sinh dữ liệu ngẫu nhiên tới thời điểm hiện tại.
"""
import random
import sqlite3
import sys
from datetime import datetime, timedelta

if len(sys.argv) < 2:
    print(__doc__)
    sys.exit(1)

db_path = sys.argv[1]
two_shifts = "--two-shifts" in sys.argv
con = sqlite3.connect(db_path)
cur = con.cursor()

cur.execute("""
CREATE TABLE IF NOT EXISTS "ModelGroups" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ModelGroups" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Prefixes" TEXT NOT NULL DEFAULT '',
    "PlanShift1" INTEGER NOT NULL DEFAULT 0,
    "PlanShift2" INTEGER NOT NULL DEFAULT 0,
    "PlanShift3" INTEGER NOT NULL DEFAULT 0,
    "SortOrder" INTEGER NOT NULL DEFAULT 0
)""")

if two_shifts:
    cur.execute("UPDATE ShiftSettings SET StartTime='06:00:00', EndTime='18:00:00', IsUse=1 WHERE ShiftNumber=1")
    cur.execute("UPDATE ShiftSettings SET StartTime='18:00:00', EndTime='06:00:00', IsUse=1 WHERE ShiftNumber=2")
    cur.execute("UPDATE ShiftSettings SET IsUse=0 WHERE ShiftNumber=3")

# (Tên nhóm, tiền tố, KH ca 1, KH ca 2, KH ca 3, thứ tự)
groups = [
    ("NR-C33ES", "NR-C33ES", 560, 500, 0, 1),
    ("NR-TL351", "NR-TL351", 600, 533, 0, 2),
    ("NR-SP275", "NR-SP275", 520, 460, 0, 3),
    ("NR-BX420", "NR-BX420", 420, 380, 0, 4),
    ("Khác", "", 300, 200, 0, 999),
]
cur.execute("DELETE FROM ModelGroups")
cur.executemany(
    "INSERT INTO ModelGroups (Name, Prefixes, PlanShift1, PlanShift2, PlanShift3, SortOrder) VALUES (?,?,?,?,?,?)",
    groups)

models = {
    "NR-C33ES2-C": (14, 20), "NR-C33ES2L-C": (12, 19), "NR-C33ES2L-W": (12, 18),
    "NR-C33ES2-W": (12, 18), "NR-C33ES2-WJ": (12, 18),
    "NR-TL351BPKP": (12, 18), "NR-TL351BWCP": (12, 18), "NR-TL351GKVV": (12, 22), "NR-TL351VGMV": (12, 18),
    "NR-SP275CPAV": (20, 26), "NR-SP275CPSV": (20, 26), "NR-SP275GPKV": (20, 26), "NR-SP275GPSV": (20, 26),
    "NR-BX420GKVN": (15, 18), "NR-BX420XNVN": (15, 18), "NR-BX420WKVN": (15, 18), "NR-BX420QKVN": (15, 18),
    "NR-BV280QSVN": (10, 13), "NR-BL381WKVN": (10, 12), "NR-DZ601VGKV": (10, 12), "NR-YW590YMMV": (10, 12),
}

now = datetime.now()
day_start_hour = 6
prod_day = now.date() if now.hour >= day_start_hour else (now - timedelta(days=1)).date()
start = datetime.combine(prod_day - timedelta(days=1), datetime.min.time()) + timedelta(hours=day_start_hour)
end = now

cur.execute("DELETE FROM ProductionData WHERE DateTimeInsert >= ?", (start.strftime("%Y-%m-%d %H:%M:%S"),))

rows = []
serial = random.randint(1000000, 5000000)
t = start
while t < end:
    for model, (lo, hi) in models.items():
        for _ in range(random.randint(lo, hi)):
            ts = t + timedelta(seconds=random.randint(0, 3599))
            if ts >= end:
                continue
            serial += 1
            code = f"{model:<12}{serial % 10000000:07d}"
            rows.append((code, model, f"{serial % 10000000:07d}", ts.strftime("%Y-%m-%d %H:%M:%S.%f") + "0"))
    t += timedelta(hours=1)

cur.executemany("INSERT INTO ProductionData (Codeback, Model, Serial, DateTimeInsert) VALUES (?,?,?,?)", rows)
con.commit()
print(f"Đã thêm {len(rows)} bản ghi ProductionData từ {start} tới {end}")
