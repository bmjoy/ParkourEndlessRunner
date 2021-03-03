﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Generates a level
/// </summary>
public class LevelGenerator : MonoBehaviour
{       
    /// <summary>
    /// The width of a lane
    /// </summary>
    public float m_laneWidth = 2;
    /// <summary>
    /// The height of a tile
    /// </summary>
    public float m_tileHeight = 2;
    /// <summary>
    /// The length of a tile
    /// </summary>
    public float m_tileLength = 7;
    /// <summary>
    /// The number of lanes
    /// </summary>
    public uint m_numberOfLanes = 3;
    /// <summary>
    /// The number of layers
    /// </summary>
    public uint m_numberOfLayers = 3;
    /// <summary>
    /// The maximum change in height
    /// </summary>
    public uint m_maxHeightChange = 3;
    /// <summary>
    /// The length of the level. Currently used for debugging only
    /// </summary>
    public int m_levelLengthDEBUG = 20;
    /// <summary>
    /// The prefab to use for general tiles
    /// </summary>
    public GameObject m_tilePrefab = null;
    /// <summary>
    /// The prefab used for slopes
    /// </summary>
    public GameObject m_slopePrefab = null;
    /// <summary>
    /// The positional offset of the level
    /// </summary>
    public Vector3 m_generateOffset = new Vector3();
    /// <summary>
    /// A list of all active tiles for debugging purposes
    /// </summary>
    public List<TileInfo> m_tiles = new List<TileInfo>();
    /// <summary>
    /// The min number of tiles between obstacle spawns
    /// </summary>
    public uint minSpaceBetweenObstacles = 2;
    /// <summary>
    /// The max number of tiles between obstacle spawns
    /// </summary>
    public uint maxSpaceBetweenObstacles = 4;
    /// <summary>
    /// All of the obstacles you want to be able to spawn
    /// </summary>
    public GameObject[] obstacles;
    /// <summary>
    /// The probability for the height of a lane to change
    /// </summary>
    [Range(0,1)]
    public float m_probabilityToChangeHeight = 0.1f;
    /// <summary>
    /// The probability for a ramp to spawn even if its not needed
    /// </summary>
    [Range(0,1)]
    public float m_probabilityForNonRequiredRamps = 0.5f;

    private uint[] laneObstacleTimer = new uint[0];
    private int m_currentLength = 0;
    /// <summary>
    /// Initalises some variables
    /// </summary>
    private void Awake()
    {
        m_currentLength = 0;
    }
    /// <summary>
    /// Generates a level upon starting the game for debugging purposes
    /// </summary>
    private void Start()
    {
        GenerateLevel((uint)m_levelLengthDEBUG);
    }
    /// <summary>
    /// Generates the world
    /// </summary>
    [ContextMenu("Regenerate Level")]
    void CreateTestLevel()
    {
        GenerateLevel((uint)m_levelLengthDEBUG);
    }

