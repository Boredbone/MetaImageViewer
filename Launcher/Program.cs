using System;
using System.Collections.Generic;
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
                    //ミューテックスの所有権を要求する
                    if (mutex.WaitOne(0, false))
                    {
                        isServerRunning = false;
                    }
                }
                //Console.WriteLine(isServerRunning.ToString());
                //
                //foreach(var l in args)
                //{
                //    Console.WriteLine(l);
                //}
                //Console.ReadLine();

                if (!isServerRunning)
                {
                    var dir = System.AppDomain.CurrentDomain.BaseDirectory;
                    //var assembly = Assembly.GetEntryAssembly();
                    //var directory = System.IO.Path.GetDirectoryName(assembly.Location);
                    var path = dir + serverPath;
                    //Console.WriteLine(path);
                    //Console.ReadLine();
                    var p = System.Diagnostics.Process.Start(path);
                }

                using (var pipeClient =
                    new NamedPipeClientStream(".", pipeId, PipeDirection.Out))
                {


                    pipeClient.Connect();

                    //Console.WriteLine("Connected to pipe.");
                    //Console.WriteLine("There are currently {0} pipe server instances open.",
                    //   pipeClient.NumberOfServerInstances);

                    // Read user input and send that to the client process.
                    using (var sw = new StreamWriter(pipeClient))
                    {
                        sw.AutoFlush = true;
                        foreach (var line in args)
                        {
                            //Console.WriteLine(line);
                            sw.WriteLine(line);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //Console.WriteLine("ERROR: {0}", e.Message);
            }
        }
    }
}
