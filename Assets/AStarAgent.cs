using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;


public class AStarAgent
{
    public AStarAgent(AStarDemandsScheduler scheduler, AStarMap map)
    {
        demandsScheduler = scheduler;
        this.map = map;
    }

    private AStarMap map;
    private AStarDemandsScheduler demandsScheduler;
    
    public int HeuristicFunction(Vector2Int a,Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy);
    }

    public IEnumerator AStar(Vector2 start, Vector2 goal, float W)
    {
        MinTree openList = new MinTree();
        HashSet<Node> openSet = new HashSet<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        Vector3[] waypoints = new Vector3[0];
        Node[,] nodes = map.GetFreshNodes(); // Copie de la map sans rien

        Vector2Int startCoords = map.WorldToMapCoords(start);
        Vector2Int goalCoords = map.WorldToMapCoords(goal);

        if (!map.IsInMap(startCoords) || !map.IsInMap(goalCoords)) // Si le goal / le commencement est en dehors de la map alors on annule la demande.
        {
            demandsScheduler.FinishedProcessingPath(new Vector3[0], false);
            yield break;
        }

        Node startNode = nodes[startCoords.x, startCoords.y];
        Node goalNode = nodes[goalCoords.x, goalCoords.y];

        startNode.G = 0; 
        startNode.F = HeuristicFunction(startNode.Coords, goalNode.Coords);
        openList.Add(startNode);
        openSet.Add(startNode);

        bool pathSuccess = false;
        int iterations = 0;
        
        bool ghostWalk = !startNode.Walkable; // Si on commence sur une case non marchable alors on autorise (temporairement) de NoClip
        //Cela a été fait apres avoir vu que la map pouvais avoir un degré varié de detail, ou la case est détecté non marchable alors que si

        if (goalNode.Walkable)
        {
            while (openList.Count > 0)
            {
                Node current = openList.Pop();
                ghostWalk = ghostWalk && current.Walkable; //Si la case courante est marchable alors, on autorise plus le NoClip
                openSet.Remove(current);
                closedSet.Add(current);

                if (current.Coords == goalNode.Coords)
                {
                    pathSuccess = true;
                    break;
                }

                var currentNeighbours = map.GetNeighbours(nodes, current.Coords, ghostWalk);
                foreach (Node neighbour in currentNeighbours)
                {
                    if (closedSet.Contains(neighbour))
                        continue;

                    float tentativeGForce = current.G + Vector2Int.Distance(current.Coords, neighbour.Coords);
                    if (tentativeGForce < neighbour.G)
                    {
                        neighbour.Parent = current;
                        neighbour.G = tentativeGForce;
                        neighbour.F = tentativeGForce + HeuristicFunction(neighbour.Coords, goalNode.Coords) * W; // Ici nous somme dans un WA* donc W représente un poid que l'on met.

                        if (!openSet.Contains(neighbour))
                        {
                            openList.Add(neighbour);
                            openSet.Add(neighbour);
                        }
                    }
                }

                iterations++;
                if (iterations % 128 == 0) //On passe une frame toute les 128 tour de boucle
                    yield return null;
            }
        }

        if (pathSuccess)
        {
            waypoints = RetracePath(startNode, goalNode);
        }

        demandsScheduler.FinishedProcessingPath(waypoints, pathSuccess);
    }
    
    public IEnumerator StealthAStar(Vector2 start, Vector2 goal, Species spicie, float distance, float W) // Variante Permettant d'esquiver des ennemeies
    {
        MinTree openList = new MinTree();
        HashSet<Node> openSet = new HashSet<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        Vector3[] waypoints = new Vector3[0];
        Node[,] nodes = map.GetFreshNodes();

        Vector2Int startCoords = map.WorldToMapCoords(start);
        Vector2Int goalCoords = map.WorldToMapCoords(goal);
        var distanceMap = map.GetEnemyMap(spicie);

        if (!map.IsInMap(startCoords) || !map.IsInMap(goalCoords))
        {
            demandsScheduler.FinishedProcessingPath(new Vector3[0], false);
            yield break;
        }

        Node startNode = nodes[startCoords.x, startCoords.y];
        Node goalNode = nodes[goalCoords.x, goalCoords.y];

        startNode.G = 0;
        startNode.F = HeuristicFunction(startNode.Coords, goalNode.Coords);
        openList.Add(startNode);
        openSet.Add(startNode);

        bool pathSuccess = false;
        int iterations = 0;
        
        bool ghostWalk = !startNode.Walkable; 

        if (goalNode.Walkable)
        {
            while (openList.Count > 0)
            {
                Node current = openList.Pop();
                openSet.Remove(current);
                closedSet.Add(current);
                
                ghostWalk = ghostWalk && current.Walkable;

                if (current.Coords == goalNode.Coords)
                {
                    pathSuccess = true;
                    break;
                }

                var currentNeighbours = map.GetNeighbours(nodes, current.Coords,  ghostWalk);
                foreach (Node neighbour in currentNeighbours)
                {
                    if (closedSet.Contains(neighbour))
                        continue;
                    
                    float tentativeGForce = current.G + Vector2Int.Distance(current.Coords, neighbour.Coords);
                    if (tentativeGForce < neighbour.G)
                    {
                        float d = distanceMap[neighbour.x, neighbour.y];

                        if (d <= distance) // Permet de debug la ou le chemin de peut pas passer
                        {
                            demandsScheduler.RecordStealthBlockedNode(neighbour.RealCoords);
                            continue; // Skip la case ou l'on est trop proche d'un ennemie
                        }
                        
                        float avoidancePenalty = d <= distance*1.5f ? 40 : 0; // Pénalité de raprochement, on prefere etre au plus loin des ennemies

                        neighbour.Parent = current;
                        neighbour.G = tentativeGForce;
                        neighbour.F = tentativeGForce +
                                      (HeuristicFunction(neighbour.Coords, goalNode.Coords)*0.5f + avoidancePenalty) * W;

                        if (!openSet.Contains(neighbour))
                        {
                            openList.Add(neighbour);
                            openSet.Add(neighbour);
                        }
                    }
                }

                iterations++;
                if (iterations % 128 == 0)
                    yield return null;
            }
        }

        if (pathSuccess)
        {
            waypoints = RetracePath(startNode, goalNode);
        }

        demandsScheduler.FinishedProcessingPath(waypoints, pathSuccess);
    }
    
    Vector3[] RetracePath(Node startNode, Node endNode) {
        List<Node> path = new List<Node>();
        
        Node currentNode = endNode;
		
        while (currentNode != startNode) {
            path.Add(currentNode);
            currentNode = currentNode.Parent;
        }
        Vector3[] waypoints = path.Count > 1 ? SimplifyPath(path, map.GetFreshNodes()) : new Vector3[0];
        Array.Reverse(waypoints);
        return waypoints;
		
    }

    Vector3[] SimplifyPath(List<Node> path, Node[,] nodes) {
        if (path.Count < 2)
            return new Vector3[0];
            
        List<Vector3> waypoints = new List<Vector3>();
        
        waypoints.Add(path[0].RealCoords);
        int lastAddedIndex = 0;
        
        for (int i = 1; i < path.Count - 1; i++) {
            Vector2 dirToPrev = new Vector2(path[lastAddedIndex].rx - path[i].rx, path[lastAddedIndex].ry - path[i].ry).normalized;
            Vector2 dirToNext = new Vector2(path[i].rx - path[i + 1].rx, path[i].ry - path[i + 1].ry).normalized;
            
            float anglePrev = Mathf.Atan2(dirToPrev.y, dirToPrev.x);
            float angleNext = Mathf.Atan2(dirToNext.y, dirToNext.x);
            
            float angleDiff = Mathf.Abs(anglePrev - angleNext);

            if (angleDiff > Mathf.PI) //Normalize l'angle
                angleDiff = 2f * Mathf.PI - angleDiff;
            
            //Ajoute un point si l'angle est trop grand
            bool significantAngleChange = angleDiff > MathF.PI/6; // ~5.7 degrees
            bool pathNotWalkable = !IsPathWalkable(path[lastAddedIndex].Coords, path[i].Coords, nodes);
            
            if (significantAngleChange || pathNotWalkable) {
                waypoints.Add(path[i].RealCoords);
                lastAddedIndex = i;
            }
        }
        
        // Always add the end point
        waypoints.Add(path[path.Count - 1].RealCoords);
        
        return waypoints.ToArray();
    }

    private bool IsPathWalkable(Vector2Int from, Vector2Int to, Node[,] nodes)
    {
        // regarde si une ligne droite entre 2pts sont marchable
        int dx = Mathf.Abs(to.x - from.x);
        int dy = Mathf.Abs(to.y - from.y);
        int sx = from.x < to.x ? 1 : -1;
        int sy = from.y < to.y ? 1 : -1;
        int err = dx - dy;

        int x = from.x;
        int y = from.y;

        while (true)
        {
            if (!map.IsInMap(new Vector2Int(x, y)) || !nodes[x, y].Walkable)
                return false;

            if (x == to.x && y == to.y)
                break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }

        return true;
    }
    
}

