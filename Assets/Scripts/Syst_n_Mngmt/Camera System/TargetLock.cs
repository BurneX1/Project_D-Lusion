using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.UI;
using System;
public class TargetLock : MonoBehaviour
{
    public event Action<GameObject> UpdatedTarget = delegate { };
    [Header("Objects")]
    [Space]
    public Camera_System camSys;
    [SerializeField] private Transform baseReference;
     public Camera mainCamera;         
     public CinemachineFreeLook cinemachineFreeLook;
    [Space]
    [Header("UI")]
    [SerializeField] private Image aimIcon;  
    [Space]
    [Header("Settings")]
    [Space]
    [Range(0.0f,1.0f)] public float mouseInfluence;
    public float adjustmentSpeed;
    public float distance;
    public float detectRadius; 
    public float syncTargetRadius;
    public LayerMask layerMask;

 
    [SerializeField] private Vector2 targetLockOffset;
    [SerializeField] private float minDistance; 
    [SerializeField] private float maxDistance;
    
    public bool isTargeting;
    public Transform currentTarget;
    private float mouseX;
    private float mouseY;

    private float _defaultRadius;
    private float _defaultDistance;
    private float _defaultAdjSpd;

    void Start()
    {
        _defaultRadius = detectRadius;
        _defaultDistance = distance;
        _defaultAdjSpd = adjustmentSpeed;
        //cinemachineFreeLook.m_XAxis.m_InputAxisName = "";
        //cinemachineFreeLook.m_YAxis.m_InputAxisName = "";
    }
    public void ResetValues()
    {
        detectRadius = _defaultRadius;
        distance = _defaultDistance;
        adjustmentSpeed = _defaultAdjSpd;
    }
    void Update()
    {
        TargetLogic();

    }
    public void TargetLogic()
    {
        if (currentTarget == null)
        {
            AssignTarget();
        }
        else
        {
            Vector2 viewPos = mainCamera.WorldToViewportPoint(currentTarget.position);

            Vector2 mousePos = new Vector2(
                cinemachineFreeLook.m_XAxis.m_InputAxisValue,
                cinemachineFreeLook.m_YAxis.m_InputAxisValue);

            
            if ((Mathf.Abs(mousePos.x) + Mathf.Abs(mousePos.y)) * 4.5f > (detectRadius/2))
            {
                AssignTarget();

            }
        }

        if (isTargeting)
        {
            NewInputTarget(currentTarget);
            if (aimIcon)
                aimIcon.gameObject.SetActive(isTargeting);
            cinemachineFreeLook.m_XAxis.m_InputAxisValue = mouseX;
            cinemachineFreeLook.m_YAxis.m_InputAxisValue = mouseY;
        }
    }

    private void AssignTarget()
    {
        if (SphereCast())
        {
            currentTarget = SphereCast().transform;
            isTargeting = true;
            cinemachineFreeLook.m_XAxis.m_InputAxisName = "";
            cinemachineFreeLook.m_YAxis.m_InputAxisName = "";
        }
    }
    public void RemoveTarget()
    {
        if (isTargeting)
        {
            isTargeting = false;
            currentTarget = null;
            cinemachineFreeLook.m_XAxis.m_InputAxisName = "Mouse X";
            cinemachineFreeLook.m_YAxis.m_InputAxisName = "Mouse Y";
            return;
        }
    }

    private void NewInputTarget(Transform target) 
    {
        if (!currentTarget) return;

        Vector3 viewPos = mainCamera.WorldToViewportPoint(target.position);
        
        if(aimIcon)
            aimIcon.transform.position = mainCamera.WorldToScreenPoint(target.position);

        if ((target.position - baseReference.position).magnitude < minDistance) return;
        float tmpX;
        if(Mathf.Abs(Input.GetAxis("Mouse X")) > 0.1f) tmpX = Mathf.Lerp((viewPos.x - 0.5f + targetLockOffset.x) * adjustmentSpeed, Input.GetAxis("Mouse X"), mouseInfluence);
        else tmpX = (viewPos.x - 0.5f + targetLockOffset.x) * adjustmentSpeed;

        float tmpY;
        if (Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.1f) tmpY = Mathf.Lerp((viewPos.y - 0.5f + targetLockOffset.y) * adjustmentSpeed, Input.GetAxis("Mouse Y"), mouseInfluence);
        else tmpY = (viewPos.y - 0.5f + targetLockOffset.y) * adjustmentSpeed;


        mouseX = tmpX;            
        mouseY = tmpY;             
    }


    //private GameObject ClosestTarget() 
    //{
    //    GameObject[] gos;
    //    gos = GameObject.FindGameObjectsWithTag(targetTag);
    //    GameObject closest = null;
    //    float distance = maxDistance;
    //    float currAngle = maxAngle;
    //    Vector3 position = baseReference.position;
    //    foreach (GameObject go in gos)
    //    {
    //        Vector3 diff = go.transform.position - position;
    //        float curDistance = diff.magnitude;
    //        if (curDistance < distance)
    //        {
    //            Vector3 viewPos = mainCamera.WorldToViewportPoint(go.transform.position);
    //            Vector2 newPos = new Vector3(viewPos.x - 0.5f, viewPos.y - 0.5f);
    //            if (Vector3.Angle(diff.normalized, mainCamera.transform.forward) < maxAngle)
    //            {
    //                closest = go;
    //                currAngle = Vector3.Angle(diff.normalized, mainCamera.transform.forward.normalized);
    //                distance = curDistance;
    //            }
    //        }
    //    }
    //    return closest;
    //}

    private GameObject SphereCast()
    {
        Vector3 p1 = mainCamera.transform.position;

        RaycastHit[] hits = Physics.SphereCastAll(p1, detectRadius, mainCamera.transform.forward, distance,layerMask);
        GameObject returnObj = null;

        /*for(int i = 0; i<hits.Length;i++)
        {
            //Debug.Log(hits[i].collider.gameObject.name);
        }*/
        if(hits.Length<=0)
        {
            if (currentTarget != null) UpdatedTarget.Invoke(null);
            currentTarget = null;
            RemoveTarget();
            return null;
        }


        if (hits[0].collider.gameObject.transform != currentTarget || hits.Length == 1)
        {
            
            returnObj = hits[0].collider.gameObject;
            
        }
        else 
        {
            returnObj = hits[1].collider.gameObject;
         
        }

        //currentTarget = returnObj.transform;
        if (returnObj != currentTarget) UpdatedTarget.Invoke(returnObj);
        return returnObj;
        
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
       
        Vector3 drawPoint;
        if (currentTarget!=null)
        {
            drawPoint = mainCamera.transform.position + (mainCamera.transform.forward * Vector3.Distance(mainCamera.transform.position,currentTarget.transform.position));
        }
        else
        {
            drawPoint = mainCamera.transform.position + (mainCamera.transform.forward * distance);
        }
        Gizmos.DrawWireSphere(drawPoint, detectRadius);
        Gizmos.DrawLine(mainCamera.transform.position, drawPoint);
        
        //Gizmos.DrawWireSphere(mainCamera.transform.position, detectRadius);
        //Gizmos.Draw
        //if (Physics.SphereCast(mainCamera.transform.position, detectRadius, mainCamera.transform.forward * distance, out hit, distance, layerMask))
            //Gizmos.DrawWireSphere(SphereCase(), detectRadius);
    }

}
