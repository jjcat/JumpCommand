using UnityEngine;
using System.Collections;

public class CubeFactory : MonoBehaviour {

    public GameObject CubePrefab;
    public Transform  Parent;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    [JumpCommandRegister("create", "Create New Cube", "CubeFactory")]
    GameObject Create(Vector3 birthPos ){
        var go = GameObject.Instantiate(CubePrefab, birthPos, Quaternion.identity) as GameObject;
        go.transform.parent = Parent;
        return go;
    } 

}
