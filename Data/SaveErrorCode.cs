/// <summary>
/// 存档操作错误码
/// </summary>
public enum SaveErrorCode
{
    None = 0,
    FileNotFound,
    FileCorrupted,
    DecryptionFailed,
    DeserializationFailed,
    MigrationFailed,
    InvalidSlot,
    IoError
}
