namespace PipBoy
{
    public static class DebugSettings
    {
        public static bool DumpInitialPacketParsing = true;
        public static bool DumpInitialPacketContent = true;
        public static bool DumpPacketParsing = true;

        public static bool DumpTcpStream = true;                    // dump incoming network data to TcpDumpFile
        public static string TcpDumpFile = "tcp.dump";

        public static bool UseTcp = true;                           // if false; read InputFile, if true; use Host/Port
        public static string InputFile = "complex.dump";
        public const string Host = "192.168.0.133";
        public const int Port = 27000;
    }
}
