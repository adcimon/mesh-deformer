using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class MeshDeformer : MonoBehaviour
{
	private Mesh mesh;
    private MeshCollider meshCollider;

    private NativeArray<Vector3> vertices;
    private NativeArray<Vector3> normals;

    private bool scheduled = false;
	private MeshDeformerJob job;
	private JobHandle handle;

	private void Start()
	{
		mesh = gameObject.GetComponent<MeshFilter>().mesh;
		mesh.MarkDynamic();

        meshCollider = gameObject.GetComponent<MeshCollider>();
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;

        // This memory setup assumes the vertex count will not change.
        vertices = new NativeArray<Vector3>(mesh.vertices, Allocator.Persistent);
        normals = new NativeArray<Vector3>(mesh.normals, Allocator.Persistent);
	}

	private void LateUpdate()
	{
		if( !scheduled )
		{
			return;
		}

		handle.Complete();
        scheduled = false;

		// Copy the results to the managed array.
        job.vertices.CopyTo(vertices);

		// Assign the modified vertices to the mesh.
        mesh.vertices = vertices.ToArray();

        // Normals and tangents have not changed but the mesh has new bounds.
		//mesh.RecalculateNormals();
		//mesh.RecalculateTangents();
		mesh.RecalculateBounds();

        // There is an odd behaviour with the mesh collider, the mesh is updated as expected but internally it still uses the unmodified mesh.
        meshCollider.enabled = false;
        meshCollider.enabled = true;
	}

    private void OnDestroy()
    {
        vertices.Dispose();
        normals.Dispose();
    }

    public void Deform( Vector3 point, float radius, float force )
    {
        job = new MeshDeformerJob();
        job.deltaTime = Time.deltaTime;
        job.center = transform.InverseTransformPoint(point); // Transform the point from world space to local space.
        job.radius = radius;
        job.force = force;
        job.vertices = vertices;
        job.normals = normals;

        scheduled = true;
        handle = job.Schedule(vertices.Length, 64);
    }
}