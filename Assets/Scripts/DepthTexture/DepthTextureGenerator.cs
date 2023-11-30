using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DepthTextureGenerator : MonoBehaviour
{
    public Camera renderCam;
    public RenderTexture renderTex;
    public Shader mipMapShader;

    private int renderTexDimension;
    private Material renderTexMat;

    private int depthTextureShaderID;

    // Start is called before the first frame update
    void Start()
    {
        renderTexDimension = Mathf.NextPowerOfTwo(Mathf.Max(Screen.width, Screen.height));

        // Initialize the render texture
        renderTex = new RenderTexture(renderTexDimension, renderTexDimension, 0, RenderTextureFormat.RHalf);
        renderTex.autoGenerateMips = false;     // Manually create mipmap
        renderTex.useMipMap = true;
        renderTex.filterMode = FilterMode.Point;
        renderTex.Create();

        // Generate render material
        renderTexMat = new Material(mipMapShader);
        renderCam.depthTextureMode = DepthTextureMode.Depth;        // Set mode to depth

        depthTextureShaderID = Shader.PropertyToID("_CameraDepthTexture");
    }

    private void OnPostRender()
    {
        int w = renderTex.width;
        int mipmapLevel = 0;

        RenderTexture currentRenderTexture = null;
        RenderTexture prevRenderTexture = null;

        // Current width of mipmap is bigger than 8
        while (w > 8)
        {
            currentRenderTexture = RenderTexture.GetTemporary(w, w, 0, RenderTextureFormat.RHalf);
            currentRenderTexture.filterMode = FilterMode.Point;

            if (prevRenderTexture == null)
            {
                // No previous render texture
                // Creating mipmap[0]
                Graphics.Blit(Shader.GetGlobalTexture(depthTextureShaderID), currentRenderTexture);
            }
            else
            {
                // Blit mipmap[i] to the mipmap[i + 1]
                Graphics.Blit(prevRenderTexture, currentRenderTexture, renderTexMat);
                RenderTexture.ReleaseTemporary(prevRenderTexture);
            }

            Graphics.CopyTexture(currentRenderTexture, 0, 0, renderTex, 0, mipmapLevel);
            prevRenderTexture = currentRenderTexture;

            // Move to next mipmap level
            w /= 2;
            mipmapLevel++;
        }

        RenderTexture.ReleaseTemporary(prevRenderTexture);
    }

    public int GetTextureDimension()
    {
        return renderTexDimension;
    }

    private void OnDestroy()
    {
        renderTex.Release();
        //Destroy(renderTex);
    }
}
