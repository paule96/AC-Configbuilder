using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using McMaster.Extensions.CommandLineUtils;
using Sprache;
using Newtonsoft.Json;
using System.Linq;
using System.Reflection;

namespace ACConfigBuilder
{
    class Program
    {
        static void Main(string[] args)

        {
            try
            {
                Commands obj = new Commands();
                obj.Idel(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
    public class Commands
    {
        public int Idel(string[] commands)  // start 
        {
            var app = new CommandLineApplication();
            Execute obj = new Execute();
            var helptemplate = "-h|--help"; //definition of the helptemplate
            app.HelpOption(helptemplate);
            app.Command("replace", u => //should search and replace configs that allready exist
            {
                u.HelpOption(helptemplate);
                u.Description = "Dieser Befehl soll es ermöglichen die hinterlegte Konfiguration zu editieren.";
                var path = u.Option("--path <fullpath>", "Setzt einen dauerhaften benutzerdefinierten Pfad. Wenn dieser Befehl nicht benutzt wird, wird der Pfad , welcher in der Config.json als userpath angegeben ist verwendet.", CommandOptionType.SingleValue);
                var configPath = u.Option("--config <fullpath>", "Benutzt ein benutzerdefinierte Konfiguration. Wenn dieser Befehl nicht benutzt wird die Standardkonfiguration verwendet.", CommandOptionType.SingleValue);
                var templatePath = u.Option("--template <fullpath>", "Benutzt einen benutzerdefiniertes Templateverzeichnis. Wenn dieser Befehl nicht benutzt wird, werden die Standardtemplates verwendet.", CommandOptionType.SingleValue);
                u.OnExecute(() => { obj.runReplace(path, configPath, templatePath); });
            });
            app.Command("create", c => //creats a new config 
            {
                c.HelpOption(helptemplate);
                c.Description = "Erstellt eine neue Configvorlage.";
                var path = c.Option("--path <fullpath>", "Benutzt einen benutzerdefinierten Pfad. Wenn dieser Befehl nicht benutzt wird, wird der Pfad , welcher in der Config.json als changeDirectory angegeben ist verwendet.", CommandOptionType.SingleValue);
                var configPath = c.Option("--config <fullpath>", "Benutzt ein benutzerdefinierte Konfiguration. Wenn dieser Befehl nicht benutzt wird die Standardkonfiguration verwendet.", CommandOptionType.SingleValue);
                var templatePath = c.Option("--template <fullpath>", "Benutzt einen benutzerdefiniertes Templateverzeichnis. Wenn dieser Befehl nicht benutzt wird, werden die Standardtemplates verwendet.", CommandOptionType.SingleValue);
                var Net = c.Option("--networkdev <anzahl>", "Setzt die Anzahl für Networkdevabschnitte. Normal ist dieser Wert auf 1", CommandOptionType.SingleValue);
                var Int = c.Option("--interfacenetworkif <anzahl>", "Setzt die Anzahl für Interfacenetworkifabschnitte. Normal ist dieser Wert auf 1", CommandOptionType.SingleValue);
                var Set = c.Option("--proxyset <anzahl>", "Setzt die Anzahl für Proxysetabschnitte. Normal ist dieser Wert auf 1", CommandOptionType.SingleValue);
                var Ip = c.Option("--proxyip <anzahl>", "Setzt die Anzahl für Proxyipabschnitte. Normal ist dieser Wert auf 1", CommandOptionType.SingleValue);
                c.OnExecute(() => { obj.RunCreate(path, configPath, templatePath, Net, Int, Set, Ip); });
            });
            app.Command(null, c =>
            {
                app.ShowHelp();
            });
            return app.Execute(commands);

        }
    }

    public class Execute
    {
        protected void setuserpath(string configPath, string changePath)
        {
            StreamWriter writer = new StreamWriter(configPath);
            string[] file = File.ReadAllLines(configPath + @"\Config.json");
            List<string> list = new List<string>(file);
            list[3] = "\"changeDirectory\": " + changePath;
            foreach (string line in list)
            {
                writer.WriteLine(line);
            }
        }
        public void runReplace(
            CommandOption Path,
            CommandOption configPath,
            CommandOption templatePath) //run for replace
        {
            Execute exe = new Execute();
            ACConfig AC = new ACConfig();
            Output obj = new Output();

            var paths = getDefaultPaths(Path, configPath, templatePath);
            fileproof();
            var config = File.ReadAllText(paths.configPath + @"\Config.json"); //get json
            var configuration = JsonConvert.DeserializeObject<ACConfig>(config); //get path to json
            var changePath = configuration.userpath;
            var myconfig = JsonConvert.DeserializeObject<ACConfig>(File.ReadAllText(changePath));//open json to use
            var dirs = exe.findFilesInDirectory(configuration.changeDirectory); //search all files in Directory 
            foreach (var file in dirs)
            {
                AC = exe.parseinobject(new StreamReader(file)); //parses current configuration into the AC object
                if (myconfig.configureNetwork != null)
                {
                    AC = exe.replaceitem(AC, myconfig.configureNetwork.networkdev, "networkdev");
                    AC = exe.replaceitem(AC, myconfig.configureNetwork.interfacenetworkif, "interfacenetworkif");
                }
                if (myconfig.configureviop != null)
                {
                    AC = exe.replaceitem(AC, myconfig.configureviop.proxyset, "proxyset");
                    AC = exe.replaceitem(AC, myconfig.configureviop.proxyip, "proxyip"); //replaces the wanted details
                }
                var objList = obj.objectToList(AC);
                obj.writeOutput(objList, file); //output
            }

        }
        public void fileproof()
        {
            bool exists = System.IO.Directory.Exists(EnviromentVariable.changeDirectory);
            if (!exists)
            {
                System.IO.Directory.CreateDirectory(EnviromentVariable.configDirectory);
            }
        }
        protected List<string> findFilesInDirectory(string mypath) // opens the .txt files in the directorypath
        {
            string[] dirs = Directory.GetFiles(mypath, "*.txt", SearchOption.TopDirectoryOnly);//only the top not sup directorys
            return dirs.ToList<string>();
        }
        protected void getIdentNameAndValue(string line, out bool configureExit, out bool subIdentExit, out string subIdent, out string subIdentValue) // parses the Indentifyer for the current block of the configuration
        {
            subIdent = ParserGrammar.getsubident.Parse(line);
            configureExit = false;
            subIdentExit = false;
            subIdentValue = String.Empty;
            if (subIdent == ParserVariables.networkDev ||
                subIdent == ParserVariables.interfaceNetwokIf ||
                subIdent == ParserVariables.proxySet ||
                subIdent == ParserVariables.proxyIp ||
                subIdent == ParserVariables.exit)
            {
                if (subIdent == ParserVariables.exit)
                {
                    configureExit = true;
                    subIdentExit = true;
                    return;
                }
                subIdentExit = false;
                subIdentValue = ParserGrammar.subidentvalue.Parse(line);
            }
        }
        public void getConfigureIdent(string line, out bool configureExit, out string ident) //parses the head of the currend configurationblock
        {
            ident = String.Empty;
            configureExit = true;
            var parsedLine = ParserGrammar.getidentifier.Parse(line);
            if (parsedLine == "configure network" || parsedLine == "configure voip" || parsedLine == Environment.NewLine || parsedLine == "\n")
            {
                ident = parsedLine;
                configureExit = false;
            }
        }
        protected ACConfig parseinobject(StreamReader Reader)
        {
            Configureviop vo = new Configureviop();
            ConfigureNetwork co = new ConfigureNetwork();
            List<Networkdev> networkDevs = new List<Networkdev>();
            List<Interfacenetworkif> interfaceNetworkIfs = new List<Interfacenetworkif>();
            List<Proxyip> proxyIps = new List<Proxyip>();
            List<Proxyset> proxySets = new List<Proxyset>();
            ACConfig AC = new ACConfig();


            AC.configureNetwork = co;
            AC.configureviop = vo;

            AC.configureNetwork.networkdev = networkDevs;
            AC.configureNetwork.interfacenetworkif = interfaceNetworkIfs;

            AC.configureviop.proxyip = proxyIps;
            AC.configureviop.proxyset = proxySets;

            string ident = " ";
            bool configureExit = true;
            string subIdent = " ";
            bool subIdentExit = true;
            string subIdentValue = "";
            int networkDevListTndex = 0;
            int interfaceNetwokIfListTndex = 0;
            int proxySetListTndex = 0;
            int proxyIpListTndex = 0;

            using (Reader)
            {
                string line = " ";
                while ((line = Reader.ReadLine()) != null)                                                  // zu editierende File einlesen (Zeile für Zeile)
                {
                    if (line == string.Empty || line == "")                                                 // wenn leere Zeile überspringe
                    {
                        continue;
                    }
                    if (configureExit)                                                                      // wenn True -> kein Überbereich(configure voip // configure network) in dem die Konfig definiert wird
                    {
                        getConfigureIdent(line, out configureExit, out ident);
                        continue;
                    }
                    if (subIdentExit)
                    {
                        getIdentNameAndValue(line, out configureExit, out subIdentExit, out subIdent, out subIdentValue);  // wenn true -> keien Bereich(networkDev / interfaceNetworkIf) in dem Konfig konfiguriert wird
                        var Name = returnRealName(subIdent);
                        if (Name != null && subIdent == ParserVariables.networkDev)
                        {
                            Networkdev newListNetworkDev = new Networkdev();
                            networkDevs.Add(newListNetworkDev);                                            //neue Liste wird erstellt für jede Bereich von networkDev
                            AC = ListParsing(AC, Name, subIdentValue, networkDevListTndex, AC.configureNetwork.networkdev, subIdent);
                        }
                        else if (Name != null && subIdent == ParserVariables.interfaceNetwokIf)
                        {
                            Interfacenetworkif newListInterfaceNetworkIf = new Interfacenetworkif();
                            interfaceNetworkIfs.Add(newListInterfaceNetworkIf);
                            AC = ListParsing(AC, Name, subIdentValue, interfaceNetwokIfListTndex, AC.configureNetwork.interfacenetworkif, subIdent);
                        }
                        else if (Name != null && subIdent == ParserVariables.proxySet)
                        {
                            Proxyset newListProxySet = new Proxyset();
                            proxySets.Add(newListProxySet);
                            AC = ListParsing(AC, Name, subIdentValue, proxySetListTndex, AC.configureviop.proxyset, subIdent);
                        }
                        else if (Name != null && subIdent == ParserVariables.proxyIp)
                        {
                            Proxyip newListProxyIp = new Proxyip();
                            proxyIps.Add(newListProxyIp);
                            AC = ListParsing(AC, Name, subIdentValue, proxyIpListTndex, AC.configureviop.proxyip, subIdent);
                        }
                        continue;
                    }

                    if (configureExit == false && subIdentExit == false && ident == "configure network" && subIdent == ParserVariables.networkDev)
                    {
                        var Name = ParserGrammar.NameParser.Parse(line);
                        if (Name == ParserVariables.exit) // Bereich wird durch exit beendet -> Listenindex wird erhöht und es gibt keinen Bereich in dem definiert die Konfig defoiniert wird
                        {
                            subIdentExit = true;
                            networkDevListTndex++;
                            continue;
                        }
                        Name = returnRealName(Name);
                        if (Name == null)
                        {
                            continue;
                        }
                        if (Name == ParserVariables.activate) //boolscher wert der in der KOnfiguration keinen weiteren Wert zugewieden bekommt -> entweder da oder nicht
                        {
                            var Value = true;
                            AC = ListParsing(AC, Name, Value, networkDevListTndex, AC.configureNetwork.networkdev, subIdent);
                        }
                        else
                        {
                            var Value = ParserGrammar.ValueParser.Parse(line);
                            AC = ListParsing(AC, Name, Value, networkDevListTndex, AC.configureNetwork.networkdev, subIdent);
                        }


                        continue;
                    }
                    else if (configureExit == false && subIdentExit == false && ident == "configure network" && subIdent == ParserVariables.interfaceNetwokIf)
                    {
                        var Name = ParserGrammar.NameParser.Parse(line);
                        if (Name == ParserVariables.exit)
                        {
                            subIdentExit = true;
                            interfaceNetwokIfListTndex++;
                            continue;
                        }
                        Name = returnRealName(Name);
                        if (Name == null)
                        {
                            continue;
                        }
                        if (Name == ParserVariables.activate)
                        {
                            var Value = true;
                            AC = ListParsing(AC, Name, Value, interfaceNetwokIfListTndex, AC.configureNetwork.interfacenetworkif, subIdent);
                        }
                        else
                        {
                            var Value = ParserGrammar.ValueParser.Parse(line);
                            AC = ListParsing(AC, Name, Value, interfaceNetwokIfListTndex, AC.configureNetwork.interfacenetworkif, subIdent);
                        }
                        continue;
                    }
                    if (configureExit == false && subIdentExit == false && ident == "configure voip" && subIdent == ParserVariables.proxySet)
                    {
                        var Name = ParserGrammar.NameParser.Parse(line);
                        if (Name == ParserVariables.exit)
                        {
                            subIdentExit = true;
                            proxySetListTndex++;
                            continue;
                        }
                        Name = returnRealName(Name);
                        if (Name == null)
                        {
                            continue;
                        }
                        if (Name == ParserVariables.activate)
                        {
                            var Value = true;
                            AC = ListParsing(AC, Name, Value, proxySetListTndex, AC.configureviop.proxyset, subIdent);
                        }
                        else
                        {
                            var Value = ParserGrammar.ValueParser.Parse(line);
                            AC = ListParsing(AC, Name, Value, proxySetListTndex, AC.configureviop.proxyset, subIdent);
                        }
                        continue;
                    }
                    else if (configureExit == false && subIdentExit == false && ident == "configure voip" && subIdent == ParserVariables.proxyIp)
                    {
                        var Name = ParserGrammar.NameParser.Parse(line);
                        if (Name == ParserVariables.exit)
                        {
                            subIdentExit = true;
                            proxyIpListTndex++;
                            continue;
                        }
                        Name = returnRealName(Name);
                        if (Name == null)
                        {
                            continue;
                        }
                        if (Name == ParserVariables.activate)
                        {
                            var Value = true;
                            AC = ListParsing(AC, Name, Value, proxyIpListTndex, AC.configureviop.proxyip, subIdent);
                        }
                        else
                        {
                            var Value = ParserGrammar.ValueParser.Parse(line);
                            AC = ListParsing(AC, Name, Value, proxyIpListTndex, AC.configureviop.proxyip, subIdent);
                        }
                        continue;
                    }
                }
            }
            return AC;
        }
        public string returnRealName(string Name) // wandelt die in der Konfiguration enthaltenen bezeichner in die Variablen Namen der Listen um da diese nicht zu 100% übereinstimmen 
        {
            switch (Name)
            {
                case ParserVariables.proxySet:
                case ParserVariables.networkDev:
                case ParserVariables.interfaceNetwokIf:
                    return ParserVariables.listid;
                case ParserVariables.proxyIp:
                    return ParserVariables.ip;
                case ParserVariables.activate:
                    return ParserVariables.activate;
                case ParserVariables.vlanid:
                    return ParserVariables.vlan;
                case ParserVariables.underlyingdev:
                    return ParserVariables.udev;
                case ParserVariables.Name:
                    return ParserVariables.name;
                case ParserVariables.tagging:
                    return ParserVariables.Tag;
                case ParserVariables.apptype:
                    return ParserVariables.appt;
                case ParserVariables.ipaddress:
                    return ParserVariables.ipa;
                case ParserVariables.prefixlength:
                    return ParserVariables.prefix;
                case ParserVariables.gateway:
                    return ParserVariables.gate;
                case ParserVariables.underlyingif:
                    return ParserVariables.uif;
                case ParserVariables.proxyname:
                    return ParserVariables.pname;
                case ParserVariables.proxyenablekeepalive:
                    return ParserVariables.proxyalive;
                case ParserVariables.srdname:
                    return ParserVariables.sname;
                case ParserVariables.sbcipv4sipintname:
                    return ParserVariables.sbcipv;
                case ParserVariables.keepalivefailresp:
                    return ParserVariables.keepresp;
                case ParserVariables.successdetectretries:
                    return ParserVariables.successdet;
                case ParserVariables.successdetectint:
                    return ParserVariables.successdetint;
                case ParserVariables.proxyredundancymode:
                    return ParserVariables.proxymode;
                case ParserVariables.isproxyhotswap:
                    return ParserVariables.proxyswap;
                case ParserVariables.proxyloadbalancingmethod:
                    return ParserVariables.proxymethod;
                case ParserVariables.minactiveservlb:
                    return ParserVariables.minlb;
                case ParserVariables.proxyaddress:
                    return ParserVariables.padress;
                case ParserVariables.transporttype:
                    return ParserVariables.ttype;
                default:
                    return null;
            }
        }
        protected ACConfig ListParsing(ACConfig Config, string Name, dynamic Value, int Index, dynamic myList, string subIdent) //setzt die Value in aus dfem Bereich in die Passende liste an der Richtigen stelle
        {
            if (String.IsNullOrWhiteSpace(Value.ToString())) // falls es keine Value gibt brauch es nix machen
            {
                return Config;
            }
            foreach (var item in myList[Index].GetType().GetProperties()) //property aus index X   bekommeb
            {
                var property = item;
                if (property.Name != Name) // wenn die property nicht den selben bezeichner hat wie der gegebene Name
                {
                    continue;
                }
                var propertyType = property.PropertyType; // Gets the type of the property

                if (propertyType == typeof(Int32) || propertyType == typeof(Nullable<Int32>)) //if type = int then parse Value into an integer
                {
                    property.SetValue(myList[Index], Convert.ToInt32(Value));
                }
                else if (propertyType.IsEnum) // if type = enum then parse Value into a enum
                {
                    var enumMember = propertyType
                        .GetFields(); // List all fields of an enum
                    foreach (var member in enumMember)
                    {
                        if (member.Name == Convert.ToString(Value) ||
                             Convert.ToString(Value) == Convert.ToString(member.GetCustomAttributes(typeof(NameAttribute), false))) // if the name of the field = Name or the Attribute = Name 
                        {
                            if (member == null)
                            {
                                Console.WriteLine($"Warn: skip property {property.Name} because the value {Value} is not valid for this field.");
                                return Config;
                            }
                            var enumValue = Enum.Parse(propertyType, member.Name);  // parses Value into an enumValue
                            property.SetValue(myList[Index], enumValue);

                        }  //Console.WriteLine($"Warn: skip property {property.Name} because enums currently not supported.");
                    }
                }
                else if (propertyType == typeof(Boolean)) // if type = boolean then parse Value into a boolean
                {
                    property.SetValue(myList[Index], Convert.ToBoolean(Value));
                }
                else                            //if type is something else (hopefully a string) then replace the old value with the new
                {
                    property.SetValue(myList[Index], Value);
                }
                switch (subIdent) // returns the right Config 
                {
                    case ParserVariables.networkDev:
                        Config.configureNetwork.networkdev = myList;
                        return Config;
                    case ParserVariables.interfaceNetwokIf:
                        Config.configureNetwork.interfacenetworkif = myList;
                        return Config;
                    case ParserVariables.proxySet:
                        Config.configureviop.proxyset = myList;
                        return Config;
                    case ParserVariables.proxyIp:
                        Config.configureviop.proxyip = myList;
                        return Config;
                    default:
                        break;
                }
                return Config;
            }
            return Config;
        }
        protected string validpath(string filepath, string otherPath) //validate the userpath
        {
            var path = String.Empty;
            if (otherPath != null)
            {
                path = otherPath;
            }
            else
            {
                path = filepath;
            }
            path = path.Replace(@"\\", ":"); // to cancel out c:\\\\test.text
            string temp = Path.GetPathRoot(path); //For cases like: \text.txt
            //if (temp.StartsWith(@"\")) return null;
            string pt = Path.GetFullPath(path);
            return pt;

        }
        protected void change(dynamic i, dynamic item) //replaces the Item
        {
            foreach (var propertyInfo in item.GetType().GetProperties())
            {
                var value = propertyInfo.GetValue(item);
                if (value != null)
                {
                    i.GetType().GetProperty(propertyInfo.Name).SetValue(i, value);
                }
            }
        }
        public ACConfig replaceitem(ACConfig AC, dynamic list, string whatlist)
        {
            if (list == null)
            {
                return AC;
            }
            foreach ( var config in list)
            {
                switch (whatlist)   // switches on which list is now given 
                {
                    case "networkdev":
                        foreach (var i in AC.configureNetwork.networkdev)
                        {
                            if (config.listid == i.listid)
                            {
                                change(i, config);
                            }
                        }
                        break;
                    case "interfacenetworkif":
                        foreach (var i in AC.configureNetwork.interfacenetworkif)
                        {
                            if (config.listid == i.listid)
                            {
                                change(i, config);
                            }
                        }
                        break;
                    case "proxyset":
                        foreach (var i in AC.configureviop.proxyset)
                        {
                            if (config.listid == i.listid)
                            {
                                change(i, config);
                            }
                        }
                        break;
                    case "proxyip":
                        foreach (var i in AC.configureviop.proxyip)
                        {
                            if (config.ip == i.ip)
                            {
                                change(i, config);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            return AC;
        }

        public void RunCreate(
            CommandOption Path,
            CommandOption configPath,
            CommandOption templatePath,
            CommandOption Net,
            CommandOption Dev,
            CommandOption Set,
            CommandOption Ip) // second command -> creates an empty configuration with x list of the diffrent blocks
        {
            fileproof();
            var paths = getDefaultPaths(Path, configPath, templatePath);
            DateTime time = new DateTime();
            time = DateTime.Now;
            var filepath = paths.path + @"\" + time.Year.ToString() + "." + time.Month.ToString() + "." + time.Day.ToString() + "-" + time.Hour.ToString() + "." + time.Minute.ToString() + ".txt"; //creats a time
            Write(Net, Dev, Set, Ip, filepath, paths.configPath, paths.tempaltePath);
        }
        protected void Write(CommandOption Net, CommandOption Dev, CommandOption Set, CommandOption Ip, string mypath, string configPath, string tempaltePath)
        {
            var netcounter = 1;
            var devcounter = 1;
            var setcounter = 1;
            var ipcounter = 1;
            if (Net.HasValue() == true && Net.Value() != null)
            {
                int.TryParse(Net.Value(), out netcounter);
            }
            if (Ip.HasValue() == true && Ip.Value() != null)
            {
                int.TryParse(Ip.Value(), out ipcounter);
            }
            if (Set.HasValue() == true && Set.Value() != null)
            {
                int.TryParse(Set.Value(), out setcounter);
            }
            if (Dev.HasValue() == true && Dev.Value() != null)
            {
                int.TryParse(Dev.Value(), out devcounter);
            }

            var Networkdevvorlage = File.ReadAllText(System.IO.Path.Combine(tempaltePath, @"NetworkDev.template"));
            var Interfacenetworkifvorlage = File.ReadAllText(System.IO.Path.Combine(tempaltePath, @"InterfaceNetwokIf.template"));
            var Proxysetvorlage = File.ReadAllText(System.IO.Path.Combine(tempaltePath, @"ProxySet.template"));
            var Proxyipvorlage = File.ReadAllText(System.IO.Path.Combine(tempaltePath, @"ProxyIp.template"));
            using (StreamWriter writer = new StreamWriter(mypath))
            {
                writer.WriteLine("configure network");
                for (int i = 0; i < netcounter; i++)
                {
                    writer.WriteLine(Networkdevvorlage);
                }
                writer.WriteLine(@" exit");
                for (int i = 0; i < devcounter; i++)
                {
                    writer.WriteLine(Interfacenetworkifvorlage);
                }
                writer.WriteLine(@" exit");
                writer.WriteLine("exit");
                writer.WriteLine("configure voip");
                for (int i = 0; i < setcounter; i++)
                {
                    writer.WriteLine(Proxysetvorlage);
                }
                writer.WriteLine(@" exit");
                for (int i = 0; i < ipcounter; i++)
                {
                    writer.WriteLine(Proxyipvorlage);
                }
                writer.WriteLine(@" exit");
                writer.WriteLine("exit");
            }

        }
        private string GetToolPath()
        {
            var path = Assembly.GetExecutingAssembly().Location;
            path = System.IO.Path.GetDirectoryName(path).ToString();
           
            // Console.WriteLine($"Source path is: {path}");
            return path;
        }
        private (string path, string configPath, string tempaltePath) getDefaultPaths(
            CommandOption Path,
            CommandOption configpath,
            CommandOption templatePath)
        {

                var configPath = configpath.HasValue() ? configpath.Value() : this.GetToolPath();
            if (configPath == System.IO.Path.GetFullPath(@"..\netcoreapp2.2"))
            {
                Directory.SetCurrentDirectory(@"..\..\..\..");
                configPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), EnviromentVariable.configDirectory);
            }
            return (
                path: Path.HasValue() ? Path.Value() : Directory.GetCurrentDirectory(),
                configPath,
                tempaltePath: templatePath.HasValue() ? templatePath.Value() : System.IO.Path.Combine(this.GetToolPath(), EnviromentVariable.configDirectory, "Template")
                );
        }
    }
}

