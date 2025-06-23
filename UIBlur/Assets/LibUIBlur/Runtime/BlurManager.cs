
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class NewBehaviourScript : MonoBehaviour
{
    public static bool NeedBulur = false;
    private static Texture2D screenTexture;
    public Material blurMaterialH;
    public Material blurMaterialV;
    [Range(0,10)]
    public float blurRadius = 1.0f;
    [Range(1,10)]
    public int blurLoop = 4;
    [Range(1,10)]
    public int downSample = 8;
    
    private int blurRadiusID = Shader.PropertyToID("_blurSize");
    private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("BlitToRTHandle_CopyColor");
    // private  RTHandle m_InputHandle;
    private RTHandle m_OutputHandle;
    private RTHandle m_TemporaryRT;
    private const string k_OutputName = "_CopyColorBlurTexture";
    private static int m_OutputId = Shader.PropertyToID(k_OutputName);


    private void Start()
    {
        RenderPipelineManager.endContextRendering += OnPostRenderCallback;
    }

    private void OnPostRenderCallback(ScriptableRenderContext arg1, List<Camera> arg2)
    {
        if (NeedBulur)
        {
            BlurScreen();
            NeedBulur = false;
        }   
    }
    

    public void BlurScreen()
    {
        Init();
        screenTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenTexture.Apply();
        
        CommandBuffer cmd = CommandBufferPool.Get();
        cmd.Blit(screenTexture,m_OutputHandle);
        blurMaterialH.SetFloat(blurRadiusID,blurRadius);
        blurMaterialV.SetFloat(blurRadiusID,blurRadius);
        for (int i = 0; i < blurLoop; i++)
        {
            Blitter.BlitCameraTexture(cmd, m_OutputHandle, m_TemporaryRT, blurMaterialH, 0);
            Blitter.BlitCameraTexture(cmd, m_TemporaryRT, m_OutputHandle, blurMaterialV, 0);
        }
        cmd.SetGlobalTexture(m_OutputId, m_OutputHandle.nameID);
        Graphics.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }

    private void Init()
    {
        if (screenTexture == null || screenTexture.width != Screen.width || screenTexture.height != Screen.height)
        {
            if (screenTexture != null)
            {
                GameObject.Destroy(screenTexture);
            }
            
            screenTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            screenTexture.filterMode = FilterMode.Point;
            screenTexture.wrapMode = TextureWrapMode.Clamp;
            screenTexture.name = "BlurScreen";
        }
        
        RenderTextureDescriptor desc = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default);
        desc.depthBufferBits = 0;
        desc.msaaSamples = 1;
        desc.width /= downSample;
        desc.height /=  downSample;
        
        if (m_OutputHandle != null)
        {
            if ( m_OutputHandle.rt.width != desc.width || m_OutputHandle.rt.height != desc.height)
            {
                m_OutputHandle.Release();
                GameObject.Destroy(m_OutputHandle);
                m_OutputHandle = null;
                RenderingUtils.ReAllocateIfNeeded(ref m_OutputHandle, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: k_OutputName );
            }
        }
        else
        {
            RenderingUtils.ReAllocateIfNeeded(ref m_OutputHandle, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: k_OutputName );
        }
        
        if (m_TemporaryRT!= null)
        {
            if ( m_TemporaryRT.rt.width!= desc.width || m_TemporaryRT.rt.height!= desc.height)
            {
                m_TemporaryRT.Release();
                GameObject.Destroy(m_TemporaryRT);
                m_TemporaryRT = null;
                RenderingUtils.ReAllocateIfNeeded(ref m_TemporaryRT,desc,FilterMode.Bilinear,TextureWrapMode.Clamp,name:"_BlurCopyTex");  
            }
        }
        else
        {
            RenderingUtils.ReAllocateIfNeeded(ref m_TemporaryRT,desc,FilterMode.Bilinear,TextureWrapMode.Clamp,name:"_BlurCopyTex");  
        }
        
    }

    private void OnDestroy()
    {
        if (m_OutputHandle != null)
        {
            m_OutputHandle.Release();
            GameObject.Destroy(m_OutputHandle);
            m_OutputHandle = null;
        }
        if (m_TemporaryRT!= null)
            {
            m_TemporaryRT.Release();
            GameObject.Destroy(m_TemporaryRT);
            m_TemporaryRT = null;
            }
        if (screenTexture!= null)
            {
            GameObject.Destroy(screenTexture);
            screenTexture = null;
            }
    }
}
