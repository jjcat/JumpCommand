using System;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;
using Object = System.Object;


public class JumpCommandObject  {
    public string      Command     {get;private set;}
    public MethodInfo  Method      {get;private set;}
    public string      Help        {get;private set;}
    public string      GameObjFullName {get;private set;}
    public Type Type {
        get {
            return Method.DeclaringType;
        }
    }

    public JumpCommandObject(string command, MethodInfo method, string help, string gameObject) {
        Command     = command;
        Method      = method;
        Help        = help;
        GameObjFullName = gameObject;
    }

    public override string ToString() {
        string parminfo = ParametersInfo();
        if(parminfo!="") {
            parminfo =  parminfo;
        }
        string helpinfo = Help;
        if(helpinfo != "") {
            helpinfo = "\"" + helpinfo + "\"";
        }
        return string.Format("{0,-10} {1} {2}", Command, parminfo, helpinfo);
    }

    private string ParametersInfo() {
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
                    throw new Exception(string.Format("Missing argument '#{0}' when call method {1}", i, Method.Name));
                }
                else {
                    paramLst[i] = paramInfoLst[i].RawDefaultValue;    
                }
            }
            else {
                if(paramInfoLst[i].ParameterType == typeof(Vector3)) {  
                    paramLst[i] = ConvertVector3FromString(paramStr[i]);
                }
                else if(paramInfoLst[i].ParameterType == typeof(Vector2)) {
                    paramLst[i] = ConvertVector2FromString(paramStr[i]);                    
                }
                else if(paramInfoLst[i].ParameterType == typeof(Color)) {
                    paramLst[i] = ConvertColorFromString(paramStr[i]);
                }
                else if( JumpCommand.ComponentTypes.Contains(paramInfoLst[i].ParameterType)) {  // try to parse component
                    paramLst[i] = JumpCommand.FindComponent(paramStr[i], paramInfoLst[i].ParameterType);
                }
                else {
                    paramLst[i] = TypeDescriptor.GetConverter(paramInfoLst[i].ParameterType).ConvertFrom(paramStr[i]);
                }
            }
        }

        // call method with parameter list
        Object invokeResult = null;
        if(Method.IsStatic) {
            invokeResult = Method.Invoke(null, paramLst);
            if(printResult && invokeResult != null) {
                Debug.Log(invokeResult.ToString());
            }
        }
        else {
            if(callee == null) {
                //Debug.LogError(string.Format("Need type of \"{0}\" instance object to execute", Type.Name));
                throw new Exception(string.Format("Need type of \"{0}\" instance object to execute", Type.Name));
            }
            else {
                invokeResult = Method.Invoke(callee, paramLst);
                if(printResult && invokeResult != null ) {
                    Debug.Log(invokeResult.ToString());
                }
            }
        }
        return true;
    }

    static public Vector2 ConvertVector2FromString(string value) {
        try {
            string tmp =(string) value;
            string[] point = tmp.Split(',');
            float x = Convert.ToSingle(point[0]);
            float y = Convert.ToSingle(point[1]);
            return new Vector2(x, y);            
        } catch {
            throw new Exception("Can not convert " + value + " to type Vector2");
        }
        
    }

    static public Vector3 ConvertVector3FromString(string value) {
        try {
            string tmp =(string) value;
            string[] point = tmp.Split(',');
            float x = Convert.ToSingle(point[0]);
            float y = Convert.ToSingle(point[1]);
            float z = Convert.ToSingle(point[2]);
            return new Vector3(x, y, z);
        } catch {
            throw new Exception("Can not convert " +"\""+value+"\"" + " to type Vector3");      
        }
    }

    static public Color ConvertColorFromString(string value) {
        try {
            string tmp =(string) value;
            string[] point = tmp.Split(',');
            float x = Convert.ToSingle(point[0]);
            float y = Convert.ToSingle(point[1]);
            float z = Convert.ToSingle(point[2]);
            return new Color(x, y, z);
        } catch {
            throw new Exception("Can not convert " +"\""+value+"\"" + " to type Color");      
        }
    }


}
