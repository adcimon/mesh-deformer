using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Pointer: MonoBehaviour
{
    public float distance = 100;
    public float radius = 0.2f;
    public float force = 0.3f;

    private Camera cam;

    private void Awake()
    {
        cam = gameObject.GetComponent<Camera>();
    }

    private void Update()
    {
        // Create a ray from the screen point where the mouse is to the scene.
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // Check if the left mouse button is held down.
        if( !Input.GetMouseButton(0) )
        {
            Debug.DrawLine(ray.origin, ray.origin + ray.direction * distance);
            return;
        }

        // Check if the ray hit a gameobject with the mesh deformer component.
        RaycastHit hit;
        if( Physics.Raycast(ray, out hit, distance) )
        {
            MeshDeformer deformer = hit.collider.GetComponent<MeshDeformer>();
            if( deformer )
            {
                Debug.DrawLine(ray.origin, ray.origin + ray.direction * distance, Color.green);
                deformer.Deform(hit.point, radius, force);
            }
        }
        else
        {
            Debug.DrawLine(ray.origin, ray.origin + ray.direction * distance);
        }
    }
}