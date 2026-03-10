using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace APP.Helper;

public class PLC : IDisposable
{
    static readonly byte[] TemplateReadWord = { 0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x01, 0x04, 0x00, 0x00 };
    static readonly byte[] TemplateReadBit = { 0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x01, 0x04, 0x01, 0x00 };
    static readonly byte[] TemplateWrite = { 0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00, 0x0D, 0x00, 0x00, 0x00, 0x01, 0x14, 0x01, 0x00 };
    static readonly byte[] TemplateWritePath1 = { 0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00 };
    static readonly byte[] TemplateWritePath3 = { 0x00, 0x00, 0x01, 0x14, 0x00, 0x00 };

    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
    private bool _disposed = false;

    public PLC()
    {
    }

    public async Task<bool> ReadBit(NetworkStream stream, int timeOut, PlcDeviceType typeDevice, int device)
    {
        await _lock.WaitAsync();
        try
        {
            byte[] dataReceiveFromPLC = new byte[100];

            // Build command
            List<byte> cmdSendPLC = new List<byte>();
            cmdSendPLC.AddRange(TemplateReadBit);
            cmdSendPLC.AddRange(BitConverter.GetBytes(device).Take(3));
            cmdSendPLC.Add((byte)typeDevice);
            cmdSendPLC.AddRange(BitConverter.GetBytes((ushort)1));

            // Send & Receive
            using var cts = new CancellationTokenSource(timeOut);
            byte[] cmdArray = cmdSendPLC.ToArray();

            try
            {
                await stream.WriteAsync(cmdArray, 0, cmdArray.Length, cts.Token);
                int bytesRead = await stream.ReadAsync(dataReceiveFromPLC, 0, dataReceiveFromPLC.Length, cts.Token);

                // Validate response
                if (bytesRead < 15)
                    throw new InvalidOperationException($"Invalid response length: {bytesRead}");

                if (dataReceiveFromPLC[9] != 0 || dataReceiveFromPLC[10] != 0)
                {
                    int errorCode = BitConverter.ToUInt16(dataReceiveFromPLC, 9);
                    throw new InvalidOperationException($"PLC error code: 0x{errorCode:X4} for {typeDevice}{device}");
                }

                return BitConverter.ToInt32(dataReceiveFromPLC, 11) != 0;
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"{typeDevice}{device} - Operation timed out after {timeOut}ms");
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<object> ReadData(NetworkStream stream, int timeOut, PlcDeviceType typeDevice, int device, int numberByte, DataType dataType)
    {
        await _lock.WaitAsync();
        try
        {
            byte[] dataReceiveFromPLC = new byte[100];

            // Build command
            List<byte> cmdReadDataPLC = new List<byte>();
            cmdReadDataPLC.AddRange(TemplateReadWord);
            cmdReadDataPLC.AddRange(BitConverter.GetBytes(device).Take(3));
            cmdReadDataPLC.Add((byte)typeDevice);
            cmdReadDataPLC.AddRange(BitConverter.GetBytes((ushort)numberByte));

            // Send & Receive
            using var cts = new CancellationTokenSource(timeOut);
            byte[] cmdArray = cmdReadDataPLC.ToArray();

            try
            {
                await stream.WriteAsync(cmdArray, 0, cmdArray.Length, cts.Token);
                int bytesRead = await stream.ReadAsync(dataReceiveFromPLC, 0, dataReceiveFromPLC.Length, cts.Token);

                // Validate response
                if (bytesRead < 15)
                    throw new InvalidOperationException($"Invalid response length: {bytesRead}");

                if (dataReceiveFromPLC[9] != 0 || dataReceiveFromPLC[10] != 0)
                {
                    int errorCode = BitConverter.ToUInt16(dataReceiveFromPLC, 9);
                    throw new InvalidOperationException($"PLC error code: 0x{errorCode:X4} for {typeDevice}{device}");
                }

                // Parse data based on type
                return dataType switch
                {
                    DataType.FLOAT => BitConverter.ToSingle(dataReceiveFromPLC, 11),
                    DataType.DECIMAL => BitConverter.ToInt32(dataReceiveFromPLC, 11),
                    DataType.ASCII => Encoding.ASCII.GetString(dataReceiveFromPLC, 11, FindNullTerminator(dataReceiveFromPLC))
                                        .TrimEnd('\0', '\r', '\n'),
                    _ => throw new ArgumentException($"Unsupported data type: {dataType}")
                };
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"{typeDevice}{device} - Operation timed out after {timeOut}ms");
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task WriteBit(NetworkStream stream, int timeOut, PlcDeviceType typeDevice, int device, bool value = true)
    {
        await _lock.WaitAsync();
        try
        {
            byte[] dataReceiveFromPLC = new byte[100];

            // Build command
            List<byte> cmdSendPLC = new List<byte>();
            cmdSendPLC.AddRange(TemplateWrite);
            cmdSendPLC.AddRange(BitConverter.GetBytes(device).Take(3));
            cmdSendPLC.Add((byte)typeDevice);
            cmdSendPLC.AddRange(new byte[] { 0x01, 0x00 });
            cmdSendPLC.Add(value ? (byte)0x10 : (byte)0x00);

            // Send & Receive
            using var cts = new CancellationTokenSource(timeOut);
            byte[] cmdArray = cmdSendPLC.ToArray();

            try
            {
                await stream.WriteAsync(cmdArray, 0, cmdArray.Length, cts.Token);
                int bytesRead = await stream.ReadAsync(dataReceiveFromPLC, 0, dataReceiveFromPLC.Length, cts.Token);

                // Validate response
                if (dataReceiveFromPLC[9] != 0 || dataReceiveFromPLC[10] != 0)
                {
                    int errorCode = BitConverter.ToUInt16(dataReceiveFromPLC, 9);
                    throw new InvalidOperationException($"PLC error code: 0x{errorCode:X4} for {typeDevice}{device}");
                }
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"{typeDevice}{device} - Operation timed out after {timeOut}ms");
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task WriteASCII(NetworkStream stream, int timeOut, PlcDeviceType typeDevice, int headerDevice, int numberOfDevice, string data)
    {
        await _lock.WaitAsync();
        try
        {
            byte[] dataReceiveFromPLC = new byte[100];

            // Padding data if needed
            string paddedData = data.PadRight(numberOfDevice * 2, '\0');
            byte[] dataBytes = Encoding.ASCII.GetBytes(paddedData.Substring(0, numberOfDevice * 2));

            // Build command
            List<byte> cmdSendPLC = new List<byte>();
            cmdSendPLC.AddRange(TemplateWritePath1);

            ushort path2Length = (ushort)(TemplateWritePath3.Length + 6 + dataBytes.Length);
            cmdSendPLC.AddRange(BitConverter.GetBytes(path2Length));

            cmdSendPLC.AddRange(TemplateWritePath3);
            cmdSendPLC.AddRange(BitConverter.GetBytes(headerDevice).Take(3));
            cmdSendPLC.Add((byte)typeDevice);
            cmdSendPLC.AddRange(BitConverter.GetBytes((ushort)numberOfDevice));
            cmdSendPLC.AddRange(dataBytes);

            // Send & Receive
            using var cts = new CancellationTokenSource(timeOut);
            byte[] cmdArray = cmdSendPLC.ToArray();

            try
            {
                await stream.WriteAsync(cmdArray, 0, cmdArray.Length, cts.Token);
                int bytesRead = await stream.ReadAsync(dataReceiveFromPLC, 0, dataReceiveFromPLC.Length, cts.Token);

                // Validate response
                if (dataReceiveFromPLC[9] != 0 || dataReceiveFromPLC[10] != 0)
                {
                    int errorCode = BitConverter.ToUInt16(dataReceiveFromPLC, 9);
                    throw new InvalidOperationException($"PLC error code: 0x{errorCode:X4} for {typeDevice}{headerDevice}");
                }
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"{typeDevice}{headerDevice} - Operation timed out after {timeOut}ms");
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task WriteDEC(NetworkStream stream, int timeOut, PlcDeviceType typeDevice, int headerDevice, int data)
    {
        await _lock.WaitAsync();
        try
        {
            byte[] dataReceiveFromPLC = new byte[100];
            byte[] dataBytes = BitConverter.GetBytes(data);

            // Build command
            List<byte> cmdSendPLC = new List<byte>();
            cmdSendPLC.AddRange(TemplateWritePath1);

            ushort path2Length = (ushort)(TemplateWritePath3.Length + 6 + dataBytes.Length);
            cmdSendPLC.AddRange(BitConverter.GetBytes(path2Length));

            cmdSendPLC.AddRange(TemplateWritePath3);
            cmdSendPLC.AddRange(BitConverter.GetBytes(headerDevice).Take(3));
            cmdSendPLC.Add((byte)typeDevice);
            cmdSendPLC.AddRange(BitConverter.GetBytes((ushort)2));
            cmdSendPLC.AddRange(dataBytes);

            // Send & Receive
            using var cts = new CancellationTokenSource(timeOut);
            byte[] cmdArray = cmdSendPLC.ToArray();

            try
            {
                await stream.WriteAsync(cmdArray, 0, cmdArray.Length, cts.Token);
                int bytesRead = await stream.ReadAsync(dataReceiveFromPLC, 0, dataReceiveFromPLC.Length, cts.Token);

                // Validate response
                if (dataReceiveFromPLC[9] != 0 || dataReceiveFromPLC[10] != 0)
                {
                    int errorCode = BitConverter.ToUInt16(dataReceiveFromPLC, 9);
                    throw new InvalidOperationException($"PLC error code: 0x{errorCode:X4} for {typeDevice}{headerDevice}");
                }
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"{typeDevice}{headerDevice} - Operation timed out after {timeOut}ms");
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task WriteFloat(NetworkStream stream, int timeOut, PlcDeviceType typeDevice, int headerDevice, float data)
    {
        await _lock.WaitAsync();
        try
        {
            byte[] dataReceiveFromPLC = new byte[100];
            byte[] dataBytes = BitConverter.GetBytes(data);

            // Build command
            List<byte> cmdSendPLC = new List<byte>();
            cmdSendPLC.AddRange(TemplateWritePath1);

            ushort path2Length = (ushort)(TemplateWritePath3.Length + 6 + dataBytes.Length);
            cmdSendPLC.AddRange(BitConverter.GetBytes(path2Length));

            cmdSendPLC.AddRange(TemplateWritePath3);
            cmdSendPLC.AddRange(BitConverter.GetBytes(headerDevice).Take(3));
            cmdSendPLC.Add((byte)typeDevice);
            cmdSendPLC.AddRange(BitConverter.GetBytes((ushort)2));
            cmdSendPLC.AddRange(dataBytes);

            // Send & Receive
            using var cts = new CancellationTokenSource(timeOut);
            byte[] cmdArray = cmdSendPLC.ToArray();

            try
            {
                await stream.WriteAsync(cmdArray, 0, cmdArray.Length, cts.Token);
                int bytesRead = await stream.ReadAsync(dataReceiveFromPLC, 0, dataReceiveFromPLC.Length, cts.Token);

                // Validate response
                if (dataReceiveFromPLC[9] != 0 || dataReceiveFromPLC[10] != 0)
                {
                    int errorCode = BitConverter.ToUInt16(dataReceiveFromPLC, 9);
                    throw new InvalidOperationException($"PLC error code: 0x{errorCode:X4} for {typeDevice}{headerDevice}");
                }
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"{typeDevice}{headerDevice} - Operation timed out after {timeOut}ms");
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private int FindNullTerminator(byte[] input)
    {
        int index = 0;
        for (int i = 11; i < input.Length; i++)
        {
            if (input[i] < 32 || input[i] > 126)
            {
                return index;
            }
            index++;
        }
        return 80;
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