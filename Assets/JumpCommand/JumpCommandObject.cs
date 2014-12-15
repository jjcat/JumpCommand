using System;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;
using Object = System.Object;


public class JumpCommandObject  {
    public string      Name     {get;private set;}
    public MethodInfo  Method      {get;private set;}
    public string      Help        {get;private set;}
    public string      GameObjFullName {get;private set;}
    public Type Type {
        get {
            return Method.DeclaringType;
        }
    }

    public JumpCommandObject(string command, MethodInfo method, string help, string gameObject) {
        Name     = command;
        Method      = method;
        Help        = help;
        GameObjFullName = gameObject;
    }

    public override string ToString() {
        string parminfo = ParametersInfoString();
        string helpinfo = Help;
        if(helpinfo != "") {
            helpinfo = "\"" + helpinfo + "\"";
        }
        return string.Format("{0,-10} {1}   {2}", Name, parminfo, helpinfo);
    }

    public string ParametersInfoString() {
        string result = "";
        var paramInfoLst = Method.GetParameters();
        for (int i = 0; i < paramInfoLst.Length-1; ++i) {
            result += paramInfoLst[i].ParameterType.Name + " ";
        }
        if(paramInfoLst.Length > 0) {
            result += paramInfoLst[paramInfoLst.Length-1].ParameterType.Name;
        }
        return result;
    }


    public bool Call(string[] paramStr, Object callee, bool printResult = false) {        
        // create parameter list
        ParameterInfo[] paramInfoLst = Method.GetParameters();
        Object[] paramLst = new Object[paramInfoLst.Length];
        
        for (int i = 0; i < paramInfoLst.Length; ++i) {
            if(i >= paramStr.Length) { // if param string item number is less than param info item number, check defalut value from param info. 
                if(paramInfoLst[i].RawDefaultValue == null) {
                    throw new JumpCommandException(string.Format("Missing argument '#{0}' when call method {1}", i, Method.Name));
                }
                else {
                    paramLst[i] = paramInfoLst[i].RawDefaultValue;    
                }
            }
            else {
                if( JumpCommand.ComponentTypes.Contains(paramInfoLst[i].ParameterType)) {  // try to parse component
                    paramLst[i] = JumpCommand.FindComponent(paramStr[i], paramInfoLst[i].ParameterType);
                }
                else {
                    try {
                        paramLst[i] = TypeDescriptor.GetConverter(paramInfoLst[i].ParameterType).ConvertFrom(paramStr[i]);
                    }
                    catch {
                        paramLst[i] = InvokeConstructorByParameterString(paramInfoLst[i].ParameterType, paramStr[i]);                        
                    }
                }
            }
        }

        // call method with parameter list
        Object invokeResult = null;
        if(Method.IsStatic) {
            invokeResult = Method.Invoke(null, paramLst);
        }
        else if(callee != null){
            invokeResult = Method.Invoke(callee, paramLst);
        }
        else {
            throw new JumpCommandException(string.Format("Missing component \"{0}\" to execute command", Type.Name));
        }

        if(printResult ) {
            if(invokeResult != null){
                Debug.Log(invokeResult.ToString());
            }
            else {
                Debug.Log("No return value");
            }
        }
        return true;
    }

    // split parameterString by ',' 
    static public string[] ParseParameterString(string parameterString) {
        char[] parmChars = parameterString.ToCharArray();
        bool inQuote = false;
        for (int index = 0; index < parmChars.Length; index++) {
            if (parmChars[index] == '"')
                inQuote = !inQuote;
            if (!inQuote && parmChars[index]==',')
                parmChars[index] = '\n';
        }

        // remove double quote from begin and end
        string[] result = new string(parmChars).Split(new char[]{'\n'}, StringSplitOptions.RemoveEmptyEntries);
        for(int i = 0; i< result.Length; i++) {
            result[i] = result[i].Trim('"');
        }
        return result;            
    }

    // A simple parser to invoke constructor by parameter string
    //
    // Example:
    //   type = typeof(Vector3), parameterStr = "1,2,3"
    //   invoke "Vector3(1,2,3)"
    //
    // Notic:
    //   Only can handle constructor that receives build-in type as parameters, like int, float, enum, string, bool.
    static public Object InvokeConstructorByParameterString(Type type, string parameterStr) {
        string[] paramStr = ParseParameterString(parameterStr);
        bool success = false;
        Object result = null;
        foreach(var constructorInfo in type.GetConstructors()) {
            try {
                success = true;
                result  = null;
                ParameterInfo[] paramInfoLst = constructorInfo.GetParameters();
                Object[] paramLst = new Object[paramInfoLst.Length];

                for (int i = 0; i < paramInfoLst.Length; ++i) {
                    if(i >= paramStr.Length) {
                        if(paramInfoLst[i].RawDefaultValue == null) {
                            throw new JumpCommandException(string.Format("Missing argument '#{0}' when call method {1}", i, constructorInfo.Name));
                        }
                        else {
                            paramLst[i] = paramInfoLst[i].RawDefaultValue;    
                        }                    
                    }
                    else {
                        paramLst[i] = TypeDescriptor.GetConverter(paramInfoLst[i].ParameterType).ConvertFrom(paramStr[i]);
                    }
                }

                result = constructorInfo.Invoke(paramLst);
            }
            catch {
                success = false;
                continue;
            }
            if(success) {
                return result;
            }
        }
        throw new JumpCommandException(string.Format("Can not invoke constructor of type {0} by {1}", type.Name, parameterStr));
    }

}