    [ContextMenu("Extend Level")]
    void ExtendTestLevel()
    {
        GenerateLevel((uint)m_levelLengthDEBUG, false);
    }
    /// <summary>
    /// Generates a section of the world
    /// </summary>
    /// <param name="layersToGenerate">The number of layers to generate</param>
    /// <param name="regenerate">Wether or not the level should be completely rengerated or if new layers should be added</param>
    void GenerateLevel(uint layersToGenerate, bool regenerate = true)
    {   //If we already have tiles and are trying to generate more, delete the previous ones. Primarily for debugging
        if (regenerate && m_tiles.Count != 0)
        {
            m_currentLength = 0;
            DeleteLevel();
        }
        if (laneObstacleTimer.Length != m_numberOfLanes)
            laneObstacleTimer = new uint[m_numberOfLanes];

        for (int lane = 0; lane < m_numberOfLanes; lane++)
            laneObstacleTimer[lane] = (uint)Random.Range((int)minSpaceBetweenObstacles, (int)maxSpaceBetweenObstacles);

        GameObject obstacle = null;
        bool canChangeHeight;
        int[] heights = new int[m_numberOfLanes];
        int[] prevHeights = new int[m_numberOfLanes];

        for (int i = m_currentLength; i < layersToGenerate + m_currentLength; i++)
        {
            for (int lane = 0; lane < m_numberOfLanes; lane++)
            {
                TileInfo tile = new TileInfo();
                //Initialise it to nothing
                TileInfo prevTile = new TileInfo();
                //Calculate if the height of this tile should be randomized
                float prob = Random.Range(0, (float)1);
                canChangeHeight = prob < m_probabilityToChangeHeight;
                int prevTileHeight = 0;

                if (i == 0)
                    canChangeHeight = true;
                else
                {   //Now we have passed the first row, we can propperly initialise the tile
                    prevTile = m_tiles[((i - 1) * (int)m_numberOfLanes) + lane];
                    prevTileHeight = (int)prevTile.Height;
                }

                prevHeights[lane] = prevTileHeight;

                int min, max;
                min = prevTileHeight - (int)m_maxHeightChange < 0 ? 0 : prevTileHeight - (int)m_maxHeightChange;
                max = prevTileHeight + (int)m_maxHeightChange >= m_numberOfLayers ? (int)m_numberOfLayers - 1 : prevTileHeight + (int)m_maxHeightChange;

                if (prevTile.IsRamp)
                    canChangeHeight = false;

                tile.Initialise(ref obstacle, (uint)lane, canChangeHeight ? (uint)Random.Range(min, max + 1) : (uint)prevTileHeight, (uint)i, false);
                heights[lane] = (int)tile.Height;
                m_tiles.Add(tile);
            }
            //We don't need to solve for the very first tiles since they have no previous tiles
            if (i != 0)
                //Solve to make sure there are no death walls
                for (int lane = 0; lane < m_numberOfLanes; lane++)
                {
                    int validLanes = 0;
                    int heightChange = heights[lane] - prevHeights[lane];
                    //If there is no height change or the height change went downwards, we don't need to solve for anything
                    if (heightChange <= 0)
                        continue;
                    //We know the rest now are height changes upwards.
                    //Check if there is a valid path in the left or right lane
                    //Make sure we aren't reading invalid memory
                                     //Check if the left lane has a height equal to or less than our current height
                                                                                    //Check that the change in height of the left lane is pathable
                    if (lane - 1 >= 0 && prevHeights[lane - 1] <= prevHeights[lane] && heights[lane - 1] - prevHeights[lane - 1] <= 0)
                        validLanes++;
                    //Repeat for the right side
                    if (lane + 1 < m_numberOfLanes && prevHeights[lane + 1] <= prevHeights[lane] && heights[lane + 1] - prevHeights[lane + 1] <= 0)
                        validLanes++;

                    TileInfo current = m_tiles[i * (int)m_numberOfLanes + lane];
                    //If not, we make this tile a ramp and reduce its height
                    if (validLanes == 0)
                    {
                        current.IsRamp = true;
                        //Reduce the height
                        current.Height = (uint)prevHeights[lane] + 1;

                        //We also reduce it here sp that, when we are checking for valid lanes, a lane with a ramp 
                        //will be treated as having equal height so it can be counted as a valid lane
                        heights[lane] = prevHeights[lane];
                    }
                    //Even if there is a valid path, if the heightChange is only 1, roll to convert it into a ramp
                    else if (heightChange == 1 && Random.Range(0, (float)1) <= m_probabilityForNonRequiredRamps)
                    {
                        current.IsRamp = true;
                        //We also reduce it here sp that, when we are checking for valid lanes, a lane with a ramp 
                        //will be treated as having equal height so it can be counted as a valid lane
                        heights[lane]--;
                    }
                    m_tiles[i * (int)m_numberOfLanes + lane] = current;
                }
            //Instantiate any obstacles to slide or vault over
            //Make sure we have obstacles
            if (obstacles.Length != 0)
                //Decrement the timers if they haven't already reached 0
                for (int lane = 0; lane < m_numberOfLanes; lane++)
                {
                    int currentTile = i * (int)m_numberOfLanes + lane;
                    if (m_tiles[currentTile].IsRamp)
                        continue;

                    if (laneObstacleTimer[lane] != 0)
                        laneObstacleTimer[lane]--;
                    //If they have reached 0, check if we can spawn an obstacle on that tile
                    else
                    {
                        //Select a random obstacle to spawn
                        obstacle = obstacles[Random.Range(0, obstacles.Length)];
                        TileInfo tile = m_tiles[currentTile];
                        tile.AddObstacle(obstacle, m_laneWidth, m_tileHeight, m_tileLength, m_generateOffset);

                        obstacle = null;

                        m_tiles[currentTile] = tile;
                        //Reset the timer
                        laneObstacleTimer[lane] = (uint)Random.Range((int)minSpaceBetweenObstacles, (int)maxSpaceBetweenObstacles);
                    }
                }

            //Actually generate the lanes boxes and stuff
            for (int lane = 0; lane < m_numberOfLanes; lane++)
                m_tiles[i * (int)m_numberOfLanes + lane].GenerateTiles(m_tilePrefab, m_slopePrefab, m_laneWidth, m_tileHeight, m_tileLength, m_generateOffset);
        }

        m_currentLength += (int)layersToGenerate;
    }
    /// <summary>
    /// Delets the level
    /// </summary>
    [ContextMenu("Delete Level")]
    void DeleteLevel()
    {
        for (int i = 0; i < m_tiles.Count; i++)
            for (int objects = 0; objects < m_tiles[i].m_objectsOnTile.Length; objects++)
                DestroyImmediate(m_tiles[i].m_objectsOnTile[objects]);
        m_tiles.Clear();
    }
}
/// <summary>
/// Stores information about tiles
/// </summary>
[System.Serializable]
public struct TileInfo
{   /// <summary>
    /// Stores the objects on this tile for ease of access for deleting
    /// </summary>
    public GameObject[] m_objectsOnTile;
    /// <summary>
    /// The lane this tile exists on
    /// </summary>
    public uint m_lane;
    /// <summary>
    /// The height of this tile
    /// </summary>
    private uint m_height;
    /// <summary>
    /// The Z location of the tile
    /// </summary>
    public uint m_forwardPoint;
    /// <summary>
    /// Is the tile a ramp
    /// </summary>
    private bool m_isRamp;
    /// <summary>
    /// Was this a tile that has been propperly generated
    /// </summary>
    public bool isValid;
    /// <summary>
    /// Gets or sets the height
    /// </summary>
    public uint Height
    {
        get { return m_height; }
        set
        {
            m_height = value;
        }
    }

