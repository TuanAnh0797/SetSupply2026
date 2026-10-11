"""
PLC giả lập (Mitsubishi MC protocol 3E binary) để thử phần giao tiếp PLC của SetSupply mà không cần PLC thật.

Cách dùng:
    python tools/plc_simulator.py [--port 5000] [--interval 2] [--duration 60] [--drop-after 0]

Cài đặt PLC trong app (Cài đặt > PLC Setting): IP 127.0.0.1, Port 5000,
AddressData = 100 (D100), TotalRegisterData = 10 (word), AddressTrigger = 10 (M10), AddressAlive = 20 (M20).

Mỗi <interval> giây, nếu app đã xác nhận mã trước (M10 = OFF) thì ghi mã mới vào D100 và bật M10.
Mã thứ 5 là mã trùng (gửi lại mã ngay trước đó), mã thứ 8 là mã sai định dạng (quá ngắn).
--drop-after N: sau N giây ngắt kết nối 1 lần để thử app tự kết nối lại.
"""
import argparse
import random
import socket
import struct
import sys
import threading
import time

sys.stdout.reconfigure(encoding="utf-8")

parser = argparse.ArgumentParser()
parser.add_argument("--port", type=int, default=5000)
parser.add_argument("--interval", type=float, default=2.0)
parser.add_argument("--duration", type=float, default=60.0)
parser.add_argument("--drop-after", type=float, default=0.0)
args = parser.parse_args()

TRIGGER, ALIVE, DATA_START, DATA_WORDS = 10, 20, 100, 10
M = {}            # bit device M
D = {}            # word device D
lock = threading.Lock()
stats = {"sent": 0, "acked": 0, "alive_writes": 0}
MODELS = ["NR-C33ES2-C", "NR-TL351BPKP", "NR-SP275CPAV", "NR-BX420GKVN", "NR-BV280QSVN"]


def write_code(code):
    raw = code.encode("ascii")
    raw = raw + b"\0" * (DATA_WORDS * 2 - len(raw))
    with lock:
        for i in range(DATA_WORDS):
            D[DATA_START + i] = raw[2 * i] | (raw[2 * i + 1] << 8)
        M[TRIGGER] = 1
    stats["sent"] += 1
    print(f"[SIM] -> {code!r}", flush=True)


def producer(stop):
    n = 0
    last = None
    while not stop.is_set():
        time.sleep(args.interval)
        with lock:
            busy = M.get(TRIGGER, 0) == 1
        if busy:
            continue
        n += 1
        if n == 5 and last:
            code = last                                  # mã trùng
        elif n == 8:
            code = "BAD"                                 # mã sai định dạng
        else:
            code = f"{random.choice(MODELS):<12}{random.randint(0, 9999999):07d}"
        last = code
        write_code(code)


def response(data=b"", end=0):
    body = struct.pack("<H", end) + data
    return b"\xD0\x00\x00\xFF\xFF\x03\x00" + struct.pack("<H", len(body)) + body


def recv_exact(conn, n):
    buf = b""
    while len(buf) < n:
        chunk = conn.recv(n - len(buf))
        if not chunk:
            raise ConnectionError("closed")
        buf += chunk
    return buf


def handle(conn, drop_at):
    while True:
        if drop_at and time.time() > drop_at:
            print("[SIM] !!! cố ý ngắt kết nối", flush=True)
            return "dropped"
        header = recv_exact(conn, 9)
        assert header[:2] == b"\x50\x00", header
        length = struct.unpack("<H", header[7:9])[0]
        body = recv_exact(conn, length)
        cmd, sub = struct.unpack("<HH", body[2:6])
        dev = body[6] | (body[7] << 8) | (body[8] << 16)
        code = body[9]
        points = struct.unpack("<H", body[10:12])[0]
        with lock:
            if cmd == 0x0401 and sub == 0x0001:          # đọc bit
                bits = [M.get(dev + i, 0) for i in range(points)]
                out = bytearray()
                for i in range(0, points, 2):
                    hi = bits[i]
                    lo = bits[i + 1] if i + 1 < points else 0
                    out.append((hi << 4) | lo)
                conn.sendall(response(bytes(out)))
            elif cmd == 0x0401 and sub == 0x0000:        # đọc word
                out = b"".join(struct.pack("<H", D.get(dev + i, 0)) for i in range(points))
                conn.sendall(response(out))
            elif cmd == 0x1401 and sub == 0x0001:        # ghi bit
                val = (body[12] >> 4) & 1
                if dev == TRIGGER and val == 0 and M.get(TRIGGER):
                    stats["acked"] += 1
                    print("[SIM] <- app xác nhận (reset M10)", flush=True)
                if dev == ALIVE:
                    stats["alive_writes"] += 1
                M[dev] = val
                conn.sendall(response())
            elif cmd == 0x1401 and sub == 0x0000:        # ghi word
                for i in range(points):
                    D[dev + i] = struct.unpack("<H", body[12 + 2 * i:14 + 2 * i])[0]
                conn.sendall(response())
            else:
                conn.sendall(response(end=0xC059))       # lệnh không hỗ trợ


def main():
    stop = threading.Event()
    threading.Thread(target=producer, args=(stop,), daemon=True).start()
    srv = socket.socket()
    srv.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    srv.bind(("127.0.0.1", args.port))
    srv.listen(5)
    srv.settimeout(1)
    end = time.time() + args.duration
    drop_at = time.time() + args.drop_after if args.drop_after else 0
    connections = 0
    print(f"[SIM] listening 127.0.0.1:{args.port}", flush=True)
    while time.time() < end:
        try:
            conn, _ = srv.accept()
        except socket.timeout:
            continue
        connections += 1
        print(f"[SIM] app kết nối (lần {connections})", flush=True)
        conn.settimeout(max(1, end - time.time()))
        try:
            if handle(conn, drop_at) == "dropped":
                drop_at = 0
        except Exception as ex:
            print(f"[SIM] kết nối đóng: {ex}", flush=True)
        finally:
            conn.close()
    stop.set()
    print(f"[SIM] KẾT QUẢ: số lần kết nối={connections}, mã gửi={stats['sent']}, app xác nhận={stats['acked']}, ghi alive={stats['alive_writes']}", flush=True)


if __name__ == "__main__":
    main()
