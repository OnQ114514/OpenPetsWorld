using System.Diagnostics;
using static OpenPetsWorld.Program;

namespace OpenPetsWorld
{
    /// <summary>
    /// 硬核日志类（仅控制台）
    /// </summary>
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
            LogCoverLine($"[{time}] [{level}]: {message}");
        }
        
        private static void LogCoverLine(string text = "")
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Debug.WriteLine(text);
        }
    }
}