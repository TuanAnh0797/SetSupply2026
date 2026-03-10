using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsFoundation.Sequence.Constants;
public enum eMotorAlarmType
{
    NOT_HOME = 0,
    AMP_ALARM,
    SOFT_MIN_LIMIT,
    SOFT_MAX_LIMIT,
    MIN_LIMIT,
    MAX_LIMIT,
    HOME_START_FAIL,
    HOME_FAIL,
    TIMEOUT,
    START_FAIL,
    RESTORE_FAIL,
}

