namespace ThemeParkWorldRPC
{
    public struct TpwMessageEventArgs
    {
        public string Message { get; }

        public TpwMessageEventArgs(string message)
        {
            Message = message;
        }
    }
}
