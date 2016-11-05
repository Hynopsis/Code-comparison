using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading; 

#pragma warning disable 0168// variable declared but not used.
#pragma warning disable 0219// variable assigned but not used.
#pragma warning disable 0414// private field assigned but not used.

public class BlockStrength : MonoBehaviour {

UnityThreading.Task myThread; // not sure if I really need this..?
//pre threading these are used to wait a frame every float blocks checked for these function
public float checkAroundPathWait = 0;
public float strCheckWait = 10;
public RenderGroup ourRender;

public bool split = false;
public bool waitingToRender = false;
public bool gettingAllMissing = false;
public bool checkingNewStrength = false;
public bool doingStrength = false;//bool to see if already running strength 
public bool gotAroundInfo = false;
public bool checkingBelow = false;
public bool searchingAround = false;
public bool assigning = false;
public bool addingStrength = false;
public bool updatingStrength = false;
GameObject thisChunk;


public bool foundBelow = false;
public bool doingPass = false;
public bool doingPath = false;
public int aroundIteration;
public int possibleDirection = 0;
public int possibleCount;
public Vector3 blockPosition;
public Vector3 possibleLink = new Vector3 (-1,-1,-1);
public int possibleStrength = 0;
public Vector3 aroundPosition = Vector3.zero;
public Vector3 chunkOffset = Vector3.zero;

