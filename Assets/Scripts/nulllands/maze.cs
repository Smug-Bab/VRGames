using System.Collections.Generic;
using UnityEngine;

// Generates a maze as a single Mesh on the same GameObject.
// Attach this to an empty GameObject and assign a Material.
public class Maze : MonoBehaviour
{
	public int width = 10;
	public int height = 10;
	public float cellSize = 1f;
	public float wallHeight = 2f;
	public Material material;
	public bool generateOnStart = true;
	public Transform playerTransform; // Assign player Transform in Inspector
	public int mazeCount = 0; // Counter for mazes generated

	const int N = 1, E = 2, S = 4, W = 8;

	int[,] cells; // wall bits

	void Start()
	{
		if (generateOnStart) Generate();
	}

	[ContextMenu("Generate Maze")]
	public void Generate()
	{
		if (width <= 0 || height <= 0) return;

		mazeCount++;

		cells = new int[width, height];
		for (int x = 0; x < width; x++)
			for (int y = 0; y < height; y++)
				cells[x, y] = N | E | S | W; // all walls

		CarveMaze(0, 0);

		BuildCombinedMesh();
	}

	void CarveMaze(int startX, int startY)
	{
		var rand = new System.Random();
		var stack = new Stack<Vector2Int>();
		bool[,] visited = new bool[width, height];
		stack.Push(new Vector2Int(startX, startY));
		visited[startX, startY] = true;

		int[] dx = { 0, 1, 0, -1 };
		int[] dy = { 1, 0, -1, 0 };
		int[] dir = { N, E, S, W };
		int[] opposite = { S, W, N, E };

		while (stack.Count > 0)
		{
			var cell = stack.Pop();
			int cx = cell.x, cy = cell.y;

			var neighbors = new List<int>();
			for (int i = 0; i < 4; i++)
			{
				int nx = cx + dx[i], ny = cy + dy[i];
				if (nx >= 0 && nx < width && ny >= 0 && ny < height && !visited[nx, ny]) neighbors.Add(i);
			}

			if (neighbors.Count > 0)
			{
				stack.Push(cell);
				int pick = neighbors[rand.Next(neighbors.Count)];
				int nx = cx + dx[pick], ny = cy + dy[pick];

				cells[cx, cy] &= ~dir[pick];
				cells[nx, ny] &= ~opposite[pick];

				visited[nx, ny] = true;
				stack.Push(new Vector2Int(nx, ny));
			}
		}
	}

	void BuildCombinedMesh()
	{
		var verts = new List<Vector3>();
		var tris = new List<int>();
		var uvs = new List<Vector2>();

		// Floor
		float totalW = width * cellSize;
		float totalH = height * cellSize;
		float offsetX = -totalW * 0.5f; // center on X
		float offsetZ = -totalH * 0.5f; // center on Z
		int baseIndex = verts.Count;
		verts.Add(new Vector3(offsetX, 0, offsetZ));
		verts.Add(new Vector3(offsetX + totalW, 0, offsetZ));
		verts.Add(new Vector3(offsetX + totalW, 0, offsetZ + totalH));
		verts.Add(new Vector3(offsetX, 0, offsetZ + totalH));
		tris.AddRange(new int[] { baseIndex + 0, baseIndex + 2, baseIndex + 1, baseIndex + 0, baseIndex + 3, baseIndex + 2 });
		uvs.AddRange(new Vector2[] { Vector2.zero, Vector2.right * width, Vector2.one * new Vector2(width, height), Vector2.up * height });

		// Walls (quads per wall)
		for (int x = 0; x < width; x++)
		for (int y = 0; y < height; y++)
		{
			float wx = offsetX + x * cellSize;
			float wz = offsetZ + y * cellSize;
			int w = cells[x, y];

			// North (toward +z)
			if ((w & N) != 0)
				AddWallQuad(verts, tris, uvs, new Vector3(wx, 0, wz + cellSize), new Vector3(wx + cellSize, 0, wz + cellSize));

			// East (+x)
			if ((w & E) != 0)
				AddWallQuad(verts, tris, uvs, new Vector3(wx + cellSize, 0, wz), new Vector3(wx + cellSize, 0, wz + cellSize));

			// South (0 or -z)
			if ((w & S) != 0)
				AddWallQuad(verts, tris, uvs, new Vector3(wx + cellSize, 0, wz), new Vector3(wx, 0, wz));

			// West (-x)
			if ((w & W) != 0)
				AddWallQuad(verts, tris, uvs, new Vector3(wx, 0, wz + cellSize), new Vector3(wx, 0, wz));
		}

		var mesh = new Mesh();
		if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		mesh.SetVertices(verts);
		mesh.SetTriangles(tris, 0);
		mesh.SetUVs(0, uvs);
		mesh.RecalculateNormals();

		var mf = GetComponent<MeshFilter>();
		if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
		var mr = GetComponent<MeshRenderer>();
		if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();
		mf.sharedMesh = mesh;
		if (material != null) mr.sharedMaterial = material;

		// Optional: add/replace collider
		var mc = GetComponent<MeshCollider>();
		if (mc == null) mc = gameObject.AddComponent<MeshCollider>();
		mc.sharedMesh = mesh;

		// Setup trigger collider that encompasses the entire maze
		var triggerCollider = gameObject.GetComponent<BoxCollider>();
		if (triggerCollider == null) triggerCollider = gameObject.AddComponent<BoxCollider>();
		triggerCollider.isTrigger = true;
		triggerCollider.size = new Vector3(width * cellSize, wallHeight, height * cellSize);
		triggerCollider.center = new Vector3(0, wallHeight * 0.5f, 0);
	}

	void AddWallQuad(List<Vector3> verts, List<int> tris, List<Vector2> uvs, Vector3 a, Vector3 b)
	{
		// a and b are the bottom-left and bottom-right positions along XZ plane for the wall segment
		Vector3 bl = a;
		Vector3 br = b;
		Vector3 tr = b + Vector3.up * wallHeight;
		Vector3 tl = a + Vector3.up * wallHeight;
		int i = verts.Count;
		verts.Add(bl);
		verts.Add(br);
		verts.Add(tr);
		verts.Add(tl);
		tris.AddRange(new int[] { i + 0, i + 2, i + 1, i + 0, i + 3, i + 2 });
		float width = Vector3.Distance(a, b) / cellSize;
		uvs.AddRange(new Vector2[] { Vector2.zero, Vector2.right * width, Vector2.one, Vector2.up });
	}
	void OnTriggerExit(Collider other)
	{
		if (playerTransform != null && other.transform == playerTransform)
		{
			// Regenerate maze
			Generate();
			// Teleport maze to player
			transform.position = playerTransform.position;
		}
	}}

