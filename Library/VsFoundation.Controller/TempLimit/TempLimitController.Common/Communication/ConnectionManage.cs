using VSLibrary.Communication;

namespace VsFoundation.Controller.TempLimit.TempLimitController.Common.Communication;

public class ConnectionManage : IConnectionManage
{
    CancellationToken _cancellationToken;
    int _timeoutMs;
    int _retryCount;
    public IProtocolFrame Frame { get; set; }
    public ICommunication Connection { get; set; }
    public ConnectionManage(IProtocolFrame frame, ICommunication connection, CancellationToken cancellationToken, int timeoutMs = 3000, int retryCount = 3)
    {
        Frame = frame;
        Connection = connection;
        _timeoutMs = timeoutMs;
        _retryCount = retryCount;
        _cancellationToken = cancellationToken;
    }

    public async Task<List<short>> ReadData(byte slaveId, ushort startAddress, ushort numRegisters)
    {
        for (int count = 0; count < _retryCount; count++)
        {
            var tcs = new TaskCompletionSource<List<short>>(TaskCreationOptions.RunContinuationsAsynchronously);
            List<byte> receiveBuffer = new();
            string error;

            void ReceiveChanged(object? sender, byte[] e)
            {
                receiveBuffer.AddRange(e);

                if (Frame.ValidByteReceive(receiveBuffer.ToArray(), out error))
                {
                    Connection.DataReceived -= ReceiveChanged;
                    List<short> data = Frame.GetListReceiveData(receiveBuffer.ToArray());
                    tcs.TrySetResult(data);
                }
            }

            Connection.DataReceived += ReceiveChanged;

            using (_cancellationToken.Register(() =>
            {
                Connection.DataReceived -= ReceiveChanged;
                tcs.TrySetCanceled(_cancellationToken);
            }))
            {
                var dataSent = Frame.CreateRead(slaveId, startAddress, numRegisters);
                receiveBuffer.Clear();
                await Connection.SendAsync(dataSent);

                var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(_timeoutMs, _cancellationToken));

                if (completedTask == tcs.Task)
                {
                    return await tcs.Task;
                }
                else
                {
                    Connection.DataReceived -= ReceiveChanged;
                }
            }
        }
        throw new TimeoutException($"readData timeout after {_retryCount} retries.");
    }
    public async Task<bool> WriteData(byte slaveId, ushort startAddress, short[] values)
    {
        for (int count = 0; count < _retryCount; count++)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            List<byte> receiveBuffer = new();
            string error;

            void ReceiveChanged(object? sender, byte[] e)
            {
                receiveBuffer.AddRange(e);

                if (Frame.ValidByteReceive(receiveBuffer.ToArray(), out error))
                {
                    Connection.DataReceived -= ReceiveChanged;
                    tcs.TrySetResult(true);
                }
            }

            Connection.DataReceived += ReceiveChanged;

            using (_cancellationToken.Register(() =>
            {
                Connection.DataReceived -= ReceiveChanged;
                tcs.TrySetCanceled(_cancellationToken);
            }))
            {
                var dataSent = Frame.CreateWrite(slaveId, startAddress, values);
                receiveBuffer.Clear();
                await Connection.SendAsync(dataSent);

                var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(_timeoutMs, _cancellationToken));

                if (completedTask == tcs.Task)
                {
                    return await tcs.Task;
                }
                else
                {
                    Connection.DataReceived -= ReceiveChanged;
                }
            }
        }
        throw new TimeoutException($"Write Data timeout after {_retryCount} retries.");
    }

    public async Task WriteDataNotResponse(byte slaveId, ushort startAddress, short[] values)
    {
        await Connection.SendAsync
            (Frame.CreateWrite(slaveId, startAddress, values));
    }
}