	// Use this for initialization

public bool initialized = false;
Vector3[] posAroundBlock;//  = new Vector3[6];//cardinal directions
int[] directions;//  = new int[6];
public GameObject[] chunkParent;//  = new GameObject[6];
public Vector3[] chunkPosition;//  = new Vector3[6];
public Vector3[] chunkArrayPos;
//public Vector3[] positionFind;
public int[] neighborStr;
public int[] neighborDirection;
//public List<byte>[48,48,48] blockStr;// = 
//public List<byte>[,,] blockStr;
public blockData[,,] blockStr;//bhange
public List<Vector3> chunkPassPos;
public List<GameObject> chunkPassGo;
public List<Vector3> chunkAroundPos;//.Add(chunkPosition(x));//this is list for blocks that need to check around themselves
public List<GameObject> chunkAroundGo;//.Add(chunkParent(X));
public List<Vector3> checkStrPos;//.Add(chunkPosition(x));///later this is for when we recheck the strength for these blocks
public List<GameObject> checkStrGo;//.Add(chunkParent(x));
public List<GameObject> otherUpdateChunks;
public List<GameObject> fullChunkList;

//for block pieces function
public List<GameObject> parentList = new List<GameObject>();
public List<Vector3> positionList = new List<Vector3>();
	public List<BlockInfo> thisBlockInfo;// = new List<BlockInfo>();
	BlockInfo tempInfo;
	public List<GameObject> finalParents = new List<GameObject>();
	public List<Vector3> finalPositions = new List<Vector3>();

	
//so there are the lists that hold all infomation when multiple blocks are removed or are updated through terrain changes	
public List<GameObject> updateFoundationGo;
public List<Vector3> updateFoundationPos;
	

GameObject possibleParent;
TerrainGenerator terrainScript;
public ChunkScript chunkScript;
BlockStrength blockScript;
ChunkScript ourChunkScript;


private Vector3 tempPos;
//all changes to switch over the arrays to non-list methods will be marker  bhange like above
//just search for it and change all instances



public void Initialize(){

	thisBlockInfo = new List<BlockInfo>();
		//tempInfo = new BlockInfo();

	initialized = true;
	chunkPassPos = new List<Vector3>();
	chunkPassGo = new List<GameObject>();
	chunkAroundPos = new List<Vector3>();
	chunkAroundGo = new List<GameObject>();
	checkStrPos = new List<Vector3>();
	checkStrGo = new List<GameObject>();
	otherUpdateChunks = new List<GameObject>();
	updateFoundationGo = new List<GameObject>();
	fullChunkList  = new List<GameObject>();
		
	posAroundBlock  = new Vector3[6];
	neighborStr  = new int[6];
	neighborDirection  = new int[6];
	directions  = new int[6];
	chunkParent  = new GameObject[6];
	chunkPosition  = new Vector3[6];
	chunkArrayPos  = new Vector3[6];
	blockStr = new blockData[16,16,16];
	chunkScript = gameObject.GetComponent<ChunkScript>() as ChunkScript;
	ourChunkScript = gameObject.GetComponent<ChunkScript>() as ChunkScript;
	terrainScript = GameObject.FindWithTag("Generator").GetComponent<TerrainGenerator>() as TerrainGenerator;
		
	for(int x = 0; x < 16; x++){
	for(int y = 0; y < 16; y++){
	for(int z = 0; z < 16; z++){
		blockStr[x,y,z].strength = 0;//bhange
		blockStr[x,y,z].direction = 7;
		blockStr[x,y,z].length = 0;
		}}}				
	
	//create posAroundblock cardinal positions...up,down, etc
	//this array is the basic directions we check around a block...6 cardinal directions
	posAroundBlock[0] = Vector3.up;
	posAroundBlock[1] = -Vector3.up;
	posAroundBlock[2] = Vector3.right;
	posAroundBlock[3] = -Vector3.right;
	posAroundBlock[4] = Vector3.forward;
	posAroundBlock[5] = -Vector3.forward;
	
	//create the directions that are tied to teh posAroundBlock array
	//this direction is what byte will be saved to determine positions
	//so when our blockStr[x,y,z] list is [5,1,2,4,5] translates to 5 strength
	//then down, right, forward, back....hypothetical but so makes sense
	
	directions[0] = 0;
	directions[1] = 1;
	directions[2] = 2;
	directions[3] = 3;
	directions[4] = 4;
	directions[5] = 5;
		
	}
	
public void MinimizeArray(){
	//make it a small array of nothing to minimize local memory
	blockStr  = new blockData[1,1,1];
	posAroundBlock  = new Vector3[1];
	directions  = new int[1];
	chunkParent  = new GameObject[1];
	 chunkPosition  = new Vector3[1];
	}
	
public void GetAroundInfo(Vector3 thisPosition){
	//there are two methods, one where it assumes thisPosition is ours and another that gets passed the gameobject
	//this also assumes that the chunkScript reference is already set to ourselves...

	aroundPosition = Vector3.zero;
	chunkOffset = Vector3.zero;
	//Vector3 tempPos;
		
	for(int x = 0; x < 6; x++){//go through 6 positions around us and see who they belong to
		
		aroundPosition = thisPosition + posAroundBlock[x];
		chunkOffset = PositionOfSpot(ref aroundPosition);//this give a Vector3 for the direction to the chunk who owns this position

		
			
		if(chunkOffset == Vector3.zero){//then this is within our chunk, we were given no directional offset
			chunkParent[x] = gameObject;
			chunkPosition[x] = aroundPosition;
			//chunkArrayPos[x] = chunkScript.GeneratorArrayPosition;
			//positionFind[x] = chunkParent[x].transform.position;
			//neighborStr[x] = chunkParent[x].GetComponent<BlockStrength>().blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z].strength;
			//neighborDirection[x] = chunkParent[x].GetComponent<BlockStrength>().blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z].direction;
			}
		else{

			//PROBLEM - what if on get around info we are checking between two block on different chunks...?
		//	chunkArrayPos[x] = chunkScript.GeneratorArrayPosition  + chunkOffset;

			if(chunkScript.gameObject != gameObject){
				chunkScript = gameObject.GetComponent<ChunkScript>();
				Debug.Log("This is probably a problem, since chunkScript is not ours");
				//Debug.Break();
				}
			tempPos = chunkScript.GeneratorArrayPosition + chunkOffset;//temp position, makes less code
			//Debug.Log("Position we are checking, then around " + thisPosition + " " + aroundPosition + " " + chunkScript.GeneratorArrayPosition + " " + chunkOffset);
			//Debug.Log("This is what we would get if changed to ourChunkScript... " + ourChunkScript.GeneratorArrayPosition  + chunkOffset);
			//Debug.Log(" Outside of us at this array location " + tempPos + " calling from " + gameObject.transform.position);
			chunkParent[x] = terrainScript.renderChunks[(int)tempPos.x, (int)tempPos.y,  (int)tempPos.z];//get chunk from the terrain script
			/*
			if(chunkParent[x].GetComponent<ChunkScript>().GeneratorArrayPosition != new Vector3((int)tempPos.x, (int)tempPos.y,  (int)tempPos.z)){
				Debug.Log(chunkParent[x].GetComponent<ChunkScript>().GeneratorArrayPosition);
				Debug.Log( new Vector3((int)tempPos.x, (int)tempPos.y,  (int)tempPos.z));
				Debug.Log("Shit is completely fucked");
				}
				*/
		
			//Debug.Log("Computed block position " + aroundPosition + " + " + transform.position + " - " + chunkParent[x].transform.position + " " + ((aroundPosition + transform.position) - chunkParent[x].transform.position));
			chunkPosition[x] = (((aroundPosition * 3) + transform.position) - chunkParent[x].transform.position)/3;//get position relative to this othe //chunkScript.gameObject.transform.position);//
			/*
				//so since increase scale of blocks (*3) then we cant directly use the world position of this point
				if(chunkPosition[x].x > 15 ){
					//Debug.Log("Before pos " + chunkPosition[x]);
					chunkPosition[x].x = Mathf.FloorToInt(chunkPosition[x].x/3);
					//Debug.Log("Combined local x" + chunkPosition[x].x);
				}
				if(chunkPosition[x].y > 15){
					//Debug.Log("Before pos " + chunkPosition[x]);
					chunkPosition[x].y = Mathf.FloorToInt(chunkPosition[x].y/3);
					//Debug.Log("Combined local y" + chunkPosition[x].y);
					
				}
				if(chunkPosition[x].z > 15){
					Debug.Log("Before pos " + chunkPosition[x]);
					chunkPosition[x].z = Mathf.FloorToInt(chunkPosition[x].z/3);
					Debug.Log("Combined local z" + chunkPosition[x].z);
					
				}

				if(chunkPosition[x].x < 0 ){
					//Debug.Log("Before pos " + chunkPosition[x]);
					chunkPosition[x].x = 0;
					//Debug.Log("Combined local x" + chunkPosition[x].x);
				}
				if(chunkPosition[x].y < 0){
					//Debug.Log("Before pos " + chunkPosition[x]);
					chunkPosition[x].y = 0;
					//Debug.Log("Combined local y" + chunkPosition[x].y);
					
				}
				if(chunkPosition[x].z < 0){
					//Debug.Log("Before pos " + chunkPosition[x]);
					chunkPosition[x].z = 0;
					//Debug.Log("Combined local z" + chunkPosition[x].z);
					
				}
                */
				//chunkArrayPos[x] = tempPos;
				//positionFind[x] = chunkParent[x].transform.position;
			//neighborStr[x] = chunkParent[x].GetComponent<BlockStrength>().blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z].strength;
			//neighborDirection[x] = chunkParent[x].GetComponent<BlockStrength>().blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z].direction;
			
			blockScript = chunkParent[x].GetComponent<BlockStrength>();

		
			//neighborStr[x] = blockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z][0];
					//neighborType[x] = (int)chunkParent[x].GetComponent<ChunkScript>().vegeByte[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z];
			//****Should change this to only initialize neighbors that need it, there is a tons of data that gers initialized

			if(!blockScript.initialized){//if these neightbors are not initialized then we need to make sure they are
				//keep in mind this will only initialize them if one of the directions falls outside of the chunk...
				blockScript.Initialize();
				}
			//neighborStr[x] = blockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z].strength;
			//neighborDirection[x] = blockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z].direction;
			//}
			}
			//Debug.Log(" Final position " + x + " " + chunkParent[x].transform.position + " " + chunkPosition[x] +  " " + chunkOffset);
	}	
		
		
		
}

public void GetAroundInfo(Vector3 thisPosition, GameObject thisGo){

		//now this is a method specific to blockpiece, when this method is called the position is not within our chunk
		//so we need to account for that
		//Debug.Log("Gettng info around this spot on overloaded method " + thisPosition + " " + thisGo.transform.position);
		aroundPosition = Vector3.zero;
		chunkOffset = Vector3.zero;
		Vector3 tempPos;
		
		for(int x = 0; x < 6; x++){//go through 6 positions around us and see who they belong to
			
			aroundPosition = thisPosition + posAroundBlock[x];
			chunkOffset = PositionOfSpot(ref aroundPosition);//this give a Vector3 for the direction to the chunk this belongs to
			//now this method will work in either GetAroundinfo version, since it just checks if position is out of our possible range
						
			
			if(chunkOffset == Vector3.zero){//then this is within thisGo
				chunkScript = thisGo.GetComponent<ChunkScript>() as ChunkScript;
				chunkParent[x] = thisGo;
				chunkPosition[x] = aroundPosition;
				//chunkArrayPos[x] = chunkScript.GeneratorArrayPosition;
				//positionFind[x] = chunkParent[x].transform.position;
				//neighborStr[x] = chunkParent[x].GetComponent<BlockStrength>().blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z].strength;
				//neighborDirection[x] = chunkParent[x].GetComponent<BlockStrength>().blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z].direction;
				//chunkScript should be current to the block we are checking, small chance this could be a problem
			//	chunkArrayPos[x] = chunkScript.GeneratorArrayPosition;
			//	positionFind[x] = chunkParent[x].transform.position;
			//	neighborStr[x] = chunkParent[x].GetComponent<BlockStrength>().blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z][0];
			//	neighborType[x] = (int)chunkScript.vegeByte[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z];
			}
			else{
				
				chunkScript = thisGo.GetComponent<ChunkScript>() as ChunkScript;
				//so for determining our chunk array position we our position, this is set to thisGo which is the calling block...
				//so basically if not from calling chunk, then we need to get the array position of this around block

			//	chunkArrayPos[x] = chunkScript.GeneratorArrayPosition  + chunkOffset;//all relative to who owns the chunk
				tempPos = chunkScript.GeneratorArrayPosition + chunkOffset;//temp position, makes less code
				//at this position the chunkScript is not right...it is saying we are the origin chunk, then we to the right then checking back to it

			//	Debug.Log("Position we are checking, then around " + thisPosition + " " + aroundPosition + " " + chunkScript.GeneratorArrayPosition + " " + chunkOffset);
				//Debug.Log("This is what we would get if changed to ourChunkScript... " + ourChunkScript.GeneratorArrayPosition  + chunkOffset);
				
			//	Debug.Log(" Outside of us " + tempPos + " calling from " + gameObject.transform.position);
				chunkParent[x] = terrainScript.renderChunks[(int)tempPos.x, (int)tempPos.y,  (int)tempPos.z];//get chunk from the terrain script
				/*
				if(chunkParent[x].GetComponent<ChunkScript>().GeneratorArrayPosition != new Vector3((int)tempPos.x, (int)tempPos.y,  (int)tempPos.z)){
					Debug.Log(chunkParent[x].GetComponent<ChunkScript>().GeneratorArrayPosition);
					Debug.Log( new Vector3((int)tempPos.x, (int)tempPos.y,  (int)tempPos.z));
					Debug.Log("Shit is completely fucked");
				}
				*/

			//	Debug.Log("Computed block position " + aroundPosition + " + " + thisGo.transform.position + " - " + chunkParent[x].transform.position + " " + ((aroundPosition + thisGo.transform.position) - chunkParent[x].transform.position));

				//so this gets changed since increase scale on the blocks
				chunkPosition[x] = (((aroundPosition * 3) + thisGo.transform.position) - chunkParent[x].transform.position)/3;//get position relative to this othe //chunkScript.gameObject.transform.position);//
                /*
				//so since increase scale of blocks (*3) then we cant directly use the world position of this point
				if(chunkPosition[x].x > 15){
					Debug.Log("Before pos " + chunkPosition[x]);
					chunkPosition[x].x = Mathf.FloorToInt(chunkPosition[x].x/3);
					Debug.Log("Combined local x" + chunkPosition[x].x);
				}
				if(chunkPosition[x].y > 15){
					Debug.Log("Before pos " + chunkPosition[x]);
					chunkPosition[x].y = Mathf.FloorToInt(chunkPosition[x].y/3);
					Debug.Log("Combined local y" + chunkPosition[x].y);
					
				}
				if(chunkPosition[x].z > 15){
					Debug.Log("Before pos " + chunkPosition[x]);
					chunkPosition[x].z = Mathf.FloorToInt(chunkPosition[x].z/3);
					Debug.Log("Combined local z" + chunkPosition[x].z);
					
				}

				if(chunkPosition[x].x < 0 ){
					Debug.Log("Before pos " + chunkPosition[x]);
					chunkPosition[x].x = 0;
					Debug.Log("Combined local x" + chunkPosition[x].x);
				}
				if(chunkPosition[x].y < 0){
					Debug.Log("Before pos " + chunkPosition[x]);
					chunkPosition[x].y = 0;
					Debug.Log("Combined local y" + chunkPosition[x].y);
					
				}
				if(chunkPosition[x].z < 0){
					Debug.Log("Before pos " + chunkPosition[x]);
					chunkPosition[x].z = 0;
					Debug.Log("Combined local z" + chunkPosition[x].z);
					
				}
                */
				//	positionFind[x] = chunkParent[x].transform.position; ///this is already wrong
				
				blockScript = chunkParent[x].GetComponent<BlockStrength>();

				//chunkPosition[x] = aroundPosition;
				//chunkArrayPos[x] = tempPos;
				//positionFind[x] = chunkParent[x].transform.position;
				//neighborStr[x] = blockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z].strength;
				//neighborDirection[x] = blockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z].direction;
				//neighborStr[x] = blockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z][0];
				//neighborType[x] = (int)chunkParent[x].GetComponent<ChunkScript>().vegeByte[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z];
				
				if(!blockScript.initialized){//if these neightbors are not initialized then we need to make sure they are
					//keep in mind this will only initialize them if one of the directions falls outside of the chunk...
					blockScript.Initialize();
				}
				//}
			}
			//Debug.Log(" Final position " + x + " " + chunkParent[x].transform.position + " " + chunkPosition[x] +  " " + chunkOffset);
		}	
		
		
		
	}


public byte BlockStr(Vector3 thisPosition, bool onTerrain){//take the voxel position we are checking the strength for...
	//recently change to return whether it found something or not - so we can stop checking strength
	//this method checks around our block to find something with a path and and a strength that we can connect to
	//strange but it returns a byte to return info
	//Debug.Log("Doing block strength at " + thisPosition + " on terrain " + onTerrain);	
	doingStrength = true;
	searchingAround = false;
	assigning = false;
	gotAroundInfo = false;
	blockPosition = thisPosition;
	
	//Debug.Log("Starting blockStr " + thisPosition + " " + onTerrain);
	//Debug.DrawLine(transform.position + thisPosition, (transform.position + thisPosition + (Vector3.up + Vector3.right + Vector3.forward) * 1.2f), Color.yellow, 1.25f);
		
	if(terrainScript == null){
		terrainScript = GameObject.FindWithTag("Generator").GetComponent<TerrainGenerator>() as TerrainGenerator;
		}
		
	
	//terrainScript.DebugCube(thisPosition,transform.position, 1, Color.magenta, .08f);
	//later need to switch this all arrays are initialized, but in reality we should minimize these arrays while not in use
		
	if(!initialized){
		Initialize();///create arrays and such
		}
		
	possibleDirection = 0;
	possibleLink = new Vector3 (-1,-1,-1);
	possibleStrength = 0;
	aroundPosition = Vector3.zero;
	chunkOffset = Vector3.zero;
	int x1 = (int)thisPosition.x;
	int y1 = (int)thisPosition.y;
	int z1 = (int)thisPosition.z;
		
	foundBelow = false;	
	//just a is doing bool	
	
	//clear this position of all strength and path
	blockStr[x1, y1, z1].strength = 0;
	blockStr[x1, y1, z1].direction = 7;
	blockStr[x1, y1, z1].length = 0;
		
	GetAroundInfo(thisPosition);//this fills go and positions arrays with information about blocks around 
	gotAroundInfo = true;
			
	if(onTerrain){
		//			
		//Debug.Log("On terrain is true");
		//what a mess - this is all just to check if there is a foundation blocks below us or not				
		if(chunkParent[1] == gameObject){	//so if position below is ours wont cause an out of array exception
			
			//Debug.Log(chunkParent[0].transform.position + " " + chunkPosition[0] + " " + posAroundBlock[0] + " " + aroundPosition[0]);

				if(	blockStr[(int) thisPosition.x, (int) thisPosition.y-1, (int) thisPosition.z].strength == 8 && blockStr[(int) thisPosition.x, (int) thisPosition.y-1, (int) thisPosition.z].direction == 7){
					//foundation above foundation so check strength like normal
					//Debug.Log("Checking like normal");
				}
				
				else{
					blockStr[(int) thisPosition.x, (int) thisPosition.y, (int) thisPosition.z].strength = 8;
					blockStr[(int) thisPosition.x, (int) thisPosition.y, (int) thisPosition.z].direction = 7;//direct foundation blocks have no path - so give invalid direction
					blockStr[(int) thisPosition.x, (int) thisPosition.y, (int) thisPosition.z].length = 0;//direct foundation blocks no path length
					//Debug.Log("Hitting terrain has full strength " + thisPosition);

					doingStrength = false;
					return 1;	
				}


			}

		else{// if it is not ours then to plan on that so prevent out of range exceptions
				blockScript = chunkParent[1].GetComponent<BlockStrength>() as BlockStrength;

				if(blockScript.blockStr[(int) chunkPosition[1].x, (int) chunkPosition[1].y, (int) chunkPosition[1].z].strength == 8 && blockScript.blockStr[(int) chunkPosition[1].x, (int) chunkPosition[1].y, (int) chunkPosition[1].z].direction == 7){
					
					//foundation above foundation so check strength like normal
				}
				
				else{
					blockStr[(int) thisPosition.x, (int) thisPosition.y, (int) thisPosition.z].strength = 8;
					blockStr[(int) thisPosition.x, (int) thisPosition.y, (int) thisPosition.z].direction = 7;//direct foundation blocks have no path
					blockStr[(int) thisPosition.x, (int) thisPosition.y, (int) thisPosition.z].length = 0;//direct foundation blocks have no path length
					//Debug.Log("Hitting terrain has full strength " + thisPosition);

					doingStrength = false;
					return 1;	
				}

			}
		
			Debug.Log("Should have full strength");	
		}

	//first thing to check is directly below our block (thisPosition), if this block has max strength then it is directly connected to foundation
	//so we would attach to that since it can be any stronger
	possibleLink = -Vector3.one;//just setting to impossible default value since cant null
		
	//this is checking for foundation blocks to connect to under us...
	//but in reality the foundation block could be in any direction eventually, but will most likely be down or left and right and forward,back
	checkingBelow = true;	
		
	if(chunkParent[1] == gameObject){//one is down...chunkParent and chunkPosition are parrellel arrays
		//so if the aroundBlock below us is within our own gamobject then check if we can connect below

		if(blockStr[x1, y1 - 1, z1].strength == 8){//eventually this will be max for type
				
				blockStr[x1, y1, z1].strength = 8;//or max for type	

				//so since we are not saving full path anymore we only want this length
				blockStr[x1,y1,z1].length = ((byte)(blockStr[x1, y1 - 1, z1].length + 1));//copies their path and adds one
				blockStr[x1,y1,z1].direction = 1;//so the direction we took was down which is 1
				//should be fine, if connected to foundation block then (0+1) = 1, and will continue up the chain
				foundBelow = true;
				possibleLink = thisPosition + -Vector3.up;
				possibleStrength =  8;
				possibleParent = gameObject;
				possibleDirection = 1;
				possibleCount = blockStr[x1, y1 - 1, z1].length;//need to account for sideways foundation pathing connections will not grab the closest
				
		}
		//Debug.Log("Is this where we are erroring ");
		//Debug.Log(chunkPosition[1]);
		//Debug.Log(blockScript);

		//JUST REMOVE THIS, IT MAKE NO SENSE DOES THE SAME THING AS ABOVE, JUST ALL CONVOLUDED
			/*
		if(blockScript.blockStr[(int)chunkPosition[1].x, (int)chunkPosition[1].y,(int)chunkPosition[1].z].strength == 8){//eventually this will be max for type
				blockStr[x1, y1, z1].strength = 8;//or max for type	
				blockStr[x1,y1,z1].length = ((byte)(blockScript.blockStr[(int)chunkPosition[1].x, (int)chunkPosition[1].y,(int)chunkPosition[1].z].length + 1));//copies their path and adds one
				blockStr[x1,y1,z1].direction = 1;//so the direction we took was down which is 1
				foundBelow = true;
				possibleLink =  thisPosition + -Vector3.up;
				possibleStrength =  8;//blockStr[(int)combinedLocal.x, (int)combinedLocal.y, (int)combinedLocal.z][0];
				possibleParent = chunkParent[1];
				possibleDirection = 1;
				possibleCount = blockScript.blockStr[(int)chunkPosition[1].x, (int)chunkPosition[1].y,(int)chunkPosition[1].z].length;//.Count;
			}
			*/


		
		}
		
	checkingBelow = false;
		
	//Debug.Log("Getting through down check");	
	//if we didnt find anything below us then we have to check all around	
	
		
	if(!foundBelow){
		
		searchingAround = true;
			
		for(int x = 0; x < 6; x++){
			//look for something arond to connect to, if something is found keep checking around to find the best of all around us if more than one	
			
			aroundIteration = x;
			Vector3 combinedLocal = thisPosition + posAroundBlock[x];//so this is our around position
			//why are we using combinedLocal when chunkPosition[x] is the same thing?

			if(possibleLink == -Vector3.one){//we have nothing to connect to
				 	
				if(chunkParent[x] == gameObject){//if this position around us is part of our chunk	
					//see if this around spot has a block or any strength
					//neighborStr[x] = blockStr[(int)combinedLocal.x, (int)combinedLocal.y, (int)combinedLocal.z][0];

					if(blockStr[(int)combinedLocal.x, (int)combinedLocal.y, (int)combinedLocal.z].strength > 1){//then this block could pass strength
						possibleLink = chunkPosition[x];
						possibleStrength =  blockStr[(int)combinedLocal.x, (int)combinedLocal.y, (int)combinedLocal.z].strength;
						possibleParent = chunkParent[x];
						possibleDirection = directions[x];
						possibleCount = blockStr[(int)combinedLocal.x, (int)combinedLocal.y, (int)combinedLocal.z].length;
							
						}

					}
				else{
                    //so this is erroring for some reason...what is combined local even doing, we already have the data
                    //of this around point, it is stored in the around arrays
					blockScript = chunkParent[x].GetComponent<BlockStrength>();
					//Debug.Log("Before combine " + combinedLocal);
					combinedLocal = ((combinedLocal * 3) + transform.position - chunkParent[x].transform.position)/3;
					//Debug.Log(combinedLocal + " " + chunkParent[x].transform.position + " " + chunkPosition[x]);
					//neighborStr[x] = blockScript.blockStr[(int)combinedLocal.x, (int)combinedLocal.y, (int)combinedLocal.z][0];
                        /*
						if(combinedLocal.x > 15){
							
							combinedLocal.x = Mathf.FloorToInt(combinedLocal.x/3);
							//Debug.Log("Combined local " + combinedLocal.x);
						}
						if(combinedLocal.y > 15){
							combinedLocal.y = Mathf.FloorToInt(combinedLocal.y/3);
							
						}
						if(combinedLocal.z > 15){
							combinedLocal.z = Mathf.FloorToInt(combinedLocal.z/3);
							
						}

						if(combinedLocal.x < 0){
							
							combinedLocal.x = 0;
							//Debug.Log("Combined local " + combinedLocal.x);
						}
						if(combinedLocal.y < 0){
							combinedLocal.y = 0;
							
						}
						if(combinedLocal.z > 15){
							combinedLocal.z = 0;
							
						}
                    */
                     //****10/23 removed below possible count and removed above working with combined Local, not sure why I was ever using it
					if(blockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z].strength > 1){//then this block could pass strength
						possibleLink = chunkPosition[x];
						possibleStrength =  blockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z].strength;
						possibleParent = chunkParent[x];
						possibleDirection = directions[x];
                        possibleCount = blockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z].length;//[(int)combinedLocal.x, (int)combinedLocal.y, (int)combinedLocal.z].length;
							
					}
	
					}
				}
			else{//already found something just need to find if there is a stronger connection to the foundation	
			
				//so running into problem...blocks need to find their shortest path if the same strength, otherwise they will loop weird and will cause a path to include going
				//back through us - hard to explain, in short if two paths have the same strength, we need to choose the one with the shorter path, if possible...

				if(chunkParent[x] == gameObject){//if this position around us is part of our chunk	
					//see if this around spot has a block or any strength
					//neighborStr[x] = blockStr[(int)combinedLocal.x, (int)combinedLocal.y, (int)combinedLocal.z][0];
//bhange
					if(blockStr[(int)combinedLocal.x, (int)combinedLocal.y, (int)combinedLocal.z].strength > possibleStrength){//then this block could pass strength
						possibleLink = chunkPosition[x];
						possibleStrength =  blockStr[(int)combinedLocal.x, (int)combinedLocal.y, (int)combinedLocal.z].strength;
						possibleParent = chunkParent[x];
						possibleDirection = directions[x];
						possibleCount = blockStr[(int)combinedLocal.x, (int)combinedLocal.y, (int)combinedLocal.z].length;
							
						}
						//also need to check, if this path has the same strength as one we already found, then we need to see if it has a closer connection to the foundation
						//by comparing the length of their lists
					else if (blockStr[(int)combinedLocal.x, (int)combinedLocal.y, (int)combinedLocal.z].strength == possibleStrength && blockStr[(int)combinedLocal.x, (int)combinedLocal.y, (int)combinedLocal.z].length < possibleCount){
						possibleLink = chunkPosition[x];
						possibleStrength =  blockStr[(int)combinedLocal.x, (int)combinedLocal.y, (int)combinedLocal.z].strength;
						possibleParent = chunkParent[x];
						possibleDirection = directions[x];	
						possibleCount = blockStr[(int)combinedLocal.x, (int)combinedLocal.y, (int)combinedLocal.z].length;
													
						}

					}
				else{

					blockScript = chunkParent[x].GetComponent<BlockStrength>();
					
					combinedLocal = ((combinedLocal * 3) + transform.position - chunkParent[x].transform.position)/3;
						/*
						if(combinedLocal.x > 15){
							
							combinedLocal.x = Mathf.FloorToInt(combinedLocal.x/3);
							//Debug.Log("Combined local " + combinedLocal.x);
						}
						if(combinedLocal.y > 15){
							combinedLocal.y = Mathf.FloorToInt(combinedLocal.y/3);
							
						}
						if(combinedLocal.z > 15){
							combinedLocal.z = Mathf.FloorToInt(combinedLocal.z/3);
							
						}

						if(combinedLocal.x < 0){
							
							combinedLocal.x = 0;
							//Debug.Log("Combined local " + combinedLocal.x);
						}
						if(combinedLocal.y < 0){
							combinedLocal.y = 0;
							
						}
						if(combinedLocal.z > 15){
							combinedLocal.z = 0;
							
						}
					*/
					//shit switching to larger scale means this number is not right...on edge of chunk since using real position
					//it will find -47 which needs to be divided by 3 and rounded down...
					Debug.Log(thisPosition + " " + posAroundBlock[x] + " " + combinedLocal + " " + chunkParent[x] + " " + chunkPosition[x]);

					
					if(blockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z].strength > possibleStrength){//then this block could pass strength
						possibleLink = chunkPosition[x];
						possibleStrength =  blockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z].strength;
						possibleParent = chunkParent[x];
						possibleDirection = directions[x];
						possibleCount = blockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z].length;
							
						}
					else if (blockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z].strength == ((byte)possibleStrength) && blockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z].length < possibleCount){
						possibleLink = chunkPosition[x];
                        //****This was erroring as was using combined local, stiched to chunkPosition, if continues to error then maybe need combined
                        possibleStrength =  blockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z].strength;
						possibleParent = chunkParent[x];
						possibleDirection = directions[x];	
						possibleCount = blockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z].length;	
							
						}

					}
			}
				
		}
		aroundIteration = 10;
		searchingAround = false;
		
		
		if(terrainScript.strDebug){
		Debug.Log("Getting through around check");
		}
			
		if(possibleLink == -Vector3.one){//then nothing was ever found to connect to this should drop
			//Debug.Log("Nothing found to connect to " + thisPosition + " " + transform.position);

			//bhange
			blockStr[x1,y1,z1].strength = 0;
			blockStr[x1,y1,z1].direction = 7;
			blockStr[x1,y1,z1].length = 0;

			//blockStr[x1,y1,z1][0] = 0;
			doingStrength = false;
			//Debug.Log("Not finding any strength");
			return 0;
			//so if we have found nothing to link either around us, or below us...then we add no strength			
				
			}
		else{
			
			assigning = true;
			if(possibleParent == gameObject){//connecting to block within our chunk
					
				//so if our block is finding strength horizontally when we subtract strength 
				if(possibleDirection == 0 || possibleDirection == 1){//if up or down connection dont remove strength
					//bhange 
					blockStr[x1,y1,z1].strength = (byte)(possibleStrength);//when strength is passed it is reduced by one
					blockStr[x1,y1,z1].direction = (byte)(possibleDirection);//so this possible direction is our direction
					blockStr[x1,y1,z1].length = (byte)(possibleCount + 1);//so path equals this possible count + 1 for our additional directional path
					//Debug.Log("Connecting up/down " + possibleLink + " with strength " + possibleStrength + " in direction " + terrainScript.positionNames[possibleDirection]);
					//Debug.Log("This block has connecting direction of 
					
					}	
				else{
					//this is horizonatal connection so subtract one strength
					//bhange 
					blockStr[x1,y1,z1].strength = (byte)(possibleStrength - 1);//when strength is passed it is reduced by one
					blockStr[x1,y1,z1].direction = (byte)(possibleDirection);//so this possible direction is our direction
					blockStr[x1,y1,z1].length = (byte)(possibleCount + 1);//so path equals this possible count + 1 for our additional directional path
					//Debug.Log("Connecting horizontal " + possibleLink + " with strength " + possibleStrength + " in direction " + terrainScript.positionNames[possibleDirection]);
					}

	
					if(terrainScript.strDebug){
					//Debug.Log("Finishing adding self path ");
					}
				}
			else{//found on another block
				
				//so if our block is finding strength horizontally when we subtract strength 
				if(possibleDirection == 0 || possibleDirection == 1){//if up or down connection dont remove strength
					//bhange 
					blockStr[x1,y1,z1].strength = (byte)(possibleStrength);//when strength is passed it is reduced by one
					blockStr[x1,y1,z1].direction = (byte)(possibleDirection);//so this possible direction is our direction
					blockStr[x1,y1,z1].length = (byte)(possibleCount + 1);//so path equals this possible count + 1 for our additional directional path
					//Debug.Log("Connecting up/down " + possibleLink + " with strength " + possibleStrength + " in direction " + terrainScript.positionNames[possibleDirection]);
					
					}	
				else{
					//this is horizonatal connection so subtract one strength	
					//bhange 
					blockStr[x1,y1,z1].strength = (byte)(possibleStrength - 1);//when strength is passed it is reduced by one
					blockStr[x1,y1,z1].direction = (byte)(possibleDirection);//so this possible direction is our direction
					blockStr[x1,y1,z1].length = (byte)(possibleCount + 1);//so path equals this possible count + 1 for our additional directional path
					//	Debug.Log("Connecting horizontal " + possibleLink + " with strength " + possibleStrength + " in direction " + terrainScript.positionNames[possibleDirection] + " actual " + blockStr[x1,y1,z1].direction);
					}
					
				if(possibleParent == null){
					
					Debug.Log("parent is null " + possibleParent + " " + possibleStrength + " " + possibleLink + " " + possibleDirection);
					}

				
				}
			
			if(terrainScript.strDebug){
			Debug.Log("Getting through this check ");
			}
			assigning = false;
			}
	}
	
	
		
