using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.Threading;
using System.ComponentModel;
using System.IO;

namespace pulseCheck
{
    class Program
    {

        //[System.Runtime.InteropServices.DllImport("user32.d ll")]
        //private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        //private const int SW_MINIMIZE = 6;
        //private const int SW_MAXIMIZE = 3;
        //private const int SW_RESTORE = 9;

        private static decimal beatsPerMinute;
        private static decimal appbeatsPerMinute;
        private static string directory;
        private static string filename;
        private static string pulseFile;
        private static string messageDirectory;
        private static string processToEnd;
        private static string appToStart;
        private static string postProcessCommand;
        private static int fileReadErrors = 0;

        private static int starttime;
        private static int startday;
        private static int lastread;
        private static int lastbeat = -1;

        private const int secAllowance = 10;
        private const int fileReadErrorsAllowed = 5;
        private const string messagesFile = "pulseMessages.txt";

        private static List<string> messages = new List<string>();

        //private BackgroundWorker bw;

        private static bool called;

        //[STAThread]
        static void Main(string[] args)
        {

            //IntPtr winHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            //ShowWindow(winHandle, SW_MINIMIZE);

            Console.Title = "FreezeGuard";

            unpackCommandline();
            messagesInitialise();

            if (called)
            {

                //BackgroundWorker bw = new BackgroundWorker();

                //bw.DoWork -= new DoWorkEventHandler(worker);
                //bw.DoWork += new DoWorkEventHandler(worker);
                //bw.WorkerSupportsCancellation = true;
                //bw.RunWorkerAsync();

                worker();

            }

        }


        //private static void worker(object sender, DoWorkEventArgs e)
        private static void worker()
        {

            //string beat = "";
            List<string> beatData = new List<string>();
            int systole = 0;
            int diastole = 0;
            int intBeat = 0;

            lastread = 0;
            setStart();

            messagesAdd(dateTime() + " - " + "#### FreezeGuard Active ####");
            messagesAdd(dateTime() + " - " + "#### FreezeGuard App Beats Per Minute: " + appbeatsPerMinute.ToString("0.##") + " ####");
            messagesAdd(dateTime() + " - " + "#### FreezeGuard Checks Per Minute: " + beatsPerMinute.ToString("0.##") + " ####");
            messagesAdd(dateTime() + " - " + "pulse file: " + directory + filename);
            Console.WriteLine(dateTime() + " - " + "Checking app for pulse...");
            Console.WriteLine(dateTime() + " - " + "FreezeGuard App Beats Per Minute: " + appbeatsPerMinute.ToString("0.##"));
            Console.WriteLine(dateTime() + " - " + "FreezeGuard Checks Per Minute: " + beatsPerMinute.ToString("0.##"));
            Console.WriteLine(dateTime() + " - " + "pulse file: " + directory + filename);
            messagesWrite();


            while (true)
            {

                if (secondsSinceStart() - (lastread + secAllowance) >= (int)Math.Floor((60 / appbeatsPerMinute)))
                {

                    //string fileName = directory + filename;
                    //bool fileExists = File.Exists(fileName);
                    //Console.WriteLine("fileexists: " + fileExists.ToString());

                    systole = lastbeat;
                    beatData.Clear();
                    beatData = readFile();
                    //beat = readFile();
                    lastread = secondsSinceStart();

                    if (beatData[0] != "cannot read")
                    {
                        if (beatData[1].Trim() != "") postProcessCommand = @" /" + beatData[1];

                        try { intBeat = Convert.ToInt32(beatData[0]); }
                        catch { intBeat = 0; }

                        if (intBeat != 0) lastbeat = intBeat;

                        diastole = lastbeat;

                    }

                    //Console.WriteLine("systole/diastole: " + systole.ToString() + "/" + diastole.ToString());
                    //Console.WriteLine("fileReadErrors: " + fileReadErrors.ToString());

                    if (systole == diastole || fileReadErrors >= fileReadErrorsAllowed)
                    {
                        messagesAdd(dateTime() + " - " + "!!!NO PULSE DETECTED!!!");
                        Console.WriteLine(dateTime() + " - " + "!!!NO PULSE DETECTED!!!");
                        messagesAdd(dateTime() + " - " + "App needs to be stopped and restarted...");
                        Console.WriteLine(dateTime() + " - " + "App needs to be stopped and restarted...");
                        messagesAdd(dateTime() + " - " + "Stopping app...");
                        Console.WriteLine(dateTime() + " - " + "Stopping app...");
                        killProcess();
                        Console.WriteLine(dateTime() + " - " + "Restarting app...");
                        messagesAdd(dateTime() + " - " + "Restarting app...");

                        Console.WriteLine(dateTime() + " - " + "Command line: " + postProcessCommand);
                        messagesAdd(dateTime() + " - " + "Command line: " + postProcessCommand);

                        startProcess();
                        Console.WriteLine(dateTime() + " - " + "App restarted...");
                        messagesAdd(dateTime() + " - " + "App restarted...");
                        messagesWrite();
                        Console.WriteLine(dateTime() + " - " + "Waiting for 2 minutes...");
                        messagesAdd(dateTime() + " - " + "Waiting for 2 minutes...");
                        Thread.Sleep(120000);
                        Console.WriteLine(dateTime() + " - " + "Checking pulse again...");
                        messagesAdd(dateTime() + " - " + "Checking pulse again...");
                        messagesWrite();
                    }

                }

                Thread.Sleep((int)Math.Floor((60 / appbeatsPerMinute) * 500));

            }

        }


