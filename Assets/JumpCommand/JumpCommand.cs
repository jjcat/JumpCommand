using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using Object = System.Object;
using Debug = UnityEngine.Debug;
using System.ComponentModel;
using System.IO;

public class JumpCommandException : Exception {
    public JumpCommandException(string msg) : base(msg) {}
}

public class CommandItem : Attribute {
    public string commandName;
    public string help;
    public string gameObjFullName;

    public CommandItem(string commandName, string help = "", string gameObjFullName = "") {
        this.commandName     = commandName;
        this.help            = help;
        this.gameObjFullName = gameObjFullName;
    }
}

static public class JumpCommand {
    static private  Dictionary<string, List<JumpCommandObject>> mCmdLst = new Dictionary<string, List<JumpCommandObject>>();
    static public   Object       Callee {get;private set;}
    static private  List<string> History = new List<string>();
    static public   HashSet<Type>   ComponentTypes = new HashSet<Type>();
    static private  int  MaxHistoryNum = 100; 
    static private  int  FirstHistoryIndex = -1;
    static public   event Action<Object> OnChangeCallee;
    static private  string historyFile;

    static JumpCommand() {}

    static public void Init() {
        RegisterAllCommandObjects();

        // read history
        historyFile = Application.temporaryCachePath + "/jumpcommand.history";
        if(!File.Exists(historyFile)) {
            TextWriter tw = new StreamWriter(historyFile);
            tw.Close();
        }
        ReadHistoryFile();
    }

    static public void Deinit() {
        SaveHistoryFile();
    }

    static private bool IsComponentType(Type type) {
        if(type.BaseType == null) {
            return false;
        }
        else if( type.BaseType == typeof(MonoBehaviour) || ComponentTypes.Contains(type.BaseType)) {
            return true;
        }
        else {
            return IsComponentType(type.BaseType);
        }
    }

    static private void ReadHistoryFile() {
        TextReader tr = new StreamReader(historyFile);
        while(tr.Peek() != -1) {
            AddHistory(tr.ReadLine());
        }
        tr.Close();
    }

    static private void SaveHistoryFile() {
        TextWriter tw = new StreamWriter(historyFile);
        for(int i = HistoryNum - 1; i >= 0; i--) {
            tw.WriteLine(GetHistory(i));
        }
        tw.Close();
    }