//		if(	blockStr[(int) thisPosition.x, (int) thisPosition.y, (int) thisPosition.z][0] == 8 && blockStr[(int) thisPosition.x, (int) thisPosition.y, (int) thisPosition.z].Count < 2){
//			Debug.Log("Got a problem here");
//			Debug.DrawLine(transform.position + thisPosition, (transform.position + thisPosition + (Vector3.up + Vector3.right + Vector3.forward) * 5.2f), Color.black, 1.25f);
//			
		//if(blockStr[x1,y1,z1]
		//Debug.Log("finishing with block strength " + chunkScript);	
		//chunkScript.GetVegeInfo(thisPosition);

		doingStrength = false;	
		//so we are going to return byte of 1 if we found some strength, 0 if nothing is found.

//		bhange
		if(blockStr[(int) thisPosition.x, (int) thisPosition.y, (int) thisPosition.z].strength == 0){
			
			return 0;
			}
		else{
			return 1;
			}

//		if(blockStr[(int) thisPosition.x, (int) thisPosition.y, (int) thisPosition.z][0] == 0){
//			
//			return 0;
//			}
//		else{
//			return 1;
//			}
		

	}

public IEnumerator AddStrength(Vector3 thisPosition, bool onTerrain){
	
	addingStrength = true;
		
	BlockStr(thisPosition, onTerrain);//first get our local block strength
//bhange - just replace
	if(blockStr[(int)thisPosition.x, (int)thisPosition.y, (int)thisPosition.z].strength == 0){
//	if(blockStr[(int)thisPosition.x, (int)thisPosition.y, (int)thisPosition.z][0] == 0){
		//then we dont pass our strength since we have none	
		}
	else{//our strength may effect others and we need to check around us
		otherUpdateChunks.Clear();
			
		//NEED SOMETHING TO CHECK IF BLOCK CREATES closer FOUNATION LINK

		CheckAroundPass(gameObject, thisPosition);
		//if something is found to pass to around us, it will get added to chunkPassPos list for checking
		//it will creep through the blocks and find those that need updating
			
		for(int x = 0; x < chunkPassPos.Count; x++){
				
			if(chunkPassGo[x] == gameObject){//then this block is within our chunk
				BlockStr(chunkPassPos[x], false);//recheck this blocks strength, since we know we will make it better	
				CheckAroundPass(gameObject, chunkPassPos[x]);//so check to see if this block needs to update others	
				}
			else{//belongs to another chunk make things more complicated but same appoach
				blockScript = chunkPassGo[x].GetComponent<BlockStrength>() as BlockStrength;
				blockScript.doingStrength = true;
				blockScript.BlockStr(chunkPassPos[x], false);
					
				while(blockScript.doingStrength){
					yield return new WaitForSeconds(.001f);
					}
				//this keeps track of other chunks that will need their mesh rerendered
				//this is only needed for viewing strength in real time
				if(!otherUpdateChunks.Contains(chunkPassGo[x])){//if not already flagged for an update
					otherUpdateChunks.Add(chunkPassGo[x]);
					}
			
				blockScript.doingPass = true;
				blockScript.CheckAroundPass(gameObject, chunkPassPos[x]);//remember we give it our gameobject so it can add to our lists
					
				while(blockScript.doingPass){
					yield return new WaitForSeconds(.001f);
					}
				}
	
			}
		
		
		chunkPassPos.Clear();//clear both of these list
		chunkPassGo.Clear();
		
		}
	addingStrength = false;
		
	//yield return null;	
	}
public void CheckAroundPass(GameObject caller, Vector3 thisPosition){
	doingPass = true;	
	BlockStrength thisStrength;
	
		
	//so if we are max strength - and another above us is not max then make it max
		
	GetAroundInfo(thisPosition);
		
	for(int x = 0; x < 6; x++){
		
		if(chunkParent[x] == gameObject){//this block is within our chunk
			//so first we make sure the block even has strength to update...if it does...
			//Debug.Log("This is our block we are checking");
			//Debug.Log(blockStr[(int)thisPosition.x, (int)thisPosition.y, (int)thisPosition.z][0] + " " + (blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][0] + 1));
			//if(blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][0] != 0){
			//bhange
			if(blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z].strength != 0){

				//if this block has strength, then if our strengh is higher then their value +1, they need to be updated...
				// for example if a block is placed as 7, the strength we could pass is 6 - so if 6 is larger than 5, we need this block to update it strength
				///     5
				/// 	7
			//bhange
			if(blockStr[(int)thisPosition.x, (int)thisPosition.y, (int)thisPosition.z].strength -  1 > blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z].strength){

			//if(blockStr[(int)thisPosition.x, (int)thisPosition.y, (int)thisPosition.z][0] -  1 > blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][0]){
				//if this is true, then our block will provide higher strength to this block, so this other block should be updated
				
				//Debug.Log("This is our block we are adding");
				//Debug.Log(blockStr[(int)thisPosition.x, (int)thisPosition.y, (int)thisPosition.z][0] + " " + (blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][0] + 1));
				
					
				if(caller == gameObject){//if we called this function this block gets added to our list to get blockstrength -> checkaroundpass
					chunkPassPos.Add(chunkPosition[x]);
					chunkPassGo.Add(chunkParent[x]);
					}
				else{//then this is called from other script, need to add to their list
					//shoul prob be able to use single public blockScript but not sure
					thisStrength = caller.GetComponent<BlockStrength>() as BlockStrength;
					thisStrength.chunkPassPos.Add(chunkPosition[x]);
					thisStrength.chunkPassGo.Add(chunkParent[x]);
						
					}
				}
			//will need to be compared to max strength when using other blocks types...
			//bhange
			else if (blockStr[(int)thisPosition.x, (int)thisPosition.y, (int)thisPosition.z].strength == 8){

			//else if (blockStr[(int)thisPosition.x, (int)thisPosition.y, (int)thisPosition.z][0] == 8){//blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][0]){
					//so if we are max strength and their is a block above us without max strength, have them update themselves..
					//Also may need to check if their is a stronger link to foundation
					
					//bhange
					if(blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z].strength != 8){

					//if(blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][0] != 8){
						if(caller == gameObject){//if we called this function this block gets added to our list to get blockstrength -> checkaroundpass
						chunkPassPos.Add(chunkPosition[x]);
						chunkPassGo.Add(chunkParent[x]);
						}
						else{//then this is called from other script, need to add to their list
						//shoul prob be able to use single public blockScript but not sure
						thisStrength = caller.GetComponent<BlockStrength>() as BlockStrength;
						thisStrength.chunkPassPos.Add(chunkPosition[x]);
						thisStrength.chunkPassGo.Add(chunkParent[x]);
						}	
					
						}
					//bhange
					//this does happen when say blocks are placed against a vertical wall
					//this method will need to be changed in the future, since connected to terrain it should just be solid foundation not have a path
					//but for now this seems to work
					
					else if(x == 0 && blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z].strength == 8){
					//else if(x == 0 && blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][0] == 8){//if x = 0 we are checking the block above us
						//so if a block hits terrain when laid, it will not even strength it just get set...so it will try to add strength, which will flag block above to recheck and 

						Debug.Log("Finding block above us with 8 strength...at position " + terrainScript.positionNames[x]);
						Debug.Log("This position " + chunkPosition[x] + " our position " + thisPosition);
						//Debug.Log
						//Debug.Break();
						//bhange
						//Okay so what is happening here is it is checking if the length of the path isnt one higher than ours, then recheck it
						if(blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z].length != blockStr[(int)thisPosition.x, (int)thisPosition.y, (int)thisPosition.z].length + 1){
						//if(blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][1] != blockStr[(int)thisPosition.x, (int)thisPosition.y, (int)thisPosition.z][1] + 1){
						if(caller == gameObject){//if we called this function this block gets added to our list to get blockstrength -> checkaroundpass
						chunkPassPos.Add(chunkPosition[x]);
						chunkPassGo.Add(chunkParent[x]);
							}
						else{//then this is called from other script, need to add to their list
						//shoul prob be able to use single public blockScript but not sure
						thisStrength = caller.GetComponent<BlockStrength>() as BlockStrength;
						thisStrength.chunkPassPos.Add(chunkPosition[x]);
						thisStrength.chunkPassGo.Add(chunkParent[x]);
						}	
					
						}

						}
//					else if (blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][0] == 8){
//						Debug.Log("Finding block above us with 8 strength...at position " + terrainScript.positionNames[x]);
//						Debug.Log("This position " + chunkPosition[x] + " our position " + thisPosition);
//						//Debug.Log
//						Debug.Break();
//						}
						
					}
//					PRETTY SURE THIS COULD NEVER HAPPEN...
					//this can happen if foundation block placed when already one above it, so we need to check if they have the right path, if not send them for recheck...
					
					
					}
			
				
			}
		else{//this is not our block
			
			thisStrength = chunkParent[x].GetComponent<BlockStrength>() as BlockStrength;
			//bhange
			if(thisStrength.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z].strength != 0){	
					
				if(blockStr[(int)thisPosition.x, (int)thisPosition.y, (int)thisPosition.z].strength - 1 > thisStrength.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z].strength){

//			if(thisStrength.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][0] != 0){	
//					
//				if(blockStr[(int)thisPosition.x, (int)thisPosition.y, (int)thisPosition.z][0] - 1 > thisStrength.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][0]){
//				
					if(caller == gameObject){//if we called this function this block gets added to our list to get blockstrength -> checkaroundpass
					chunkPassPos.Add(chunkPosition[x]);
					chunkPassGo.Add(chunkParent[x]);
					}
					else{//then this is called from other script, need to add to their list
					//shoul prob be able to use single public blockScript but not sure
					thisStrength = caller.GetComponent<BlockStrength>() as BlockStrength;
					thisStrength.chunkPassPos.Add(chunkPosition[x]);
					thisStrength.chunkPassGo.Add(chunkParent[x]);
						
					}
				
				
				}
			//will need to be compared to max strength when using other blocks types...
			//bhange
			else if (blockStr[(int)thisPosition.x, (int)thisPosition.y, (int)thisPosition.z].strength == 8){//blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][0]){
			//else if (blockStr[(int)thisPosition.x, (int)thisPosition.y, (int)thisPosition.z][0] == 8){//blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][0]){
					//so if we are max strength and their is a block above us without max strength, have them update themselves..
					//Also may need to check if their is a stronger link to foundation

					//bhange
					if(thisStrength.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z].strength != 8){
						
					//if(thisStrength.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][0] != 8){
						if(caller == gameObject){//if we called this function this block gets added to our list to get blockstrength -> checkaroundpass
						chunkPassPos.Add(chunkPosition[x]);
						chunkPassGo.Add(chunkParent[x]);
						}
						else{//then this is called from other script, need to add to their list
						//shoul prob be able to use single public blockScript but not sure
						thisStrength = caller.GetComponent<BlockStrength>() as BlockStrength;
						thisStrength.chunkPassPos.Add(chunkPosition[x]);
						thisStrength.chunkPassGo.Add(chunkParent[x]);
						}	
					
						}

					//bhange 
					else if(x == 0 && thisStrength.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z].strength == 8){//if x = 0 we are checking the block above us
					//else if(x == 0 && thisStrength.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][0] == 8){//if x = 0 we are checking the block above us
						//so if a block hits terrain when laid, it will not even strength it just get set...so it will try to add strength, which will flag block above to recheck and 
						//Debug.Log("Finding block above us with 8 strength...at position " + terrainScript.positionNames[x]);
						//Debug.Log("This position " + chunkPosition[x] + " our position " + thisPosition);
						//Debug.Log
						//Debug.Break();
						//bhange
						if(thisStrength.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z].length != blockStr[(int)thisPosition.x, (int)thisPosition.y, (int)thisPosition.z].length + 1){

						//if(thisStrength.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][1] != blockStr[(int)thisPosition.x, (int)thisPosition.y, (int)thisPosition.z][1] + 1){
						if(caller == gameObject){//if we called this function this block gets added to our list to get blockstrength -> checkaroundpass
						chunkPassPos.Add(chunkPosition[x]);
						chunkPassGo.Add(chunkParent[x]);
							}
						else{//then this is called from other script, need to add to their list
						//shoul prob be able to use single public blockScript but not sure
						thisStrength = caller.GetComponent<BlockStrength>() as BlockStrength;
						thisStrength.chunkPassPos.Add(chunkPosition[x]);
						thisStrength.chunkPassGo.Add(chunkParent[x]);
						}	
					
						}
						}
						
					}
				}
				
				
				
		}
			
			
			
		}
		
		
	doingPass = false;	
	}
	
	
	
