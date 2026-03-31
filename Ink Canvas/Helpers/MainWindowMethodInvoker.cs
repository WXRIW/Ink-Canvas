using System;
using System.Reflection;

namespace Ink_Canvas.Helpers
{
    internal sealed class MainWindowMethodInvoker
    {
        private readonly Func<MainWindow> getMainWindow;

        public MainWindowMethodInvoker(Func<MainWindow> getMainWindow)
        {
            this.getMainWindow = getMainWindow;
        }

        public MainWindow GetMainWindow()
        {
            return getMainWindow?.Invoke();
        }

        public void Invoke(string methodName, params object[] parameters)
        {
            MainWindow mainWindow = GetMainWindow();
            if (mainWindow == null) return;

            MethodInfo method = typeof(MainWindow).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(mainWindow, parameters);
        }
    }
}
