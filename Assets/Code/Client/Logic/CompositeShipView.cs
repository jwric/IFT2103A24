using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Client.Logic
{
    public enum ShipPartType
    {
        Hull,
        Thruster
    }
    
    public class ShipPart
    {
        public ShipPartType Type;
        public Vector2Int Position;
        public int Rotation;
    }
    
    public class ShipPartView : MonoBehaviour
    {
        private ShipPart _shipPart;
        
        // neighbors
        private ShipPartView _left;
        private ShipPartView _right;
        private ShipPartView _up;
        private ShipPartView _down;
        
        public void Init(ShipPart shipPart)
        {
            _shipPart = shipPart;
            var posUnit = 8/32f;
            transform.localPosition = new Vector3(shipPart.Position.x * posUnit, shipPart.Position.y * posUnit, 0);
            transform.localRotation = Quaternion.Euler(0, 0, shipPart.Rotation * 90);
        }
        
        public void SetLeft(ShipPartView left)
        {
            _left = left;
        }
        
        public void SetRight(ShipPartView right)
        {
            _right = right;
        }
        
        public void SetUp(ShipPartView up)
        {
            _up = up;
        }
        
        public void SetDown(ShipPartView down)
        {
            _down = down;
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = Color.red;

            // Draw wire cube with cut edges based on neighbors
            var unit = 8 / 32f;
            var pos = transform.localPosition;
            var upperLeft = new Vector2(pos.x - unit / 2, pos.y + unit / 2);
            var upperRight = new Vector2(pos.x + unit / 2, pos.y + unit / 2);
            var lowerRight = new Vector2(pos.x + unit / 2, pos.y - unit / 2);
            var lowerLeft = new Vector2(pos.x - unit / 2, pos.y - unit / 2);

            // Green for neighbors
            // Red for no neighbors
            void DrawEdge(Vector2 start, Vector2 end, ShipPartView neighbor)
            {
                Gizmos.color = neighbor == null ? Color.red : Color.green;
                Gizmos.DrawLine(new Vector3(start.x, start.y, 0), new Vector3(end.x, end.y, 0));
            }


            // Handle slanted edges for missing corners
            if (_up == null && _left == null)
            {
                upperLeft.x += unit;
            }
            if (_up == null && _right == null)
            {
                upperRight.x -= unit;
            }
            if (_down == null && _left == null)
            {
                lowerLeft.x += unit;
            }
            if (_down == null && _right == null)
            {
                lowerRight.x -= unit;
            }
            
            DrawEdge(upperLeft, upperRight, _up);
            DrawEdge(upperRight, lowerRight, _right);
            DrawEdge(lowerRight, lowerLeft, _down);
            DrawEdge(lowerLeft, upperLeft, _left);
            
        }

    }
    
    public class CompositeShipView : MonoBehaviour
    {
        private ShipPartView[] _shipParts;

        private void Awake()
        {
            // self init a random ship with noise
            // Vector2 perlinOffset = new Vector2(UnityEngine.Random.Range(0, 1000), UnityEngine.Random.Range(0, 1000));
            // int x = 10;
            // int y = 10;
            List<ShipPart> shipParts = new();
            // for (int ix = 0; ix < x; ix++)
            // {
            //     for (int iy = 0; iy < y; iy++)
            //     {
            //         // noise
            //         var noiseValue = Mathf.PerlinNoise((ix + perlinOffset.x) * 0.1f, (iy + perlinOffset.y) * 0.1f);
            //         if (noiseValue > 0.5f)
            //         {
            //             shipParts.Add(new ShipPart
            //             {
            //                 Type = ShipPartType.Hull,
            //                 Position = new Vector2Int(ix, iy),
            //                 Rotation = 0
            //             });
            //         }
            //     }
            // }
            // create a diagonal line of thickness 2
            for (int i = 0; i < 11; i++)
            {
                shipParts.Add(new ShipPart
                {
                    Type = ShipPartType.Hull,
                    Position = new Vector2Int(i, i),
                    Rotation = 0
                });
                shipParts.Add(new ShipPart
                {
                    Type = ShipPartType.Hull,
                    Position = new Vector2Int(i, i + 1),
                    Rotation = 0
                });
            }
            // other side of the diagonal
            for (int i = 0; i < 9; i++)
            {
                shipParts.Add(new ShipPart
                {
                    Type = ShipPartType.Hull,
                    Position = new Vector2Int(i + 9, 9 - i),
                    Rotation = 0
                });
                shipParts.Add(new ShipPart
                {
                    Type = ShipPartType.Hull,
                    Position = new Vector2Int(i + 9, 8 - i),
                    Rotation = 0
                });
            }
            //down side of the ship
            
            Init(shipParts.ToArray());
        }

        public void Init(ShipPart[] shipParts)
        {
            _shipParts = new ShipPartView[shipParts.Length];
            for (int i = 0; i < shipParts.Length; i++)
            {
                var shipPart = shipParts[i];
                var part = new GameObject("Part");
                part.transform.SetParent(transform);
                var partView = part.AddComponent<ShipPartView>();
                partView.Init(shipPart);
                _shipParts[i] = partView;
            }
            
// set neighbors using brute force
            for (int i = 0; i < _shipParts.Length; i++)
            {
                var part = _shipParts[i];
                var partPosition = part.transform.localPosition;

                for (int j = 0; j < _shipParts.Length; j++)
                {
                    if (i == j) continue;

                    var other = _shipParts[j];
                    var otherPosition = other.transform.localPosition;

                    // Check for neighbors based on position
                    if (Mathf.Approximately(otherPosition.x, partPosition.x - 0.25f) &&
                        Mathf.Approximately(otherPosition.y, partPosition.y))
                    {
                        part.SetLeft(other);
                    }
                    else if (Mathf.Approximately(otherPosition.x, partPosition.x + 0.25f) &&
                             Mathf.Approximately(otherPosition.y, partPosition.y))
                    {
                        part.SetRight(other);
                    }
                    else if (Mathf.Approximately(otherPosition.x, partPosition.x) &&
                             Mathf.Approximately(otherPosition.y, partPosition.y + 0.25f))
                    {
                        part.SetUp(other);
                    }
                    else if (Mathf.Approximately(otherPosition.x, partPosition.x) &&
                             Mathf.Approximately(otherPosition.y, partPosition.y - 0.25f))
                    {
                        part.SetDown(other);
                    }
                }
            }

        }
    }
}