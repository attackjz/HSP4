using Perforce.P4;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace HijackSystem
{
    public class HijackFileManager
    {
        public HijackFileManager()
        {
            rep = rep = new Repository(new Server(new ServerAddress("")));
        }


        // create destructor to clean up the connection
        ~HijackFileManager()
        {
            rep.Connection.Disconnect();
        }

        public void InitWithArgs(string[] args)
        {
            // we're always going to have at least 3 arguments to link p4 server
            InitP4Server(args);
            if (rep.Connection.Status != ConnectionStatus.Connected)
            {
                return;
            }

            // linked to p4 server now
            // read path info from file
            InitPathInfo();

            // we don't have any files to hijack at this time
            if (args.Length <= 3)
            {
                return;
            }

            ParseHijackList(args.Skip(3).ToArray());
        }

        public void RevertWithDepotList(List<string> depotList)
        {
            IList<FileSpec> files = new List<FileSpec> { };
            foreach (var revertFile in depotList)
            {
                files.Add(new FileSpec(new DepotPath(revertFile)));
            }
            SyncFilesCmdOptions syncOpts = new SyncFilesCmdOptions(SyncFilesCmdFlags.Force);
            rep.Connection.Client.SyncFiles(files, syncOpts);

            foreach (var pendingPath in depotList)
            {
                // remove if pathInfoList depotPath equal to the pathInfo
                PathInfo pathInfo = PathInfoList.Find(x => x.depotPath == pendingPath);
                PathInfoList.Remove(pathInfo);
            }

            WritePathInfo();
        }

        private void ParseHijackList(string[] args)
        {
            foreach (var arg in args)
            {
                HijackFile(arg);
            }

            WritePathInfo();
        }

        private bool IsFileHijacked(string fileLocalPath)
        {
            // TODO: check if the file is really hijacked or not
            foreach (var pathInfo in PathInfoList)
            {
                if (pathInfo.localPath == fileLocalPath)
                {
                    return true;
                }
            }

            return false;
        }

        private void HijackFile(string fileLocalPath)
        {
            // get the depot path of the file
            Options opts = new Options();
            IList<FileSpec> files = new List<FileSpec> { new FileSpec(new LocalPath(fileLocalPath)) };
            var fileResult = rep.GetDepotFiles(files, opts);

            // which means the file is not in the depot, 
            // we don't need to hijack it
            if (fileResult == null)
            {
                return;
            }

            foreach (var file in fileResult)
            {
                string path = file.DepotPath.Path.ToString();
                PathInfo pathInfo = new PathInfo(path, fileLocalPath);
                HijackFileInternal(pathInfo);
            }
        }

        private void HijackFileInternal(PathInfo filePath)
        {
            // check if the file is already hijacked
            if (IsFileHijacked(filePath.localPath))
            {
                return;
            }


            if (System.IO.File.Exists(filePath.localPath))
            {
                FileAttributes attributes = System.IO.File.GetAttributes(filePath.localPath);

                bool isReadOnly = (attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
                if (isReadOnly)
                {
                    System.IO.File.SetAttributes(filePath.localPath, attributes & ~FileAttributes.ReadOnly);
                }
            }
            else
            {
                Console.WriteLine("File not found: " + filePath.localPath);
            }

            PathInfoList.Add(filePath);
        }

        private void RevertFile(PathInfo pathInfo)
        {
            if (rep.Connection.Status != ConnectionStatus.Connected)
            {
                return;
            }

            SyncFilesCmdOptions syncOpts = new SyncFilesCmdOptions(SyncFilesCmdFlags.Force);
            IList<FileSpec> files = new List<FileSpec> { new FileSpec(new DepotPath(pathInfo.depotPath)) };
            rep.Connection.Client.SyncFiles(files, syncOpts);

        }

        private void InitP4Server(string[] args)
        {
            if (args.Length < 3)
            {
                return;
            }

            // get these info from p4 custom tools
            serverPort = args[0];
            clientName = args[1];
            userName = args[2];

            LinkToEnvironmentP4Server(serverPort, clientName, userName);

        }

        public void LinkToEnvironmentP4Server(string serverPort, string clientName, string userName)
        {
            // define the server, repository and connection
            Server server = new Server(new ServerAddress(serverPort));
            rep = new Repository(server);
            Connection con = rep.Connection;

            // use the connection variables for this connection
            con.UserName = userName;
            con.Client = new Client();
            con.Client.Name = clientName;

            // connect to the server
            con.Connect(null);

            if (con.Status != ConnectionStatus.Connected)
            {
                // connection failed
                MessageBox.Show("Failed to connect to Perforce Server");
            }

        }

        private void InitPathInfo()
        {
            ReadPathInfo();
        }

        private void WritePathInfo()
        {
            string jsonString = JsonSerializer.Serialize(PathInfoList, new JsonSerializerOptions { WriteIndented = true });

            string filePath = GetHSFilePath();
            System.IO.File.WriteAllText(filePath, jsonString);
        }

        private void ReadPathInfo()
        {
            string filePath = GetHSFilePath();
            // check if file path valid
            if (!System.IO.File.Exists(filePath))
            {
                return;
            }
            string jsonString = System.IO.File.ReadAllText(filePath);
            var pathInfos = JsonSerializer.Deserialize<List<PathInfo>>(jsonString);
            PathInfoList = pathInfos != null ? pathInfos : new List<PathInfo>();
        }

        private string GetHSFilePath()
        {
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDataFolderPath = Path.Combine(localAppDataPath, "HSP4");
            if (!Directory.Exists(appDataFolderPath))
            {
                Directory.CreateDirectory(appDataFolderPath);
            }

            string filePath = Path.Combine(appDataFolderPath, "pathInfo.json");
            return filePath;
        }

        private string serverPort = "";
        private string clientName = "";
        private string userName = "";

        private Repository rep;

        public List<PathInfo> PathInfoList { get; private set; } = new List<PathInfo>();

    }

    public struct PathInfo
    {
        public PathInfo(string depotPath, string localPath)
        {
            this.depotPath = depotPath;
            this.localPath = localPath;
        }
        public string depotPath { get; set; }
        public string localPath { get; set; }
    }
}
