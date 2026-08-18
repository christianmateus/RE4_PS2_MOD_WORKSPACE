namespace RE4_PS2_MOD_WORKSPACE
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) => LogStartupError(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, e) => LogStartupError(e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString()));
            try
            {
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                LogStartupError(ex);
                MessageBox.Show("O RE4 PS2 Mod Workspace encontrou um erro ao iniciar.\n\nUm log foi salvo em %AppData%\\RE4_PS2_MOD_WORKSPACE\\startup_error.log", "Erro ao iniciar", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void LogStartupError(Exception ex)
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RE4_PS2_MOD_WORKSPACE");
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "startup_error.log"), $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\r\n\r\n");
            }
            catch { }
        }
    }
}
