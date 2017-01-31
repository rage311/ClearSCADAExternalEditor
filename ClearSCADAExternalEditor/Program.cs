using System;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace ClearSCADAExternalEditor
{
    class Program
    {
        const string system     = "MAIN";
        const string user       = "scada";
        const string password   = "scada";
        const string mimicPath  = "test.new_mimic";
        const string editorPath = @"C:\Program Files (x86)\Vim\vim80\gvim.exe";

        static ViewX.Mimic mimic;
        static Process editorProcess;
        static ViewX.Application ViewXApp;
        static string myDocPath;

        static string filename = mimicPath + ".vbs";
        static string fullPath;

        static void InitApp()
        {
            // Set a variable to the My Documents path.
            myDocPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            fullPath = myDocPath + @"\" + filename;

            Console.WriteLine("Opening ViewX client and logging on to system...");

            ViewXApp = new ViewX.Application();
            if (ViewXApp.IsSystemConnected[system])
            {
                try
                {
                    ViewXApp.Logon(system, user, password);
                    ViewXApp.Visible = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Server is offline or cannot be found.\r\n" + e.Message);
                    Environment.Exit(1);
                }
            }
        }

        static void OpenMimic(string mimicFullPath)
        {
            try
            {
                mimic = (ViewX.Mimic)ViewXApp.Mimics.OpenFromServer(false, system, mimicFullPath);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to open Mimic " + mimicFullPath, e.Message);
                Environment.Exit(1);
            }
        }

        static void StartEditor(string editorFullPath, string fileFullPath)
        {
            try
            {
                Console.WriteLine("Starting editor...");

                editorProcess = Process.Start(editorFullPath, fileFullPath);
                editorProcess.EnableRaisingEvents = true;
                editorProcess.Exited += Process_Exited;
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to start editor process.\r\n" + e.Message);
                Environment.Exit(1);
            }
        }

        private static void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("Watcher_Changed");
            using (StreamReader inputFile = new StreamReader(fullPath))
            {
                mimic.Script = inputFile.ReadToEnd();
                mimic.Save();
            }
        }

        private static void Process_Exited(object sender, EventArgs e)
        {
            Console.WriteLine("Process_Exited");
            CleanUp();
        }

        private static void CleanUp()
        {
            mimic.Close();
            ViewXApp.Logoff(system);
            Environment.Exit(0);
        }

        static void Main(string[] args)
        {
            Console.WriteLine(String.Join(", ", args));
            InitApp();
            OpenMimic(mimicPath);

            // The vim equivalent of the built-in ViewX editor settings
            string script = "'vim: set noexpandtab sts=4 ts=4 sw=4:";
            if (mimic.ScriptEnabled)
            {
                if (mimic.Script.Length > 0)
                    script = mimic.Script;
            }
            else
            {
                Console.WriteLine("Enabling scripting on mimic");
                mimic.ScriptEnabled = true;
            }

            // Write the string to a new file
            using (StreamWriter outputFile = new StreamWriter(fullPath))
            {
                outputFile.Write(script);
            }

            // Open editor
            StartEditor(editorPath, fullPath);

            // Create a new FileSystemWatcher and set its properties.
            FileSystemWatcher watcher = new FileSystemWatcher(myDocPath, filename);
            // Watch for changes in LastWrite time
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            // Begin watching.
            watcher.EnableRaisingEvents = true;
            watcher.Changed += Watcher_Changed;

            // Just keep the main process alive while we wait for events
            while (true)
            {
                Thread.Sleep(10000);
            }
        }
    }
}