public class MinTree
{
    private readonly List<Node> _heap = new List<Node>();

    public int Count => _heap.Count;

    public bool Contains(Node node) => _heap.Contains(node);

    public void Add(Node node)
    {
        _heap.Add(node);
        HeapifyUp(_heap.Count - 1);
    }

    public Node Pop()
    {
        if (_heap.Count == 0)
            throw new InvalidOperationException("Heap is empty");

        Node root = _heap[0];

        // Move last element to root and shrink
        _heap[0] = _heap[^1];
        _heap.RemoveAt(_heap.Count - 1);

        if (_heap.Count > 0)
            HeapifyDown(0);

        return root;
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;

            if (_heap[index].F >= _heap[parent].F)
                break;

            (_heap[index], _heap[parent]) = (_heap[parent], _heap[index]);
            index = parent;
        }
    }

    private void HeapifyDown(int index)
    {
        int last = _heap.Count - 1;

        while (true)
        {
            int left = index * 2 + 1;
            int right = index * 2 + 2;
            int smallest = index;

            if (left <= last && _heap[left].F < _heap[smallest].F)
                smallest = left;

            if (right <= last && _heap[right].F < _heap[smallest].F)
                smallest = right;

            if (smallest == index)
                break;

            (_heap[index], _heap[smallest]) = (_heap[smallest], _heap[index]);
            index = smallest;
        }
    }
}




public class Node : IEquatable<Node>
{
    public int x => Coords.x;
    public int y => Coords.y;
    
    public float rx => RealCoords.x;
    public float ry => RealCoords.y;
    
    public Vector2Int Coords;
    public Vector2 RealCoords;
    public float G;
    public float F;
    
    public bool Walkable;

    public Node Parent;
    public bool Equals(Node other)
    {
        return Coords.Equals(other.Coords);
    }

    public override bool Equals(object obj)
    {
        return obj is Node other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Coords);
    }
}
