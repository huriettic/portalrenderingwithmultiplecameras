using System.Collections.Generic;
using UnityEngine;

public class PortalManager : MonoBehaviour
{
    [Header("Player Settings")]
    public float speed = 7f;
    public float jumpHeight = 2f;
    public float gravity = 5f;
    public float sensitivity = 10f;
    public float clampAngle = 90f;
    public float smoothFactor = 25f;

    [Header("References")]
    public CharacterController Player;
    public Camera Cam;
    public PortalPairs zoneZero = new PortalPairs();
    public PortalPairs zoneOne = new PortalPairs();
    public int currentZone;

    private Vector2 targetRotation;
    private Vector2 currentRotation;
    private Vector3 targetMovement;
    private Vector3 currentForce;

    private PortalPairs zone = new PortalPairs();
    private Plane[] planes;
    private int lastZone = -1;
    private Vector3 posCam;
    private Quaternion rotCam;
    private Vector3 posPlayer;
    private Quaternion rotPlayer;

    [System.Serializable]
    public class PortalPairs
    {
        public List<PortalSet> PortalSets = new List<PortalSet>();

        [System.Serializable]
        public class PortalSet
        {
            public GameObject FromPortal;
            public GameObject ToPortal;
        } 
    }

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleInput();

        UpdateZone();

        HandlePortals();
    }

    void FixedUpdate()
    {
        if (!Player.isGrounded)
        {
            currentForce.y -= gravity * Time.fixedDeltaTime;
        }
    }

    public void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();

        if (Input.GetKeyDown(KeyCode.Space) && Player.isGrounded)
        {
            currentForce.y = jumpHeight;
        }

        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");

        targetRotation.x -= mouseY * sensitivity;
        targetRotation.y += mouseX * sensitivity;
        targetRotation.x = Mathf.Clamp(targetRotation.x, -clampAngle, clampAngle);

        currentRotation = Vector2.Lerp(currentRotation, targetRotation, smoothFactor * Time.deltaTime);

        Cam.transform.localRotation = Quaternion.Euler(currentRotation.x, 0f, 0f);
        Player.transform.rotation = Quaternion.Euler(0f, currentRotation.y, 0f);

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        targetMovement = (Player.transform.right * horizontal + Player.transform.forward * vertical).normalized;

        Player.Move((targetMovement + currentForce) * speed * Time.deltaTime);
    }

    public void HandlePortals()
    {
        posCam = Cam.transform.position;

        rotCam = Cam.transform.rotation;

        posPlayer = Player.transform.position;

        rotPlayer = Player.transform.rotation;

        planes = GeometryUtility.CalculateFrustumPlanes(Cam);

        for (int i = 0; i < zone.PortalSets.Count; i++)
        {
            Portal portalFrom = zone.PortalSets[i].FromPortal.GetComponent<Portal>();

            Portal portalTo = zone.PortalSets[i].ToPortal.GetComponent<Portal>();

            float side = portalFrom.portalPlane.GetDistanceToPoint(posCam);

            if (portalFrom.isInside)
            {
                if (side >= 0)
                {
                    SetPortalCam(portalFrom.transform, portalTo.transform, portalFrom.portalCam);
                    UpdatePortalMatrix(portalFrom.portalCam, portalTo.portalPlane);
                    portalFrom.portalCam.Render();
                }
                else
                {
                    TeleportPlayer(portalFrom.transform, portalTo.transform);
                    currentZone = portalFrom.connectedPortalZone;
                    portalFrom.isInside = false;

                    SetPortalCam(portalTo.transform, portalFrom.transform, portalTo.portalCam);
                    UpdatePortalMatrix(portalTo.portalCam, portalFrom.portalPlane);
                    portalTo.portalCam.Render();
                }
            }
            else
            {
                if (GeometryUtility.TestPlanesAABB(planes, portalFrom.GetComponent<Renderer>().bounds))
                {
                    SetPortalCam(portalFrom.transform, portalTo.transform, portalFrom.portalCam);
                    UpdatePortalMatrix(portalFrom.portalCam, portalTo.portalPlane);
                    portalFrom.portalCam.Render();
                }
            }
        }
    }

    public void TeleportPlayer(Transform fromPortal, Transform toPortal)
    {
        Player.enabled = false;

        Vector3 playerFromPos = fromPortal.InverseTransformPoint(posPlayer);
        Quaternion playerFromRot = Quaternion.Inverse(fromPortal.rotation) * rotPlayer;

        Vector3 playerToPos = toPortal.TransformPoint(playerFromPos);
        Quaternion playerToRot = toPortal.rotation * playerFromRot;

        Player.transform.SetPositionAndRotation(playerToPos, playerToRot);

        Player.enabled = true;
    }

    void SetPortalCam(Transform fromPortal, Transform toPortal, Camera portalCam)
    {
        Matrix4x4 camMatrix = Cam.transform.localToWorldMatrix;
        Matrix4x4 fromToMatrix = toPortal.localToWorldMatrix * fromPortal.worldToLocalMatrix;
        Matrix4x4 newCamMatrix = fromToMatrix * camMatrix;

        portalCam.transform.SetPositionAndRotation(newCamMatrix.GetColumn(3), newCamMatrix.rotation);
    }

    public void UpdatePortalMatrix(Camera portalCam, Plane camPlane)
    {
        Vector4 camSpaceClipPlane = CameraSpacePlane(portalCam, camPlane);
        portalCam.projectionMatrix = portalCam.CalculateObliqueMatrix(camSpaceClipPlane);
    }

    public Vector4 CameraSpacePlane(Camera cam, Plane plane)
    {
        Matrix4x4 worldToCam = cam.worldToCameraMatrix;

        Vector3 pointWorld = -plane.normal * plane.distance;
        Vector3 camPoint = worldToCam.MultiplyPoint(pointWorld);
        Vector3 camNormal = worldToCam.MultiplyVector(plane.normal).normalized;

        float camDistance = -Vector3.Dot(camPoint, camNormal);

        return new Vector4(camNormal.x, camNormal.y, camNormal.z, camDistance);
    }

    public void UpdateZone()
    {
        if (currentZone != lastZone)
        {
            switch (currentZone)
            {
                case 0:
                    zone = zoneZero;
                    break;
                case 1:
                    zone = zoneOne;
                    break;
            }
            lastZone = currentZone;
        }
    }
}
