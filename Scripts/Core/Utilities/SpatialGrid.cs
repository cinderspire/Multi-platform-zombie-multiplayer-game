using UnityEngine;
using System.Collections.Generic;

namespace DeadFrontier.Core
{
    /// <summary>
    /// Spatial partitioning grid for efficient proximity queries (zombies, players, loot)
    /// Dramatically improves performance by avoiding Physics.OverlapSphere for large numbers
    /// </summary>
    public class SpatialGrid<T> where T : MonoBehaviour
    {
        private Dictionary<Vector2Int, HashSet<T>> grid;
        private Dictionary<T, Vector2Int> objectToCell;
        private float cellSize;

        /// <summary>
        /// Creates a new spatial grid
        /// </summary>
        /// <param name="cellSize">Size of each grid cell in meters (default: 10m)</param>
        public SpatialGrid(float cellSize = Constants.SPATIAL_GRID_CELL_SIZE)
        {
            this.cellSize = cellSize;
            this.grid = new Dictionary<Vector2Int, HashSet<T>>();
            this.objectToCell = new Dictionary<T, Vector2Int>();
        }

        /// <summary>
        /// Inserts or updates an object in the grid
        /// </summary>
        public void Insert(T obj)
        {
            if (obj == null)
                return;

            Vector2Int newCell = GetCell(obj.transform.position);

            // Check if object already exists
            if (objectToCell.TryGetValue(obj, out Vector2Int oldCell))
            {
                // Object moved to different cell
                if (oldCell != newCell)
                {
                    Remove(obj);
                    InsertInternal(obj, newCell);
                }
                // Same cell, no action needed
            }
            else
            {
                // New object
                InsertInternal(obj, newCell);
            }
        }

        /// <summary>
        /// Removes an object from the grid
        /// </summary>
        public void Remove(T obj)
        {
            if (obj == null || !objectToCell.ContainsKey(obj))
                return;

            Vector2Int cell = objectToCell[obj];

            if (grid.TryGetValue(cell, out HashSet<T> objects))
            {
                objects.Remove(obj);

                // Remove empty cells to save memory
                if (objects.Count == 0)
                {
                    grid.Remove(cell);
                }
            }

            objectToCell.Remove(obj);
        }

        /// <summary>
        /// Gets all objects within a radius of a position
        /// Much faster than Physics.OverlapSphere for large numbers
        /// </summary>
        public List<T> Query(Vector3 position, float radius)
        {
            List<T> results = new List<T>();
            Vector2Int centerCell = GetCell(position);
            int cellRadius = Mathf.CeilToInt(radius / cellSize);

            // Check all cells in range
            for (int x = -cellRadius; x <= cellRadius; x++)
            {
                for (int z = -cellRadius; z <= cellRadius; z++)
                {
                    Vector2Int cell = centerCell + new Vector2Int(x, z);

                    if (grid.TryGetValue(cell, out HashSet<T> objects))
                    {
                        foreach (var obj in objects)
                        {
                            if (obj != null)
                            {
                                // Additional distance check for accuracy
                                float distance = Vector3.Distance(position, obj.transform.position);
                                if (distance <= radius)
                                {
                                    results.Add(obj);
                                }
                            }
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Gets the nearest object to a position within radius
        /// </summary>
        public T QueryNearest(Vector3 position, float radius)
        {
            T nearest = null;
            float nearestDistance = float.MaxValue;

            List<T> candidates = Query(position, radius);

            foreach (var obj in candidates)
            {
                float distance = Vector3.Distance(position, obj.transform.position);
                if (distance < nearestDistance)
                {
                    nearest = obj;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Gets all objects in a specific cell
        /// </summary>
        public List<T> QueryCell(Vector2Int cell)
        {
            if (grid.TryGetValue(cell, out HashSet<T> objects))
            {
                return new List<T>(objects);
            }
            return new List<T>();
        }

        /// <summary>
        /// Gets count of objects in grid
        /// </summary>
        public int Count => objectToCell.Count;

        /// <summary>
        /// Gets count of active cells
        /// </summary>
        public int CellCount => grid.Count;

        /// <summary>
        /// Clears the entire grid
        /// </summary>
        public void Clear()
        {
            grid.Clear();
            objectToCell.Clear();
        }

        /// <summary>
        /// Updates all objects in the grid (call this periodically if objects move)
        /// </summary>
        public void UpdateAll()
        {
            // Create a copy of keys to avoid modification during iteration
            List<T> objects = new List<T>(objectToCell.Keys);

            foreach (var obj in objects)
            {
                if (obj != null)
                {
                    Insert(obj); // Will update position if cell changed
                }
                else
                {
                    Remove(obj); // Clean up null references
                }
            }
        }

        private void InsertInternal(T obj, Vector2Int cell)
        {
            if (!grid.ContainsKey(cell))
            {
                grid[cell] = new HashSet<T>();
            }

            grid[cell].Add(obj);
            objectToCell[obj] = cell;
        }

        private Vector2Int GetCell(Vector3 position)
        {
            return new Vector2Int(
                Mathf.FloorToInt(position.x / cellSize),
                Mathf.FloorToInt(position.z / cellSize)
            );
        }

        /// <summary>
        /// Draws debug gizmos for the grid
        /// </summary>
        public void DrawGizmos(Vector3 origin, int gridWidth, int gridHeight)
        {
            Gizmos.color = Color.green;

            for (int x = -gridWidth / 2; x <= gridWidth / 2; x++)
            {
                for (int z = -gridHeight / 2; z <= gridHeight / 2; z++)
                {
                    Vector3 cellCenter = origin + new Vector3(x * cellSize, 0f, z * cellSize);
                    Gizmos.DrawWireCube(cellCenter, new Vector3(cellSize, 0.1f, cellSize));
                }
            }

            // Draw occupied cells in different color
            Gizmos.color = Color.red;
            foreach (var cell in grid.Keys)
            {
                Vector3 cellCenter = origin + new Vector3(cell.x * cellSize, 0f, cell.y * cellSize);
                Gizmos.DrawCube(cellCenter, new Vector3(cellSize, 0.2f, cellSize));
            }
        }
    }
}
