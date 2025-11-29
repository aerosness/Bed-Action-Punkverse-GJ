using UnityEngine;

[ExecuteAlways]
public class PlayAreaVisualizer : MonoBehaviour
{
    public Transform center;
    public float playRadius = 150f;

    [Header("References")]
    public Mesh sphereMesh;            // обычная UV Sphere
    public Material boundaryMaterial;  // прозрачный материал купола

    private GameObject sphereObject;

    private void OnEnable()
    {
        GenerateSphere();
    }

    private void OnValidate()
    {
        GenerateSphere();
    }

    private void GenerateSphere()
    {
        if (center == null)
            center = transform;

        if (sphereMesh == null || boundaryMaterial == null)
            return;

        // Если сфера ещё не создана
        if (sphereObject == null)
        {
            sphereObject = new GameObject("PlayAreaBoundary");
            sphereObject.transform.parent = transform;

            var mf = sphereObject.AddComponent<MeshFilter>();
            var mr = sphereObject.AddComponent<MeshRenderer>();

            mf.sharedMesh = sphereMesh;
            mr.sharedMaterial = boundaryMaterial;

            // инвертируем нормали (модель должна смотреть внутрь)
            InvertNormals(mf.sharedMesh);

            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        // центр
        sphereObject.transform.position = center.position;

        // масштаб: диаметр = 2 * радиус
        float diameter = playRadius * 2f;
        sphereObject.transform.localScale = new Vector3(diameter, diameter, diameter);
    }

    private void InvertNormals(Mesh mesh)
    {
        Vector3[] normals = mesh.normals;
        for (int i = 0; i < normals.Length; i++)
            normals[i] = -normals[i];
        mesh.normals = normals;

        // переворачиваем порядок вершин
        for (int sub = 0; sub < mesh.subMeshCount; sub++)
        {
            var triangles = mesh.GetTriangles(sub);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int temp = triangles[i];
                triangles[i] = triangles[i + 1];
                triangles[i + 1] = temp;
            }
            mesh.SetTriangles(triangles, sub);
        }
    }
}
