namespace ThemeParkWorldRPC
{
    struct TpwRpcData
    {
        public int Cash { get; set; }
        public string Level { get; set; }
        public string LastLoad { get; set; }
        public int GoldenTicketCount { get; set; }
        public int GoldenKeyCount { get; set; }
        public string SaveName { get; set; }
        public bool InLobby { get; set; }
    }
}
