using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace APP.Helper;

/// <summary>
/// Giao tiếp PLC Mitsubishi qua MC protocol, khung 3E dạng binary.
/// Mỗi lệnh: gửi request, đọc ĐỦ header 9 byte + phần dữ liệu theo "Response data length".
/// Nếu có lỗi giữa chừng, stream có thể bị lệch => bên gọi phải đóng kết nối và kết nối lại.
/// </summary>
public class PLC : IDisposable
{
    // Subheader 5000 | Network 00 | PC FF | I/O 03FF | Station 00 | Length (2) | Monitoring timer 0010 (4s) | Command | Subcommand
    static readonly byte[] TemplateReadWord = { 0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00, 0x0C, 0x00, 0x10, 0x00, 0x01, 0x04, 0x00, 0x00 };
    static readonly byte[] TemplateReadBit = { 0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00, 0x0C, 0x00, 0x10, 0x00, 0x01, 0x04, 0x01, 0x00 };
    static readonly byte[] TemplateWrite = { 0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00, 0x0D, 0x00, 0x10, 0x00, 0x01, 0x14, 0x01, 0x00 };
    static readonly byte[] TemplateWritePath1 = { 0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00 };
    static readonly byte[] TemplateWritePath3 = { 0x10, 0x00, 0x01, 0x14, 0x00, 0x00 };

    private const int ResponseHeaderLength = 9;   // D000 + net + pc + io(2) + station + length(2)
    private const int MaxWordsPerRead = 960;      // giới hạn của lệnh batch read (word)

    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
    private bool _disposed = false;

    public PLC()
    {
    }

    public async Task<bool> ReadBit(NetworkStream stream, int timeOut, PlcDeviceType typeDevice, int device)
    {
        List<byte> cmd = new List<byte>();
        cmd.AddRange(TemplateReadBit);
        cmd.AddRange(BitConverter.GetBytes(device).Take(3));
        cmd.Add((byte)typeDevice);
        cmd.AddRange(BitConverter.GetBytes((ushort)1));

        byte[] data = await SendAndReceiveAsync(stream, cmd.ToArray(), timeOut, $"{typeDevice}{device}");
        if (data.Length < 1)
            throw new InvalidOperationException($"{typeDevice}{device} - Response has no data");

        // Đọc theo bit: mỗi byte chứa 2 điểm, điểm đầu tiên ở 4 bit cao
        return (data[0] & 0x10) != 0;
    }

    /// <summary>
    /// Đọc <paramref name="numberByte"/> word (tên giữ nguyên như bản cũ) bắt đầu từ device.
    /// </summary>
    public async Task<object> ReadData(NetworkStream stream, int timeOut, PlcDeviceType typeDevice, int device, int numberByte, DataType dataType)
    {
        if (numberByte < 1 || numberByte > MaxWordsPerRead)
            throw new ArgumentOutOfRangeException(nameof(numberByte), $"Số word đọc phải từ 1 đến {MaxWordsPerRead}");

        List<byte> cmd = new List<byte>();
        cmd.AddRange(TemplateReadWord);
        cmd.AddRange(BitConverter.GetBytes(device).Take(3));
        cmd.Add((byte)typeDevice);
        cmd.AddRange(BitConverter.GetBytes((ushort)numberByte));

        byte[] data = await SendAndReceiveAsync(stream, cmd.ToArray(), timeOut, $"{typeDevice}{device}");

        switch (dataType)
        {
            case DataType.FLOAT:
                EnsureLength(data, 4, typeDevice, device);
                return BitConverter.ToSingle(data, 0);
            case DataType.DECIMAL:
                EnsureLength(data, 4, typeDevice, device);
                return BitConverter.ToInt32(data, 0);
            case DataType.ASCII:
                // Lấy tới ký tự không in được đầu tiên (thường là \0 kết thúc chuỗi)
                int length = 0;
                while (length < data.Length && data[length] >= 32 && data[length] <= 126)
                    length++;
                return Encoding.ASCII.GetString(data, 0, length);
            default:
                throw new ArgumentException($"Unsupported data type: {dataType}");
        }
    }

    public async Task WriteBit(NetworkStream stream, int timeOut, PlcDeviceType typeDevice, int device, bool value = true)
    {
        List<byte> cmd = new List<byte>();
        cmd.AddRange(TemplateWrite);
        cmd.AddRange(BitConverter.GetBytes(device).Take(3));
        cmd.Add((byte)typeDevice);
        cmd.AddRange(new byte[] { 0x01, 0x00 });
        cmd.Add(value ? (byte)0x10 : (byte)0x00);

        await SendAndReceiveAsync(stream, cmd.ToArray(), timeOut, $"{typeDevice}{device}");
    }

