using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class FirstBounce : MonoBehaviour
{

    public Shader positionShader = null;
    public Shader normalShader = null;

    public RenderTexture directLightingTexture = null;
    public RenderTexture positionTexture = null;
    public RenderTexture normalTexture = null;

    private int renderSize = 8;
    private float worldVolumeBoundary = 10.0f;

    private Light spotLight = null;

    // Function used for initializing the reflective shadow map camera
    public void Initialize()
    {

        renderSize = GameObject.Find("Main Camera").GetComponent<Lighting>().renderSize;
        worldVolumeBoundary = GameObject.Find("Main Camera").GetComponent<Lighting>().worldVolumeBoundary;

        directLightingTexture = new RenderTexture(renderSize, renderSize, 16, RenderTextureFormat.ARGBFloat);
        positionTexture = new RenderTexture(renderSize, renderSize, 16, RenderTextureFormat.ARGBFloat);
        normalTexture = new RenderTexture(renderSize, renderSize, 16, RenderTextureFormat.ARGBFloat);

        spotLight = GameObject.Find("Spotlight").GetComponent<Light>();

        GetComponent<Camera>().farClipPlane = spotLight.range;
        GetComponent<Camera>().fieldOfView = spotLight.spotAngle / 2.0f;

    }

    // Function used to release the dynamically allocated render textures
    void OnDestroy()
    {

        directLightingTexture.Release();
        positionTexture.Release();
        normalTexture.Release();

    }

    // Function used for rendering the textures
    public void RenderTextures()
    {
        
        GetComponent<Camera>().targetTexture = directLightingTexture;
        GetComponent<Camera>().Render();
        
        Shader.SetGlobalFloat("_WorldVolumeBoundary", worldVolumeBoundary);
        GetComponent<Camera>().targetTexture = positionTexture;
        GetComponent<Camera>().RenderWithShader(positionShader, null);

        GetComponent<Camera>().targetTexture = normalTexture;
        GetComponent<Camera>().RenderWithShader(normalShader, null);

    }

}