using UnityEngine;
using System.Collections;

public class TemporaryObject : MonoBehaviour {

    public float life= 5;

    float time;

	// Use this for initialization
	void Start () {
        time = 0;
	}
	
	// Update is called once per frame
	void Update () {
        if (time > life)
            GameObject.Destroy(this.gameObject);

        time += Time.deltaTime;
	}
}