        private static void unpackCommandline()
        {

            string cmdLn = "";


#if DEBUG

            cmdLn = "";
            cmdLn += @"|processToEnd|TeboCam";
            cmdLn += @"|pulseDirectory|C:\Documents and Settings\Jagara\My Documents\Visual Studio 2005\Projects\TeboCam\TeboCam\bin\Debug\temp";
            cmdLn += @"|pulseFile|pulse.xml";
            cmdLn += @"|pulseMessageDirectory|C:\Documents and Settings\Jagara\My Documents\Visual Studio 2005\Projects\TeboCam\TeboCam\bin\Debug\logs";
            cmdLn += @"|beatsPerMinute|1";
            cmdLn += @"|checksPerMinute|0.75";
            cmdLn += @"|appToStart|C:\Documents and Settings\Jagara\My Documents\Visual Studio 2005\Projects\TeboCam\TeboCam\bin\Debug\TeboCam.exe";
            cmdLn += @"|firstPulse|1";
            cmdLn += @"|command|restart active";


#else

            foreach (string @arg in Environment.GetCommandLineArgs())
            {
                //Console.WriteLine("###########################ARG#################################");
                //Console.WriteLine(arg);
                //Console.WriteLine("###########################ARG#################################");
                cmdLn += arg;
            }

            if (cmdLn.IndexOf('|') == -1)
            {
                called = false;
                return;
            }

#endif

            string[] tmpCmd = cmdLn.Split('|');

            for (int i = 1; i < tmpCmd.GetLength(0); i++)
            {
                if (tmpCmd[i] == "pulseDirectory") directory = tmpCmd[i + 1];
                if (tmpCmd[i] == "pulseFile") filename = tmpCmd[i + 1];
                if (tmpCmd[i] == "pulseMessageDirectory") messageDirectory = tmpCmd[i + 1];
                if (tmpCmd[i] == "beatsPerMinute") appbeatsPerMinute = Convert.ToDecimal(tmpCmd[i + 1]);
                if (tmpCmd[i] == "checksPerMinute") beatsPerMinute = Convert.ToDecimal(tmpCmd[i + 1]);
                if (tmpCmd[i] == "processToEnd") processToEnd = tmpCmd[i + 1];
                if (tmpCmd[i] == "appToStart") appToStart = tmpCmd[i + 1];
                if (tmpCmd[i] == "firstPulse") lastbeat = Convert.ToInt32(tmpCmd[i + 1]);
                if (tmpCmd[i] == "command") postProcessCommand += @" /" + tmpCmd[i + 1];
                i++;
            }


            pulseFile = directory + filename;

            //Console.WriteLine("############################################################");
            //Console.WriteLine(postProcessCommand);
            //Console.WriteLine("#########################APPTOSTART###################################");
            //Console.WriteLine(appToStart);
            //Console.WriteLine("#########################APPTOSTART###################################");

            //Console.WriteLine("############################################################");
            //Console.WriteLine(directory);
            //Console.WriteLine(filename);
            //Console.WriteLine(pulseFile);
            //Console.WriteLine("############################################################");

            //Console.WriteLine(postProcessCommand);

            called = true;

        }


