using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class PositionWriting : MonoBehaviour {

    public Shader positionShader = null;
    public Shader normalShader = null;

    public RenderTexture positionTexture = null;
    public RenderTexture normalTexture = null;
    public RenderTexture colorTexture = null;

    private float worldVolumeBoundary = 10.0f;

    private Light spotLight = null;
    private float spotLightIntensity = 1.0f;

	// Function to initialize position writing
	public void Initialize () {

        worldVolumeBoundary = GameObject.Find("Main Camera").GetComponent<Lighting>().worldVolumeBoundary;

        positionTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGBFloat);
        normalTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGBFloat);
        colorTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGBFloat);

        spotLight = GameObject.Find("Spotlight").GetComponent<Light>();
        spotLightIntensity = spotLight.intensity;
	}
	
    // Function to deallocate the dynamically allocated resources
    void OnDestroy() {

        positionTexture.Release();
        normalTexture.Release();
        colorTexture.Release();

    }

	// Function to render the position texture
	public void RenderTextures () {

        spotLightIntensity = spotLight.intensity;

        Shader.SetGlobalFloat("_WorldVolumeBoundary", worldVolumeBoundary);
        GetComponent<Camera>().targetTexture = positionTexture;
        GetComponent<Camera>().RenderWithShader(positionShader, null);
        
        GetComponent<Camera>().targetTexture = normalTexture;
        GetComponent<Camera>().RenderWithShader(normalShader, null);

        RenderSettings.ambientIntensity = 1.0f;
        spotLight.intensity = 0.0f;

        GetComponent<Camera>().targetTexture = colorTexture;
        GetComponent<Camera>().Render();

        RenderSettings.ambientIntensity = 0.0f;
        spotLight.intensity = spotLightIntensity;
    }
}
