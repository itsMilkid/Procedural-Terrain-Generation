#region Copyright
//MIT License
//Copyright (c) 2017 , Milkid - Kristin Stock 

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

#endregion

using System.Collections;
using UnityEngine;

[System.Serializable]
public class SplatMap{
	public string terrainType;
	public Texture2D terrainTexture;
	public Texture2D terrainNormalMap;
	public Vector2 terrainTilesize;
	public Vector2 terrainTileOffset;
	[Range(0.0f,1.0f)] public float terrainMetallic;
	[Range(0.0f,1.0f)] public float terrainSmoothness;
	public float terrainOverlap;
	public int height;
}
public class TerrainGenerator : MonoBehaviour {

	[Header("Terrain Generation:")]
	[Header("Perlin Noise")]
	[Range(0.001f,0.01f)] public float bumpiness;
	[Range(0.001f,1.00f)] public float dampening;

	[Header("Mountains:")]
	public int mountainDensity;
	[Range(0.001f,0.1f)] public float heightChange;
	[Range(0.001f,0.1f)] public float mountainSlope;

	[Header("Holes:")]
	public int holeDensity;
	[Range(0.0f,1.0f)] public float  holeDepth;
	[Range(0.001f,1f)] public float holeChange;
	[Range(0.0001f,0.1f)] public float holeSlope;

	[Header("Terrain Textures:")]
	[Range(0.01f,0.075f)]public float overlapRoughness;
	public SplatMap[] splatMaps;

	private TerrainData terrainData;
	private float[,] terrainHeightData;
	private SplatPrototype[] terrainSplats;

	public void Generate(){
		terrainData = Terrain.activeTerrain.terrainData;
		SetTerrainTextures();
		GenerateTerrain();
		PaintTerrain();
	}

	private void SetTerrainTextures(){
		terrainSplats = new SplatPrototype[splatMaps.Length];

		for(int i = 0; i < splatMaps.Length; i++){
			terrainSplats[i] = new SplatPrototype();
			terrainSplats[i].texture = splatMaps[i].terrainTexture;
			terrainSplats[i].normalMap = splatMaps[i].terrainNormalMap;
			terrainSplats[i].tileSize = splatMaps[i].terrainTilesize;
			terrainSplats[i].tileOffset = splatMaps[i].terrainTileOffset;
			terrainSplats[i].metallic = splatMaps[i].terrainMetallic;
			terrainSplats[i].smoothness = splatMaps[i].terrainSmoothness;
		}

		terrainData.splatPrototypes = terrainSplats;
	}

	private void GenerateTerrain(){
		terrainHeightData = new float[terrainData.alphamapWidth,terrainData.alphamapHeight];

		//Apply Perlin Noise to terrain to evaluate height;
		for(int y = 0; y <terrainData.alphamapHeight; y++){
			for(int x = 0; x <terrainData.alphamapWidth; x++){
				terrainHeightData[x,y] = Mathf.PerlinNoise(x * bumpiness, y * bumpiness) * dampening;
			}
		}

		//Adding Mountains
		for(int i = 0; i < mountainDensity; i++){
			int x = Random.Range(0,terrainData.alphamapWidth);
			int y = Random.Range(0,terrainData.alphamapHeight);
			float newHeight = terrainHeightData[x,y] + heightChange;
			 GenerateMountain(x,y,newHeight,mountainSlope);
		}

		//Adding Holes
		for(int i = 0; i < holeDensity; i++){
			int x = Random.Range(0,terrainData.alphamapWidth);
			int y = Random.Range(0,terrainData.alphamapHeight);
			float newHeight = terrainHeightData[x,y] - holeChange;
			GenerateHole(x,y,newHeight,holeSlope);
		}
	
		terrainData.SetHeights(0,0,terrainHeightData);
	}

	private void GenerateMountain(int x,int y,float height,float slope){
		if(x <= 0 || x >= terrainData.alphamapWidth){
			return;
		}

		if(y <= 0 || y >= terrainData.alphamapHeight){
			return;
		}

		if(height <= 0){
			return;
		}

		if(terrainHeightData[x,y] >= height){
			return;
		}

		terrainHeightData[x,y] = height;
		GenerateMountain(x-1,y,height - Random.Range(0.001f,slope), slope);
		GenerateMountain(x+1,y,height - Random.Range(0.001f,slope), slope);
		GenerateMountain(x,y-1,height - Random.Range(0.001f,slope), slope);
		GenerateMountain(x,y+1,height - Random.Range(0.001f,slope), slope);
	}

	private void GenerateHole(int x,int y,float height,float slope){
		if(x < 0 || x >= terrainData.alphamapWidth){
			return;
		}

		if(y < 0 || y >= terrainData.alphamapHeight){
			return;
		}

		if(height <= holeDepth){
			return;
		}

		if(terrainHeightData[x,y] <= height){
			return;
		}

		terrainHeightData[x,y] = height;
		GenerateHole(x-1,y,height - Random.Range(0.001f,slope), slope);
		GenerateHole(x+1,y,height - Random.Range(0.001f,slope), slope);
		GenerateHole(x,y-1,height - Random.Range(0.001f,slope), slope);
		GenerateHole(x,y+1,height - Random.Range(0.001f,slope), slope);
	}

	private void PaintTerrain(){
		float[,,] splatmapData = new float[terrainData.alphamapWidth,terrainData.alphamapHeight,terrainData.alphamapLayers];

		for(int y = 0; y <terrainData.alphamapHeight;y++){
			for(int x = 0; x <terrainData.alphamapWidth; x++){
				float terrainHeight = terrainData.GetHeight(y,x);
				float[] splat = new float[splatMaps.Length];

				for(int i = 0;i <splatMaps.Length; i++){
					float noise = Mapping(Mathf.PerlinNoise(x * overlapRoughness,y*overlapRoughness),0f,1f,0.5f,1f);
					float currentHeight = splatMaps[i].height * noise - splatMaps[i].terrainOverlap * noise;
					float nextHeight = 0;

					if(i != splatMaps.Length-1){
						nextHeight = splatMaps[i+1].height * noise + splatMaps[i+1].terrainOverlap * noise;
					}

					if(i == splatMaps.Length-1 && terrainHeight >= currentHeight){
						splat[i] = 1;
					}

					else if(terrainHeight >= currentHeight && terrainHeight <= nextHeight){
						splat[i] = 1;
					}
				}
				NormalizeAlpha(splat);

				for(int j = 0; j <splatMaps.Length; j++){
					splatmapData[x,y,j] = splat[j];
				}
			}
		}

		terrainData.SetAlphamaps(0,0,splatmapData);
	}

	private void NormalizeAlpha(float[] values){
		float total = 0;
		for(int i = 0; i <values.Length; i++){
			total += values[i];
		}

		for(int j = 0; j < values.Length; j++){
			values[j] /= total;
		}
	}

	public float Mapping(float value,float sMin, float sMax, float mMin, float mMax){
		return(value - sMin) * (mMax - mMin)/(sMax - sMin) +mMin;
	}
}
