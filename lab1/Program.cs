using System;
using Gtk;

namespace lab1
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Application.Init();

            var app = new Application("org.lab1.lab1", GLib.ApplicationFlags.None);
            app.Register(GLib.Cancellable.Current);

            var win = new MainWindow();
            
            app.AddWindow(win);

            win.Show();
            Application.Run();
        }
    }
}