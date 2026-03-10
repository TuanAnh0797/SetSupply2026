using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSLibrary.Communication;

namespace VsFoundation.Controller.O2.O2Controller.Common;

public interface IO2ConnectionManage
{
    ICommunication Connection { get; set; }
    Task<string> ReadData(string command, params string[] pattern);
    Task<bool> WriteData(string command, params string[] pattern);
    Task WriteDataNotResponse(string command);
}
