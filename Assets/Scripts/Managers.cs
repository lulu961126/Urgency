using UnityEngine;

public static class Managers
{
    private static GameObject refugeeManager;
    private static GameObject stageManager;
    public static GameObject RefugeeManager { get
        {
            return refugeeManager ??= GameObject.FindWithTag("RefugeeManager");
        }
    }

    public static GameObject StageManager { get
        {
            return stageManager ??= GameObject.FindWithTag("StageManager");
        }
    }
}
