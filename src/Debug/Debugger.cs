namespace Ipk25Chat.Debug
{
    public static class Debugger
    {
        private static bool _isEnabeled;

        public static void Enable(bool enable)
        {
            _isEnabeled = true;
        }

        public static void Log(string message)
        {
            if (_isEnabeled)
            {
                Console.Error.WriteLine($"DEBUG: {message}");
            }
        }
    }
}