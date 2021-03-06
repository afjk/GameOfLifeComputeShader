﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOfLife : MonoBehaviour {

    public Material targetMaterial;

    public ComputeShader computeShader;

    public Color aliveColor = Color.white;
    public Color dieColor = Color.black;

    public Texture inputTexture;

    [Range(0.01f, 1.0f)]
    public float density = 0.4f;

    [Range(1, 20)]
    public int speed = 1;

    [Range(64, 16284)]
    public int size = 1024;

    RenderTexture renderTextureIn;
    RenderTexture renderTextureOut;

    int kernelIndex_GenerateRandomTexture;
    int kernelIndex_LifeGame;
    int kernelIndex_FlipBuffer;

    ThreadSize kernelThreadSize_GenerateRandomTexture;
    ThreadSize kernelThreadSize_LifeGame;
    ThreadSize kernelThreadSize_FlipBuffer;

    struct ThreadSize
    {
        public int x;
        public int y;
        public int z;

        public ThreadSize(uint x, uint y, uint z)
        {
            this.x = (int)x;
            this.y = (int)y;
            this.z = (int)z;
        }
    }

    // Use this for initialization
    void Start () {

        int width = size;
        int height = size;

        if(inputTexture)
        {
            width = inputTexture.width;
            height = inputTexture.height;
        }

        if(targetMaterial==null)
        {
            targetMaterial = gameObject.GetComponent<Renderer>().material;
        }

        // renderTextureの生成
        this.renderTextureIn  = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        this.renderTextureOut = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        this.renderTextureIn.enableRandomWrite  = true;
        this.renderTextureOut.enableRandomWrite = true;
        this.renderTextureIn.Create();
        this.renderTextureOut.Create();


        // kernel indexの取得
        this.kernelIndex_GenerateRandomTexture
            = this.computeShader.FindKernel("GenerateRandomTexture");
        this.kernelIndex_LifeGame
            = this.computeShader.FindKernel("LifeGame");
        this.kernelIndex_FlipBuffer
            = this.computeShader.FindKernel("FlipBuffer");

        // スレッドサイズの取得
        uint threadSizeX, threadSizeY, threadSizeZ;

        this.computeShader.GetKernelThreadGroupSizes
            (this.kernelIndex_GenerateRandomTexture,
             out threadSizeX, out threadSizeY, out threadSizeZ);

        Debug.Log("x,y,y : " + threadSizeX + "," + threadSizeY + "," + threadSizeZ);

        this.kernelThreadSize_GenerateRandomTexture
            = new ThreadSize(threadSizeX, threadSizeY, threadSizeZ);

        this.computeShader.GetKernelThreadGroupSizes
            (this.kernelIndex_LifeGame,
             out threadSizeX, out threadSizeY, out threadSizeZ);

        this.kernelThreadSize_LifeGame
            = new ThreadSize(threadSizeX, threadSizeY, threadSizeZ);
        
        this.computeShader.GetKernelThreadGroupSizes
            (this.kernelIndex_FlipBuffer,
             out threadSizeX, out threadSizeY, out threadSizeZ);

        this.kernelThreadSize_FlipBuffer
            = new ThreadSize(threadSizeX, threadSizeY, threadSizeZ);


        // kernelへtexture bufferを設定
        this.computeShader.SetTexture
            (this.kernelIndex_GenerateRandomTexture, "textureBufferIn", this.renderTextureIn);
        this.computeShader.SetTexture
            (this.kernelIndex_LifeGame, "textureBufferIn", this.renderTextureIn);
        this.computeShader.SetTexture
            (this.kernelIndex_LifeGame, "textureBufferOut", this.renderTextureOut);
        this.computeShader.SetTexture
            (this.kernelIndex_FlipBuffer, "textureBufferIn", this.renderTextureIn);
        this.computeShader.SetTexture
            (this.kernelIndex_FlipBuffer, "textureBufferOut", this.renderTextureOut);

        // 初期設定
        this.computeShader.SetInt("Seed", (int)Time.time);
        this.computeShader.SetFloat("Density", density);
        this.computeShader.SetFloats("aliveColor", new float[] { aliveColor.r, aliveColor.g, aliveColor.b, aliveColor.a });
        this.computeShader.SetFloats("dieColor", new float[] { dieColor.r, dieColor.g, dieColor.b, dieColor.a });

        if( inputTexture)
        {

            //メインテクスチャのコピー
            Graphics.Blit(inputTexture, this.renderTextureIn);
        }
        else
        {
            this.computeShader.Dispatch(this.kernelIndex_GenerateRandomTexture,
                                        this.renderTextureIn.width / this.kernelThreadSize_GenerateRandomTexture.x,
                                        this.renderTextureIn.height / this.kernelThreadSize_GenerateRandomTexture.y,
                                        this.kernelThreadSize_GenerateRandomTexture.z);

        }
        targetMaterial.mainTexture = this.renderTextureIn;
    }

    int count = 1;

    void Update () {
        if (count++ % speed == 0)
        {
            this.computeShader.Dispatch(this.kernelIndex_LifeGame,
                                        this.renderTextureIn.width / this.kernelThreadSize_LifeGame.x,
                                        this.renderTextureIn.height / this.kernelThreadSize_LifeGame.y,
                                        this.kernelThreadSize_LifeGame.z);

            targetMaterial.mainTexture = this.renderTextureOut;

            this.computeShader.Dispatch(this.kernelIndex_FlipBuffer,
                                            this.renderTextureIn.width / this.kernelThreadSize_FlipBuffer.x,
                                            this.renderTextureIn.height / this.kernelThreadSize_FlipBuffer.y,
                                            this.kernelThreadSize_FlipBuffer.z);
        }
    }
}
