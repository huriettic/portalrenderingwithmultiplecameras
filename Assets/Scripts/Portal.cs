using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    public bool isInside;

    public Camera portalCam;

    public Plane portalPlane;

    public int connectedPortalZone;

    public List<Vector3> planevertices = new List<Vector3>();

    public List<int> planetriangles = new List<int>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Mesh meshplane = this.GetComponent<MeshFilter>().mesh;

        meshplane.GetVertices(planevertices);

        meshplane.GetTriangles(planetriangles, 0);

        Vector3 mp0 = this.transform.TransformPoint(planevertices[planetriangles[0]]);
        Vector3 mp1 = this.transform.TransformPoint(planevertices[planetriangles[1]]);
        Vector3 mp2 = this.transform.TransformPoint(planevertices[planetriangles[2]]);

        portalPlane = new Plane(mp0, mp1, mp2);
    }

    void OnTriggerEnter(Collider other)
    {
        isInside = true;
    }

    void OnTriggerExit(Collider other)
    {
        isInside = false;
    }
}
