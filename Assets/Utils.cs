using UnityEngine;

    public static class Utils
    {
        public static Vector2[] CardinalDirections = new Vector2[8]{Vector2.up, (Vector2.up + Vector2.right).normalized ,
                                                                    Vector2.right, (Vector2.down + Vector2.right).normalized ,
                                                                    Vector2.down, (Vector2.down + Vector2.left).normalized ,
                                                                    Vector2.left, (Vector2.up + Vector2.left).normalized };
        
        public enum DirectionEnum
        {
            North, NorthEast, East, SouthEast, South, SouthWest, West, NorthWest
        }
    }