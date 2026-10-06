using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoomCategory
{
    public List<GameObject> prefabs = new List<GameObject>();
    [Range(0, 100)] public int weight = 10;
}

public class SCP_MazeGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    public int maxRooms = 50;
    public int maxGenerationRetries = 50;

    [Header("Prefabs")]
    public GameObject startRoomPrefab;
    public GameObject doorPrefab;
    public GameObject checkpointPrefab;

    [Header("Room Categories & Weights")]
    public RoomCategory oneWayRooms;
    public RoomCategory twoWayRooms;
    public RoomCategory threeWayRooms;
    public RoomCategory fourWayRooms;

    [Header("Special Rooms (Used Only Once)")]
    public List<GameObject> specialRoomPrefabs = new List<GameObject>();

    [Header("Socket & Connection Settings")]
    public string jointName = "joint";
    public float socketSnapTolerance = 0.05f;
    public float edgeTolerance = 1.5f;
    public float outerEdgeCheckDistance = 3.0f;

    [Header("Checkpoint Settings")]
    public int maxCheckpoints = 5;
    public float minCheckpointSpacing = 5.0f;

    [Header("Collision Settings")]
    public float doorwayPadding = 0.1f;
    public LayerMask roomLayerMask = ~0;

    private class SocketConnection
    {
        public Transform socketA;
        public Transform socketB;
    }

    private List<Transform> openSockets = new List<Transform>();
    private List<SocketConnection> connectedPairs = new List<SocketConnection>();
    private List<Transform> closedSockets = new List<Transform>();
    private HashSet<Transform> spawnedRooms = new HashSet<Transform>();
    private int spawnedRoomCount = 0;

    private void Start()
    {
        GenerateCompleteMaze();
    }

    public void GenerateCompleteMaze()
    {
        int attempts = 0;
        bool success = false;

        while (attempts < maxGenerationRetries && !success)
        {
            attempts++;
            ClearMaze();

            if (startRoomPrefab == null) return;

            GameObject rootRoom = Instantiate(startRoomPrefab, Vector3.zero, Quaternion.identity, transform);
            spawnedRoomCount++;
            spawnedRooms.Add(rootRoom.transform);
            RegisterRoomSockets(rootRoom.transform);

            Physics.SyncTransforms();

            // 1. Grow the core maze layout using standard weighted rooms
            int safetyBreak = 0;
            while (openSockets.Count > 0 && spawnedRoomCount < maxRooms && safetyBreak < 10000)
            {
                safetyBreak++;

                Transform currentSocket = openSockets[0];
                openSockets.RemoveAt(0);

                if (currentSocket == null) continue;

                if (TryConnectToNearestOpenSocket(currentSocket))
                {
                    continue;
                }

                GameObject selectedPrefab = GetWeightedPrefab();
                if (selectedPrefab == null)
                {
                    closedSockets.Add(currentSocket);
                    continue;
                }

                GameObject newRoom = Instantiate(selectedPrefab, transform);
                List<Transform> newRoomSockets = GetSockets(newRoom.transform);

                if (newRoomSockets.Count == 0)
                {
                    Destroy(newRoom);
                    closedSockets.Add(currentSocket);
                    continue;
                }

                Transform targetSocket = newRoomSockets[Random.Range(0, newRoomSockets.Count)];

                MatchSockets(currentSocket, targetSocket, newRoom.transform);
                Physics.SyncTransforms();

                CalculateRoomLocalBounds(newRoom, out Vector3 localCenter, out Vector3 localExtents);
                Transform parentRoom = GetRoomRoot(currentSocket);

                if (CheckPhysicsOverlap(newRoom, parentRoom, localCenter, localExtents))
                {
                    Destroy(newRoom);
                    closedSockets.Add(currentSocket);
                    continue;
                }

                spawnedRooms.Add(newRoom.transform);
                spawnedRoomCount++;

                connectedPairs.Add(new SocketConnection { socketA = currentSocket, socketB = targetSocket });

                foreach (Transform sock in newRoomSockets)
                {
                    if (sock != targetSocket)
                    {
                        openSockets.Add(sock);
                    }
                }
            }

            if (spawnedRoomCount >= maxRooms)
            {
                success = true;
            }
        }

        // 2. Post-Generation Pass: Inject unique special rooms into dead-ends or open connections
        InjectSpecialRooms();

        // 3. Finalize: Place doors and border checkpoints
        ProcessFinalOpenSockets();
    }

    private void InjectSpecialRooms()
    {
        if (specialRoomPrefabs == null || specialRoomPrefabs.Count == 0) return;

        foreach (GameObject specialPrefab in specialRoomPrefabs)
        {
            if (specialPrefab == null) continue;

            // Find a valid open socket to attach the special room
            Transform targetSocket = null;
            foreach (Transform sock in openSockets)
            {
                if (sock != null)
                {
                    targetSocket = sock;
                    break;
                }
            }

            if (targetSocket == null && closedSockets.Count > 0)
            {
                targetSocket = closedSockets[0];
            }

            if (targetSocket == null) continue;

            GameObject specialRoom = Instantiate(specialPrefab, transform);
            List<Transform> specialSockets = GetSockets(specialRoom.transform);

            if (specialSockets.Count == 0)
            {
                Destroy(specialRoom);
                continue;
            }

            Transform specialTargetSocket = specialSockets[0];

            MatchSockets(targetSocket, specialTargetSocket, specialRoom.transform);
            Physics.SyncTransforms();

            CalculateRoomLocalBounds(specialRoom, out Vector3 localCenter, out Vector3 localExtents);
            Transform parentRoom = GetRoomRoot(targetSocket);

            if (CheckPhysicsOverlap(specialRoom, parentRoom, localCenter, localExtents))
            {
                Destroy(specialRoom);
                continue;
            }

            // Successfully integrated special room
            openSockets.Remove(targetSocket);
            closedSockets.Remove(targetSocket);

            spawnedRooms.Add(specialRoom.transform);
            spawnedRoomCount++;

            connectedPairs.Add(new SocketConnection { socketA = targetSocket, socketB = specialTargetSocket });

            // Add remaining sockets of the special room to the open list
            foreach (Transform sock in specialSockets)
            {
                if (sock != specialTargetSocket)
                {
                    openSockets.Add(sock);
                }
            }
        }
    }

    private bool TryConnectToNearestOpenSocket(Transform socketA)
    {
        if (socketA == null) return false;

        Transform bestMatch = null;
        float minDistance = float.MaxValue;

        foreach (Transform socketB in openSockets)
        {
            if (socketB == null || socketB == socketA) continue;
            if (GetRoomRoot(socketA) == GetRoomRoot(socketB)) continue;

            float alignment = Vector3.Dot(socketA.forward, socketB.forward);
            if (alignment > -0.90f) continue;

            float dist = Vector3.Distance(socketA.position, socketB.position);
            if (dist <= socketSnapTolerance && dist < minDistance)
            {
                minDistance = dist;
                bestMatch = socketB;
            }
        }

        if (bestMatch != null)
        {
            openSockets.Remove(bestMatch);
            connectedPairs.Add(new SocketConnection { socketA = socketA, socketB = bestMatch });
            return true;
        }

        return false;
    }

    private void ProcessFinalOpenSockets()
    {
        foreach (var conn in connectedPairs)
        {
            if (conn.socketA != null && doorPrefab != null)
            {
                Instantiate(doorPrefab, conn.socketA.position, conn.socketA.rotation, transform);
            }
            if (conn.socketA != null) Destroy(conn.socketA.gameObject);
            if (conn.socketB != null) Destroy(conn.socketB.gameObject);
        }

        List<Transform> allUnconnected = new List<Transform>(openSockets);
        allUnconnected.AddRange(closedSockets);
        openSockets.Clear();
        closedSockets.Clear();

        Bounds mazeBounds = CalculateTotalMazeBounds();
        List<Vector3> placedCheckpointPositions = new List<Vector3>();
        int checkpointsPlaced = 0;

        foreach (Transform socket in allUnconnected)
        {
            if (socket == null) continue;

            bool isOuterPerimeter = IsTrueOuterPerimeterEdge(socket, mazeBounds);
            bool isOuterEdgeRay = IsTrueOuterEdgeRay(socket);
            bool spaceValid = true;

            foreach (Vector3 cpPos in placedCheckpointPositions)
            {
                if (Vector3.Distance(socket.position, cpPos) < minCheckpointSpacing)
                {
                    spaceValid = false;
                    break;
                }
            }

            if (isOuterPerimeter && isOuterEdgeRay && spaceValid && checkpointsPlaced < maxCheckpoints && checkpointPrefab != null)
            {
                Instantiate(checkpointPrefab, socket.position, socket.rotation, transform);
                placedCheckpointPositions.Add(socket.position);
                checkpointsPlaced++;
            }
            else if (doorPrefab != null)
            {
                Instantiate(doorPrefab, socket.position, socket.rotation, transform);
            }

            Destroy(socket.gameObject);
        }
    }

    private bool IsTrueOuterPerimeterEdge(Transform socket, Bounds mazeBounds)
    {
        Vector3 pos = socket.position;
        return Mathf.Abs(pos.x - mazeBounds.min.x) <= edgeTolerance ||
        Mathf.Abs(pos.x - mazeBounds.max.x) <= edgeTolerance ||
        Mathf.Abs(pos.z - mazeBounds.min.z) <= edgeTolerance ||
        Mathf.Abs(pos.z - mazeBounds.max.z) <= edgeTolerance;
    }

    private bool IsTrueOuterEdgeRay(Transform socket)
    {
        Vector3 pos = socket.position;
        Vector3 fwd = socket.forward;
        Vector3 right = socket.right;

        Ray rayCenter = new Ray(pos, fwd);
        Ray rayLeft = new Ray(pos - right * 0.3f, fwd);
        Ray rayRight = new Ray(pos + right * 0.3f, fwd);

        bool hitCenter = Physics.Raycast(rayCenter, outerEdgeCheckDistance, roomLayerMask, QueryTriggerInteraction.Ignore);
        bool hitLeft = Physics.Raycast(rayLeft, outerEdgeCheckDistance, roomLayerMask, QueryTriggerInteraction.Ignore);
        bool hitRight = Physics.Raycast(rayRight, outerEdgeCheckDistance, roomLayerMask, QueryTriggerInteraction.Ignore);

        return !(hitCenter || hitLeft || hitRight);
    }

    private Bounds CalculateTotalMazeBounds()
    {
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        bool initialized = false;

        foreach (Transform room in spawnedRooms)
        {
            if (room == null) continue;
            foreach (Collider col in room.GetComponentsInChildren<Collider>())
            {
                if (col.isTrigger) continue;
                if (!initialized)
                {
                    bounds = col.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(col.bounds);
                }
            }
        }
        return bounds;
    }

    private GameObject GetWeightedPrefab()
    {
        List<RoomCategory> validCategories = new List<RoomCategory>();
        if (oneWayRooms.prefabs.Count > 0 && oneWayRooms.weight > 0) validCategories.Add(oneWayRooms);
        if (twoWayRooms.prefabs.Count > 0 && twoWayRooms.weight > 0) validCategories.Add(twoWayRooms);
        if (threeWayRooms.prefabs.Count > 0 && threeWayRooms.weight > 0) validCategories.Add(threeWayRooms);
        if (fourWayRooms.prefabs.Count > 0 && fourWayRooms.weight > 0) validCategories.Add(fourWayRooms);

        if (validCategories.Count == 0) return null;

        int totalWeight = 0;
        foreach (var cat in validCategories) totalWeight += cat.weight;
        if (totalWeight <= 0) return validCategories[0].prefabs[0];

        int randVal = Random.Range(0, totalWeight);
        int currentSum = 0;
        RoomCategory chosenCat = validCategories[0];

        foreach (var cat in validCategories)
        {
            currentSum += cat.weight;
            if (randVal < currentSum)
            {
                chosenCat = cat;
                break;
            }
        }

        if (chosenCat.prefabs.Count == 0) return null;
        return chosenCat.prefabs[Random.Range(0, chosenCat.prefabs.Count)];
    }

    private void MatchSockets(Transform targetSocket, Transform incomingSocket, Transform roomTransform)
    {
        Quaternion rotAdjustment = Quaternion.LookRotation(-targetSocket.forward, targetSocket.up) *
        Quaternion.Inverse(Quaternion.LookRotation(incomingSocket.forward, incomingSocket.up));

        roomTransform.rotation = rotAdjustment * roomTransform.rotation;
        Vector3 posAdjustment = targetSocket.position - incomingSocket.position;
        roomTransform.position += posAdjustment;
    }

    private bool CheckPhysicsOverlap(GameObject tempRoom, Transform parentRoom, Vector3 localCenter, Vector3 localExtents)
    {
        Vector3 worldCenter = tempRoom.transform.TransformPoint(localCenter);
        Vector3 shrunkExtents = new Vector3(
            Mathf.Max(0.1f, localExtents.x - doorwayPadding),
                                            Mathf.Max(0.1f, localExtents.y - doorwayPadding),
                                            Mathf.Max(0.1f, localExtents.z - doorwayPadding)
        );

        Collider[] hits = Physics.OverlapBox(worldCenter, shrunkExtents, tempRoom.transform.rotation, roomLayerMask, QueryTriggerInteraction.Ignore);

        foreach (Collider hit in hits)
        {
            Transform hitRoot = GetRoomRoot(hit.transform);
            if (spawnedRooms.Contains(hitRoot) && hitRoot != parentRoom && hitRoot != tempRoom.transform)
            {
                return true;
            }
        }
        return false;
    }

    private void CalculateRoomLocalBounds(GameObject room, out Vector3 localCenter, out Vector3 localExtents)
    {
        Quaternion origRot = room.transform.rotation;
        room.transform.rotation = Quaternion.identity;

        Bounds bounds = new Bounds(room.transform.position, Vector3.zero);
        bool initialized = false;

        foreach (Collider col in room.GetComponentsInChildren<Collider>())
        {
            if (col.isTrigger) continue;
            if (!initialized)
            {
                bounds = col.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(col.bounds);
            }
        }

        if (!initialized) bounds = new Bounds(room.transform.position, new Vector3(3f, 3f, 3f));

        localCenter = room.transform.InverseTransformPoint(bounds.center);
        localExtents = bounds.extents;
        room.transform.rotation = origRot;
    }

    private List<Transform> GetSockets(Transform root)
    {
        List<Transform> sockets = new List<Transform>();
        foreach (Transform child in root.GetComponentsInChildren<Transform>())
        {
            if (child.name.Contains(jointName))
            {
                sockets.Add(child);
            }
        }
        return sockets;
    }

    private void RegisterRoomSockets(Transform roomTransform)
    {
        foreach (Transform socket in GetSockets(roomTransform))
        {
            openSockets.Add(socket);
        }
    }

    private Transform GetRoomRoot(Transform child)
    {
        Transform current = child;
        while (current != null && current.parent != transform)
        {
            current = current.parent;
        }
        return current;
    }

    private void ClearMaze()
    {
        foreach (Transform room in spawnedRooms)
        {
            if (room != null) Destroy(room.gameObject);
        }
        spawnedRooms.Clear();
        openSockets.Clear();
        closedSockets.Clear();
        connectedPairs.Clear();
        spawnedRoomCount = 0;

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }
}