    static private void RegisterAllCommandObjects(Assembly assembly) {
        // find all type in assembly, if type's attributes contains Command, then register the command.
        foreach( var t in assembly.GetTypes()) {
            if(IsComponentType(t)) {
                ComponentTypes.Add(t);
            }
            foreach(var m in t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)) {  
                foreach(var a in m.GetCustomAttributes(true)) {
                    if(a.GetType() == typeof(CommandItem)) {
                        string commandName = (a as CommandItem).commandName;
                        string help        = (a as CommandItem).help;
                        string gameObject  = (a as CommandItem).gameObjFullName;
                        if(mCmdLst.ContainsKey(commandName)) {
                            mCmdLst[commandName].Add(new JumpCommandObject(commandName, m, help, gameObject));                            
                            // Debug.LogError(string.Format("Can not register function {0} as command \"{1}\", \"{1}\" is already used by {2}.{3}", 
                            //     m.Name, commandName, mCmdLst[commandName].Type.Name, mCmdLst[commandName].Method.Name));
                        }
                        else {
                            var commandList = new List<JumpCommandObject>();
                            commandList.Add(new JumpCommandObject(commandName, m, help, gameObject));
                            mCmdLst.Add(commandName, commandList);
                        }
                    }
                }
            }
        }        
    }

    static private void RegisterAllCommandObjects() {
        mCmdLst.Clear();
        ComponentTypes.Clear();
        var csharpDLL = Assembly.GetExecutingAssembly();  // unity csharp.dll assembly
        //var editorDLL = Assembly.GetAssembly(typeof(EditorTest));
        RegisterAllCommandObjects(csharpDLL);        
        return;
    }

    static private bool ConstainsCommand(string commandName) {
        return mCmdLst.ContainsKey(commandName);
    }

    // retrieve i-th history command, 0 is newest one.
    static public string GetHistory(int i) {
        if(History.Count == 0) return "";
        else return History[(FirstHistoryIndex + MaxHistoryNum - i) % MaxHistoryNum];
    }

    static private void AddHistory(string command) {
        FirstHistoryIndex = (FirstHistoryIndex + 1)%MaxHistoryNum;
        if(History.Count == MaxHistoryNum) {
            History[FirstHistoryIndex] = command;
        }
        else {
            History.Add(command);
        }
    }

    static public int HistoryNum {
        get {return History.Count;}
    }

    static private void CleanHistory() {
        History.Clear();
        FirstHistoryIndex = -1;
    }

    static public List<JumpCommandObject> GetAutoCompletionCommandObjects(string commandName) {
        List<JumpCommandObject> result = new List<JumpCommandObject>();

        // Go through and get all commands start with input command
        foreach(string key in mCmdLst.Keys) {
            if (key.ToUpper().StartsWith(commandName.ToUpper())) {
                foreach(var cmd in mCmdLst[key]) {
                    result.Add(cmd);
                }
            }
        }
        return result;        
    }

    static public List<string> GetAutoCompletionCommands(string command) {
        List<string> result = new List<string>();

        // Go through and get all commands start with input command
        foreach(string key in mCmdLst.Keys) {
            if (key.ToUpper().StartsWith(command.ToUpper())) {
                foreach(var cmd in mCmdLst[key]) {
                    result.Add(cmd.ToString());
                }
            }
        }
        return result;
    }

    static public string GetAutoCompletionCommand(string command, string fullCommand = "") {
        List<string> result = new List<string>();

        // Go through and get all commands start with input command
        foreach(string key in mCmdLst.Keys) {
            if (key.ToUpper().StartsWith(command.ToUpper())) {
                result.Add(key);
            }
        }

        // Choose first matched command
        if (result.Count != 0) {
            result.Sort();

            int index = result.IndexOf(fullCommand);

            if (index != -1) {
                index++;
                index %= result.Count;
                return result[index];
            }
            else {
                return result[0];
            }
        }
        return null;
    }

    static private void RemoveAt<T>(ref T[] arr, int index) {
        for (int a = index; a < arr.Length - 1; a++) {
            // moving elements downwards, to fill the gap at [index]
            arr[a] = arr[a + 1];
        }
        // finally, let's decrement Array's size by one
        Array.Resize(ref arr, arr.Length - 1);
    }

    static public void Execute(string command) {
        AddHistory(command);

        bool printResult = false;
        bool invokeAllComponents = false;
        string[] argv = ParseArguments(command);
        if (argv.Length == 0) {
            return; 
        }


        // check option flag
        // -p: print return value
        // -a: invoke all components
        int i = 1;
        while(i < argv.Length) {
            if(argv[i] == "-p" ) {
                printResult = true;
                RemoveAt<string>(ref argv, i);
            }
            else if(argv[i] == "-a") {
                invokeAllComponents = true;
                RemoveAt<string>(ref argv, i);
            }
            else if(argv[i] == "-ap" || argv[i] == "-pa") {
                invokeAllComponents = true;
                printResult = true;
                RemoveAt<string>(ref argv, i);                
            }
            else {
                break;
            }
        }

        if (ConstainsCommand(argv[0])) {
            bool success  = true;
            Exception lastException = new Exception();
            foreach(var cmd in mCmdLst[argv[0]]) {
                try {
                    success = true;
                    string[] paramStr = new string[argv.Length - 1];
                    Array.Copy(argv,1,paramStr,0,argv.Length-1);
                    if(!cmd.Method.IsStatic) {  // the callee will be the current select game object
                        // find all components and call it
                        if(invokeAllComponents) {
                            foreach (GameObject go in UnityEngine.Object.FindObjectsOfType(typeof(GameObject))) {
                                if (go.transform.parent == null) {
                                    foreach(var c in go.GetComponentsInChildren(cmd.Type)) {
                                        cmd.Call(paramStr, c, printResult);
                                    }
                                }
                            }
                        }
                        else {
                            GameObject go = null;
                            if(cmd.GameObjFullName != "") {
                                go = GameObject.Find(cmd.GameObjFullName) as GameObject;
                            }
                            else if(Callee is GameObject) {
                                go = Callee as GameObject;
                            }

                            if(go == null) {
                                throw new JumpCommandException("Missing game object to execute command");
                            }

                            var component = go.GetComponent(cmd.Type);
                            cmd.Call(paramStr, component, printResult);                            
                        }
                    }
                    else {
                        cmd.Call(paramStr, Callee, printResult);
                    }                    
                } 
                catch (JumpCommandException e) {
                    success  = false;
                    lastException = e;                    
                }
                catch {
                    throw;
                }
                if(success ) { 
                    break;                
                }
            }
            if(!success) {
                throw new JumpCommandException(lastException.Message);
            }
        }
        else {
            throw new JumpCommandException(string.Format("Can not find command \"{0}\"",argv[0]));
        }
    }

    static public string[] ParseArguments(string commandLine) {
        char[] parmChars = commandLine.ToCharArray();
        bool inQuote = false;
        for (int index = 0; index < parmChars.Length; index++) {
            if (parmChars[index] == '"')
                inQuote = !inQuote;
            if (!inQuote && Char.IsWhiteSpace(parmChars[index]))
                parmChars[index] = '\n';
        }

        // remove double quote from begin and end
        string[] result = new string(parmChars).Split(new char[]{'\n'}, StringSplitOptions.RemoveEmptyEntries);
        for(int i = 0; i< result.Length; i++) {
            result[i] = result[i].Trim('"');
        }
        return result;            
    }

    [CommandItem("help","list all the command")]
    static private void OutputAllCommnd(string command="") {
        foreach(var c in mCmdLst.Values) {
            foreach(var cmd in c) {
                if(!cmd.Name.ToUpper().StartsWith(command.ToUpper())) continue;
                Debug.Log(cmd.ToString());                
            }
        }        
    }

    static public void SetCallee(Object callee) {
        if(callee != Callee) {
            Callee = callee;
            if(OnChangeCallee != null) {
                OnChangeCallee(Callee);
            }
        }
    }

    // TODO, handle relative path
    static public Object FindComponent(string gameObjFullName, Type type) {
        GameObject foundOne = GameObject.Find(gameObjFullName);
        if(foundOne != null) {
            return foundOne.GetComponent(type);
        }
        else {
            return null;
        }
    }

}

