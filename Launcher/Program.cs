using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Launcher
{
    class Program
    {

        private const string mutexId = "ad6e4711-0ca9-4abe-b75e-0f5fcc876424";
        private const string pipeId = "bca7161c-20cd-4f98-b22b-6efc09b15ea6";
        private const string serverPath = @"MetaImageViewer.exe";

        static void Main(string[] args)
        {
            if (args == null || args.Length <= 0)
            {
                return;
            }

            try
            {
                var isServerRunning = true;
                using (var mutex = new Mutex(false, mutexId))
                {
                    try
                    {
                        //ミューテックスの所有権を要求する
                        if (mutex.WaitOne(0, false))
                        {
                            //取得できた
                            isServerRunning = false;
                            mutex.ReleaseMutex();
                            mutex.Close();
                        }
                    }
                    catch (AbandonedMutexException)
                    {
                        //isServerRunning = false;
                    }
                }

                if (!isServerRunning)
                {
                    var dir = System.AppDomain.CurrentDomain.BaseDirectory;
                    var path = dir + serverPath;

                    var psi = new ProcessStartInfo() { FileName = path, WorkingDirectory = dir };

                    var p = System.Diagnostics.Process.Start(psi);
                }

                using (var pipeClient =
                    new NamedPipeClientStream(".", pipeId, PipeDirection.Out))
                {
                    pipeClient.Connect(10000);

                    // Read user input and send that to the client process.
                    using (var sw = new StreamWriter(pipeClient))
                    {
                        sw.AutoFlush = true;
                        foreach (var line in args)
                        {
                            sw.WriteLine(line);
                        }
                    }
                }
            }
            catch (NotImplementedException)//(Exception e)
            {
            }
        }
    }
}
