using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public class AStarMap : MonoBehaviour
{
    [SerializeField] private string mapFile;
    
    [SerializeField]
    private Vector2Int mapSize;
    [SerializeField]
    private Vector2 center;

    [SerializeField] private float worldSize;
    
    [SerializeField] LayerMask layerMask;

    private float[][,] _enemyMaps;

    private List<Vector2Int>[] _saveCoverList;

    private float _enemyMapsRefreshCounter;
    
    private Node[,] _mapNodes;
    private void Start()
    {
        if (_mapNodes == null)
        {
            BuildMap();
            BuildEnemyMaps();
        }
    }

    private void Update()
    {
        if (_enemyMapsRefreshCounter >= GameManager.Instance.EnemyMapRefreshTime)
        {
            BuildEnemyMaps();
            _enemyMapsRefreshCounter = 0;
        }
        else
        {
            _enemyMapsRefreshCounter += Time.deltaTime;
        }
    }

    private void FindAllSaveCovers()
    {
        
    }

    private void FindSaveCoversOfSpecies(Species specie)
    {
        var de = GetEnemyMap(specie);
        
    }

    private void BuildEnemyMaps()
    {
        var n = Enum.GetValues(typeof(Species)).Length;
        _enemyMaps =  new float[n][,];

        for (int i = 0; i < n; i++)
        {
            _enemyMaps[i] = GenerateEnemyMap((Species)i);
        }
    }
    
    [ContextMenu("Build Map")]
    void BuildMap()
    {
        _mapNodes = new Node[mapSize.x, mapSize.y];

        for (int i = 0; i < mapSize.x; i++)
        {
            for (int j = 0; j < mapSize.y; j++)
            {
                float x = i * worldSize +  center.x;
                float y = j * worldSize +  center.y;
                
                _mapNodes[i, j] = new Node();
                _mapNodes[i, j].Coords  = new Vector2Int(i, j);
                _mapNodes[i, j].RealCoords = new Vector2(x, y);
                
                var r = Physics2D.BoxCastAll(new Vector2(x, y), new Vector2(worldSize, worldSize) * 0.5f, 0f, Vector2.zero, 1, layerMask.value);
                
                var isWalkable = r.Length == 0;
                if (!isWalkable)
                {
                    var nbrOfHit = 0;
                    
                    nbrOfHit+=Physics2D.BoxCastAll(new Vector2(x-worldSize/2, y-worldSize/2), Vector2.one * 0.5f, 0f, Vector2.zero, 1, layerMask.value).Length >0 ? 1 : 0;
                    nbrOfHit+=Physics2D.BoxCastAll(new Vector2(x+worldSize/2, y-worldSize/2), Vector2.one * 0.5f, 0f, Vector2.zero, 1, layerMask.value).Length>0 ? 1 : 0;
                    nbrOfHit+=Physics2D.BoxCastAll(new Vector2(x-worldSize/2, y+worldSize/2), Vector2.one * 0.5f, 0f, Vector2.zero, 1, layerMask.value).Length>0 ? 1 : 0;
                    nbrOfHit+=Physics2D.BoxCastAll(new Vector2(x+worldSize/2, y+worldSize)/2, Vector2.one * 0.5f, 0f, Vector2.zero, 1, layerMask.value).Length>0 ? 1 : 0;
                    if (nbrOfHit <= 1)
                    {
                        isWalkable = true;
                    }
                    
                }
                
                _mapNodes[i, j].Walkable = isWalkable;
                
                _mapNodes[i, j].G = float.PositiveInfinity;
                _mapNodes[i, j].F = float.PositiveInfinity;
                _mapNodes[i, j].Parent = null;
            }
        }
        
    }

    private float[,] GenerateEnemyMap(Species specie)
    {
        var result = new float[mapSize.x, mapSize.y];
        var ennemies = GameManager.Instance.Spiders[(int)specie];
        var queue = new Queue<Vector2Int>();

        for (int i = 0; i < mapSize.x; i++)
        {
            for (int j = 0; j < mapSize.y; j++)
            {
                result[i, j] = float.PositiveInfinity;
            }
        }

        foreach (var enemy in ennemies)
        {
            var enemyCoords = WorldToMapCoords(enemy.transform.position);
            if (!IsInMap(enemyCoords))
                continue;

            if (result[enemyCoords.x, enemyCoords.y] > 0f)
            {
                result[enemyCoords.x, enemyCoords.y] = 0f;
                queue.Enqueue(enemyCoords);
            }
        }

        var offsets = new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(-1, -1)
        };

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            float currentDistance = result[current.x, current.y];

            foreach (var offset in offsets)
            {
                var neighbour = new Vector2Int(current.x + offset.x, current.y + offset.y);
                if (!IsInMap(neighbour))
                    continue;

                bool diagonal = offset.x != 0 && offset.y != 0;
                float stepDistance = diagonal ? 1.4142136f : 1f;
                float newDistance = currentDistance + stepDistance;
                if (newDistance < result[neighbour.x, neighbour.y])
                {
                    result[neighbour.x, neighbour.y] = newDistance;
                    queue.Enqueue(neighbour);
                }
            }
        }

        return result;
    }

    public float[,] GetEnemyMap(Species specie)
    {
        return _enemyMaps[(int)specie];
    }

    public Vector2Int WorldToMapCoords(Vector2 coords)
    {
        int i = Mathf.FloorToInt((coords.x - center.x) / worldSize);
        int j = Mathf.FloorToInt((coords.y - center.y) / worldSize);
        return new Vector2Int(i, j);
    }

    public Node WorldToMapNode(Vector2 coords)
    {
        Vector2Int mapCoords = WorldToMapCoords(coords);
        return _mapNodes[mapCoords.x, mapCoords.y];
    }

    public Node[,] GetFreshNodes()
    {
        Node[,] fresh = new Node[mapSize.x, mapSize.y];
        for (int i = 0; i < mapSize.x; i++)
        {
            for (int j = 0; j < mapSize.y; j++)
            {
                Node original = _mapNodes[i, j];
                fresh[i, j] = new Node
                {
                    Coords = original.Coords,
                    RealCoords = original.RealCoords,
                    Walkable = original.Walkable,
                    G = float.PositiveInfinity,
                    F = float.PositiveInfinity,
                    Parent = null
                };
            }
        }

        return fresh;
    }

    public List<Node> GetNeighbours(Node[,] nodes, Vector2Int coords, bool ghost)
    {
        List<Node> neighbours = new List<Node>();

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) 
                    continue;

                int x = coords.x + i;
                int y = coords.y + j;

                if (x >= 0 && x < mapSize.x &&
                    y >= 0 && y < mapSize.y)
                {
                    Node n = nodes[x, y];
                    if (n != null && (ghost ||n.Walkable))
                        neighbours.Add(n);
                }
            }
        }

        return neighbours;
    }


    public bool IsInMap(Vector2Int coords)
    {
        return 0 <= coords.x && coords.x < mapSize.x && 0 <= coords.y && coords.y < mapSize.y;
    }



    private void OnDrawGizmosSelected()
    {
        if (_mapNodes == null) return; 
        
        for (int i = 0; i < mapSize.x; ++i)
        {
            for(int j = 0; j < mapSize.y; ++j)
            {
                Vector3 pos = _mapNodes[i,j].RealCoords;

                Gizmos.color = _mapNodes[i,j].Walkable ?  Color.green : Color.red;
                Gizmos.DrawWireCube(pos, new Vector3(worldSize, worldSize, 1));

            }
        }
    }
    
    private void OnDrawGizmos()
    {
        if (_mapNodes == null) return; 
        
        for (int i = 0; i < mapSize.x; ++i)
        {
            for(int j = 0; j < mapSize.y; ++j)
            {
                if (!_mapNodes[i, j].Walkable)
                {
                    Vector3 pos = _mapNodes[i,j].RealCoords;

                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(pos, new Vector3(worldSize, worldSize, 1));
                }
            }
        }
    }
}