//so for updating multiples...terrain gen will have a bnuch of lists that collected all bad foundations and stuff
//these with probably need there own lists to work with for processing, though wish I could reuse some existing lists
public IEnumerator BlockPieces(bool enableOnRender){//so this determines single blocks and grouped blocks for falling
		
	//PROBABLY DOENST NEED TO BE AN ENUM BUT NEED FOR YIELDS IN DEBUGGING	
	//so any values found and processed through mass, will end up on these lists for us to work with as falling blocks

	//so we are implimenting a method to make pieces break into smaller pieces, to reduce lag and cause cooler destructions
	//we will use this split bool to detemine when to start spliting based on a random number
	//Random rand = new Random();
	int randomNumber;
	split = false;
	int splitCheck = 0;
	//think there was a problem where not all 255s getting cleared, since only our chunkPass lists were getting cleared
	//and nothe surround blocks on positions and parents lists...
	int blocksChecked = 0;

	for (int x = 0; x < chunkPassPos.Count; x++){
        //Debug.Log("Checking this on BlockPieces " + chunkPassPos[x]);
		splitCheck = 0;//we only split after so many attached blocks so set this to zero
		blocksChecked +=1;
		
		//if we create parallel arrays for these can avoid this calls in thread...
		blockScript = chunkPassGo[x].GetComponent<BlockStrength>() as BlockStrength;
		chunkScript = chunkPassGo[x].GetComponent<ChunkScript>() as ChunkScript;
		//bhange

		if (blockScript.blockStr[(int)chunkPassPos[x].x, (int)chunkPassPos[x].y, (int)chunkPassPos[x].z].strength == 255){
			//since strength doesnt mean anything we uss 255 as a flag for when a block is processed
			//if already been processed ( or == 255 - then this has already been added to position/parent list for final block fall
			continue;
			}

		randomNumber = Random.Range(5,20);//create a random number of when to split our chunks	
	
		GetAroundInfo(chunkPassPos[x], chunkPassGo[x]);//overidden methods for calling getaroundinfo on another chunk

		//we check for == 50 (stone) and == 0  (strength) to find fallen blocks when in FindConnected method
		//it will not add blocks that have been flagged 255, so we should naturally have a unique list of blocks

		if(FindConnected(chunkPassPos[x], chunkPassGo[x])){

			//so this method returns true only if we found a connecting falling block around us...


			if(chunkPassGo[x] == gameObject){//flag to 255 since we have already done connections for this block...
				blockStr[(int)chunkPassPos[x].x, (int)chunkPassPos[x].y, (int)chunkPassPos[x].z].strength = 255;
				}
			else{
				blockScript.blockStr[(int)chunkPassPos[x].x, (int)chunkPassPos[x].y, (int)chunkPassPos[x].z].strength = 255;	
				}	
					
			//if we found something conencted they will be added to positionList and parentList from findconnected, so we need to check them
			//to find other connected blocks... which keep adding to positionList and parentList until we have all conencted...
			//if a block is marked 255, then it is alredy processed so move to the next
			
						
			for (int i = 0; i < positionList.Count; i ++){
				splitCheck +=1;
				blocksChecked +=1;

				GetAroundInfo(positionList[i], parentList[i]);

				if(splitCheck > randomNumber){
					//then we are splitting this blockpiece, all further connected calls need to go to our other method
					//this method instead of adding to positions list, adds all connecting pieces back to the chunkPass array
					//this will make sure everything connected gets check again...
					FindNextConnected(positionList[i],parentList[i]);
					}
				else{
					FindConnected(positionList[i],parentList[i]);
					}
				//list keeps expanding as more cubes call findconnected...
				//so everything on these lists was found by another block as connected, so continue the chain
				//caling findConnected will keep checking around and finding unique connected fallen, and adding them to these lists

				
								

				//here is a generic wait until things are threaded
				if(blocksChecked > strCheckWait){
					blocksChecked = 0;
					yield return null;
					}
								
				}
			//when done, we have all the blocks that are connected for this section, just add in the start block since not added through findConnected
					
			parentList.Add(chunkPassGo[x]);
			positionList.Add(chunkPassPos[x]);

            //Debug.Log("Orignal position " + chunkPassPos[x]);
			//in current method we pass these back to the terrain generator that is waiting for us to finish...
			//the terrain gen want a gameObject that is passed for rendering, so we need a custom class that holds our blockpiece information	
			//so create the object and add to the list
			//Debug.Log ("Finding chunk to fall");

			GameObject blockFall = Instantiate(terrainScript.blockFall, transform.position, transform.rotation) as GameObject; 	
			//Debug.Log(" are these the same " + transform.position +  " " + blockFall.transform.position);
			BlockFall bScript = blockFall.GetComponent<BlockFall>() as BlockFall;
			bScript.terrainScript = terrainScript;//so this block doestn have to call Gameobject.Find methods
			//bScript.blockPooler = terrainScript.objectPoolerBlock;
			
			//add this to list of positions that need to be cleared from 255
			//after this iteration, the list get cleared and repopulated so add to final list
			finalPositions.AddRange (positionList);
			finalParents.AddRange (parentList);
			//Debug.Log("check final positions or positions list on block strength");
			//Debug.Break();
			//yield return null;

			//before we add the positions to the list, we need to convert all teh positions to us locally
			//for meshing eveyrthing needs to be computed relative ot the meshing object
			for (int i = 0; i < positionList.Count; i ++){
				
				if(parentList[i] == gameObject){
					continue;
				}
				else{
					//this position resides in another chunk...convert its position to being local to us instead

					//these outside chunk need to account for new scale of 3
					//Vector3 finalPos = positionList[i];
					//this is not working, position list is being converted non-properly


					positionList[i] =	((positionList[i] * 3) + parentList[i].transform.position - gameObject.transform.position)/3;
					//positionList[i] = positionList[i]/3;
					
					Vector3 finalPos = positionList[i];
						/*
						if(finalPos.x > 15){
							
							finalPos.x = Mathf.FloorToInt(finalPos.x/3);
							//Debug.Log("Combined local " + combinedLocal.x);
						}
						if(finalPos.y > 15){
							finalPos.y = Mathf.FloorToInt(finalPos.y/3);
							
						}
						if(finalPos.z > 15){
							finalPos.z = Mathf.FloorToInt(finalPos.z/3);
							
						}
						
						if(finalPos.x < 0){
							
							finalPos.x = 0;
							//Debug.Log("Combined local " + combinedLocal.x);
						}
						if(finalPos.y < 0){
							finalPos.y = 0;
							
						}
						if(finalPos.z > 15){
							finalPos.z = 0;
							
						}
						*/

						positionList[i] = finalPos;
				}
				}
				Debug.Log("check positions again");
				//Debug.Break();
				//yield return null;
			//Debug.Log("Check positionsList these are converted to our positions ");
			//Add the positions at this gameobject..
			//this gameobject holds a big array for all the information about these blocks...
			if(!enableOnRender){//then this is a group of blocks and needs to be enabled in sequence

				bScript.voxelPosList.AddRange(positionList);
				bScript.MinimizeArrays();
				//Debug.Log(" BLocks added to voxelpos list " + positionList.Count + " added " + bScript.voxelPosList.Count);
						
				terrainScript.foundationUpdateGoList.Add(blockFall);
				}
			else if(ourRender != null){//this is for updatestrengthsingle calls, this render is what we need to add it to

				bScript.voxelPosList.AddRange(positionList);
				bScript.MinimizeArrays();
				Debug.Log(" Just  " + positionList.Count + " added " + bScript.voxelPosList.Count);
				ourRender.renderOrder.Add(blockFall);

				}
			else{//this is result from removing a single block so enable immediately...
				//we add the positions to this blockpiece, then call minimize arrays and it will get ready for rendering

				//we need to remove the original position from this list, do not know why it is on here...

				bScript.voxelPosList.AddRange(positionList);
				bScript.MinimizeArrays();
					//Debug.Log(" BLocks added to voxelpos list " + positionList.Count + " added " + bScript.voxelPosList.Count);

				terrainScript.updateSolidPiece.Add(blockFall); 

			}

			//This has a bunch of blocks, but there may be other seperate sections that may be falling, 
			//This will loop back to the chunkPass lists and check the next block we know needs to fall
			//If this block is marked 255, then we know it has already been added and checked to the previous 
			//secton...
			
			positionList.Clear();
			parentList.Clear();		
				
			//Debug.Log("Done with this connection " + positionList.Count + " out of " + chunkPassPos.Count);
			//Debug.Break();
			//once this is done we should have all blocks that are connected to one another in this chain
			
			
			}
		else{
			//Debug.Log("Found single block to fall " + chunkPassGo[x].transform.position + " " + chunkPassPos[x]);
			//we should just make a single basic gameobject for this single be piece but this easier to just do it the long way
			//terrainScript.DebugCube(chunkPassPos[x], chunkPassGo[x].transform.position, 1, Color.blue, 10f);
				
			

			//before we add the positions to the list, we need to convert all teh positions to us locally
			//for meshing eveyrthing needs to be computed relative ot the meshing object
			Vector3 posSave = chunkPassPos[x];

			if(chunkPassGo[x] == gameObject){
				//then this position is already local to us
				//continue;
				}
			else{
				//this position resides in another chunk...convert its position to being local to us instead
                    //NOT SO SURE THS NEEDS TO BE CONVERTED
				chunkPassPos[x] = ((chunkPassPos[x] * 3) + chunkPassGo[x].transform.position - gameObject.transform.position)/3;
				}
									
			if(!enableOnRender){//then this is a group of blocks and needs to be enabled in sequence
				
				if(chunkPassPos.Count > 0){
					GameObject blockFall = Instantiate(terrainScript.blockFall, transform.position, transform.rotation) as GameObject; 	
					BlockFall bScript = blockFall.GetComponent<BlockFall>() as BlockFall;
					bScript.terrainScript = terrainScript;//so this block doestn have to call Gameobject.Find methods
					//bScript.blockPooler = terrainScript.objectPoolerBlock;
					//Vector3 posSave = chunkPassPos[x];

					bScript.voxelPosList.Add(chunkPassPos[x]);
					bScript.MinimizeArrays();
					terrainScript.foundationUpdateGoList.Add(blockFall);
					}
				//terrainScript.updateSolidPiece.Add(blockFall);
			}
			else if(ourRender != null){//this is for updatestrengthsingle calls, this render is what we need to add it to
					


						GameObject blockFall = Instantiate(terrainScript.blockFall, transform.position, transform.rotation) as GameObject; 	
						BlockFall bScript = blockFall.GetComponent<BlockFall>() as BlockFall;
						bScript.terrainScript = terrainScript;//so this block doestn have to call Gameobject.Find methods
						//bScript.blockPooler = terrainScript.objectPoolerBlock;
						//Vector3 posSave = chunkPassPos[x];

						bScript.voxelPosList.Add(chunkPassPos[x]);
						bScript.MinimizeArrays();
						Debug.Log(" ADDING TO NEW RENDERPIECE " + positionList.Count + " added " + bScript.voxelPosList.Count + " renderOrder " + ourRender.renderOrder.Count);

						ourRender.renderOrder.Add (blockFall);


					//Debug.Log(" ADDING TO NEW RENDERPIECE " + positionList.Count + " added " + bScript.voxelPosList.Count + " renderOrder " + ourRender.renderOrder.Count);
					//
				}
			else{//this is result from removing a single block so enable immediately...
				//we add the positions to this blockpiece, then call minimize arrays and it will get ready for rendering
				GameObject blockFall = Instantiate(terrainScript.blockFall, transform.position, transform.rotation) as GameObject; 	
				BlockFall bScript = blockFall.GetComponent<BlockFall>() as BlockFall;
				bScript.terrainScript = terrainScript;//so this block doestn have to call Gameobject.Find methods
				//bScript.blockPooler = terrainScript.objectPoolerBlock;
				

				bScript.voxelPosList.Add(chunkPassPos[x]);
				bScript.MinimizeArrays();
				//Debug.Log(" BLocks added to voxelpos list " + positionList.Count + " added " + bScript.voxelPosList.Count);
				
				terrainScript.updateSolidPiece.Add(blockFall);
				
			}
				chunkPassPos[x] = posSave;
					
			}
		
			if(blocksChecked > strCheckWait){
				blocksChecked = 0;
				yield return null;
			}	
		}
	
	//so we have a final list of chunks and positions tht need to be changed back from 255
	int firstFound = 0;
	int secondFound = 0;
	
	finalParents.AddRange(chunkPassGo);
	finalPositions.AddRange(chunkPassPos);

	for (int x = 0; x < finalPositions.Count; x++){
			blockScript = finalParents[x].GetComponent<BlockStrength>() as BlockStrength;
			chunkScript = finalParents[x].GetComponent<ChunkScript>() as ChunkScript;

			if (chunkScript.vegeByte[(int)finalPositions[x].x, (int)finalPositions[x].y, (int)finalPositions[x].z] != 0){
				//this assures that the specific voxels are set to air, pretty sure this is not even needed
				secondFound +=1;
				chunkScript.vegeByte[(int)finalPositions[x].x, (int)finalPositions[x].y, (int)finalPositions[x].z] = 0;
			}
			
			//bhange
			if (blockScript.blockStr[(int)finalPositions[x].x, (int)finalPositions[x].y, (int)finalPositions[x].z].strength == 255){
				blockScript.blockStr[(int)finalPositions[x].x, (int)finalPositions[x].y, (int)finalPositions[x].z].strength = 0;
				secondFound +=1;
			}
		}

	if(ourRender != null){
			Debug.Log("Passing data ot our Render Group " + ourRender.renderOrder.Count);


		}
	//Debug.Log("Found " + positionList.Count + " number of blocks total to fall + single fall blocks .... original " + chunkPassPos.Count);
	chunkPassPos.Clear();
	chunkPassGo.Clear();
	positionList.Clear();
	parentList.Clear();
	finalParents.Clear();
	finalPositions.Clear ();
	//Debug.Break();
	yield return null;	

		
	}
			
