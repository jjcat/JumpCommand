using UnityEngine;
using System.Collections;

public class Cube : MonoBehaviour {

    float spinSpeed = 0.5f;
    bool  isSpin = false;

    [CommandItem("color", "change cube color")]
    Color ChangeColor(Color color) {
        renderer.material.color= color;
        return color;
    }

    [CommandItem("move", "move to position")]
    Vector3 Move(Vector3 pos) {
        transform.position += pos;
        return transform.position;
    }

    [CommandItem("move", "move to position")]
    Vector3 Move(Cube cube) {
        transform.position = cube.transform.position;
        return transform.position;
    }

    [CommandItem("spin", "begin spin")]
    void Spin() {
        isSpin = true;
    }

    [CommandItem("stop", "stop spin")]
    void Stop() {
        isSpin = false;
    }

    [CommandItem("speed", "adjust speed")]
    float AdjustSpeed(float speed) {
        spinSpeed += speed;
        return spinSpeed;
    }

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        if(isSpin) {
            transform.Rotate(new Vector3(0, 0, spinSpeed));
        }	
	}
}
