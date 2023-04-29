using static OpenPetsWorld.Program;

namespace OpenPetsWorld
{
    public class Logger
    {
        public void Info(string message)
        {
            Out(message, "INFO");
        }

        public void Warn(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Out(message, "WARN");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Out(message, "ERROR");
            Console.ForegroundColor = ConsoleColor.White;
        }

        private void Out(string message, string level)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            CoverWriteLine($"[{time}] [{level}]: {message}");
        }
    }
}