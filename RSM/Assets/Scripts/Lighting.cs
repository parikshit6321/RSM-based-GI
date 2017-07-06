using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class Lighting : MonoBehaviour {

    struct VPL
    {
        public Vector3 color;
        public Vector3 position;
        public Vector3 normal;
    }

    public Shader lightingShader = null;
    public ComputeShader firstBounceSplattingShader = null;
    public ComputeShader secondBounceSplattingShader = null;
    public int renderSize = 8;
    public float worldVolumeBoundary = 10.0f;
    public float directStrength = 1.0f;
    public float firstBounceStrength = 1.0f;
    public float secondBounceStrength = 1.0f;

    private Material lightingMaterial = null;

    private Camera[] cameras = null;
    private Camera firstBounceCamera = null;
    private Camera secondBounceCamera = null;
    private Camera childCamera = null;
    
    private VPL[] firstVPLData = null;
    private VPL[] secondVPLData = null;

    private ComputeBuffer firstVPLBuffer = null;
    private ComputeBuffer secondVPLBuffer = null;

    private Vector3 secondBounceCameraPosition = Vector3.zero;
    private Vector3 averageNormal = Vector3.zero;
    
	// Use this for initialization
	void Awake () {

        if (lightingShader != null)
            lightingMaterial = new Material(lightingShader);

        cameras = Resources.FindObjectsOfTypeAll<Camera>();

        for(int i = 0; i < cameras.Length; ++i)
        {
            if(cameras[i].name.Equals("First Bounce Camera"))
            {
                firstBounceCamera = cameras[i];
                firstBounceCamera.GetComponent<FirstBounce>().Initialize();
            }
            else if (cameras[i].name.Equals("Second Bounce Camera"))
            {
                secondBounceCamera = cameras[i];
                secondBounceCamera.GetComponent<SecondBounce>().Initialize();
            }
            else if(cameras[i].name.Equals("Child Camera"))
            {
                childCamera = cameras[i];
                childCamera.GetComponent<PositionWriting>().Initialize();
            }
        }

        InitializeVPL();
	}
	
    // Function to initialize the virtual point lights
    void InitializeVPL() {

        firstVPLData = new VPL[renderSize * renderSize];

        for(int i = 0; i < (renderSize * renderSize); ++i)
        {
            firstVPLData[i].color = Vector3.zero;
            firstVPLData[i].position = Vector3.zero;
        }

        firstVPLBuffer = new ComputeBuffer(firstVPLData.Length, 36);
        firstVPLBuffer.SetData(firstVPLData);

        secondVPLData = new VPL[renderSize * renderSize];

        for (int i = 0; i < (renderSize * renderSize); ++i)
        {
            secondVPLData[i].color = Vector3.zero;
            secondVPLData[i].position = Vector3.zero;
        }

        secondVPLBuffer = new ComputeBuffer(secondVPLData.Length, 36);
        secondVPLBuffer.SetData(secondVPLData);
    }

    // Function to generate the vpl data for the first bounce using the textures
    void GenerateFirstBounceVPL() {

        int kernel = firstBounceSplattingShader.FindKernel("SplattingMain");

        firstBounceSplattingShader.SetTexture(kernel, "_DirectLightingTexture", firstBounceCamera.GetComponent<FirstBounce>().directLightingTexture);
        firstBounceSplattingShader.SetTexture(kernel, "_PositionTexture", firstBounceCamera.GetComponent<FirstBounce>().positionTexture);
        firstBounceSplattingShader.SetTexture(kernel, "_NormalTexture", firstBounceCamera.GetComponent<FirstBounce>().normalTexture);
        firstBounceSplattingShader.SetBuffer(kernel, "_VPLBuffer", firstVPLBuffer);
        firstBounceSplattingShader.SetFloat("_WorldVolumeBoundary", worldVolumeBoundary);
        firstBounceSplattingShader.SetInt("_RenderSize", renderSize);

        firstBounceSplattingShader.Dispatch(kernel, renderSize, renderSize, 1);

    }

    // Function to generate the vpl data for the second bounce using the textures
    void GenerateSecondBounceVPL()
    {

        int kernel = secondBounceSplattingShader.FindKernel("SplattingMain");

        secondBounceSplattingShader.SetTexture(kernel, "_LightColorsTexture", firstBounceCamera.GetComponent<FirstBounce>().directLightingTexture);
        secondBounceSplattingShader.SetTexture(kernel, "_DirectLightingTexture", secondBounceCamera.GetComponent<SecondBounce>().directLightingTexture);
        secondBounceSplattingShader.SetTexture(kernel, "_PositionTexture", secondBounceCamera.GetComponent<SecondBounce>().positionTexture);
        secondBounceSplattingShader.SetTexture(kernel, "_NormalTexture", secondBounceCamera.GetComponent<SecondBounce>().normalTexture);
        secondBounceSplattingShader.SetBuffer(kernel, "_VPLBuffer", secondVPLBuffer);
        secondBounceSplattingShader.SetFloat("_WorldVolumeBoundary", worldVolumeBoundary);
        secondBounceSplattingShader.SetInt("_RenderSize", renderSize);

        secondBounceSplattingShader.Dispatch(kernel, renderSize, renderSize, 1);

    }

    // Function to compute orientation of the second bounce camera
    void PositionSecondBounceCamera() {

        firstVPLBuffer.GetData(firstVPLData);

        secondBounceCameraPosition = Vector3.zero;

        for(int i = 0; i < firstVPLData.Length; ++i)
        {
            secondBounceCameraPosition += firstVPLData[i].position;
        }

        secondBounceCameraPosition /= firstVPLData.Length;

        averageNormal = Vector3.zero;
        
        for(int i = 0; i < firstVPLData.Length; ++i)
        {
            averageNormal += firstVPLData[i].normal;
        }

        averageNormal = averageNormal.normalized;

        secondBounceCamera.transform.position = secondBounceCameraPosition;
        secondBounceCamera.transform.LookAt(averageNormal * 10.0f);

    }

	// Use this to add post-processing effects
	void OnRenderImage (RenderTexture source, RenderTexture destination) {

        childCamera.GetComponent<PositionWriting>().RenderTextures();

        firstBounceCamera.GetComponent<FirstBounce>().RenderTextures();
        GenerateFirstBounceVPL();

        PositionSecondBounceCamera();

        secondBounceCamera.GetComponent<SecondBounce>().RenderTextures();
        GenerateSecondBounceVPL();

        RenderTexture indirectTexture = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat);

        lightingMaterial.SetBuffer("_FirstVPLBuffer", firstVPLBuffer);
        lightingMaterial.SetBuffer("_SecondVPLBuffer", secondVPLBuffer);
        lightingMaterial.SetInt("_RenderSize", renderSize);
        lightingMaterial.SetFloat("_WorldVolumeBoundary", worldVolumeBoundary);
        lightingMaterial.SetTexture("_PositionTexture", childCamera.GetComponent<PositionWriting>().positionTexture);
        lightingMaterial.SetTexture("_NormalTexture", childCamera.GetComponent<PositionWriting>().normalTexture);
        lightingMaterial.SetTexture("_ColorTexture", childCamera.GetComponent<PositionWriting>().colorTexture);
        lightingMaterial.SetFloat("_FirstBounceStrength", firstBounceStrength);
        lightingMaterial.SetFloat("_SecondBounceStrength", secondBounceStrength);

        Graphics.Blit(source, indirectTexture, lightingMaterial, 0);

        lightingMaterial.SetTexture("_IndirectTexture", indirectTexture);
        lightingMaterial.SetFloat("_DirectStrength", directStrength);
        Graphics.Blit(source, destination, lightingMaterial, 1);

        RenderTexture.ReleaseTemporary(indirectTexture);

	}
}