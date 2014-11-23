using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using Object = System.Object;
using Debug = UnityEngine.Debug;
using System.ComponentModel;


public class Vector3Converter : TypeConverter {
    // must implement functions CanConvertFrom¡¢CanConvertTo¡¢ConvertFrom and ConvertTo.
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        if (sourceType == typeof(string)) { 
            return true;
        }
        return base.CanConvertFrom(context, sourceType);
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return base.CanConvertTo(context, destinationType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
    {
        if (value is string) {
            string tmp =(string) value;
            string[] point = tmp.Split(',');
            float x = Convert.ToSingle(point[0]);
            float y = Convert.ToSingle(point[1]);
            float z = Convert.ToSingle(point[2]);
            return new Vector3(x, y, z);
        }
        return base.ConvertFrom(context, culture, value);
    }

    public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
    {
        return base.ConvertTo(context, culture, value, destinationType);
    }
}

public class Vector2Converter : TypeConverter {
    // must implement functions CanConvertFrom¡¢CanConvertTo¡¢ConvertFrom and ConvertTo.
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        if (sourceType == typeof(string)) { 
            return true;
        }
        return base.CanConvertFrom(context, sourceType);
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return base.CanConvertTo(context, destinationType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
    {
        if (value is string) {
            string tmp =(string) value;
            string[] point = tmp.Split(',');
            float x = Convert.ToSingle(point[0]);
            float y = Convert.ToSingle(point[1]);
            return new Vector3(x, y);
        }
        return base.ConvertFrom(context, culture, value);
    }

    public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
    {
        return base.ConvertTo(context, culture, value, destinationType);
    }
}



static public class JumpCommand {
    static private  Dictionary<string, JumpCommandObject> mCmdLst = new Dictionary<string, JumpCommandObject>();
    static public   Object       Callee {get;private set;}
    static private  List<string> History = new List<string>();
    static public   HashSet<Type>   ComponentTypes = new HashSet<Type>();
    static private  int  MaxHistoryNum = 100; 
    static private  int  FirstHistoryIndex = -1;
    static public   event Action<Object> OnChangeCallee;
    static JumpCommand() {}

    static public void Awake() {
        RegisterAllCommandObjects();
    }

    static private bool IsBaseTypeMonoBehaviour(Type type) {
        if(type.BaseType == null) {
            return false;
        }
        else if( type.BaseType == typeof(MonoBehaviour) || ComponentTypes.Contains(type.BaseType)) {
            return true;
        }
        else {
            return IsBaseTypeMonoBehaviour(type.BaseType);
        }
    }

    static private void RegisterAllCommandObjects() {
        mCmdLst.Clear();
        var csharpDLL = Assembly.GetExecutingAssembly();  // unity csharp.dll assembly

        // find all type in csharp.dll, if type's attributes contains JumpCommandRegister, then register the command.
        foreach( var t in csharpDLL.GetTypes()) {
            if(IsBaseTypeMonoBehaviour(t)) {
                ComponentTypes.Add(t);
            }
            foreach(var m in t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)) {  
                foreach(var a in m.GetCustomAttributes(true)) {
                    if(a.GetType() == typeof(JumpCommandRegister)) {
                        string commandName = (a as JumpCommandRegister).command;
                        string help        = (a as JumpCommandRegister).help;
                        string gameObject  = (a as JumpCommandRegister).gameObjName;
                        if(mCmdLst.ContainsKey(commandName)) {
                            Debug.LogError(string.Format("Can not register function {0} as command \"{1}\", \"{1}\" is already used by {2}.{3}", 
                                m.Name, commandName, mCmdLst[commandName].Type.Name, mCmdLst[commandName].Method.Name));
                        }
                        else {
                            mCmdLst.Add(commandName, new JumpCommandObject(commandName, m, help, gameObject));
                        }
                    }
                }
            }
        }
    }

    static public bool Constains(string commandName) {
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

    static public void CleanHistory() {
        History.Clear();
        FirstHistoryIndex = -1;
    }

    static public List<string> GetAutoCompletionCommands(string command) {
        List<string> result = new List<string>();

        // Go through and get all commands start with input command
        foreach(string key in mCmdLst.Keys) {
            if (key.ToUpper().StartsWith(command.ToUpper())) {
                result.Add(mCmdLst[key].ToString());
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

    static public void Execute(string command) {
        AddHistory(command);
        string[] argv = ParseArguments(command);
        if (argv.Length == 0) {
            //Debug.LogError("Command is empty");
            return; 
        }
        if (mCmdLst.ContainsKey(argv[0])) {
            JumpCommandObject cmd = mCmdLst[argv[0]];
            string[] paramStr = new string[argv.Length - 1];
            Array.Copy(argv,1,paramStr,0,argv.Length-1);
            if(!mCmdLst[argv[0]].Method.IsStatic) {  // the callee will be the current select game object
                if(mCmdLst[argv[0]].GameObjName != "") {
                    GameObject go = GameObject.Find(mCmdLst[argv[0]].GameObjName) as GameObject;
                    var component = go.GetComponent(mCmdLst[argv[0]].Type);
                    cmd.Call(paramStr, component);
                }
                else if(Callee is GameObject) {
                    var go = Callee as GameObject;
                    var component = go.GetComponent(mCmdLst[argv[0]].Type);
                    cmd.Call(paramStr, component);
                }
                else {
                    cmd.Call(paramStr, Callee);
                }
            }
            else {
                cmd.Call(paramStr, Callee);
            }
        }
        else {
            Debug.LogError(string.Format("Can not find command \"{0}\"",argv[0]));
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

    [JumpCommandRegister("help","list all the command")]
    static private void OutputAllCommnd() {
        foreach(var c in mCmdLst.Values) {
            string paramInfo = "(";
            foreach(var p in c.Method.GetParameters()) {
                paramInfo += p.ParameterType.Name + ", ";
            }
            paramInfo += ")";
            Debug.Log(string.Format("{0,-10} {1}: \"{2}\"", c.Command, paramInfo, c.Help));
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
}