public bool FindConnected (Vector3 posInArray, GameObject parent){
	
	//1.Check around our block for other fallen blocks we can connect to
	//2. Need blocks found in other chunks to be converted to our position to group properly...
	//3. Any positions found need to be passed back to positionlist/parentlist
	//4. once they are passed they need to be flagged 255, this way they will only add unique values..
		//-though since converting to our local position if would be easy to create list.distinct
	
	//this method needs to be changed if we do this on another block...
	//otherwise dont expect this to work - duh wondering why I was having problems...
			
	bool foundSomething = false;	 
	
	//THIS IS JUST FOR INSPECTION OTHERWISE PROBABLY SHOULD HAPPEN IN HERE...calling GetAroundInfo in coroutine so can pause	
	//GetAroundInfo(posInArray);


	//fills chunkPosition and chunkParent array with infomation about surrounding blocks	
	for(int x = 0; x < 6; x++){	
			
		if(chunkParent[x] == gameObject){
			//neighborStr[x] = blockStr[(int)chunkPosition[x].x,(int)chunkPosition[x].y,(int)chunkPosition[x].z][0];
			//neighborType[x] = (int)chunkParent[x].GetComponent<ChunkScript>().vegeByte[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z];
			//if a block
			if(ourChunkScript.vegeByte[(int)chunkPosition[x].x,(int)chunkPosition[x].y,(int)chunkPosition[x].z] == 50){
				
				//and if it has not strength
				if(blockStr[(int)chunkPosition[x].x,(int)chunkPosition[x].y,(int)chunkPosition[x].z].strength == 0){
					//so this is another falling block next to us, add it to the falling group in progress
					//Debug.Log("Adding position from our chunk " + chunkPosition[x]);
					positionList.Add(chunkPosition[x]); 
					parentList.Add(chunkParent[x]);
                        //Debug.Log("Added pos " + chunkPosition[x]);
					foundSomething = true;
					//now set a flag so this block doesnt get added again and is registered as done...
					blockStr[(int)chunkPosition[x].x,(int)chunkPosition[x].y,(int)chunkPosition[x].z].strength = 255;
					}
				else if (blockStr[(int)chunkPosition[x].x,(int)chunkPosition[x].y,(int)chunkPosition[x].z].strength == 255){
				
					//if(terrainScript.strDebug){
					//terrainScript.DebugCube(chunkPosition[x], chunkParent[x].transform.position, .8f, Color.grey, 1f);
					//}
					foundSomething = true;
					//removed this below since seems impossible, maybe it is not...?
					//blockStr[(int)chunkPosition[x].x,(int)chunkPosition[x].y,(int)chunkPosition[x].z].strength = 255;			
						
					}
					
				}

				
			}
		else{
			//this block in on another chunk
				
			blockScript = chunkParent[x].GetComponent<BlockStrength>() as BlockStrength; 
			chunkScript = chunkParent[x].GetComponent<ChunkScript>() as ChunkScript;

			//neighborStr[x] = blockScript.blockStr[(int)chunkPosition[x].x,(int)chunkPosition[x].y,(int)chunkPosition[x].z][0];
			//neighborType[x] = (int)chunkScript.vegeByte[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z];
						
			//so if our position is a block and has no strength then it should fall
			if(blockScript.blockStr[(int)chunkPosition[x].x,(int)chunkPosition[x].y,(int)chunkPosition[x].z].strength == 0 && chunkScript.vegeByte[(int)chunkPosition[x].x,(int)chunkPosition[x].y,(int)chunkPosition[x].z] == 50){
		
				if(terrainScript.strDebug){
				//terrainScript.DebugCube(chunkPosition[x], chunkParent[x].transform.position, 1, Color.magenta, 1f);
				}
				positionList.Add(chunkPosition[x]);
                    //Debug.Log("Added pos " + chunkPosition[x]);
				parentList.Add(chunkParent[x]);
				foundSomething = true;
				//now set a flag so this block doesnt get added again and is registered as done...
				//bhange
				blockScript.blockStr[(int)chunkPosition[x].x,(int)chunkPosition[x].y,(int)chunkPosition[x].z].strength = 255;	
				//blockScript.blockStr[(int)chunkPosition[x].x,(int)chunkPosition[x].y,(int)chunkPosition[x].z][0] = 255;	
					
				}
	}
			
			
			
			
		}
	if(foundSomething){
		return true;
		}
	else{
		return false;
		}
		
	}

	public bool FindNextConnected (Vector3 posInArray, GameObject parent){
		
		//so this method returns connected block to the chunkpass arrays, they are not being added toa blockpiece
		//instead they could become their own blockpieces, allow to split up the chunk pieces
		
		bool foundSomething = false;	 
				
		//fills chunkPosition and chunkParent array with infomation about surrounding blocks	
		for(int x = 0; x < 6; x++){	
			
			if(chunkParent[x] == gameObject){
				//neighborStr[x] = blockStr[(int)chunkPosition[x].x,(int)chunkPosition[x].y,(int)chunkPosition[x].z][0];
				//neighborType[x] = (int)chunkParent[x].GetComponent<ChunkScript>().vegeByte[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z];
				//if a block
				if(ourChunkScript.vegeByte[(int)chunkPosition[x].x,(int)chunkPosition[x].y,(int)chunkPosition[x].z] == 50){
					
					//and if it has not strength
					if(blockStr[(int)chunkPosition[x].x,(int)chunkPosition[x].y,(int)chunkPosition[x].z].strength == 0){
						//so this is another falling block next to us, but we are not taking any more pieces, get it on passarray
						//for update strength and future passes to block pieces
						chunkPassPos.Add (chunkPosition[x]);
						chunkPassGo.Add (chunkParent[x]);
                        //Debug.Log("Added pos " + chunkPosition[x]);
						//positionList.Add(chunkPosition[x]); 
						//parentList.Add(chunkParent[x]);
						foundSomething = true;
						//now set a flag so this block doesnt get added again and is registered as done...
						//this pieces has not been connected to we dont set the strength to 255, this is flag for connected
						//blockStr[(int)chunkPosition[x].x,(int)chunkPosition[x].y,(int)chunkPosition[x].z].strength = 255;
					}
					else if (blockStr[(int)chunkPosition[x].x,(int)chunkPosition[x].y,(int)chunkPosition[x].z].strength == 255){

						foundSomething = true;
					}
					
				}
				
				
			}
			else{
				//this block in on another chunk
				
				blockScript = chunkParent[x].GetComponent<BlockStrength>() as BlockStrength; 
				chunkScript = chunkParent[x].GetComponent<ChunkScript>() as ChunkScript;
				
				//neighborStr[x] = blockScript.blockStr[(int)chunkPosition[x].x,(int)chunkPosition[x].y,(int)chunkPosition[x].z][0];
				//neighborType[x] = (int)chunkScript.vegeByte[(int)chunkPosition[x].x, (int)chunkPosition[x].y,(int)chunkPosition[x].z];
				
				//so if our position is a block and has no strength then it should fall
				if(blockScript.blockStr[(int)chunkPosition[x].x,(int)chunkPosition[x].y,(int)chunkPosition[x].z].strength == 0 && chunkScript.vegeByte[(int)chunkPosition[x].x,(int)chunkPosition[x].y,(int)chunkPosition[x].z] == 50){

					chunkPassPos.Add (chunkPosition[x]);
					chunkPassGo.Add (chunkParent[x]);
                    //Debug.Log("Added pos " + chunkPosition[x]);
					//positionList.Add(chunkPosition[x]);
					//parentList.Add(chunkParent[x]);
					foundSomething = true;
					//now set a flag so this block doesnt get added again and is registered as done...
					//bhange
					blockScript.blockStr[(int)chunkPosition[x].x,(int)chunkPosition[x].y,(int)chunkPosition[x].z].strength = 255;	
					//blockScript.blockStr[(int)chunkPosition[x].x,(int)chunkPosition[x].y,(int)chunkPosition[x].z][0] = 255;	
					
				}
			}
			
			
			
			
		}
		if(foundSomething){
			return true;
		}
		else{
			return false;
		}
		
	}

	public IEnumerator UpdateStrengthSingle(GameObject singleObject, Vector3 singlePosition, RenderGroup thisRender){//GameObject singleObject, Vector3 singlePosition

		ourRender = thisRender;
		Debug.Log("Doing updatestrength single at position " + singlePosition);
		Debug.Log("Strength " + blockStr[(int) singlePosition.x, (int) singlePosition.y, (int) singlePosition.z].strength + " direction " + blockStr[(int) singlePosition.x, (int) singlePosition.y, (int) singlePosition.z].direction + " length " + blockStr[(int) singlePosition.x, (int) singlePosition.y, (int) singlePosition.z].length);

		updateFoundationGo.Clear();
		updateFoundationPos.Clear();
		fullChunkList.Clear();
		
		if(!initialized){//missing references and array allocations
			Initialize();
		}
					
		updatingStrength = true;
			
		//update single strength at the point that has been called...
		yield return StartCoroutine(UpdateStrength(singlePosition, false, true));
		//when this is done it will have the pass arrays filled with gameobject and positions
		//not sure this necessary below, do a test...

		//if removing a single block through normal block removal it need to remove the block clicked on
		//if this is through digging, then we dont want to remove the spot...
		if(singleObject == gameObject){

			//Debug.Log("Clearing our own spot " + singlePosition + " " +  singleObject.transform.position);
			blockScript = GetComponent<BlockStrength>() as BlockStrength;
			chunkScript  = GetComponent<ChunkScript>() as ChunkScript;
			//clear the block that is being removed so doesnt group as a falling block...
		
			//so need to set from 50 to zero so it is removed if we remove the blocks
            ourChunkScript.vegeByte[(int) singlePosition.x, (int) singlePosition.y, (int) singlePosition.z] = 0;
			blockStr[(int) singlePosition.x, (int) singlePosition.y, (int) singlePosition.z].strength = 0;
			blockStr[(int) singlePosition.x, (int) singlePosition.y, (int) singlePosition.z].direction = 7;
			blockStr[(int) singlePosition.x, (int) singlePosition.y, (int) singlePosition.z].length = 0;

			}
		else{
            Debug.Log("Whats going on here this should not be");
            Debug.Break();

		}

			//before we call this we problably have ot clear that spot that is being removed and weas sent to us

			float startTime = Time.realtimeSinceStartup;
			yield return StartCoroutine(BlockPieces(true));//so this method needs to return block pieces to a 


		for(int p = 0; p < thisRender.renderOrder.Count; p++){

		Debug.Log("Positions of renderOrder " + thisRender.renderOrder[p].transform.position);

			}
			
			Debug.Log("REturning from blockPieces single  in " + (Time.realtimeSinceStartup - startTime));
			Debug.Log("So our CUSTOM BLOCK piece has " + thisRender.renderOrder.Count);
			updatingStrength = false;
		//thisRender.Add(single
			//if(thisRender.renderOrder.Count != 0){
			terrainScript.orderedRenderList.Add(thisRender);
				//Debug.Log("So our CUSTOM BLOCK piece has " + thisRender.renderOrder.Count);
				//}
			//else{
				//thisRender = null;

				//}
			ourRender = null;
			
			
		}
		

	
	public IEnumerator UpdateStrengthMass(){//GameObject singleObject, Vector3 singlePosition
	//to terrain generator collects chunks when multiple chunk has been dug and can effect multiple foundations
	//this time just grab listing from terrain generator and process them one at a time...
	//this is the modified version that works on updating multiple blocks effected at once, be it through terrain
	//modification, through explosions, by monsters -  removing tons of blocks

		Debug.Log("Doing updatestrengthmass");
		//clear our working lists from last time
		updateFoundationGo.Clear ();// = new List<GameObject>();
		updateFoundationPos.Clear();// = new List<Vector3>();
		fullChunkList.Clear();

		//not sure this is even possible, but maybe
		if(!initialized){//missing references and array allocations
			Initialize();
			}

		if(terrainScript.foundationUpdateGoList.Count == 0 ){

			//this version is for single block manual removal that might produce pieces...
			Debug.Log("Something wrong here");
			Debug.Break();
			yield return null;

			//these parameteres are only there is 
//			updateFoundationGo.AddRange(singleObject);
//			updateFoundationPos.AddRange(singlePosition);

			updatingStrength = true;
			//yield return StartCoroutine(UpdateStrength(updateFoundationPos[x], false, false));

			yield return StartCoroutine(BlockPieces(false));
			Debug.Log("REturning from blockPieces");
			//once we get back 
			
			updatingStrength = false;

			}
		else{//so this is original method where blocks are getting removed from terrain modification

		//so copy over all the lists from the terrain generator, this is all effected parents and block positions
		updateFoundationGo.AddRange(terrainScript.foundationUpdateGoList);
		updateFoundationPos.AddRange(terrainScript.foundationUpdatePosList);
		
		updatingStrength = true;
		//so for each entry just call updateStrength, lengthy method there is surely a faster way...
		//will have to revisit multi method, for speed



		for(int x = 0; x < updateFoundationPos.Count; x++){
			
			//there is no guarentee that chunkScript will be set...?
			if(updateFoundationGo[x] == gameObject){//then call on our gameobject
				yield return StartCoroutine(UpdateStrength(updateFoundationPos[x], false, false));
				
				}
			else{//call on other object
				blockScript = updateFoundationGo[x].GetComponent<BlockStrength>();
				
				yield return StartCoroutine(blockScript.UpdateStrength(updateFoundationPos[x], false, false));
			
				}
		}
		
		//now we need to check for falling blocks section so we can group, add collider, then add physics...
		//SO we have possible blocks to be grouped, or falling blocks on chunkPass arrays, 
			//to reuse them...have to review if we can always use these are not...
		
		float startTime = Time.realtimeSinceStartup;
		yield return StartCoroutine(BlockPieces(false));
		Debug.Log("REturning from blockPieces in " + (Time.realtimeSinceStartup - startTime));
		//Debug.Break();
		//once we get back 
		
		updatingStrength = false;
			
		}
		
	}

	
