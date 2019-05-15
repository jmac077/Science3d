using UnityEngine;
using System.Collections;
using System.IO;
using System;

public class ErosionSim : MonoBehaviour 
{
	public enum NOISE_STYLE { FRACTAL = 0, TURBULENCE = 1, RIDGE_MULTI_FRACTAL = 2, WARPED = 3 };
	//This will allow you to set a noise style for each terrain layer
	public NOISE_STYLE[] m_layerStyle = new NOISE_STYLE[4];
	//This will take the abs value of the final noise is set to true
	//This will make the fractal or warped noise look different.
	//It will have no effect on turbulence or ridged noise as they are all ready abs
	public bool[] m_finalNosieIsAbs = new bool[4];
	
	public AddSpheres addsphere;
	public GameObject m_sun;
	public Material m_landMat, m_waterMat, m_wireframeMat, m_lavaMat,m_tsunamiMat,m_terrainLandMat;
	public Material m_initTerrainMat, m_noiseMat, m_waterInputMat, m_terrainOutputMat, m_lavaInputMat;
	public Material m_evaprationMat, m_outFlowMat, m_fieldUpdateMat;
	public Material m_waterVelocityMat, m_diffuseVelocityMat, m_tiltAngleMat, m_lavaVelocityMat;
	public Material m_erosionAndDepositionMat, m_advectSedimentMat, m_processMacCormackMat;
	public Material m_slippageHeightMat, m_slippageOutflowMat, m_slippageUpdateMat;
	public Material m_disintegrateAndDepositMat, m_applyFreeSlipMat;
	public ComputeShader shader;
	public ComputeShader shader2;
	public ComputeShader getPix;
	public ComputeShader LavaOrWater;
	ComputeBuffer buffer;
	ComputeBuffer buffer2;
	ComputeBuffer buffer3;
	ComputeBuffer buffer4;
	ComputeBuffer buffer5;
	ComputeBuffer buffer6;

	public float m_waterInputSpeed = 0.01f;
	public Vector2 m_waterInputPoint = new Vector2(0.5f,0.5f);
	public Vector2 m_lavaInputPoint = new Vector2(20.5f,20.5f);
	public float m_waterInputAmount = 2.0f;
	public float m_lavaInputAmount = 2.0f;
	public float m_waterInputRadius = 0.008f;
	public float m_lavaInputRadius = 0.008f;
	public float m_terrainOutputAmount = 30.0f;
	public float m_terrainOutputRadius = 5.0f;
	public float m_tsunamiAmount = 20;
	
	//Noise settings. Each Component of vector is the setting for a layer
	//ie x is setting for layer 0, y is setting for layer 1 etc
	public int m_seed = 2;
	public Vector4 m_octaves = new Vector4(8,8,8,8); //Higher octaves give more finer detail
	public Vector4 m_frequency = new Vector4(2.0f, 100.0f, 200.0f, 200.0f); //A lower value gives larger scale details
	public Vector4 m_lacunarity = new Vector4(2.0f, 3.0f, 3.0f, 2.0f); //Rate of change of the noise amplitude. Should be between 1 and 3 for fractal noise
	public Vector4 m_gain = new Vector4(0.5f, 0.5f, 0.5f, 0.5f); //Rate of chage of the noise frequency
	public Vector4 m_amp = new Vector4(2.0f, 0.01f, 0.01f, 0.001f); //Amount of terrain in a layer
	public Vector4 m_offset = new Vector4(0.0f, 10.0f, 20.0f, 30.0f);
	
	//The settings for the erosion. If the value is a vector4 each component is for a layer
	public Vector4 m_dissolvingConstant = new Vector4(0.01f, 0.04f, 0.2f, 0.2f); //How easily the layer dissolves
	public float m_sedimentCapacity = 0.2f; //How much sediment the water can carry
	public float m_depositionConstant = 0.015f; //Rate the sediment is deposited on top layer
	public float m_evaporationConstant = 0.01f; //Evaporation rate of water
	public float m_minTiltAngle = 0.1f; //A higher value will increase erosion on flat areas
	public float m_regolithDamping = 0.85f; //Viscosity of regolith
	public float m_waterDamping = 0.0f; //Viscosity of water
	public float m_lavaDamping = 0.5f; //Viscosity of lava
	public float m_maxRegolith = 0.008f; //Higher number will increase dissolution rate
	public Vector4 m_talusAngle = new Vector4(45.0f, 20.0f, 15.0f, 15.0f); //The angle that slippage will occur
	
	GameObject[] m_gridLand, m_gridWater, m_gridWireframe, m_gridLava;
	RenderTexture[] m_terrainField, m_waterOutFlow, m_waterVelocity, m_lavaOutFlow, m_lavaVelocity;
	RenderTexture[] m_advectSediment, m_waterField, m_sedimentField, m_lavaField;
	RenderTexture m_tiltAngle, m_slippageHeight, m_slippageOutflow;
	RenderTexture[] m_regolithField, m_regolithOutFlow;
	ImprovedPerlinNoise m_perlin;
	int m_frameCount = 0;
	Rect m_rectLeft, m_rectRight, m_rectTop, m_rectBottom;
	TerrainData terrainData;
	GameObject ter;
	
	//The number of layers used in the simulation. Must be 1, 2, 3 or, 4
	const int TERRAIN_LAYERS = 3;
	//The resolution of the textures used for the simulation. You can change this to any number
	//Does not have to be a pow2 number. You will run out of GPU memory if made to high.
	const int TEX_SIZE = 1024;
	//The height of the terrain. You can change this
	const int TERRAIN_HEIGHT = 128;
	//This is the size and resolution of the terrain mesh you see
	//You can change this but must be a pow2 number, ie 256, 512, 1024 etc
	const int TOTAL_GRID_SIZE = 512;
	//You can make this smaller but not larger
	const float TIME_STEP = 0.1f; 
	
	//Dont change these
	const int GRID_SIZE = 128;
	const float PIPE_LENGTH = 1.0f;
	const float CELL_LENGTH = 1.0f;
	const float CELL_AREA = 1.0f; //CELL_LENGTH*CELL_LENGTH
	const float GRAVITY = 9.81f;
	const int READ = 0;
	const int WRITE = 1;

	private int count = 10;
	