    public async Task WriteASCII(NetworkStream stream, int timeOut, PlcDeviceType typeDevice, int headerDevice, int numberOfDevice, string data)
    {
        string paddedData = data.PadRight(numberOfDevice * 2, '\0');
        byte[] dataBytes = Encoding.ASCII.GetBytes(paddedData.Substring(0, numberOfDevice * 2));
        await WriteWordsAsync(stream, timeOut, typeDevice, headerDevice, numberOfDevice, dataBytes);
    }

    public async Task WriteDEC(NetworkStream stream, int timeOut, PlcDeviceType typeDevice, int headerDevice, int data)
    {
        await WriteWordsAsync(stream, timeOut, typeDevice, headerDevice, 2, BitConverter.GetBytes(data));
    }

    public async Task WriteFloat(NetworkStream stream, int timeOut, PlcDeviceType typeDevice, int headerDevice, float data)
    {
        await WriteWordsAsync(stream, timeOut, typeDevice, headerDevice, 2, BitConverter.GetBytes(data));
    }

    private async Task WriteWordsAsync(NetworkStream stream, int timeOut, PlcDeviceType typeDevice, int headerDevice, int numberOfDevice, byte[] dataBytes)
    {
        List<byte> cmd = new List<byte>();
        cmd.AddRange(TemplateWritePath1);

        ushort requestLength = (ushort)(TemplateWritePath3.Length + 6 + dataBytes.Length);
        cmd.AddRange(BitConverter.GetBytes(requestLength));

        cmd.AddRange(TemplateWritePath3);
        cmd.AddRange(BitConverter.GetBytes(headerDevice).Take(3));
        cmd.Add((byte)typeDevice);
        cmd.AddRange(BitConverter.GetBytes((ushort)numberOfDevice));
        cmd.AddRange(dataBytes);

        await SendAndReceiveAsync(stream, cmd.ToArray(), timeOut, $"{typeDevice}{headerDevice}");
    }

    /// <summary>
    /// Gửi lệnh, đọc đủ phản hồi, kiểm tra end code. Trả về phần dữ liệu (sau end code).
    /// </summary>
    private async Task<byte[]> SendAndReceiveAsync(NetworkStream stream, byte[] command, int timeOut, string deviceName)
    {
        await _lock.WaitAsync();
        try
        {
            using var cts = new CancellationTokenSource(timeOut);
            try
            {
                await stream.WriteAsync(command, 0, command.Length, cts.Token);

                byte[] header = await ReadExactAsync(stream, ResponseHeaderLength, cts.Token);
                if (header[0] != 0xD0 || header[1] != 0x00)
                    throw new InvalidOperationException($"{deviceName} - Invalid response subheader {header[0]:X2}{header[1]:X2}");

                int length = BitConverter.ToUInt16(header, 7); // end code (2) + data
                if (length < 2)
                    throw new InvalidOperationException($"{deviceName} - Invalid response length: {length}");

                byte[] body = await ReadExactAsync(stream, length, cts.Token);
                int endCode = BitConverter.ToUInt16(body, 0);
                if (endCode != 0)
                    throw new InvalidOperationException($"PLC error code: 0x{endCode:X4} for {deviceName}");

                return body.Skip(2).ToArray();
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"{deviceName} - Operation timed out after {timeOut}ms");
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private static async Task<byte[]> ReadExactAsync(NetworkStream stream, int count, CancellationToken token)
    {
        byte[] buffer = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            int read = await stream.ReadAsync(buffer, offset, count - offset, token);
            if (read == 0)
                throw new System.IO.IOException("PLC closed the connection");
            offset += read;
        }
        return buffer;
    }

    private static void EnsureLength(byte[] data, int length, PlcDeviceType typeDevice, int device)
    {
        if (data.Length < length)
            throw new InvalidOperationException($"{typeDevice}{device} - Response too short ({data.Length} bytes)");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _lock?.Dispose();
        }

        _disposed = true;
    }

    public enum PlcDeviceType : byte
    {
        X = 0x9C,
        Y = 0x9D,
        M = 0x90,
        L = 0x92,
        B = 0xA0,
        D = 0xA8,
        W = 0xB4,
        ZR = 0xB0
    }

    public enum DataType
    {
        DECIMAL,
        FLOAT,
        ASCII
    }
}
