/// <summary>
/// 存档操作结果结构体
/// </summary>
public struct SaveResult
{
    public bool Success;
    public SaveErrorCode ErrorCode;
    public string ErrorMessage;
    public GameData Data; // 仅在 Load 成功时有值

    public static SaveResult Ok()
    {
        return new SaveResult { Success = true };
    }

    public static SaveResult Ok(GameData data)
    {
        return new SaveResult { Success = true, Data = data };
    }

    public static SaveResult Fail(SaveErrorCode code, string msg)
    {
        return new SaveResult { Success = false, ErrorCode = code, ErrorMessage = msg };
    }
}
