using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

public struct MeshDeformerJob : IJobParallelFor
{
    // Delta time must be copied to the job since jobs generally don't have concept of a frame.
    // The main thread waits for the job same frame or next frame, but the job should do work deterministically
    // independent on when the job happens to run on the worker threads.
    public float deltaTime;

    public Vector3 center;
    public float radius;
    public float force;

    public NativeArray<Vector3> vertices;

    [ReadOnly]
    public NativeArray<Vector3> normals;

    public void Execute( int index )
    {
        // Due to the lack of ref returns, it is not possible to directly change the content of a NativeContainer.
        // For example, nativeArray[0]++; is the same as writing var temp = nativeArray[0]; temp++; which does not update the value in nativeArray.
        // Instead, you must copy the data from the index into a local temporary copy, modify that copy, and save it back.
        Vector3 vertex = vertices[index];

        // Check if the vertex is inside the sphere.
        float a = Mathf.Pow(vertex.x - center.x, 2);
        float b = Mathf.Pow(vertex.y - center.y, 2);
        float c = Mathf.Pow(vertex.z - center.z, 2);
        if( a + b + c < Mathf.Pow(radius, 2) )
        {
            vertex += normals[index] * force * deltaTime;

            // Save it back.
            vertices[index] = vertex;
        }
    }
}