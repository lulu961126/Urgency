using System;
using UnityEngine;

public static class QTEStatus
{
    public static bool IsSuccess = false;
    public static bool IsFinish = true;
    public static bool AllowCallQTE = true;

    // 當需要外部強制失敗時（如受傷）觸發
    public static Action OnQTEForceFail;

    public static bool IsInQTE => !IsFinish;

    public static void QTEStart()
    {
        IsSuccess = IsFinish = false;
    }

    public static void QTEFinish(bool isSuccess)
    {
        IsSuccess = isSuccess;
        IsFinish = true;
    }
}
