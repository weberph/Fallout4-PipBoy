namespace PipBoy
{
    public static class DebugSettings
    {
        public static bool UseTcp = false;
        public static bool DumpInitialPacketParsing = true;
        public static bool DumpInitialPacketContent = true;
        public static bool DumpPacketParsing = true;

        public const string Host = "192.168.0.133";
        public const int Port = 27000;
    }
}