public IEnumerator UpdateStrength(Vector3 thisPosition, bool foundationCheck, bool singleBlock){///last perameter not being used...

		updatingStrength = true;
		bool collectZeros = false;	
		float beginTime = Time.realtimeSinceStartup;
		int fullCounter = 0;
		//so this is position that is removed...so it clears it and checks around to see what is missing a path
		//that is us, or depends on us immediately.

		//so we first clear the information for this position, since this starts the chain of effected voxels
		blockStr[(int)thisPosition.x, (int)thisPosition.y, (int)thisPosition.z].strength = 0;
		blockStr[(int)thisPosition.x, (int)thisPosition.y, (int)thisPosition.z].direction = 7;
		blockStr[(int)thisPosition.x, (int)thisPosition.y, (int)thisPosition.z].length = 0;

		chunkScript = ourChunkScript;// GetComponent<ChunkScript>();

		//this method check the 6 positions around us to see who is missing their path because they depended on 
		//us for strength, it creates an array for the parents of these positions 
		CheckAroundPath(gameObject, thisPosition);//check to see if we are missing paths
		//terrainScript.DebugCube(thisPosition,transform.position, 1.1f, Color.red, .1f);
		//yield return null;
		//Debug.Break();
		gettingAllMissing = true;
		
		if(foundationCheck){//not sure this is ever used...
		//so this is a foundation block is placed under another foundation block...to clears the above block by setting us to zero...to find the chain effected...
			//bhange  ****may have to look into this , not sure what we are doing here
			blockStr[(int)thisPosition.x, (int)thisPosition.y, (int)thisPosition.z].strength = 8;
			blockStr[(int)thisPosition.x, (int)thisPosition.y, (int)thisPosition.z].direction = 7;
			blockStr[(int)thisPosition.x, (int)thisPosition.y, (int)thisPosition.z].length = 0;
			}

		//sets their strength to zero and finds all that rely on this path
		//	if there are missing paths then they adre added to the chunkAroundPos Lists
		// this continues to call CheckAroundPath, which in turn adds more to the chunkAroundPos Array for checking
		int totalCounter = 0;
		
		for(int x = 0; x < chunkAroundPos.Count; x++){ 
			fullCounter +=1;
			totalCounter += 1;
			if(chunkAroundGo[x] == gameObject){//then we recheck ours

				blockStr[(int) chunkAroundPos[x].x,(int) chunkAroundPos[x].y, (int) chunkAroundPos[x].z].strength = 0;
				blockStr[(int) chunkAroundPos[x].x,(int) chunkAroundPos[x].y, (int) chunkAroundPos[x].z].direction = 7;
				blockStr[(int) chunkAroundPos[x].x,(int) chunkAroundPos[x].y, (int) chunkAroundPos[x].z].length = 0;
		
				CheckAroundPath(gameObject, chunkAroundPos[x]);
				//terrainScript.DebugCube(chunkAroundPos[x],transform.position, 1.1f, Color.red, .08f);
				/*STRDEBUG
				Debug.Log("Around info collected for checkAroundPath, see inspector");
				Debug.Break();
				yield return new WaitForSeconds(.1f);
				yield return null;
				*/
				}
			else{
				blockScript = chunkAroundGo[x].GetComponent<BlockStrength>() as BlockStrength;
				blockScript.doingPath = true;
			
				blockScript.blockStr[(int) chunkAroundPos[x].x,(int) chunkAroundPos[x].y, (int) chunkAroundPos[x].z].strength = 0;
				blockScript.blockStr[(int) chunkAroundPos[x].x,(int) chunkAroundPos[x].y, (int) chunkAroundPos[x].z].direction = 7;
				blockScript.blockStr[(int) chunkAroundPos[x].x,(int) chunkAroundPos[x].y, (int) chunkAroundPos[x].z].length = 0;
				blockScript.CheckAroundPath(gameObject, chunkAroundPos[x]);
				//terrainScript.DebugCube(chunkAroundPos[x],chunkAroundGo[x].transform.position, 1.1f, Color.red, .08f);
				//Debug.Log("Around info collected for checkAroundPath, see inspector");

				
				while(blockScript.doingPath){
					yield return new WaitForSeconds(.001f); 
					}
				/*STRDEBUG
				Debug.Break();
				yield return new WaitForSeconds(.1f);
				yield return null;
				*/
				}

			if(totalCounter > checkAroundPathWait){
				totalCounter = 0;
				yield return null;
				}
			
			}

		//so if this is a dig removal we need to keep the original foundation blocks, otherwise if it was  single block removal
		//we would want to remove the block and not rechecked


        //so need a way to tell if from diggign terrain or something

		if (!singleBlock)
        {//then this is terrain removal, so add back in our original block
            //this appears to run fine, when blocks are dug, they are no longer removed, creates more single pieces though
            Debug.Log("Terrain removal add origina block " + thisPosition);
            if(checkStrPos.Contains(thisPosition)){
                //checkStrPos.Remove(thisPosition);
                //checkStrGo.Remove(thisPosition);
                
                Debug.Log("Somehow we are on the list and shoundlt be " + thisPosition);
                Debug.Break();
                //this is annoying find out how it is getting on the list
                //otherwise we have to do some complicated stuff to get it, since may be multiple thisPos and multiple gameO on the list
            }
            checkStrPos.Add(thisPosition);///later this is for when we recheck the strength for these blocks
            checkStrGo.Add(gameObject);

        } else
        {//this is normal block destruction so remove original block
            Debug.Log("This is a single block so dont add it, keep removed" + thisPosition + " then count " + checkStrPos.Count);

            //for(int u = 0; u < checkStrPos.Count; u++){
                //Debug.Log("On list " + checkStrPos[u]);
                
            //}
            //make sure our original is not on the list
            if(checkStrPos.Contains(thisPosition)){
                //checkStrPos.Remove(thisPosition);
                //checkStrGo.Remove(thisPosition);



                Debug.Log("Somehow we are on the list and shoundlt be " + thisPosition);
                Debug.Break();
                //this is annoying find out how it is getting on the list
                //otherwise we have to do some complicated stuff to get it, since may be multiple thisPos and multiple gameO on the list
            }
            else{

            }

        }



		//Debug.Log("All aroundBlocks found in " + (Time.realtimeSinceStartup - beginTime) + " and total checks"  + fullCounter);
		
		gettingAllMissing = false;
		
		//now we should have all blocks that have been effected by this removal	so we can start to find new strengths\\
		//Debug.Log("RED ALL EFFECTED all str set to zero " + checkStrGo.Count + "this is number of effected blocks " + chunkAroundPos.Count);
		////STRDEBUG
		//Debug.Break();
		//Debug.Log("check all surrounding array chunkAroundPos");
		//yield return null;
		//yield return new WaitForSeconds(.11f);
		////
					
		otherUpdateChunks.Clear();	
		chunkAroundPos.Clear();
		chunkAroundGo.Clear();
			
	
		int higherStrFound;// = 0;
		
		//now go through the list of chunks that need to have their strength updated and make them recheck
		//right now it goes back and forth and keep checking parts, until it hits a wave with no strength

		beginTime = Time.realtimeSinceStartup;

		checkingNewStrength = true;
		byte blocksUpdated = 0;
		byte waitUpdated = 0;
		byte localCounter = 0;
		
		for(int x = 0; x < checkStrGo.Count; x++){
			
			totalCounter +=1;
			fullCounter +=1;

			if(checkStrGo[x] == gameObject){
				waitUpdated = BlockStr(checkStrPos[x], false);
				localCounter += waitUpdated;
				blocksUpdated += waitUpdated;

				}
			else{
				if(terrainScript.strDebug){
					Debug.Log("Checking block on other chunk ");
					}
				blockScript = checkStrGo[x].GetComponent<BlockStrength>() as BlockStrength;
				blockScript.doingStrength = true;
				waitUpdated = blockScript.BlockStr(checkStrPos[x], false);
				localCounter += waitUpdated;
				blocksUpdated += waitUpdated;
				
				while(blockScript.doingStrength){
					yield return new WaitForSeconds(.001f);
					}
				
				if(terrainScript.strDebug){
					Debug.Log("Returning from the check on update str ");
					}
				
				if(!otherUpdateChunks.Contains(checkStrGo[x])){//if not already flagged for an update
					otherUpdateChunks.Add(checkStrGo[x]);
					}
				}

			
			if(localCounter > strCheckWait){
				localCounter = 0;
				yield return null;
				}
		
			}


		
		
		//Debug.Log("Total BLocks updated on first set of checks " + blocksUpdated);



		if(terrainScript.strDebug){
			Debug.Log("Getting through first set of strength checks ");
			}
		if(blocksUpdated != 0){//do another check of these

			blocksUpdated = 0;

		for(int x = checkStrGo.Count -1; x > -1; x--){
			
			totalCounter +=1;
			fullCounter +=1;
			
			if(checkStrGo[x] == gameObject){
				
				if(blockStr[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z].strength == 0){
					waitUpdated =  BlockStr(checkStrPos[x], false);
					localCounter += waitUpdated;
					blocksUpdated += waitUpdated;
					}
				}
			else{
				if(terrainScript.strDebug){
					Debug.Log("Checking block on other chunk ");
					}
				blockScript = checkStrGo[x].GetComponent<BlockStrength>() as BlockStrength;

				if(blockScript.blockStr[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z].strength == 0){
					blockScript.doingStrength = true;
					waitUpdated = blockScript.BlockStr(checkStrPos[x], false);
					localCounter += waitUpdated;
					blocksUpdated += waitUpdated;
					}
				
				while(blockScript.doingStrength){
					yield return new WaitForSeconds(.001f);
					}
				
				if(terrainScript.strDebug){
					Debug.Log("Returning from the check on update str ");
					}
				
				if(!otherUpdateChunks.Contains(checkStrGo[x])){//if not already flagged for an update
					otherUpdateChunks.Add(checkStrGo[x]);
					}
				
				}
			
			if(localCounter > strCheckWait){
				localCounter = 0;
				yield return null;
				}
		
			}

		}
		else{
		
			collectZeros = true;
		}

		//Debug.Log("Total BLocks updated on second set of checks " + blocksUpdated);

		//so here is blocks updated == 0, then we need to just finish and collect all spots that dont have a str value...
		if(blocksUpdated != 0){//do another check of these

			blocksUpdated = 0;
						
			for(int x = 0; x < checkStrGo.Count; x++){
				totalCounter +=1;
				fullCounter +=1;
				
				if(checkStrGo[x] == gameObject){

					if(blockStr[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z].strength == 0){
						waitUpdated =  BlockStr(checkStrPos[x], false);
						localCounter += waitUpdated;
						blocksUpdated += waitUpdated;

					}
				}
				//}
				else{
					blockScript = checkStrGo[x].GetComponent<BlockStrength>() as BlockStrength;

					if(blockScript.blockStr[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z].strength == 0){
						blockScript.doingStrength = true;
						waitUpdated = blockScript.BlockStr(checkStrPos[x], false);
						localCounter += waitUpdated;
						blocksUpdated += waitUpdated;

						while(blockScript.doingStrength){
							yield return new WaitForSeconds(.001f);
						}
					}
				}
				
				if(localCounter > strCheckWait){
					localCounter = 0;
					yield return null;
				}
			}
			
			//Debug.Log("Total BLocks updated on third set of checks " + blocksUpdated);
		}
		else{

			collectZeros = true;
		}

		//so here is blocks updated == 0, then we need to just finish and collect all spots that dont have a str value...
		if(blocksUpdated != 0){
			blocksUpdated = 0;
		
			//on this last set of checks we will collect any positions that dont have any strength...
			//then we will add them to a set of cumulative arrays to make them fall together
		
			for(int x = checkStrGo.Count -1; x > -1; x--){

				totalCounter +=1;
				fullCounter +=1;

				if(checkStrGo[x] == gameObject){
					if(blockStr[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z].strength == 0){
						waitUpdated =  BlockStr(checkStrPos[x], false);
						localCounter += waitUpdated;
						blocksUpdated += waitUpdated;

					if(blockStr[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z].strength == 0){
						chunkPassPos.Add(checkStrPos[x]);
						chunkPassGo.Add(checkStrGo[x]);
                        Debug.Log("Adding this position " + checkStrPos[x]);
						//Debug.DrawLine(transform.position + checkStrPos[x], (transform.position + checkStrPos[x] + (Vector3.up + -Vector3.right) * 1.2f), Color.blue, 1.25f);
						}
				}
				}
			else{
				blockScript = checkStrGo[x].GetComponent<BlockStrength>() as BlockStrength;

				if(blockScript.blockStr[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z].strength == 0){
					blockScript.doingStrength = true;
					waitUpdated = blockScript.BlockStr(checkStrPos[x], false);
					localCounter += waitUpdated;
					blocksUpdated += waitUpdated;

					while(blockScript.doingStrength){
						yield return new WaitForSeconds(.001f);
						}

					if(blockScript.blockStr[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z].strength == 0){
						chunkPassPos.Add(checkStrPos[x]);
						chunkPassGo.Add(checkStrGo[x]);
                            Debug.Log("Adding this position " + checkStrPos[x]);
						//Debug.DrawLine(transform.position + checkStrPos[x], (transform.position + checkStrPos[x] + (Vector3.up + -Vector3.right) * 1.2f), Color.blue, 1.25f);
						}
				}
				}
			
			if(localCounter > strCheckWait){
				totalCounter = 0;
				yield return null;
				}
			}

		//Debug.Log("Total BLocks updated on fourth final set of checks " + blocksUpdated);
	
		}
		else{
			collectZeros = true;
			}

		if(collectZeros){

			for(int x = checkStrGo.Count -1; x > -1; x--){
				
				totalCounter +=1;
				fullCounter +=1;
				
				if(checkStrGo[x] == gameObject){


					if(blockStr[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z].strength == 0){

                        if(checkStrPos[x] == thisPosition){

                            Debug.Log("This could be our problems");
                            }
						chunkPassPos.Add(checkStrPos[x]);
						chunkPassGo.Add(checkStrGo[x]);
					}
				}
				else{
					blockScript = checkStrGo[x].GetComponent<BlockStrength>() as BlockStrength;

					if(blockScript.blockStr[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z].strength == 0){
						chunkPassPos.Add(checkStrPos[x]);
						chunkPassGo.Add(checkStrGo[x]);

                        if(checkStrPos[x] == thisPosition){
                            
                            Debug.Log("This could be our problems");
                        }
					}
				}
				}
			}


		checkingNewStrength = false;	

		//Debug.Log(" Including wait every " + strCheckWait +  " checks");
		//Debug.Log("Done and strength checks in " + (Time.realtimeSinceStartup - beginTime) + " " + fullCounter);

		//for now, with wanting to see strengths we need to just have them re-rendered if they were checked for
		//strength, since this will likely need new numbers...

		//so if any have 0 strength but have a vegeByte value, then they need to fall, 
		//so basically we cant render anything till block pieces are created, otherwise renderer will have wrong information

		if(terrainScript.foundationUpdateGoList.Count > 0){//then we are doing a group method and need to be added to foundationUpdate
		//this is a unique group method when a bunch of block are removed and may have a bunch of blockpieces...
		
			for(int x = checkStrGo.Count -1; x > -1; x--){
				//so if our working list of effected gameobjects is not on the re-render list, then add it
				if (!terrainScript.foundationUpdateGoList.Contains(checkStrGo[x])){
					terrainScript.foundationUpdateGoList.Add(checkStrGo[x]);
					}
				}
			}



		else{//if the list doesnt have anything on it, then we need to add to otherupdateChunks so other 
			//objects get rendered...


			for(int x = checkStrGo.Count -1; x > -1; x--){
				//so if our working list of effected gameobjects is not on the re-render list, then add it
				if (checkStrGo[x] != gameObject && !terrainScript.updateSolidPiece.Contains(checkStrGo[x])){
					//we are not adding our gameobject because that one is already accounted for...
					//this just adds to the basic block rerender list
					terrainScript.updateSolidPiece.Add(checkStrGo[x]);
				}
				
			}


		}

		checkStrPos.Clear();
		checkStrGo.Clear();
		updatingStrength = false;
	}
	
	
