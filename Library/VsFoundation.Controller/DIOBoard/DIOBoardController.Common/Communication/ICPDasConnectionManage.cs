using MySqlX.XDevAPI.Common;
using System.Data;
using System.Text;
using VSLibrary.Communication;

namespace VsFoundation.Controller.DIOBoard.DIOBoardController.Common.Communication;

public class ICPDasConnectionManage
{
    CancellationToken _cancellationToken;
    int _timeoutMs;
    int _retryCount;
    public ICommunication Connection { get; set; }
    public ICPDasConnectionManage(ICommunication connection, CancellationToken cancellationToken, int timeoutMs = 3000, int retryCount = 3)
    {
        Connection = connection;
        _timeoutMs = timeoutMs;
        _retryCount = retryCount;
        _cancellationToken = cancellationToken;
    }

    public async Task<string> ReadData(byte[] commands)
    {
        for (int count = 0; count < _retryCount; count++)
        {
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            List<byte> receiveBuffer = new();

            void ReceiveChanged(object? sender, byte[] e)
            {
                receiveBuffer.AddRange(e);
                var dataStr = Encoding.ASCII.GetString(receiveBuffer.ToArray());
                bool valid = dataStr.StartsWith(">") && dataStr.EndsWith("\r");

                if (valid)
                {
                    Connection.DataReceived -= ReceiveChanged;
                    tcs.TrySetResult(dataStr);
                }
            }

            Connection.DataReceived += ReceiveChanged;

            using (_cancellationToken.Register(() =>
            {
                Connection.DataReceived -= ReceiveChanged;
                tcs.TrySetCanceled(_cancellationToken);
            }))
            {
                receiveBuffer.Clear();
                await Connection.SendAsync(commands);

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
    public async Task<bool> WriteData(byte[] commands)
    {
        for (int count = 0; count < _retryCount; count++)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            List<byte> receiveBuffer = new();

            void ReceiveChanged(object? sender, byte[] e)
            {
                receiveBuffer.AddRange(e);
                var dataStr = Encoding.ASCII.GetString(receiveBuffer.ToArray());
                bool valid = dataStr.StartsWith(">") && dataStr.EndsWith("\r");

                if (valid)
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
                receiveBuffer.Clear();
                await Connection.SendAsync(commands);

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
}