    public bool IsRamp
    {
        get { return m_isRamp; }
        set
        {
            m_isRamp = value;
            GameObject obstacle = m_objectsOnTile[m_objectsOnTile.Length - 1];
            int length = m_isRamp ? 2 : 1;
            length += obstacle != null ? 1 : 0;
            m_objectsOnTile = new GameObject[length];
            m_objectsOnTile[length - 1] = obstacle;
        }
    }
    /// <summary>
    /// Initalises the tile
    /// </summary>
    /// <param name="obstacle">The obstacle on this tile</param>
    /// <param name="lane">The lane this tile is in</param>
    /// <param name="height">The height of this tile</param>
    /// <param name="forwardPoint">The z step of this tile</param>
    /// <param name="isRamp">If this tile is a ramp</param>
    public void Initialise(ref GameObject obstacle, uint lane, uint height, uint forwardPoint, bool isRamp)
    {
        int length = isRamp ? 2 : 1;
        length += obstacle != null ? 1 : 0;
        m_objectsOnTile = new GameObject[length];
        m_objectsOnTile[length - 1] = obstacle;
        m_lane = lane;
        m_height = height;
        m_forwardPoint = forwardPoint;
        isValid = true;
        m_isRamp = isRamp;
    }
    /// <summary>
    /// Generates the tile based on the information given in the initialiser
    /// </summary>
    /// <param name="tilePrefab">The prefab for standard ground</param>
    /// <param name="tileSlope">The prefab for slopes</param>
    /// <param name="laneWidth">The width of a tile</param>
    /// <param name="tileHeight">The height of a tile</param>
    /// <param name="tileLength">The length of a tile</param>
    /// <param name="posOffset">The position offset of the tile from the origin</param>
    public void GenerateTiles(GameObject tilePrefab, GameObject tileSlope, float laneWidth, float tileHeight, float tileLength, Vector3 posOffset)
    {
        GameObject obj;
        //Generate the regular cube for the ground
        obj = GameObject.Instantiate(tilePrefab, new Vector3(laneWidth * m_lane + posOffset.x, ((float)(m_isRamp ? m_height - 1 : m_height) / 2) * tileHeight + posOffset.y, m_forwardPoint * tileLength + posOffset.z), Quaternion.identity);
        obj.transform.localScale = new Vector3(laneWidth, tileHeight * (m_isRamp ? m_height : m_height + 1), tileLength);

        m_objectsOnTile[0] = obj;
        //If we have a ramp, create it
        if (m_isRamp)
        {
            obj = GameObject.Instantiate(tileSlope, new Vector3(laneWidth * m_lane + posOffset.x, m_height * tileHeight + posOffset.y, m_forwardPoint * tileLength + posOffset.z), Quaternion.identity);
            obj.transform.localScale = new Vector3(laneWidth, tileHeight, tileLength);

            m_objectsOnTile[1] = obj;
        }
    }
    /// <summary>
    /// Instantiates and stores an obstacle gameObject on this tile
    /// </summary>
    /// <param name="obstacle">The obstacle to create and add</param>
    public void AddObstacle(GameObject obstacle, float laneWidth, float tileHeight, float tileLength, Vector3 posOffset)
    {
        int length = m_isRamp ? 2 : 1;
        length += obstacle != null ? 1 : 0;
        m_objectsOnTile = new GameObject[length];
        GameObject obj = GameObject.Instantiate(obstacle, new Vector3(laneWidth * m_lane + posOffset.x, (m_height + 1) * tileHeight + posOffset.y, m_forwardPoint * tileLength + posOffset.z), Quaternion.identity);
        m_objectsOnTile[length - 1] = obj;
    }
    /// <summary>
    /// Stores an already existing obstacle on this tile
    /// </summary>
    /// <param name="obstacle">The obstacle to add</param>
    public void AddObstacle(ref GameObject obstacle)
    {
        int length = m_isRamp ? 2 : 1;
        length += obstacle != null ? 1 : 0;
        m_objectsOnTile = new GameObject[length];
        m_objectsOnTile[length - 1] = obstacle;
    }
}
