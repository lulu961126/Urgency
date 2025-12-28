using JetBrains.Annotations;
using UnityEngine;

public static class QTEStatus
{
    public static bool IsSuccess = false;
    public static bool IsFinish = true;
    public static bool AllowCallQTE = true;

    public static void QTEStart() => IsSuccess = IsFinish = false;

    public static void QTEFinish(bool isSuccess)
    {
        IsSuccess = isSuccess;
        IsFinish = true;
    }
}
