namespace SaveEditor;

public static class Cryptography
{
    public static byte[] Key = new byte[8]
    {
        18,
        54,
        100,
        160,
        190,
        148,
        136,
        3
    };

    public static byte[] IV = new byte[8]
    {
        82,
        242,
        164,
        132,
        119,
        197,
        179,
        20
    };
}