	void Start() 
	{
		m_waterDamping = Mathf.Clamp01(m_waterDamping);
		m_regolithDamping = Mathf.Clamp01(m_regolithDamping);
		
		float u = 1.0f/(float)TEX_SIZE;
		
		m_rectLeft = new Rect(0.0f, 0.0f, u, 1.0f);
		m_rectRight = new Rect(1.0f-u, 0.0f, u, 1.0f);
		
		m_rectBottom = new Rect(0.0f, 0.0f, 1.0f, u);
		m_rectTop = new Rect(0.0f, 1.0f-u, 1.0f, u);
		
		m_terrainField = new RenderTexture[2];
		m_waterOutFlow = new RenderTexture[2];
		m_waterVelocity = new RenderTexture[2];
		m_advectSediment = new RenderTexture[2];
		m_waterField = new RenderTexture[2];
		m_sedimentField = new RenderTexture[2];
		m_regolithField = new RenderTexture[2];
		m_regolithOutFlow = new RenderTexture[2];
		m_lavaField = new RenderTexture[2];
		m_lavaVelocity = new RenderTexture[2];
		m_lavaOutFlow = new RenderTexture[2];
		
		m_terrainField[0] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.ARGBFloat);
		m_terrainField[0].enableRandomWrite = true;
		m_terrainField[0].wrapMode = TextureWrapMode.Clamp;
		m_terrainField[0].filterMode = FilterMode.Point;
		m_terrainField[1] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.ARGBFloat);
		m_terrainField[1].wrapMode = TextureWrapMode.Clamp;
		m_terrainField[1].filterMode = FilterMode.Point;
		
		m_waterOutFlow[0] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.ARGBHalf);
		m_waterOutFlow[0].wrapMode = TextureWrapMode.Clamp;
		m_waterOutFlow[0].filterMode = FilterMode.Point;
		m_waterOutFlow[1] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.ARGBHalf);
		m_waterOutFlow[1].wrapMode = TextureWrapMode.Clamp;
		m_waterOutFlow[1].filterMode = FilterMode.Point;

		m_lavaOutFlow[0] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.ARGBHalf);
		m_lavaOutFlow[0].wrapMode = TextureWrapMode.Clamp;
		m_lavaOutFlow[0].filterMode = FilterMode.Point;
		m_lavaOutFlow[1] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.ARGBHalf);
		m_lavaOutFlow[1].wrapMode = TextureWrapMode.Clamp;
		m_lavaOutFlow[1].filterMode = FilterMode.Point;
		
		m_waterVelocity[0] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RGHalf);
		m_waterVelocity[0].wrapMode = TextureWrapMode.Clamp;
		m_waterVelocity[0].filterMode = FilterMode.Bilinear;
		m_waterVelocity[1] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RGHalf);
		m_waterVelocity[1].wrapMode = TextureWrapMode.Clamp;
		m_waterVelocity[1].filterMode = FilterMode.Bilinear;

		m_lavaVelocity[0] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RGHalf);
		m_lavaVelocity[0].wrapMode = TextureWrapMode.Clamp;
		m_lavaVelocity[0].filterMode = FilterMode.Bilinear;
		m_lavaVelocity[1] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RGHalf);
		m_lavaVelocity[1].wrapMode = TextureWrapMode.Clamp;
		m_lavaVelocity[1].filterMode = FilterMode.Bilinear;
		
		m_waterField[0] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RFloat);
		m_waterField[0].wrapMode = TextureWrapMode.Clamp;
		m_waterField[0].filterMode = FilterMode.Point;
		m_waterField[1] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RFloat);
		m_waterField[1].wrapMode = TextureWrapMode.Clamp;
		m_waterField[1].filterMode = FilterMode.Point;

		m_lavaField[0] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RFloat);
		m_lavaField[0].wrapMode = TextureWrapMode.Clamp;
		m_lavaField[0].filterMode = FilterMode.Point;
		m_lavaField[1] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RFloat);
		m_lavaField[1].wrapMode = TextureWrapMode.Clamp;
		m_lavaField[1].filterMode = FilterMode.Point;
		
		m_regolithField[0] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RFloat);
		m_regolithField[0].wrapMode = TextureWrapMode.Clamp;
		m_regolithField[0].filterMode = FilterMode.Point;
		m_regolithField[1] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RFloat);
		m_regolithField[1].wrapMode = TextureWrapMode.Clamp;
		m_regolithField[1].filterMode = FilterMode.Point;
		
		m_regolithOutFlow[0] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.ARGBHalf);
		m_regolithOutFlow[0].wrapMode = TextureWrapMode.Clamp;
		m_regolithOutFlow[0].filterMode = FilterMode.Point;
		m_regolithOutFlow[1] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.ARGBHalf);
		m_regolithOutFlow[1].wrapMode = TextureWrapMode.Clamp;
		m_regolithOutFlow[1].filterMode = FilterMode.Point;
		
		m_sedimentField[0] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RHalf);
		m_sedimentField[0].wrapMode = TextureWrapMode.Clamp;
		m_sedimentField[0].filterMode = FilterMode.Bilinear;
		m_sedimentField[1] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RHalf);
		m_sedimentField[1].wrapMode = TextureWrapMode.Clamp;
		m_sedimentField[1].filterMode = FilterMode.Bilinear;
		
		m_advectSediment[0] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RHalf);
		m_advectSediment[0].wrapMode = TextureWrapMode.Clamp;
		m_advectSediment[0].filterMode = FilterMode.Bilinear;
		m_advectSediment[1] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RHalf);
		m_advectSediment[1].wrapMode = TextureWrapMode.Clamp;
		m_advectSediment[1].filterMode = FilterMode.Bilinear;
		
		m_tiltAngle = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RHalf);
		m_tiltAngle.wrapMode = TextureWrapMode.Clamp;
		m_tiltAngle.filterMode = FilterMode.Point;
		
		m_slippageHeight = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RHalf);
		m_slippageHeight.wrapMode = TextureWrapMode.Clamp;
		m_slippageHeight.filterMode = FilterMode.Point;
		
		m_slippageOutflow = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.ARGBHalf);
		m_slippageOutflow.wrapMode = TextureWrapMode.Clamp;
		m_slippageOutflow.filterMode = FilterMode.Point;
	   	
	   	m_perlin = new ImprovedPerlinNoise(m_seed);
		m_perlin.LoadResourcesFor2DNoise();
				
		MakeGrids();
	  		
	}
	
	void WaterInput()
	{
		
		if(Input.GetKey(KeyCode.DownArrow)) m_waterInputPoint.y -= m_waterInputSpeed * Time.deltaTime;
		
		if(Input.GetKey(KeyCode.UpArrow)) m_waterInputPoint.y += m_waterInputSpeed * Time.deltaTime;
		
		if(Input.GetKey(KeyCode.LeftArrow)) m_waterInputPoint.x -= m_waterInputSpeed * Time.deltaTime;
		
		if(Input.GetKey(KeyCode.RightArrow)) m_waterInputPoint.x += m_waterInputSpeed * Time.deltaTime;
		
		if(m_waterInputAmount > 0.0f)
		{
			m_waterInputMat.SetVector("_Point", m_waterInputPoint);
			m_waterInputMat.SetFloat("_Radius", m_waterInputRadius);
			m_waterInputMat.SetFloat("_Amount", m_waterInputAmount);
			
			Graphics.Blit(m_waterField[READ], m_waterField[WRITE], m_waterInputMat);
			RTUtility.Swap(m_waterField);
		}
		
		if(m_evaporationConstant > 0.0f)
		{
			m_evaprationMat.SetFloat("_EvaporationConstant", m_evaporationConstant);
			
			Graphics.Blit(m_waterField[READ], m_waterField[WRITE], m_evaprationMat);
			RTUtility.Swap(m_waterField);
		}
	}
	
	void LavaInput()
	{	
		if(m_lavaInputAmount > 0.0f)
		{
			m_lavaInputMat.SetVector("_Point", m_lavaInputPoint);
			m_lavaInputMat.SetFloat("_Radius", m_lavaInputRadius);
			m_lavaInputMat.SetFloat("_Amount", m_lavaInputAmount);
			
			Graphics.Blit(m_lavaField[READ], m_lavaField[WRITE], m_lavaInputMat);
			RTUtility.Swap(m_lavaField);
		}
	}

	void ApplyFreeSlip(RenderTexture[] field)
	{
		float u = 1.0f/(float)TEX_SIZE;
		Vector2 offset;
		
		Graphics.Blit(field[READ], field[WRITE]);
		
		offset = new Vector2(u,0.0f);
		m_applyFreeSlipMat.SetVector("_Offset", offset);
		RTUtility.Blit(field[READ], field[WRITE], m_applyFreeSlipMat, m_rectLeft, 0, false);
		
		offset = new Vector2(0.0f,u);
		m_applyFreeSlipMat.SetVector("_Offset", offset);
		RTUtility.Blit(field[READ], field[WRITE], m_applyFreeSlipMat, m_rectBottom, 0, false);
		
		offset = new Vector2(-u,0.0f);
		m_applyFreeSlipMat.SetVector("_Offset", offset);
		RTUtility.Blit(field[READ], field[WRITE], m_applyFreeSlipMat, m_rectRight, 0, false);
				
		offset = new Vector2(0.0f,-u);
		m_applyFreeSlipMat.SetVector("_Offset", offset);
		RTUtility.Blit(field[READ], field[WRITE], m_applyFreeSlipMat, m_rectTop, 0, false);
		
		RTUtility.Swap(field);
	}
	
	void OutFlow(RenderTexture[] field, RenderTexture[] outFlow, float damping)
	{
		m_outFlowMat.SetFloat("_TexSize", (float)TEX_SIZE);
		m_outFlowMat.SetFloat("T", TIME_STEP);
		m_outFlowMat.SetFloat("L", PIPE_LENGTH);
		m_outFlowMat.SetFloat("A", CELL_AREA);
		m_outFlowMat.SetFloat("G", GRAVITY);
		m_outFlowMat.SetFloat("_Layers", TERRAIN_LAYERS);
		m_outFlowMat.SetFloat("_Damping", 1.0f - damping);
		m_outFlowMat.SetTexture("_TerrainField", m_terrainField[READ]);
		m_outFlowMat.SetTexture("_Field", field[READ]);
		
		Graphics.Blit(outFlow[READ], outFlow[WRITE], m_outFlowMat);
		RTUtility.Swap(outFlow);
		
		m_fieldUpdateMat.SetFloat("_TexSize", (float)TEX_SIZE);
		m_fieldUpdateMat.SetFloat("T", TIME_STEP);
		m_fieldUpdateMat.SetFloat("L", PIPE_LENGTH);
		m_fieldUpdateMat.SetTexture("_OutFlowField", outFlow[READ]);
		
		Graphics.Blit(field[READ], field[WRITE], m_fieldUpdateMat);
		RTUtility.Swap(field);
	}
	
	void DisintegrateAndDeposit()
	{
		m_disintegrateAndDepositMat.SetFloat("_Layers", (float)TERRAIN_LAYERS);
		m_disintegrateAndDepositMat.SetTexture("_TerrainField", m_terrainField[READ]);
		m_disintegrateAndDepositMat.SetTexture("_WaterField", m_waterField[READ]);
		m_disintegrateAndDepositMat.SetTexture("_RegolithField", m_regolithField[READ]);
		m_disintegrateAndDepositMat.SetFloat("_MaxRegolith", m_maxRegolith);
		
		RenderTexture[] terrainAndRegolith = new RenderTexture[2]{m_terrainField[WRITE],m_regolithField[WRITE]};
		
		RTUtility.MultiTargetBlit(terrainAndRegolith, m_disintegrateAndDepositMat);
		RTUtility.Swap(m_terrainField);
		RTUtility.Swap(m_regolithField);
	}
	
	void LavaVelocity()
	{
		m_lavaVelocityMat.SetFloat("_TexSize", (float)TEX_SIZE);
		m_lavaVelocityMat.SetFloat("L", CELL_LENGTH);
		m_lavaVelocityMat.SetTexture("_LavaField", m_lavaField[READ]);
		m_lavaVelocityMat.SetTexture("_LavaFieldOld", m_lavaField[WRITE]);
		m_lavaVelocityMat.SetTexture("_OutFlowField", m_lavaOutFlow[READ]);
		
		Graphics.Blit(null, m_lavaVelocity[READ], m_lavaVelocityMat);

		const float viscosity = 10.5f;
		const int iterations = 2;
		
		m_diffuseVelocityMat.SetFloat("_TexSize", (float)TEX_SIZE);
		m_diffuseVelocityMat.SetFloat("_Alpha", CELL_AREA / (viscosity * TIME_STEP));
		
		for(int i = 0; i < iterations; i++) 
		{
			Graphics.Blit(m_lavaVelocity[READ], m_lavaVelocity[WRITE], m_diffuseVelocityMat);
			RTUtility.Swap(m_lavaVelocity);
		}
	}

	void WaterVelocity()
	{
		m_waterVelocityMat.SetFloat("_TexSize", (float)TEX_SIZE);
		m_waterVelocityMat.SetFloat("L", CELL_LENGTH);
		m_waterVelocityMat.SetTexture("_WaterField", m_waterField[READ]);
		m_waterVelocityMat.SetTexture("_WaterFieldOld", m_waterField[WRITE]);
		m_waterVelocityMat.SetTexture("_OutFlowField", m_waterOutFlow[READ]);
		
		Graphics.Blit(null, m_waterVelocity[READ], m_waterVelocityMat);
		
		const float viscosity = 10.5f;
		const int iterations = 2;
		
		m_diffuseVelocityMat.SetFloat("_TexSize", (float)TEX_SIZE);
		m_diffuseVelocityMat.SetFloat("_Alpha", CELL_AREA / (viscosity * TIME_STEP));
		
		for(int i = 0; i < iterations; i++) 
		{
			Graphics.Blit(m_waterVelocity[READ], m_waterVelocity[WRITE], m_diffuseVelocityMat);
			RTUtility.Swap(m_waterVelocity);
		}
	}
	
	void ErosionAndDeposition()
	{
		m_tiltAngleMat.SetFloat("_TexSize", (float)TEX_SIZE);
		m_tiltAngleMat.SetFloat("_Layers", TERRAIN_LAYERS);
		m_tiltAngleMat.SetTexture("_TerrainField", m_terrainField[READ]);
		
		Graphics.Blit(null, m_tiltAngle, m_tiltAngleMat);
		
		m_erosionAndDepositionMat.SetTexture("_TerrainField", m_terrainField[READ]);
		m_erosionAndDepositionMat.SetTexture("_SedimentField", m_sedimentField[READ]);
		m_erosionAndDepositionMat.SetTexture("_VelocityField", m_waterVelocity[READ]);
		m_erosionAndDepositionMat.SetTexture("_TiltAngle", m_tiltAngle);
		m_erosionAndDepositionMat.SetFloat("_MinTiltAngle", m_minTiltAngle);
		m_erosionAndDepositionMat.SetFloat("_SedimentCapacity", m_sedimentCapacity);
		m_erosionAndDepositionMat.SetVector("_DissolvingConstant", m_dissolvingConstant);
		m_erosionAndDepositionMat.SetFloat("_DepositionConstant", m_depositionConstant);
		m_erosionAndDepositionMat.SetFloat("_Layers", (float)TERRAIN_LAYERS);
		
		RenderTexture[] terrainAndSediment = new RenderTexture[2]{m_terrainField[WRITE],m_sedimentField[WRITE]};
		
		RTUtility.MultiTargetBlit(terrainAndSediment, m_erosionAndDepositionMat);
		RTUtility.Swap(m_terrainField);
		//RTUtility.Swap(m_sedimentField);
	}
	
	void AdvectSediment()
	{
		m_advectSedimentMat.SetFloat("_TexSize", (float)TEX_SIZE);
		m_advectSedimentMat.SetFloat("T", TIME_STEP);
		m_advectSedimentMat.SetFloat("_VelocityFactor", 1.0f);
		m_advectSedimentMat.SetTexture("_VelocityField", m_waterVelocity[READ]);
		
		Graphics.Blit(m_sedimentField[READ], m_advectSediment[0], m_advectSedimentMat);
		
		m_advectSedimentMat.SetFloat("_VelocityFactor", -1.0f);
		Graphics.Blit(m_advectSediment[0], m_advectSediment[1], m_advectSedimentMat);
		
		m_processMacCormackMat.SetFloat("_TexSize", (float)TEX_SIZE);
		m_processMacCormackMat.SetFloat("T", TIME_STEP);
		m_processMacCormackMat.SetTexture("_VelocityField", m_waterVelocity[READ]);
		m_processMacCormackMat.SetTexture("_InterField1", m_advectSediment[0]);
		m_processMacCormackMat.SetTexture("_InterField2", m_advectSediment[1]);
		
		Graphics.Blit(m_sedimentField[READ], m_sedimentField[WRITE], m_processMacCormackMat);
		RTUtility.Swap(m_sedimentField);
	}
	
	void ApplySlippage()
	{
		for(int i = 0; i < TERRAIN_LAYERS; i++)
		{
			if(m_talusAngle[i] < 90.0f)
			{
				float talusAngle = (Mathf.PI * m_talusAngle[i]) / 180.0f;
				float maxHeightDif = Mathf.Tan(talusAngle) * CELL_LENGTH;
				
				m_slippageHeightMat.SetFloat("_TexSize", (float)TEX_SIZE);
				m_slippageHeightMat.SetFloat("_Layers", (float)(i+1));
				m_slippageHeightMat.SetFloat("_MaxHeightDif", maxHeightDif);
				m_slippageHeightMat.SetTexture("_TerrainField", m_terrainField[READ]);
				
				Graphics.Blit(null, m_slippageHeight, m_slippageHeightMat);
				
				m_slippageOutflowMat.SetFloat("_TexSize", (float)TEX_SIZE);
				m_slippageOutflowMat.SetFloat("_Layers", (float)(i+1));
				m_slippageOutflowMat.SetFloat("T", TIME_STEP);
				m_slippageOutflowMat.SetTexture("_MaxSlippageHeights", m_slippageHeight);
				m_slippageOutflowMat.SetTexture("_TerrainField", m_terrainField[READ]);
				
				Graphics.Blit(null, m_slippageOutflow, m_slippageOutflowMat);
				
				m_slippageUpdateMat.SetFloat("T", TIME_STEP);
				m_slippageUpdateMat.SetFloat("_TexSize", (float)TEX_SIZE);
				m_slippageUpdateMat.SetFloat("_Layers", (float)(i+1));
				m_slippageUpdateMat.SetTexture("_SlippageOutflow", m_slippageOutflow);
				
				Graphics.Blit(m_terrainField[READ], m_terrainField[WRITE], m_slippageUpdateMat);
				RTUtility.Swap(m_terrainField);
			}
		}
		
	}
		
	void Update()
	{
		//You cant call the graphics blit function on the first frame in Unity for a dx9 build
		//Wait to second frame then init maps. You only need to do this if you want to make a dx9 build
		if(m_frameCount == 1){ 
			InitMaps();
			//initMeshCollider();
		}
		/*if( m_frameCount>1800 && count >0 && m_frameCount%100 == 0){
			Tsunami();
			count--;
		}*/
		m_frameCount++;
		
		RTUtility.SetToPoint(m_terrainField);
		RTUtility.SetToPoint(m_waterField);
		RTUtility.SetToPoint(m_lavaField);

		
		WaterInput();
		LavaInput();
		
		ApplyFreeSlip(m_terrainField);
		ApplyFreeSlip(m_sedimentField);
		ApplyFreeSlip(m_waterField);
		ApplyFreeSlip(m_regolithField);
		ApplyFreeSlip(m_lavaField);

		OutFlow(m_waterField, m_waterOutFlow, m_waterDamping);
		OutFlow(m_lavaField, m_lavaOutFlow, m_lavaDamping);
		
		WaterVelocity();
		LavaVelocity();
		
		ErosionAndDeposition();
		ApplyFreeSlip(m_terrainField);
		ApplyFreeSlip(m_sedimentField);
		
		//AdvectSediment();
		
		//DisintegrateAndDeposit();
		ApplyFreeSlip(m_terrainField);
		ApplyFreeSlip(m_regolithField);
		
		OutFlow(m_regolithField, m_regolithOutFlow, m_regolithDamping);
	
		ApplySlippage();
		
		RTUtility.SetToBilinear(m_terrainField);
		RTUtility.SetToBilinear(m_waterField);
		RTUtility.SetToBilinear(m_lavaField);
		
		//if the size of the mesh does not match the size of the teture 
		//the y axis needs to be scaled 
		float scaleY = (float)TOTAL_GRID_SIZE / (float)TEX_SIZE;
		
		m_landMat.SetFloat("_ScaleY", scaleY);
		m_landMat.SetFloat("_TexSize", (float)TEX_SIZE);
		m_landMat.SetTexture("_MainTex", m_terrainField[READ]);
		m_landMat.SetFloat("_Layers", (float)TERRAIN_LAYERS);

		/*m_terrainLandMat.SetFloat("_ScaleY", scaleY);
		m_terrainLandMat.SetFloat("_TexSize", (float)TEX_SIZE);
		m_terrainLandMat.SetTexture("_LandTex", m_terrainField[READ]);
		m_terrainLandMat.SetFloat("_Layers", (float)TERRAIN_LAYERS);*/

		m_waterMat.SetTexture("_SedimentField", m_sedimentField[READ]);
		m_waterMat.SetTexture("_VelocityField", m_waterVelocity[READ]);
		m_waterMat.SetFloat("_ScaleY", scaleY);
		m_waterMat.SetFloat("_TexSize", (float)TEX_SIZE);
		m_waterMat.SetTexture("_WaterField", m_waterField[READ]);
		m_waterMat.SetTexture("_MainTex", m_terrainField[READ]);
		m_waterMat.SetFloat("_Layers", (float)TERRAIN_LAYERS);
		m_waterMat.SetVector("_SunDir", m_sun.transform.forward*-1.0f);

		m_lavaMat.SetTexture("_VelocityField", m_lavaVelocity[READ]);
		m_lavaMat.SetFloat("_ScaleY", scaleY);
		m_lavaMat.SetFloat("_TexSize", (float)TEX_SIZE);
		m_lavaMat.SetTexture("_LavaField", m_lavaField[READ]);
		m_lavaMat.SetTexture("_MainTex", m_terrainField[READ]);
		m_lavaMat.SetFloat("_Layers", (float)TERRAIN_LAYERS);
	
		m_wireframeMat.SetFloat("_ScaleY", scaleY);
		m_wireframeMat.SetFloat("_TexSize", (float)TEX_SIZE);
		m_wireframeMat.SetTexture("_MainTex", m_terrainField[READ]);
		m_wireframeMat.SetFloat("_Layers", (float)TERRAIN_LAYERS);
		
		//updateTerrainHeight();		
	}
	
	void InitMaps()
	{
		RTUtility.ClearColor(m_terrainField);
		RTUtility.ClearColor(m_waterOutFlow);
		RTUtility.ClearColor(m_waterVelocity);
		RTUtility.ClearColor(m_advectSediment);
		RTUtility.ClearColor(m_waterField);
		RTUtility.ClearColor(m_sedimentField);
		RTUtility.ClearColor(m_regolithField);
		RTUtility.ClearColor(m_regolithOutFlow);
		RTUtility.ClearColor(m_lavaOutFlow);
		RTUtility.ClearColor(m_lavaVelocity);
		RTUtility.ClearColor(m_lavaField);

		RenderTexture[] noiseTex = new RenderTexture[2];
		
		noiseTex[0] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RFloat);
		noiseTex[0].wrapMode = TextureWrapMode.Clamp;
		noiseTex[0].filterMode = FilterMode.Bilinear;
		
		noiseTex[1] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RFloat);
		noiseTex[1].wrapMode = TextureWrapMode.Clamp;
		noiseTex[1].filterMode = FilterMode.Bilinear;
		
		m_noiseMat.SetTexture("_PermTable1D", m_perlin.GetPermutationTable1D());
		m_noiseMat.SetTexture("_Gradient2D", m_perlin.GetGradient2D());
		
		for(int j = 0; j < 1; j++)
		{
			m_noiseMat.SetFloat("_Offset", m_offset[j]);
			
			float amp = 0.5f;
			float freq = m_frequency[j];
			
			//Must clear noise from last pass
			RTUtility.ClearColor(noiseTex);
			
			//write noise into texture with the settings for this layer
			for(int i = 0; i < m_octaves[j]; i++)
			{
				m_noiseMat.SetFloat("_Frequency", freq);
				m_noiseMat.SetFloat("_Amp", amp);
				m_noiseMat.SetFloat("_Pass", (float)i);
	
				Graphics.Blit(noiseTex[READ], noiseTex[WRITE], m_noiseMat, (int)m_layerStyle[j]);
				RTUtility.Swap(noiseTex);
				
				freq *= m_lacunarity[j];
				amp *= m_gain[j];
			}
			
			float useAbs = 0.0f;
			if(m_finalNosieIsAbs[j]) useAbs = 1.0f;
			
			//Mask the layers that we dont want to write into
			Vector4 mask = new Vector4(0.0f,0.0f,0.0f,0.0f);
			mask[j] = 1.0f;
			
			m_initTerrainMat.SetFloat("_Amp", m_amp[j]);
			m_initTerrainMat.SetFloat("_UseAbs", useAbs);
			m_initTerrainMat.SetVector("_Mask", mask);
			m_initTerrainMat.SetTexture("_NoiseTex", noiseTex[READ]);
			m_initTerrainMat.SetFloat("_Height", TERRAIN_HEIGHT);
			
			//Apply the noise for this layer to the terrain field
			Graphics.Blit(m_terrainField[READ], m_terrainField[WRITE], m_initTerrainMat);
			RTUtility.Swap(m_terrainField);
		}
		
		//dont need this tex anymore
		noiseTex[0] = null;
		noiseTex[1] = null;

		/*Texture2D myTexture2D = new Texture2D(1024,1024,TextureFormat.RGBAFloat,false);
		string path = "Assets\\Textures\\terrain.raw";
		myTexture2D.LoadRawTextureData(File.ReadAllBytes(path));
		myTexture2D.Apply();
		m_gridLand[0].AddComponent<GUITexture>();
		m_gridLand[0].GetComponent<GUITexture>().texture = myTexture2D;
		Graphics.Blit(myTexture2D, m_terrainField[READ]);*/
	}
	
	void MakeGrids()
	{
		int numGrids = TOTAL_GRID_SIZE / GRID_SIZE;
		
		m_gridLand = new GameObject[numGrids*numGrids];
		m_gridWater = new GameObject[numGrids*numGrids];
		m_gridWireframe = new GameObject[numGrids*numGrids];
		m_gridLava = new GameObject[numGrids*numGrids];
		
		/*Vector3 worldSize = new Vector3( TOTAL_GRID_SIZE, 128, TOTAL_GRID_SIZE );

		terrainData = new TerrainData();
 		
 		terrainData.SetDetailResolution(512,8);

 		terrainData.heightmapResolution = 1024;

        terrainData.size = worldSize;
 
        ter = Terrain.CreateTerrainGameObject( terrainData );

        ter.GetComponent<Terrain>().materialType = Terrain.MaterialType.Custom; 

        ter.GetComponent<Terrain>().materialTemplate = m_terrainLandMat;*/

		for(int x = 0; x < numGrids; x++)
		{
			for(int y = 0; y < numGrids; y++)
			{
				int idx = x+y*numGrids;
				
				int posX = x * (GRID_SIZE-1);
				int posY = y * (GRID_SIZE-1);
				
				Mesh mesh = MakeMesh(GRID_SIZE, TOTAL_GRID_SIZE, posX, posY);
				
				mesh.bounds = new Bounds(new Vector3(GRID_SIZE/2, 0, GRID_SIZE/2), new Vector3(GRID_SIZE, TERRAIN_HEIGHT*2, GRID_SIZE));
				
				m_gridLand[idx] = new GameObject("Grid Land " + idx.ToString());
				m_gridLand[idx].AddComponent<MeshFilter>();
				m_gridLand[idx].AddComponent<MeshRenderer>();
				m_gridLand[idx].GetComponent<Renderer>().material = m_landMat;
				m_gridLand[idx].GetComponent<MeshFilter>().mesh = mesh;
				m_gridLand[idx].transform.localPosition = new Vector3(-TOTAL_GRID_SIZE/2 + posX, 0, -TOTAL_GRID_SIZE/2 + posY);

				m_gridWater[idx] = new GameObject("Grid Water " + idx.ToString());
				m_gridWater[idx].AddComponent<MeshFilter>();
				m_gridWater[idx].AddComponent<MeshRenderer>();
				m_gridWater[idx].GetComponent<Renderer>().material = m_waterMat;
				m_gridWater[idx].GetComponent<MeshFilter>().mesh = mesh;
				m_gridWater[idx].transform.localPosition = new Vector3(-TOTAL_GRID_SIZE/2 + posX, 0, -TOTAL_GRID_SIZE/2 + posY);

				m_gridLava[idx] = new GameObject("Grid Lava " + idx.ToString());
				m_gridLava[idx].AddComponent<MeshFilter>();
				m_gridLava[idx].AddComponent<MeshRenderer>();
				m_gridLava[idx].GetComponent<Renderer>().material = m_lavaMat;
				m_gridLava[idx].GetComponent<MeshFilter>().mesh = mesh;
				m_gridLava[idx].transform.localPosition = new Vector3(-TOTAL_GRID_SIZE/2 + posX, 0, -TOTAL_GRID_SIZE/2 + posY);

				m_gridWireframe[idx] = new GameObject("Grid Wireframe " + idx.ToString());
				m_gridWireframe[idx].AddComponent<MeshFilter>();
				m_gridWireframe[idx].AddComponent<MeshRenderer>();
				m_gridWireframe[idx].GetComponent<Renderer>().material = m_wireframeMat;
				m_gridWireframe[idx].GetComponent<MeshFilter>().mesh = mesh;
				m_gridWireframe[idx].transform.localPosition = new Vector3(-TOTAL_GRID_SIZE/2 + posX, 0, -TOTAL_GRID_SIZE/2 + posY);
				m_gridWireframe[idx].layer = 8;
			}
		}
	}
	
	Mesh MakeMesh(int size, int totalSize, int posX, int posY) 
	{
		
		Vector3[] vertices = new Vector3[size*size];
		Vector2[] texcoords = new Vector2[size*size];
		Vector3[] normals = new Vector3[size*size];
		int[] indices = new int[size*size*6];
		
		for(int x = 0; x < size; x++)
		{
			for(int y = 0; y < size; y++)
			{
				Vector2 uv = new Vector3( (posX + x) / (totalSize-1.0f), (posY + y) / (totalSize-1.0f));
				Vector3 pos = new Vector3(x, 0.0f, y);
				Vector3 norm = new Vector3(0.0f, 1.0f, 0.0f);
				
				texcoords[x+y*size] = uv;
				vertices[x+y*size] = pos;
				normals[x+y*size] = norm;
			}
		}
		
		int num = 0;
		for(int x = 0; x < size-1; x++)
		{
			for(int y = 0; y < size-1; y++)
			{
				indices[num++] = x + y * size;
				indices[num++] = x + (y+1) * size;
				indices[num++] = (x+1) + y * size;
		
				indices[num++] = x + (y+1) * size;
				indices[num++] = (x+1) + (y+1) * size;
				indices[num++] = (x+1) + y * size;
			}
		}
		
		Mesh mesh = new Mesh();
	
		mesh.vertices = vertices;
		mesh.uv = texcoords;
		mesh.triangles = indices;
		mesh.normals = normals;
		
		return mesh;
	}

	public void grabElement(float x, float y, int layer){
		RTUtility.SetToPoint(m_terrainField);
		Vector2 m_terrainOutputPoint = new Vector2(x,y);
		m_terrainOutputMat.SetVector("_Point", m_terrainOutputPoint);
		m_terrainOutputMat.SetFloat("_Radius", m_terrainOutputRadius);
		m_terrainOutputMat.SetFloat("_Amount", m_terrainOutputAmount);
		m_terrainOutputMat.SetFloat("_Layer", layer);
		Graphics.Blit(m_terrainField[READ], m_terrainField[WRITE], m_terrainOutputMat);
		RTUtility.SetToBilinear(m_terrainField);
		RTUtility.Swap(m_terrainField);
	}

	public void addWater(float x, float y){
		RTUtility.SetToPoint(m_waterField);
		Vector2 m_waterOutputPoint = new Vector2(x,y);
		m_waterInputMat.SetVector("_Point", m_waterOutputPoint);
		m_waterInputMat.SetFloat("_Radius", m_terrainOutputRadius);
		m_waterInputMat.SetFloat("_Amount", m_terrainOutputAmount);
		Graphics.Blit(m_waterField[READ], m_waterField[WRITE], m_waterInputMat);
		RTUtility.SetToBilinear(m_waterField);
		RTUtility.Swap(m_waterField);
	}

	public void addLava(float x, float y){
		RTUtility.SetToPoint(m_lavaField);
		Vector2 m_lavaOutputPoint = new Vector2(x,y);
		m_lavaInputMat.SetVector("_Point", m_lavaOutputPoint);
		m_lavaInputMat.SetFloat("_Radius", m_terrainOutputRadius);
		m_lavaInputMat.SetFloat("_Amount", m_terrainOutputAmount);
		Graphics.Blit(m_lavaField[READ], m_lavaField[WRITE], m_lavaInputMat);
		RTUtility.SetToBilinear(m_lavaField);
		RTUtility.Swap(m_lavaField);
	}

	public void addElement(float x, float y,int layer){
		RTUtility.SetToPoint(m_terrainField);
		float amnt = -m_terrainOutputAmount;
		Vector2 m_terrainOutputPoint = new Vector2(x,y);
		m_terrainOutputMat.SetVector("_Point", m_terrainOutputPoint);
		m_terrainOutputMat.SetFloat("_Radius", m_terrainOutputRadius);
		m_terrainOutputMat.SetFloat("_Amount", amnt);
		m_terrainOutputMat.SetFloat("_Layer", layer);
		Graphics.Blit(m_terrainField[READ], m_terrainField[WRITE], m_terrainOutputMat);
		RTUtility.SetToBilinear(m_terrainField);
		RTUtility.Swap(m_terrainField);
	}

	public void grabWater(float x, float y){
		RTUtility.SetToPoint(m_waterField);
		float amnt = -m_terrainOutputAmount;
		Vector2 m_waterOutputPoint = new Vector2(x,y);
		m_waterInputMat.SetVector("_Point", m_waterOutputPoint);
		m_waterInputMat.SetFloat("_Radius", m_terrainOutputRadius);
		m_waterInputMat.SetFloat("_Amount", amnt);
		Graphics.Blit(m_waterField[READ], m_waterField[WRITE], m_waterInputMat);
		RTUtility.SetToBilinear(m_waterField);
		RTUtility.Swap(m_waterField);
	}

	public void grabLava(float x, float y){
		RTUtility.SetToPoint(m_lavaField);
		float amnt = -m_terrainOutputAmount;
		Vector2 m_lavaOutputPoint = new Vector2(x,y);
		m_lavaInputMat.SetVector("_Point", m_lavaOutputPoint);
		m_lavaInputMat.SetFloat("_Radius", m_terrainOutputRadius);
		m_lavaInputMat.SetFloat("_Amount", amnt);
		Graphics.Blit(m_lavaField[READ], m_lavaField[WRITE], m_lavaInputMat);
		RTUtility.SetToBilinear(m_lavaField);
		RTUtility.Swap(m_lavaField);
	}

	public void updateTerrainHeight(){
		int terWidth = ter.GetComponent<Terrain>().terrainData.heightmapWidth;
		int terHeight = ter.GetComponent<Terrain>().terrainData.heightmapHeight;
		float[,] HeightMap = new float[TOTAL_GRID_SIZE, TOTAL_GRID_SIZE];
		RenderTexture tex = m_terrainField[READ];
        tex.filterMode = FilterMode.Bilinear;
        float[] texData = new float[tex.width*tex.height];
        for(int i = 0; i < tex.width*tex.height; ++i){
        	texData[i] = 0;
        }
        buffer = new ComputeBuffer(tex.width*tex.height, sizeof(float), ComputeBufferType.Default);
        tex.Create();
        shader.SetTexture(0, "tex", tex);
        shader.SetBuffer(0,"buffer", buffer);
		shader.Dispatch(0, 32, 32, 1);
		buffer.GetData(texData);
		int offset = 0;
		int index = 0;
		int size = TOTAL_GRID_SIZE * TOTAL_GRID_SIZE;
		for(int z=0; z<16384;z++){
			if(z%128 == 0)
					offset = (z/128);
			index = z-(128*offset)+(offset*1024);
			HeightMap[z%128,z/128] = texData[index]/256.0f;
		}
		buffer.Release();
		ter.GetComponent<Terrain>().terrainData.SetHeights(0,0,HeightMap);
	}

	/*public void saveTerrainData(){
		RenderTexture.active = m_terrainField[READ];
		Texture2D myTexture2D = new Texture2D(1024,1024,TextureFormat.RGBAFloat,false);
		myTexture2D.ReadPixels(new Rect(0, 0, m_terrainField[READ].width, m_terrainField[READ].height), 0, 0);
		myTexture2D.Apply();
		byte[] rawTexData = myTexture2D.GetRawTextureData();
		string path = "C:\\Users\\Jason\\Downloads\\Science 3D Terrain Proto\\Science 3D Terrain Proto\\Science 3d Terrain Proto\\Assets\\Textures\\terrain.raw";
		File.WriteAllBytes(path,rawTexData);
	}*/

	public void customRayCast(Ray ray,int p,String name){
		Vector3 dir = ray.direction;
		if(dir.z == 0 && dir.x == 0){
			//return these x z coord
		}
		float x = ray.origin.x+256;
		float z = ray.origin.z+256;
		/*if(Abs(dir.x)>Abs(dir.z)){
			xInc = dir.x/Abs(dir.x);
			zInc = dir.z/Abs(dir.x);
		}
		else{
			zInc = dir.z/Abs(dir.z);
			xInc = dir.x/Abs(dir.z);
		}*/
		//determine number of incr till we hit world boundry
		//then create array of that size and with the appropriate uv cord
		//pass uv array to compute shader to get height values
		//check for the nearest value thaat is greater than or equal to the y value of the ray at that dist
		int xSteps = 0;
		int zSteps = 0;
		int totalSteps = 0;
		if(dir.x>0){
			xSteps = (int)((509-x)/Math.Abs(dir.x));
		}
		if(dir.x<0){
			xSteps = (int)(x/Math.Abs(dir.x));
		}
		if(dir.z>0){
			zSteps = (int)((509-z)/Math.Abs(dir.z));
		}
		if(dir.z<0){
			zSteps = (int)(z/Math.Abs(dir.z));
		}
		if(dir.x==0){
			totalSteps = zSteps;
		}
		else if(dir.z==0){
			totalSteps = xSteps;
		}
		else{
			totalSteps = Math.Min(xSteps,zSteps);
		}
		buffer = new ComputeBuffer(2*totalSteps, sizeof(float), ComputeBufferType.Default);
		buffer2 = new ComputeBuffer(totalSteps, sizeof(float), ComputeBufferType.Default);
		buffer3 = new ComputeBuffer(totalSteps, sizeof(float), ComputeBufferType.Default);
		buffer4 = new ComputeBuffer(totalSteps, sizeof(float), ComputeBufferType.Default);
		buffer5 = new ComputeBuffer(totalSteps, sizeof(float), ComputeBufferType.Default);
		float[] points = new float[totalSteps*2];
		for(int n=0;n<totalSteps;n++){
			points[n*2] = (x + n*dir.x)/511;
			points[n*2+1] = (z + n*dir.z)/511;
		}
		buffer.SetData(points);
		//compute buffer stuff
		//buffer is input load coord multiply values by 2 before load
		//buffer1 is y values
		//make numofthreads = to total steps
		RenderTexture tex = m_terrainField[READ];
        tex.filterMode = FilterMode.Bilinear;
		tex.Create();
		RenderTexture tex2 = m_waterField[READ];
        tex2.filterMode = FilterMode.Bilinear;
		tex2.Create();
		RenderTexture tex3 = m_lavaField[READ];
        tex3.filterMode = FilterMode.Bilinear;
		tex3.Create();
        shader2.SetTexture(0, "tex", tex);
        shader2.SetTexture(0, "tex2", tex2);
        shader2.SetTexture(0, "tex3", tex3);
        shader2.SetBuffer(0,"buffer", buffer);
        shader2.SetBuffer(0,"buffer2", buffer2);
        shader2.SetBuffer(0,"buffer3", buffer3);
        shader2.SetBuffer(0,"buffer4", buffer4);
        shader2.SetBuffer(0,"buffer5", buffer5);
		shader2.Dispatch(0, 32, 1, 1);
		float[] output = new float[totalSteps];
		float[] output2 = new float[totalSteps];
		float[] output3 = new float[totalSteps];
		float[] output4 = new float[totalSteps];
		buffer2.GetData(output);
		buffer3.GetData(output2);
		buffer4.GetData(output3);
		buffer5.GetData(output4);
		float y;
		bool hit = false;
		int j = 0;
		while(j<totalSteps && !hit){
			y = ray.origin.y + j*dir.y;
			//Debug.Log(y);
			//Debug.Log(output[j]);
			if((output[j]/2)>=y)
				hit = true;
			j++;
		}
		j--;
		//Debug.Log((x+(j*dir.x))/511);
		//Debug.Log((z+(j*dir.z))/511);
		if(hit && p == 0){
			if(output2[j]>0){
				grabWater((x+(j*dir.x))/511,(z+(j*dir.z))/511);
				addsphere.Water((x+(j*dir.x)-256),(z+(j*dir.z)-256));//create sphere at this point
			}
			else if(output3[j]>0){
				grabLava((x+(j*dir.x))/511,(z+(j*dir.z))/511);
				addsphere.Lava((x+(j*dir.x)-256),(z+(j*dir.z)-256));
			}
			else if(output4[j]==2){
				grabElement((x+(j*dir.x))/511,(z+(j*dir.z))/511,2);
				addsphere.Stone((x+(j*dir.x)-256),(z+(j*dir.z)-256));
			}else if(output4[j]==1){
				grabElement((x+(j*dir.x))/511,(z+(j*dir.z))/511,1);
				addsphere.Mud((x+(j*dir.x)-256),(z+(j*dir.z)-256));
			}else if(output4[j]==0){
				grabElement((x+(j*dir.x))/511,(z+(j*dir.z))/511,0);
				addsphere.Earth((x+(j*dir.x)-256),(z+(j*dir.z)-256));
			}
		}
		if(hit && p == 1){
			if(String.Compare(name, "Water", true) == 0){
				addWater((x+(j*dir.x))/511,(z+(j*dir.z))/511);
			}
			else if(String.Compare(name, "Lava", true) == 0){
				addLava((x+(j*dir.x))/511,(z+(j*dir.z))/511);
			}
			else if(String.Compare(name, "Earth", true) == 0){
				addElement((x+(j*dir.x))/511,(z+(j*dir.z))/511,0);
			}else if(String.Compare(name, "Mud", true) == 0){
				addElement((x+(j*dir.x))/511,(z+(j*dir.z))/511,1);
			}else if(String.Compare(name, "Stone", true) == 0){
				addElement((x+(j*dir.x))/511,(z+(j*dir.z))/511,2);
			}
		}
		//if hit return x z coord else return -1,-1
		buffer.Release();
		buffer2.Release();
		buffer3.Release();
		buffer4.Release();
		buffer5.Release();
	}

	public float getHeight(float x, float z){
		//Debug.Log(x);
		//Debug.Log(z);
		buffer6 = new ComputeBuffer(2, sizeof(float), ComputeBufferType.Default);
		RenderTexture tex = m_terrainField[READ];
        tex.filterMode = FilterMode.Bilinear;
		tex.Create();
		float[] input = new float[2];
		input[0] = x;
		input[1] = z;
		buffer6.SetData(input);
		getPix.SetTexture(0,"tex", tex);
		getPix.SetBuffer(0,"buf",buffer6);
		getPix.Dispatch(0,1,1,1);
		float[] output = new float[2];
		buffer6.GetData(output);
		float y = output[0];
		buffer6.Release();
		//Debug.Log(y);
		return y;
	}

	public bool onLavaOrWater(float x, float z){
		buffer6 = new ComputeBuffer(2, sizeof(float), ComputeBufferType.Default);
		RenderTexture tex = m_waterField[READ];
        tex.filterMode = FilterMode.Bilinear;
		tex.Create();
		RenderTexture tex2 = m_lavaField[READ];
        tex2.filterMode = FilterMode.Bilinear;
		tex2.Create();
		float[] input = new float[2];
		input[0] = x;
		input[1] = z;
		buffer6.SetData(input);
		LavaOrWater.SetTexture(0,"tex", tex);
		LavaOrWater.SetTexture(0,"tex2", tex2);
		LavaOrWater.SetBuffer(0,"buf",buffer6);
		LavaOrWater.Dispatch(0,1,1,1);
		float[] output = new float[2];
		buffer6.GetData(output);
		float y = output[0];
		buffer6.Release();
		if(y > 0){
			return true;
		}
		return false;
	}

	public void Tsunami(){
		RTUtility.SetToPoint(m_waterField);
		float line = 0f;
		m_tsunamiMat.SetFloat("_Z", line);
		m_tsunamiMat.SetFloat("_Amount",  m_tsunamiAmount);
		Graphics.Blit(m_waterField[READ], m_waterField[WRITE], m_tsunamiMat);
		RTUtility.SetToBilinear(m_waterField);
		RTUtility.Swap(m_waterField);
	}
}
