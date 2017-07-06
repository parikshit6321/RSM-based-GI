//#define TEST_ARENA

using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
//FPS class to calc FPS and show fps, total frame count and time till game start
//this class can be added to camera game object.
public class FPSCounter : MonoBehaviour
{
    // Use this for initialization
    string strFPS = "";
    float nextTime = 0;
    int frames = 0;
    
    //SpriteText spComp;
    void Start()
    {
        strFPS = "FPS: " + frames.ToString();
        nextTime = Time.realtimeSinceStartup + 1;
        //Application.targetFrameRate = 500;
        Screen.SetResolution((int)(Screen.width * 0.7f), (int)(Screen.height * 0.7), true);
    }

    // Update is called once per frame
    void Update()
    {
        frames += 1;
        if (Time.realtimeSinceStartup >= nextTime)
        {
            strFPS = "FPS: " + frames.ToString();
            frames = 0;
            nextTime = Time.realtimeSinceStartup + 1;
        }
    }


    void OnGUI()
    {
        GUI.Label(new Rect(500, Screen.height - 20, 200, 50), strFPS);
    }

}
