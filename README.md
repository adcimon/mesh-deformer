# Unity Job System: Mesh Deformer

Mesh deformation using the Unity Job System.

<p align="center">
  <img align="center" src="example.gif" title="Beware the mutant bunnies."><br>
</p>

This project is a proof of concept application that deforms a mesh using the new Unity Job System. The <a href="https://docs.unity3d.com/Manual/JobSystem.html">Unity Job System</a> is a way to write <a href="https://en.wikipedia.org/wiki/Multithreading_(computer_architecture)">multithreaded</a> code in the CPU providing high performance boost to the games using it. It is integrated with the Unity’s native job system which creates a thread per CPU core and manages small units of work named jobs. This design avoids the thread context switches that cause a waste of CPU resources.<br>

To create a new job you need to implement one interface corresponding to the type of job you want to execute. There are several types of jobs, `IJob`, `IJobParallelFor` and `IJobParallelForTransform` are the most common. The basic one, `IJob`, allows you to execute the code in the secondary threads. It is also very common to want to execute the same operations on large collections of data, for this task you have the job `IJobParallelFor` (which is the one used in this example). The last one, `IJobParallelForTransform`, is another parallel job that is designed for operations using `Transform` components.<br>

Another important thing to consider when writing high performance code is the memory layout of your data. Memory allocations are slow and to gain meaningful speed ups you have to control the lifecycle of your data, avoiding the garbage collector. A new set of native collections of <a href="https://en.wikipedia.org/wiki/Blittable_types">blittable types</a> are exposed to the managed side of Unity to achieve this.<br>

The namespaces that are necessary to use the Job System and the native collections are the following ones:
```
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Jobs;
```

The job that performs the vertex displacement is an `IJobParallelFor` job and receives the following inputs:
<ul>
  <li><strong>deltaTime</strong>. Time in seconds it took to complete the last frame.</li>
  <li><strong>center</strong>. Center of the sphere.</li>
  <li><strong>radius</strong>. Radius of the sphere.</li>
  <li><strong>force</strong>. Force that is going to be applied to offset the vertices.</li>
  <li><strong>normals</strong>. The normal for each vertex to obtain the displacement direction (read only).</li>
  <li><strong>vertices</strong>. The vertex positions that are going to be updated.</li>
</ul>

It is also important to highlight that the delta time must be copied because the jobs are <a href="https://en.wikipedia.org/wiki/Asynchrony_(computer_programming)">asynchronous</a> and don't have the concept of frame.
The operation that is executed is a vertex inside sphere check and a displacement across the normal with the given force.

```
public struct MeshDeformerJob : IJobParallelFor
{
    [ReadOnly] public float deltaTime;
    [ReadOnly] public Vector3 center;
    [ReadOnly] public float radius;
    [ReadOnly] public float force;
    [ReadOnly] public NativeArray<Vector3> normals;

    public NativeArray<Vector3> vertices;

    public void Execute( int index )
    {
        Vector3 vertex = vertices[index];

        float a = Mathf.Pow(vertex.x - center.x, 2);
        float b = Mathf.Pow(vertex.y - center.y, 2);
        float c = Mathf.Pow(vertex.z - center.z, 2);
        if( a + b + c < Mathf.Pow(radius, 2) )
        {
            vertex += normals[index] * force * deltaTime;
            vertices[index] = vertex;
        }
    }
}
```

The execution of this job is performed in the `MeshDeformer.cs` script after the helper class `Pointer.cs` calls it when the mouse button is pressed. The class declares 2 native arrays for the normals and vertices and a `Mesh` that will be shared by the `MeshFilter` and the `MeshCollider`.<br>

```
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

        vertices = new NativeArray<Vector3>(mesh.vertices, Allocator.Persistent);
        normals = new NativeArray<Vector3>(mesh.normals, Allocator.Persistent);
    }

    ...
}
```

Each time the method `public void Deform( Vector3 point, float radius, float force )` is called, the job is scheduled for execution.<br>

```
public void Deform( Vector3 point, float radius, float force )
{
    job = new MeshDeformerJob();
    job.deltaTime = Time.deltaTime;
    job.center = transform.InverseTransformPoint(point);
    job.radius = radius;
    job.force = force;
    job.vertices = vertices;
    job.normals = normals;

    handle = job.Schedule(vertices.Length, 64);
}
```

The job is completed in the `LateUpdate`, the vertices are copied from the job's native array to the mesh and the bounds are recalculated.<br>
```
private void LateUpdate()
{
    handle.Complete();
    job.vertices.CopyTo(vertices);
    mesh.vertices = vertices.ToArray();
    mesh.RecalculateBounds();
}
```

Lastly, don't forget to free resources when the process is done, remember that the native collections are not managed.
```
private void OnDestroy()
{
    vertices.Dispose();
    normals.Dispose();
}
```

References.
> <a href="https://docs.unity3d.com/Manual/JobSystem.html">Unity Manual: C# Job System</a><br>
> <a href="https://www.youtube.com/watch?v=AXUvnk7Jws4">Unite Europe 2017 - C# job system & compiler</a><br>
> <a href="https://www.youtube.com/watch?v=tGmnZdY5Y-E">Unite Austin 2017 - Writing High Performance C# Scripts</a>