public IEnumerator UpdateStrengthMulti(){//bool fromTerrain
		//fromTerrain bool is whether this is being called beacuse of terrain modifications, or something else
		//so can be used for explosions too
		
		//OTHER METHOD
//		if(!initialized){//terrainScript == null ){
//			Initialize();
//			//Debug.Log("Missing terrain script refrence " + initialized);
//			}
//		//to copy over all the list from the terrain generator, this is all effected parents and block positions
//		if(terrainScript.foundationUpdateGoList.Count == 0 ){
//			Debug.Log("Something wrong here");
//			}
//		
//		updateFoundationGo.AddRange(terrainScript.foundationUpdateGoList);
//		updateFoundationPos.AddRange(terrainScript.foundationUpdatePosList);
//		
//		updatingStrength = true;
//		//so for each entry just call updateStrength, length method since there is surely a faster way...
//		for(int x = 0; x < updateFoundationPos.Count; x++){
//			
//			
//			if(updateFoundationGo[x] == gameObject){//then call on our gameobject
//				yield return StartCoroutine(UpdateStrength(updateFoundationPos[x], false));
//				
//				}
//			else{//call on other object
//				blockScript = updateFoundationGo[x].GetComponent<BlockStrength>();
//				
//				yield return StartCoroutine(blockScript.UpdateStrength(updateFoundationPos[x], false));
//			
//				}
//		}
		
		//this this is the modified version that works on updating multiple blocks effected at once, be it through terrain
		//modification or through explosions removing tons of blocks
		updateFoundationGo.Clear();// = new List<GameObject>();
		updateFoundationPos.Clear();// = new List<Vector3>();
		
		if(!initialized){//terrainScript == null ){
			Initialize();
			//Debug.Log("Missing terrain script refrence " + initialized);
			}
		//to copy over all the list from the terrain generator, this is all effected parents and block positions
		updateFoundationGo.AddRange(terrainScript.foundationUpdateGoList);
		updateFoundationPos.AddRange(terrainScript.foundationUpdatePosList);
		//terrainScript.foundationUpdateGoList.Clear();
		//terrainScript.foundationUpdatePosList.Clear();
		
		checkStrGo.Clear();
		checkStrPos.Clear();
		//difference from removal method, we need to make sure we recheck all these blocks since they are not really removed like
		//it currently assumes...
		checkStrGo.AddRange(updateFoundationGo);
		checkStrPos.AddRange(updateFoundationPos);
		
		//so we have our list of effected positions...now we need to call missingstrength on each one
		//calling Missingpath clears the path and then adds any missing to chunkaround pos
		
		Debug.Log("Getting to update mult " + updateFoundationPos.Count);
		//Debug.Break();
		updatingStrength = true;
		
		//so now this could be our block or on another chunk....so this is where we would set the loop to go through 
		//all items collected on this list and form a master list of all blocks that need their strengths updated as a result
		
	//so this is our loop, we go through all the positions we were given from terrain generator and process them, then after all
	//the bad ones have been collected we go through and check all their strengths again...
	
		
//THIS WAS OLD SEMIFUNCTION METHOD TRYING NEW VERSION
		
//	for(int y = 0; y < updateFoundationPos.Count; y++){
//		
//		Debug.Log(updateFoundationPos[y] + " " + updateFoundationGo[y].transform.position + " " + transform.position);
//			
//		if(updateFoundationGo[y] == gameObject){//if this spot in question is ours
//		//we start by clearing this position to see what others rely on us...
//			Debug.DrawLine(updateFoundationGo[y].transform.position + updateFoundationPos[y], (updateFoundationGo[y].transform.position + updateFoundationPos[y] + (Vector3.up + Vector3.forward) * 1.2f), Color.magenta, 1.25f);
//			blockStr[(int)updateFoundationPos[y].x, (int)updateFoundationPos[y].y, (int)updateFoundationPos[y].z].Clear();
//			blockStr[(int)updateFoundationPos[y].x, (int)updateFoundationPos[y].y, (int)updateFoundationPos[y].z].Add(0);
//			}	
//		else{//this spot is on another chunk
//			Debug.DrawLine(updateFoundationGo[y].transform.position + updateFoundationPos[y], (updateFoundationGo[y].transform.position + updateFoundationPos[y] + (Vector3.up + Vector3.forward) * 1.2f), Color.magenta, 1.25f);
//			blockScript = updateFoundationGo[y].GetComponent<BlockStrength>() as BlockStrength;		
//			blockScript.blockStr[(int)updateFoundationPos[y].x, (int)updateFoundationPos[y].y, (int)updateFoundationPos[y].z].Clear();
//			blockScript.blockStr[(int)updateFoundationPos[y].x, (int)updateFoundationPos[y].y, (int)updateFoundationPos[y].z].Add(0);
//			}
//		
//		//so this check around path can be called for us or another chunk depending on where it is....
//		if(updateFoundationGo[y] = gameObject){//if this spot in question is ours
//			CheckAroundPath(gameObject, updateFoundationPos[y]);//check to see if our neighbors are missing paths because of us being cleared...
//			}	
//		else{//this spot is on another chunk
//			blockScript = updateFoundationGo[y].GetComponent<BlockStrength>() as BlockStrength;	
//			blockScript.CheckAroundPath(gameObject, updateFoundationPos[y]);//check to see if our neighbors are missing paths because of us being cleared...		
//			}
//				
//		
//		
//		//Debug.Log("Returning from check around will all effected " +  chunkAroundPos.Count);
//		gettingAllMissing = true;
//		/*
//		//this is kind of confusing, so everytime checkAroundPath is called it finds all blocks whos path to foundation is ruined by setting the
//			block to being zero(above).  If it finds any then it adds them to the chunkAroundPos list and checks these ones, which may add more, etc.
//			so if creeps through and slowly finds all blocks that are missing paths all the way through the structure...
//			
//		this is where we will loop it, checking each on teh list we were given from teh terrain generator, assembling and final long list of effected
//			blocks, then brute force rechecking their strengths...
//			
//		*/
//		for(int x = 0; x < chunkAroundPos.Count; x++){
//			
//			if(chunkAroundGo[x] == gameObject){//then we recheck ours
//				//Debug.DrawLine(chunkAroundGo[x].transform.position + chunkAroundPos[x], (chunkAroundGo[x].transform.position + chunkAroundPos[x] + (Vector3.up + Vector3.forward) * 1.2f), Color.magenta, 1.25f);
//				//THINK WE NEED TO CLEAR THESE SPOTS TO FIND OTHERS
//				blockStr[(int) chunkAroundPos[x].x,(int) chunkAroundPos[x].y, (int) chunkAroundPos[x].z].Clear();
//				blockStr[(int) chunkAroundPos[x].x,(int) chunkAroundPos[x].y, (int) chunkAroundPos[x].z].Add(0);
//				
//				CheckAroundPath(gameObject, chunkAroundPos[x]);
//				}
//			else{
//				//Debug.DrawLine(chunkAroundGo[x].transform.position + chunkAroundPos[x], (chunkAroundGo[x].transform.position + chunkAroundPos[x] + (Vector3.up + Vector3.forward) * 1.2f), Color.magenta, 1.25f);
//				blockScript = chunkAroundGo[x].GetComponent<BlockStrength>() as BlockStrength;
//				blockScript.doingPath = true;
//				blockScript.blockStr[(int) chunkAroundPos[x].x,(int) chunkAroundPos[x].y, (int) chunkAroundPos[x].z].Clear();
//				blockScript.blockStr[(int) chunkAroundPos[x].x,(int) chunkAroundPos[x].y, (int) chunkAroundPos[x].z].Add(0);
//				
//				blockScript.CheckAroundPath(gameObject, chunkAroundPos[x]);
//				
//				while(blockScript.doingPath){
//					yield return new WaitForSeconds(.001f);
//					}
//				}
//			
//			}
//			
//		}
		gettingAllMissing = false;
		
		//now we should have all blocks that have been effected by this removal	so we can start to find new strengths
		//Debug.Log("Found all missing marked with magenta" + checkStrGo.Count);
		//Debug.Break();
		otherUpdateChunks.Clear();	
		chunkAroundPos.Clear();
		chunkAroundGo.Clear();
		
		int totalCounter = 0;
		int higherStrFound;// = 0;
		
		//now go through the list of chunks that need to have their strength updated and make them recheck
		
		//right now it goes back and forth and keep checking parts, a total of 4 times
		//later modify this so that it goes back and forth but stop once it goes through an iteration and doesnt find any changes...
		//then it should probably be done...this is important since it will drastically reduce the checks by at least half
		
		// so in reality for multiple block removal just do one at a time,process everything, then remove the next block... 
		//there is surely a quicker way to do it, 
		
		//lets think, if we modify this to do all above code, and find all blocks effected by the removal, then go back and check all blocks 
		//that should work fine and probably result is much less for checks...
		
		//then we need to modify it so it stops checking once it does a strength check loop and doest find anything to update...
		
		
		
		checkingNewStrength = true;
		
		for(int x = 0; x < checkStrGo.Count; x++){
			
			totalCounter +=1;
			
			if(checkStrGo[x] == gameObject){
				BlockStr(checkStrPos[x], false);
				//bhange    ***** check whole function if need to change
				if(blockStr[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z].strength != 0 && chunkScript.vegeByte[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z] == 0){
					Debug.Log("Shit just got weird!");
					Debug.Break();
					}
				//Debug.DrawLine(transform.position + thisPosition, (transform.position + thisPosition + (Vector3.up + Vector3.right + Vector3.forward) * 1.2f), Color.yellow, 1.25f);	
					//Debug.DrawLine(transform.position + checkStrPos[x], (transform.position + checkStrPos[x] + -(Vector3.up + -Vector3.right + -Vector3.forward) * 1.2f), Color.red, 2);
					//}
				}
			else{
				if(terrainScript.strDebug){
					Debug.Log("Checking block on other chunk ");
					}
				blockScript = checkStrGo[x].GetComponent<BlockStrength>() as BlockStrength;
				blockScript.doingStrength = true;
				blockScript.BlockStr(checkStrPos[x], false);
				
				while(blockScript.doingStrength){
					yield return new WaitForSeconds(.001f);
					}
				
				if(terrainScript.strDebug){
					Debug.Log("Returning from the check on update str ");
					}
				
				if(!otherUpdateChunks.Contains(checkStrGo[x])){//if not already flagged for an update
					otherUpdateChunks.Add(checkStrGo[x]);
					}
				//if(blockScript.blockStr[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z][0] == 0){
					//Debug.DrawLine(transform.position + thisPosition, (transform.position + thisPosition + (Vector3.up + -Vector3.right + -Vector3.forward) * 1.2f), Color.red, 2);
					//}
				
				
				}
			
		//	if(terrainScript.debug){
			//	Debug.Break();
			//	yield return null;
			//	}
			
			if(totalCounter > 20){
				totalCounter = 0;
				yield return null;
				}
		
			}

		if(terrainScript.strDebug){
			Debug.Log("Getting through first set of strength checks ");
			}
				
		for(int x = checkStrGo.Count -1; x > -1; x--){
			
			totalCounter +=1;
			
			if(checkStrGo[x] == gameObject){
				BlockStr(checkStrPos[x], false);
				//if(blockStr[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z][0] == 0){
				//Debug.DrawLine(transform.position + thisPosition, (transform.position + thisPosition + (Vector3.up + Vector3.forward) * 1.2f), Color.green, 1.25f);	
					//Debug.DrawLine(transform.position + checkStrPos[x], (transform.position + checkStrPos[x] + -(Vector3.up + -Vector3.right + -Vector3.forward) * 1.2f), Color.red, 2);
					//}
				}
			else{
				if(terrainScript.strDebug){
					Debug.Log("Checking block on other chunk ");
					}
				blockScript = checkStrGo[x].GetComponent<BlockStrength>() as BlockStrength;
				blockScript.doingStrength = true;
				blockScript.BlockStr(checkStrPos[x], false);
				
				while(blockScript.doingStrength){
					yield return new WaitForSeconds(.001f);
					}
				
				if(terrainScript.strDebug){
					Debug.Log("Returning from the check on update str ");
					}
				
				if(!otherUpdateChunks.Contains(checkStrGo[x])){//if not already flagged for an update
					otherUpdateChunks.Add(checkStrGo[x]);
					}
				//if(blockScript.blockStr[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z][0] == 0){
					//Debug.DrawLine(transform.position + thisPosition, (transform.position + thisPosition + (Vector3.up + -Vector3.right + -Vector3.forward) * 1.2f), Color.red, 2);
					//}
				
				
				}
			
		//	if(terrainScript.strDebug){
			//	Debug.Break();
			//	yield return null;
			//	}
			
			if(totalCounter > 20){
				totalCounter = 0;
				yield return null;
				}
		
			}

		if(terrainScript.strDebug){
			Debug.Log("Getting through second set of strength checks ");
			}
		
		for(int x = 0; x < checkStrGo.Count; x++){
			
			if(checkStrGo[x] == gameObject){
				//bhange
				if(blockStr[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z].strength == 0){
				BlockStr(checkStrPos[x], false);
					if(blockStr[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z].strength == 0){
					//Debug.DrawLine(transform.position + checkStrPos[x], (transform.position + checkStrPos[x] + (Vector3.up + -Vector3.right) * 1.2f), Color.blue, 1.25f);
					}
				//if(terrainScript.debug){
				//Debug.Break();
				//yield return null;
				}
				}
				//}
			else{
				blockScript = checkStrGo[x].GetComponent<BlockStrength>() as BlockStrength;
				blockScript.doingStrength = true;
				if(blockScript.blockStr[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z].strength  == 0){
					blockScript.BlockStr(checkStrPos[x], false);
					
					while(blockScript.doingStrength){
					yield return new WaitForSeconds(.001f);
					}
				//if(terrainScript.debug){
				//Debug.Break();
				//yield return null;
				//}	
				}
				
				
				
				}
			
			if(totalCounter > 20){
				totalCounter = 0;
				yield return null;
				}
		
			}
		
		if(terrainScript.strDebug){
			Debug.Log("Getting through third set of strength checks ");
			}
		
		for(int x = checkStrGo.Count -1; x > -1; x--){
			
			if(checkStrGo[x] == gameObject){
				if(blockStr[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z].strength  == 0){
				BlockStr(checkStrPos[x], false);
					if(blockStr[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z].strength  == 0){
					Debug.DrawLine(transform.position + checkStrPos[x], (transform.position + checkStrPos[x] + (Vector3.up + -Vector3.right) * 1.2f), Color.blue, 1.25f);
					}
				//if(terrainScript.debug){
				//Debug.Break();
				//yield return null;
				//}
				}
				}
			else{
				blockScript = checkStrGo[x].GetComponent<BlockStrength>() as BlockStrength;
				blockScript.doingStrength = true;
				if(blockScript.blockStr[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z].strength  == 0){
				blockScript.BlockStr(checkStrPos[x], false);
					while(blockScript.doingStrength){
					yield return new WaitForSeconds(.001f);
					}
				//if(terrainScript.debug){
				//Debug.Break();
				//yield return null;
				//}	
				}
				
				
				
				}
			
			if(totalCounter > 20){
				totalCounter = 0;
				yield return null;
				}
			
		
			}
		
		//so here we go through all the remaining with no strength, then we finalize them by setting their values to air
		//MOVED TO DOING THIS ON THE UPDATEMESH METHOD...IF IT GETS THAT FAR WITH NO STRENGTH THEN REMOVE IT
		/*
		for(int x = 0; x < checkStrGo.Count; x++){
			
			if(checkStrGo[x] == gameObject){
				if(blockStr[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z][0] == 0){
					chunkScript.vegeByte[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z] = 0;
				//BlockStr(checkStrPos[x], false);
				}
				}
			else{
				blockScript = checkStrGo[x].GetComponent<BlockStrength>() as BlockStrength;
				
				if(blockScript.blockStr[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z][0] == 0){
					chunkScript.vegeByte[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z] = 0;
					//blockScript.doingStrength = true;
					//blockScript.BlockStr(checkStrPos[x], false);
				
					//while(blockScript.doingStrength){
					//	yield return new WaitForSeconds(.001f); 
					//}
				}
				
				}
			
		
			}
		
		*/
		
		
		if(terrainScript.strDebug){
			Debug.Log("Getting through fourth set of strength checks ");
			}
			checkingNewStrength = false;	
		
			
		//if(terrainScript.debug){
				//Debug.Break();
				//yield return null;
				//}
		/*
		for(int x = checkStrGo.Count -1; x > -1; x--){
			
			if(checkStrGo[x] == gameObject){
				if(blockStr[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z][0] == 0){
				BlockStr(checkStrPos[x], false);
				}
				}
			else{
				blockScript = checkStrGo[x].GetComponent<BlockStrength>() as BlockStrength;
				
				if(blockScript.blockStr[(int)checkStrPos[x].x, (int)checkStrPos[x].y, (int)checkStrPos[x].z][0] == 0){
					blockScript.doingStrength = true;
					blockScript.BlockStr(checkStrPos[x], false);
				
					while(blockScript.doingStrength){
						yield return new WaitForSeconds(.001f); 
					}
				}
				
				}
			
		
			}
		*/
		
		for(int x = checkStrGo.Count -1; x > -1; x--){
			//so if our working list of effected gameobjects is not on the re-render list, then add it
			if (!terrainScript.foundationUpdateGoList.Contains(checkStrGo[x])){
				terrainScript.foundationUpdateGoList.Add(checkStrGo[x]);
				}
			
			}
		//terrainScript.initialChunkUpdate = null;
		//terrainScript.fou
		Debug.Log("FINSIHED DOING MULTI METHOD " + checkStrGo.Count);
		//Debug.Break();
		
		// this is the full listing of gos that may be affected instead of original foundation list
		//so we should let terrain gen sort this list, pull all parents and clear them
		//checkStrPos.Clear();
		//checkStrGo.Clear();
		
		
		updatingStrength = false;
		//yield return null;
	}
 
	
public void CheckAroundPath(GameObject caller, Vector3 thisPosition){
	
	//probably need to cleanup these local variables for garbage collection
	//Debug.Log("Called from " + caller.transform.position);
	doingPath = true;
	//thisChunk;
	BlockStrength thisBlockScript;
	int firstPath;
	Vector3 combined;


	//this function fills a 6-slot array (1 for each position around us) one for parents, and one for positions
	//
	GetAroundInfo(thisPosition);

	if(terrainScript.strDebug){
	//Debug.Log("Pausing after getting around info for " + caller.transform.position + " " + thisPosition);
	//terrainScript.DebugCube(thisPosition, transform.position, 1.1, Color.yellow, .1f);
	//Debug.Break();
	}
			
	for(int x = 0; x < 6; x++){
			
	//	if(terrainScript.strDebug){

	//so this is visual debugging for around checks and what information we are finding 

//		if(chunkParent[x] == gameObject){//checking between two blocks we own	
//			if(blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][0] != 0){
//				terrainScript.DebugCube(chunkPosition[x],chunkParent[x].transform.position, 1, Color.green, .1f);
//				}
//			else{
//				terrainScript.DebugCube(chunkPosition[x],chunkParent[x].transform.position, 1, Color.blue, .1f);	
//				}
//			}
//		else{
//			thisBlockScript = chunkParent[x].GetComponent<BlockStrength>() as BlockStrength;	
//			//if (	
//			if(thisBlockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][0] != 0){
//				terrainScript.DebugCube(chunkPosition[x],chunkParent[x].transform.position, 1, Color.green, .1f);
//				}
//			else{
//				terrainScript.DebugCube(chunkPosition[x],chunkParent[x].transform.position, 1, Color.green, .1f);
//				}
//			}
	//		}
		
		
		if(chunkParent[x] == gameObject){//checking against block we own
				
			//terrainScript.DebugCube(chunkPosition[x],transform.position, 1, Color.red, .1f);
			//bhange
				if(blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z].strength == 0){
					
					continue;//if the block has no strength then there is nothing to check
				}
				if(blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z].strength == 8 && blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z].length == 0){
					
					continue;//if the block is foundation no need to check
				}
//			if(blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][0] == 0){
//				
//				continue;//if the block has no strength then there is nothing to check
//				}
//			if(blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][0] == 8 && blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][1] == 0){
//				
//				continue;//if the block is foundation no need to check
//				}
			
			//so blocks that are foundation should not be checked...if 8 strength and zero path, they are connected ot the foundation and dont need checking...
			//bhange
				if(blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z].strength == 8){
					//so if stacked upon foundation will be ex: 8,4 - 4 is the number of moves down...so the first path is down instead of what the numbers say...
					firstPath = 1;//for down
				}
				else{
					//so now after changed to blockData - firstPath is just .direction
					firstPath = blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z].direction;//[blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z].Count -1];
				}
