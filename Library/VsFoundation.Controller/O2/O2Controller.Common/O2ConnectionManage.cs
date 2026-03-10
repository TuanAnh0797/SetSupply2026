using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSLibrary.Communication;

namespace VsFoundation.Controller.O2.O2Controller.Common;

public class O2ConnectionManage : IO2ConnectionManage
{
    CancellationToken _cancellationToken;
    int _timeoutMs;
    int _retryCount;
    public ICommunication Connection { get; set; }
    public O2ConnectionManage(ICommunication connection, CancellationToken cancellationToken, int timeoutMs = 3000, int retryCount = 3)
    {
        Connection = connection;
        _timeoutMs = timeoutMs;
        _retryCount = retryCount;
        _cancellationToken = cancellationToken;
    }

    public async Task<string> ReadData(string command, params string[] pattern)
    {
        for (int count = 0; count < _retryCount; count++)
        {
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            List<byte> receiveBuffer = new();

            void ReceiveChanged(object? sender, byte[] e)
            {
                receiveBuffer.AddRange(e);
                string rec = Encoding.ASCII.GetString(e);
                bool ret = true;
                foreach (var tmp in pattern) { if (!rec.Contains(tmp)) ret = false; }
                if (ret)
                {
                    Connection.DataReceived -= ReceiveChanged;
                    tcs.TrySetResult(rec);
                }
            }

            Connection.DataReceived += ReceiveChanged;

            using (_cancellationToken.Register(() =>
            {
                Connection.DataReceived -= ReceiveChanged;
                tcs.TrySetCanceled(_cancellationToken);
            }))
            {
                var dataSent = Encoding.ASCII.GetBytes(command);
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
    public async Task<bool> WriteData(string command, params string[] pattern)
    {
        for (int count = 0; count < _retryCount; count++)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            List<byte> receiveBuffer = new();

            void ReceiveChanged(object? sender, byte[] e)
            {
                receiveBuffer.AddRange(e);
                string rec = Encoding.ASCII.GetString(e);
                bool ret = true;
                foreach (var tmp in pattern) { if (!rec.Contains(tmp)) ret = false; }
                if (ret)
                {
                    Connection.DataReceived -= ReceiveChanged;
                    tcs.TrySetResult(ret);
                }
            }

            Connection.DataReceived += ReceiveChanged;

            using (_cancellationToken.Register(() =>
            {
                Connection.DataReceived -= ReceiveChanged;
                tcs.TrySetCanceled(_cancellationToken);
            }))
            {
                var dataSent = Encoding.ASCII.GetBytes(command);
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

    public async Task WriteDataNotResponse(string command)
    {
        await Connection.SendAsync(Encoding.ASCII.GetBytes(command));
    }
}