        private static void messagesInitialise()
        {

            string msgFile = messageDirectory + @"\" + messagesFile;
            string line = null;

            if (!File.Exists(msgFile))
            {

                try
                {
                    TextWriter tw = new StreamWriter(msgFile);
                    tw.WriteLine("Pulse Messages");
                    tw.WriteLine("");
                    tw.Close();
                }
                catch { }

            }

            try
            {
                TextReader tr = new StreamReader(msgFile);
                while ((line = tr.ReadLine()) != null) messages.Add(line);

                tr.Close();
                tr = null;
            }
            catch { }

        }

        private static void messagesWrite()
        {

            string msgFile = messageDirectory + @"\" + messagesFile;

            try
            {
                TextWriter tw = new StreamWriter(msgFile);
                foreach (string line in messages)
                {
                    tw.WriteLine(line);
                }
                tw.Close();
            }
            catch { }

        }

        private static void messagesAdd(string line)
        {
            line = DateTime.Now.ToString("yyyyMMdd-HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + "|" + line;
            messages.Add(line);
        }


        private static List<string> readFile()
        {

            List<string> returnList = new List<string>();
            //string tmpInt = "";
            fileReadErrors = 0;

            XmlTextReader pulseData = new XmlTextReader(pulseFile);


            do
            {

                try
                {
                    while (pulseData.Read())
                    {
                        if (pulseData.NodeType == XmlNodeType.Element)

                            if (pulseData.LocalName.Equals("beat"))
                            {
                                returnList.Add(pulseData.ReadString());
                                //tmpInt = pulseData.ReadString();
                            }

                        if (pulseData.LocalName.Equals("restartCommand"))
                        {
                            returnList.Add(pulseData.ReadString());
                            //postProcessCommand = @" /" + pulseData.ReadString();
                        }

                    }

                    pulseData.Close();
                    fileReadErrors = 0;

                }
                catch
                {

                    pulseData.Close();
                    fileReadErrors++;

                }

            } while (fileReadErrors > 0 && (fileReadErrors < fileReadErrorsAllowed));


            if (fileReadErrors == 0)
            {
                //return tmpInt;
                return returnList;//
            }
            else
            {
                //return "cannot read";
                returnList.Clear();
                returnList.Add("cannot read");
                return returnList;
            }

        }


        private static bool killProcess()
        {

            bool processKilled = false;
            int attempts = 0;

            try
            {

                while (processKilled == false && attempts < 10)
                {

                    Process[] processes = Process.GetProcesses();

                    foreach (Process process in processes)
                    {
                        if (process.ProcessName == processToEnd)
                        {
                            process.Kill();
                            processKilled = true;
                        }

                    }

                    attempts++;
                    if (!processKilled) Thread.Sleep(1000);

                }

                return processKilled;

            }
            catch
            {

                return false;

            }

        }

        private static void startProcess()
        {

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = appToStart;
            startInfo.Arguments = postProcessCommand;
            Process.Start(startInfo);

        }

        private static void setStart()
        {
            startday = Convert.ToInt32(DateTime.Now.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture));
            starttime = secondsSinceMidnight();
        }

        private static int secondsSinceStart()
        {

            int secsInDay = 86400;
            int thisday = Convert.ToInt32(DateTime.Now.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture));
            int daysSinceStart = Math.Abs(thisday - startday);


            int result = (daysSinceStart * secsInDay) - starttime + secondsSinceMidnight();
            return result;
        }

        private static int secondsSinceMidnight()
        {
            string tmpStr = DateTime.Now.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            int hour = Convert.ToInt32(LeftRightMid.Left(tmpStr, 2));
            int mins = Convert.ToInt32(LeftRightMid.Mid(tmpStr, 3, 2));
            int secs = Convert.ToInt32(LeftRightMid.Right(tmpStr, 2));
            int secsSinceMidnight = (hour * 3600) + (mins * 60) + secs;
            return secsSinceMidnight;
        }

        private static string dateTime()
        {

            string tmpStr = DateTime.Now.ToString("yyyyMMdd - HH:mm:ss:fff", System.Globalization.CultureInfo.InvariantCulture);
            return tmpStr;

        }




    }
}