//			if(blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][0] == 8){
//				//so if stacked upon foundation will be ex: 8,4 - 4 is the number of moves down...so the first path is down instead of what the numbers say...
//				firstPath = 1;//for down
//				}
//			else{		
//				firstPath = blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z].Count -1];
//				}
			//firstPath is actually the last on the list
			//Debug.Log(firstPath + " " + chunkPosition[x]);
			combined = chunkPosition[x] + posAroundBlock[firstPath];//position we are checking around us
			chunkOffset = PositionOfSpot(ref combined); //finding who owns this first path spot 
			//is this returns Vector3.zero then the position is within our chunk...
				
			if(chunkOffset == Vector3.zero){//then this position is within our block 
				//bhange
				if(blockStr[(int)combined.x, (int) combined.y, (int) combined.z].strength == 0){//then our first path is null and we need to be rechecked

				//if(blockStr[(int)combined.x, (int) combined.y, (int) combined.z][0] == 0){//then our first path is null and we need to be rechecked
					
					if(caller == gameObject){//then we add to our own list
						//this was causing problems, since a chunk can have the same position just a different parent on tall buildings...
						//probably beter just adding everything then doing linq distinct method...that way we are only searching through everything once...
							
						if(!chunkAroundPos.Contains(chunkPosition[x])){
							chunkAroundPos.Add(chunkPosition[x]);//this is list for blocks that need to check around themselves
							chunkAroundGo.Add(chunkParent[x]);
							checkStrPos.Add(chunkPosition[x]);///later this is for when we recheck the strength for these blocks
							checkStrGo.Add(chunkParent[x]);
								
							}
						else{
							//tricky dicky...so we need to go through the list and find this position, and see if the parent at this index is different...
								
							}
						
						}
					else{//this is not called from our gameobject, so add to their lists
						
						thisBlockScript = caller.GetComponent<BlockStrength>() as BlockStrength;
						if(!thisBlockScript.chunkAroundPos.Contains(chunkPosition[x])){
							thisBlockScript.chunkAroundPos.Add(chunkPosition[x]);//this is list for blocks that need to check around themselves
							thisBlockScript.chunkAroundGo.Add(chunkParent[x]);
							thisBlockScript.checkStrPos.Add(chunkPosition[x]);///later this is for when we recheck the strength for these blocks
							thisBlockScript.checkStrGo.Add(chunkParent[x]);	
							}
						}
						
					}
				else{ 
					//terrainScript.DebugCube(chunkPosition[x],transform.position, 1.2f, Color.yellow, .1f);
					//Debug.Log("Finding path " + chunkPosition[x] + " " + combined + " " + blockStr[(int)combined.x, (int) combined.y, (int) combined.z][0]);
					//Debug showing that this still has a path	
					//Debug.DrawLine(transform.position + combined + Vector3.right, (transform.position + combined + (Vector3.up + -Vector3.right + Vector3.forward) * 1.2f), Color.red, 1.25f);	
					}
					
					
					
				}
			else{//this position is not within our chunk
				Vector3 tempPos = chunkScript.GeneratorArrayPosition + chunkOffset;//temp position, makes less code
				thisChunk = terrainScript.renderChunks[(int)tempPos.x, (int)tempPos.y,  (int)tempPos.z];

				
				thisBlockScript = thisChunk.GetComponent<BlockStrength>() as BlockStrength;	
					
				tempPos =((combined * 3) + transform.position - thisChunk.transform.position)/3;//this converts to local to this other chunk
					//bhange
                    Debug.Log("tempPos " + tempPos + " " + thisChunk.transform.position + " " + transform.position + " " + combined);
                    Debug.Break();
                    if(thisBlockScript.blockStr[(int)tempPos.x, (int)tempPos.y,  (int)tempPos.z].strength == 0){//then this is missing path
					//if(thisBlockScript.blockStr[(int)tempPos.x, (int)tempPos.y,  (int)tempPos.z][0] == 0){//then this is missing path
						//terrainScript.DebugCube(chunkPosition[x], chunkParent[x].transform.position, 1.2f, Color.red, .1f);
						if(caller == gameObject){//then we add to our own list
						chunkAroundPos.Add(chunkPosition[x]);//this is list for blocks that need to check around themselves
						chunkAroundGo.Add(chunkParent[x]);
						checkStrPos.Add(chunkPosition[x]);///later this is for when we recheck the strength for these blocks
						checkStrGo.Add(chunkParent[x]);
						}
						else{//this is not called from our gameobject, so add to their lists
						thisBlockScript = caller.GetComponent<BlockStrength>() as BlockStrength;
						thisBlockScript.chunkAroundPos.Add(chunkPosition[x]);//this is list for blocks that need to check around themselves
						thisBlockScript.chunkAroundGo.Add(chunkParent[x]);
						thisBlockScript.checkStrPos.Add(chunkPosition[x]);///later this is for when we recheck the strength for these blocks
						thisBlockScript.checkStrGo.Add(chunkParent[x]);	
						}
					}
					else{
						//terrainScript.DebugCube(chunkPosition[x], chunkParent[x].transform.position, 1.2f, Color.yellow, .1f);
						Debug.Log("Finding path this pos " + tempPos + " has path " + thisBlockScript.blockStr[(int)tempPos.x, (int)tempPos.y,  (int)tempPos.z].strength);
						}
				}
				//Debug.Break();
				
				
			}
		else{//gets even stranger - now checking between two blocks that are not ours?
				/*
				So there is where the problem must be, we are not finding invalid paths, when checking between two chunks we dont own
				*/
				thisBlockScript = chunkParent[x].GetComponent<BlockStrength>() as BlockStrength; 
			//	Debug.Log("SO original gen array position " + chunkScript.gameObject.transform.position + " " + chunkScript.GeneratorArrayPosition);

			//	Debug.Log(chunkParent[x].transform.position + " " + x + " " + chunkPosition[x] + " " + thisBlockScript.initialized);
			//	Debug.Log("This position we are checking around " + thisPosition);
				//bhange
				if(thisBlockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z].strength == 0){
					continue;//if the block has no strength then there is nothing to check
				}
				if(thisBlockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z].strength == 8 && thisBlockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z].length == 0){
					continue;//if the block has no strength then there is nothing to check
				}
				
				if(thisBlockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z].strength == 8){// && thisBlockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][1] != 0){
					//so if stacked upon foundation will be ex: 8,4 - 4 is the number of moves down...so the first path is down instead of what the numbers say...
					firstPath = 1;//for down
				}
				else{		
					firstPath = thisBlockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z].direction;//[thisBlockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z].Count -1];
				}
//				if(thisBlockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][0] == 0){
//					continue;//if the block has no strength then there is nothing to check
//					}
//				if(thisBlockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][0] == 8 && thisBlockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][1] == 0){
//					continue;//if the block has no strength then there is nothing to check
//					}
//				
//				if(thisBlockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][0] == 8){// && thisBlockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][1] != 0){
//					//so if stacked upon foundation will be ex: 8,4 - 4 is the number of moves down...so the first path is down instead of what the numbers say...
//					firstPath = 1;//for down
//					}
//				else{		
//					firstPath = thisBlockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z][thisBlockScript.blockStr[(int)chunkPosition[x].x, (int)chunkPosition[x].y, (int)chunkPosition[x].z].Count -1];
//					}
			
				if(x > 5 || firstPath > 5){
					Debug.Log("Going to get a NRE " + x + " " + firstPath);
					}
				combined = chunkPosition[x] + posAroundBlock[firstPath];
				chunkOffset = PositionOfSpot(ref combined); //finding who owns this first path spot

			//	Debug.Log("This is our chunkOffset " + chunkOffset);
				//Debug.Break();
				//yield return null;
				
			if(chunkOffset == Vector3.zero){//then this position is within this other chunk
					
				//bhange
				if(thisBlockScript.blockStr[(int)combined.x, (int) combined.y, (int) combined.z].strength == 0){//then our first path is null and we need to be rechecked
				//if(thisBlockScript.blockStr[(int)combined.x, (int) combined.y, (int) combined.z][0] == 0){//then our first path is null and we need to be rechecked
				//	Debug.Log("Within other chunk");
					//terrainScript.DebugCube(chunkPosition[x],chunkParent[x].transform.position, 1, Color.red, .1f);
					if(caller = gameObject){//then we add to our own list
					//	Debug.Log("we called this");
						chunkAroundPos.Add(chunkPosition[x]);//this is list for blocks that need to check around themselves
						chunkAroundGo.Add(chunkParent[x]);
						checkStrPos.Add(chunkPosition[x]);///later this is for when we recheck the strength for these blocks
						checkStrGo.Add(chunkParent[x]);
						}
						else{//this is not called from our gameobject, so add to their lists
					//	Debug.Log("Called on another go");
						thisBlockScript = caller.GetComponent<BlockStrength>() as BlockStrength;
						thisBlockScript.chunkAroundPos.Add(chunkPosition[x]);//this is list for blocks that need to check around themselves
						thisBlockScript.chunkAroundGo.Add(chunkParent[x]);
						thisBlockScript.checkStrPos.Add(chunkPosition[x]);///later this is for when we recheck the strength for these blocks
						thisBlockScript.checkStrGo.Add(chunkParent[x]);	
						}
						
					}
					else{
						//terrainScript.DebugCube(chunkPosition[x],chunkParent[x].transform.position, 1, Color.yellow, .1f);	
						//Debug.Log("Finding path " + chunkPosition[x] + " " + combined + " " + thisBlockScript.blockStr[(int)combined.x, (int) combined.y, (int) combined.z][0]);
						}
					
					
					
				}
			else{//this position is not within the other chunk
								
				ChunkScript otherScript = chunkParent[x].GetComponent<ChunkScript>() as ChunkScript;
				//Vector3 anotherGenPos = otherScript.GeneratorArrayPosition;
				Vector3 tempPos = otherScript.GeneratorArrayPosition + chunkOffset;//this gives us the position of the location of the block that is our first path
					
				thisChunk = terrainScript.renderChunks[(int)tempPos.x, (int)tempPos.y,  (int)tempPos.z];//so this the gameobject that owns this first path locaton
				//thisChunk = terrainScript.renderChunks[(int)tempPos.x, (int)tempPos.y,  (int)tempPos.z];
				thisBlockScript = thisChunk.GetComponent<BlockStrength>() as BlockStrength;	
				//Debug.Log("NOT Within other chunk " + chunkPosition[x] + " " + chunkParent[x].transform.position);	
				//Debug.Log("finding this chunk to check " + terrainScript.renderChunks[(int)tempPos.x, (int)tempPos.y,  (int)tempPos.z].transform.position);
				//Debug.Log(" Check these " + tempPos);
				
				//Debug.Break(); 
				
				//Debug.Log("Checkig this as path - spot " +  chunkPosition[x] + " on " + terrainScript.renderChunks[(int)tempPos.x, (int)tempPos.y,  (int)tempPos.z].transform.position);
				//this chunkPosition is not checking where it should be checking...
				//it chunkPosition is the location of the block in question, when combined is the location before converted locally...
				//so conversion would be combined + chunkParent[x].transform.position - terrainScript.renderChunks[(int)tempPos.x, (int)tempPos.y,  (int)tempPos.z].transform.position;
				//Debug.Log("Using as first path... " + (combined + chunkParent[x].transform.position - terrainScript.renderChunks[(int)tempPos.x, (int)tempPos.y,  (int)tempPos.z].transform.position) + " on " + thisChunk);
                tempPos =  ((combined * 3) + chunkParent[x].transform.position - thisChunk.transform.position)/3;//chunkPosition[x];//combined + transform.position - thisChunk.transform.position;//this converts to local to this other chunk

                Debug.Log("Taking final point * 3 " + (combined * 3) + "added to parent " + thisChunk.transform.position + " then subtract out final chunk " + chunkParent[x].transform.position); 
                    Debug.Log("So this should be the same as thisChunk " + chunkPosition[x]);
                    /*
                    if(tempPos.x > 15){
						
						tempPos.x = Mathf.FloorToInt(tempPos.x/3);
						//Debug.Log("Combined local " + combinedLocal.x);
					}
					if(tempPos.y > 15){
						tempPos.y = Mathf.FloorToInt(tempPos.y/3);
						
					}
					if(tempPos.z > 15){
						tempPos.z = Mathf.FloorToInt(tempPos.z/3);
						
					}
					
					if(tempPos.x < 0){
						
						tempPos.x = 0;
						//Debug.Log("Combined local " + combinedLocal.x);
					}
					if(tempPos.y < 0){
						tempPos.y = 0;
						
					}
					if(tempPos.z > 15){
						tempPos.z = 0;
						
					}
                    */

					//Debug.Log(tempPos + " " + x + " " + thisChunk + " " + chunkPosition[x] + " " + chunkParent[x]);
					//bhange
					Debug.Log("Around check on other chunk " + chunkPosition[x] + " " + chunkParent[x].transform.position);//
                    //so these positions are right about at this debug, somethign is gosing wrong though
					Debug.Log("Checking this position  " + tempPos + " position on " + thisChunk.transform.position + " " + combined);	
                    Debug.Log("Should this be the stop " + (tempPos/3) + " original " + combined);
					if(thisBlockScript.blockStr[(int)tempPos.x, (int)tempPos.y,  (int)tempPos.z].strength == 0){//then this is missing path
					//if(thisBlockScript.blockStr[(int)tempPos.x, (int)tempPos.y,  (int)tempPos.z][0] == 0){//then this is missing path
						//terrainScript.DebugCube(chunkPosition[x],chunkParent[x].transform.position, 1, Color.red, .1f);
						Debug.Log("This path has no strength and should be added");
						Debug.Log(thisBlockScript.blockStr[(int)tempPos.x, (int)tempPos.y,  (int)tempPos.z].strength + " " + thisBlockScript.blockStr[(int)tempPos.x, (int)tempPos.y,  (int)tempPos.z].direction);
						if(caller == gameObject){//then we add to our own list
							Debug.Log(" CAller position " + caller.transform.position);
							Debug.Log("Adding to ourselves" + gameObject.transform.position + " at " + chunkPosition[x] + " on " + chunkParent[x].transform.position);
							chunkAroundPos.Add(chunkPosition[x]);//this is list for blocks that need to check around themselves
							chunkAroundGo.Add(chunkParent[x]);
							checkStrPos.Add(chunkPosition[x]);///later this is for when we recheck the strength for these blocks
							checkStrGo.Add(chunkParent[x]);
						}
						else{//this is not called from our gameobject, so add to their lists
							Debug.Log("This path has no strength and should be added");
							Debug.Log("Adding  to other list " + caller.transform.position + " at " + chunkPosition[x] + " on " + chunkParent[x].transform.position);
							thisBlockScript = caller.GetComponent<BlockStrength>() as BlockStrength;
							thisBlockScript.chunkAroundPos.Add(chunkPosition[x]);//this is list for blocks that need to check around themselves
							thisBlockScript.chunkAroundGo.Add(chunkParent[x]);
							thisBlockScript.checkStrPos.Add(chunkPosition[x]);///later this is for when we recheck the strength for these blocks
							thisBlockScript.checkStrGo.Add(chunkParent[x]);	
						}
					}
					else{
						//terrainScript.DebugCube(chunkPosition[x],chunkParent[x].transform.position, 1, Color.yellow, .1f);	
						Debug.Log("Finding path from "  + chunkPosition[x] + " to " + tempPos + " in direction of " + thisBlockScript.blockStr[(int)tempPos.x, (int)tempPos.y,  (int)tempPos.z].direction + " with strength of " + thisBlockScript.blockStr[(int)tempPos.x, (int)tempPos.y,  (int)tempPos.z].strength);
						}
				}
				
				
				
			}
			
		}
		
	//terrainScript.DebugCube(thisPosition,transform.position, 1.1, Color.magenta, .15f);
	//Debug.Break();	
		
	doingPath = false;
	}
	
public Vector3 PositionOfSpot(ref Vector3 position){
	//checks if our spot is inside our chunk - returns vector3.zero
	//if ourside of chunk returns posVector not Vector3.zero
		//can use this in terrainScript.renderChunks[GeneratorArrayPosition + posVector]
		//so get the chunk it belongs to
		Vector3 posVector = Vector3.zero;
		
		if(position.x > 15){
		posVector += Vector3.right;
		}
		if(position.x < 0){
		posVector += -Vector3.right;
		}
		if(position.y > 15){
		posVector += Vector3.up;
		}
		if(position.y < 0){
		posVector += -Vector3.up;
		}
		if(position.z > 15){
		posVector += Vector3.forward;
		}
		if(position.z < 0){
		posVector += -Vector3.forward;
		}

		//Debug.Log(" This position " + position + " and " + posVector);

		return posVector;

}
}

public class BlockInfo{

	public Vector3 blockPosition;
	public GameObject blockParent;

	public BlockInfo(Vector3 pos,GameObject parent){
		blockPosition = pos;
		blockParent = parent;
		}



}
	

