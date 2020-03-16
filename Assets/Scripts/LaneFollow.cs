using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class LaneFollow : MonoBehaviour
{
    public GameObject[] m_Lanes;

    // looks no further than m_MaxLookRange
    public float m_MaxRange = 15.0f;

    private bool m_IsRacing = true;
 
    // true is this is an AI vehicle, else is player controlled
    public bool m_IsAi = true;
    
    // vehicle speed (m/s)
    public float m_Speed = 10f;

    public float m_OrientationSmoothTimeSec = 0.05f;
 
    private float m_SmoothingAngleVelocity;

    // an object with children as a points to follow
    public GameObject m_CurrentLane;
    
    // true if this AI is a lane changing passer
    public bool m_IsAIPasser = false;
    
    // true if this AI is a bully to player 
    public bool m_IsAiBully = false;
    
    // vehicle to bully (AI will try to block) 
    public LaneFollow m_VehicleToBully = null;
    
    // distance where AI will block vehicle
    public float m_BullyBlockDistance = 25f;
    
    private Vector3 m_StartPos = Vector3.zero;
    
    private Quaternion m_StartRot = Quaternion.identity;

    public bool m_IsShowDebugLines = false;

    private float m_BlockStartTime = 0f;
    
    public float m_BlockVehicleWaitTimeSec = 1.5f;

    private int m_LapCount = 0;
    
    public int m_RaceNumLaps = 6;

    private Rigidbody m_RigidBody = null;
    
    private Vector3 m_GravityVector = Vector3.zero;

    // time that last trigger was accepted for this vehicle
    private float m_LastTriggerTime = 0f;
    
    // time till next trigger will be accepted
    public float m_WaitTillNextTriggerSec = 10f;


    void DrawLaneLines(GameObject lanePoints)
    {
        if (lanePoints != null) {
            List<Transform> childList = new List<Transform>();        
            foreach (Transform child in lanePoints.transform) {
                childList.Add(child);        
            }
                        
            // white
            Gizmos.color = new Color(1,1,1);


            Vector3 prevPos = childList[0].position;

            for (int i = 1; i < childList.Count; ++i) {
                Gizmos.DrawLine(prevPos, childList[i].position);
                prevPos = childList[i].position;
            }
        }
    }

    private void Start()
    {
        m_RigidBody = GetComponent<Rigidbody>();  
        
        m_StartPos = transform.position;
        m_StartRot = transform.rotation;
        
        ResetVehicle();
        
        // save gravity vector
        m_GravityVector = -transform.up * 100f;

    }

    void OnDrawGizmos()
    {
        if (m_IsShowDebugLines) {

            foreach (GameObject obj in m_Lanes) {
                DrawLaneLines(obj);
            }
       
            // red line out the front of vehicle
            Gizmos.color = new Color(1,0,0);        
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 20f);
        }
    }

    Vector3 FindNearestPoint(ref float angleBetweenDeg)
    {
        // loop through all points looking for closest one in front of vehicle
        
        // return nearest point and the angle between vehicle and point (degrees)

        Vector3 nearestPoint = Vector3.zero;
           
        angleBetweenDeg = 0f;

        if (m_CurrentLane != null) {
    
            Vector3 vehicleForward = transform.forward;
            
            float minAngleRad = Mathf.PI * 2f;
            float sign = 1f;
    
            foreach (Transform child in m_CurrentLane.transform) {
            
                float dist = (child.position - transform.position).magnitude;
                
                if (dist < m_MaxRange) {
                
                    Vector3 dirV = (child.position - transform.position).normalized;
                    float angleBetweenRad = Mathf.Acos(Vector3.Dot(dirV, vehicleForward));

                    if (Mathf.Abs(angleBetweenDeg) <= 80f) {

                        if (Mathf.Abs(angleBetweenRad) <= minAngleRad) {
                            minAngleRad = angleBetweenRad;
                            sign = Vector3.Cross(dirV, vehicleForward).y;
                            nearestPoint = child.position;
                        }

                    }
                }           
            }
            
            angleBetweenDeg = Mathf.Rad2Deg * minAngleRad * sign;
        }    
            
        return nearestPoint;
    }

    private void FollowLane()
    {    
        // ---update vehicle to follow current lane ---
        
        // find nearest point and angle between vehicle and point
        float angleBetweenDeg = 0f;
        Vector3 nearestPoint = FindNearestPoint(ref angleBetweenDeg);

        if (nearestPoint == Vector3.zero) {

            // player seems to have left the field

            if (m_IsAi == false) {
                //m_GameController.m_IsRaceAiWon = true;
            }
        
        
        } else {

            // --- player is on the field ---

            if (m_RigidBody != null) {
                // orient vehicle towards
                Vector3 euler = transform.localEulerAngles;
                // smooth orientation            
                euler.y = Mathf.SmoothDamp(
                    euler.y, euler.y - angleBetweenDeg,
                    ref m_SmoothingAngleVelocity,
                    m_OrientationSmoothTimeSec);

#if false  
                transform.localEulerAngles = euler;
#else

                m_RigidBody.rotation = Quaternion.Euler(euler);
#endif

                // move vehicle towards the point
                Vector3 dPos = transform.forward * Time.deltaTime * m_Speed;  
                m_RigidBody.position += dPos;
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        m_LapCount++;

        if (m_LastTriggerTime == 0f || 
            (Time.time - m_LastTriggerTime) > m_WaitTillNextTriggerSec) 
        {
            if (m_IsAi == false) {
                // set player's race lap count
                //m_GameController.SetPlayerRaceLapCount(m_LapCount - 1);
            }
            
            if (m_LapCount >= m_RaceNumLaps) {
                //// this vehicle won
                //if (m_IsAi) {
                //    m_GameController.m_IsRaceAiWon = true;
                //} else {
                //    m_GameController.m_IsRacePlayerWon = true;
                //}
            }
            
            // store time that last trigger was accepted for this vehicle
            m_LastTriggerTime = Time.time;
        }
    }

    public bool IsRacing()
    {
        return m_IsRacing;
    }
    
    public void StopRace()
    {
        m_IsRacing = false;
        m_LapCount = 0;
        
    }
    
    public void StartRace()
    {
        m_IsRacing = true;
        m_LapCount = 0;
    }

    public void ResetVehicle()
    {
        if (m_RigidBody !=  null) {
            m_RigidBody.velocity = Vector3.zero;
            m_RigidBody.angularVelocity = Vector3.zero;
            m_RigidBody.rotation = m_StartRot;
            m_RigidBody.position = m_StartPos;
            m_RigidBody.isKinematic = true;
        }   
        transform.position = m_StartPos;
        transform.rotation = m_StartRot;

        if (m_RigidBody != null) {
            m_RigidBody.isKinematic = false;
        }
    }

    private void Update()
    {
        // gravity            
        m_RigidBody.AddForce(m_GravityVector, ForceMode.Acceleration);
        
        if (m_IsRacing) {

            if (m_IsAi) {
                // AI controller vehicle
                AiControl();
                
            } else {
                // player controlled vehicle
                PlayerControl();
            }

            // follow current lane
            FollowLane();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        //if (m_IsAi && m_IsAIPasser) {
        //    // change lanes         
        //    if (m_CurrentLane == m_InnerLane) {
        //        m_CurrentLane = m_OuterLane;
        //    } else {
        //        m_CurrentLane = m_InnerLane;
        //    }    
        // }    
    }

    void AiControl()
    {
        // --- AI controlled vehicle ---
        
        if (m_IsAiBully && m_VehicleToBully != null) {
            // if AI is a bully and vehicle exists and nearby, block it
            float dist = (m_VehicleToBully.transform.position - transform.position).magnitude;
            
            if (dist <= m_BullyBlockDistance) {
                if (m_CurrentLane != m_VehicleToBully.m_CurrentLane) {
                    if (m_BlockStartTime == 0f) {
                        m_BlockStartTime = Time.time;
                    } else if ((Time.time - m_BlockStartTime) >= m_BlockVehicleWaitTimeSec) {                  
                        // set AI lane to match vehicle
                        m_CurrentLane = m_VehicleToBully.m_CurrentLane;
                    }
                } else {
                    // reset start time                
                    m_BlockStartTime = 0f;
                }
                
            }
        }   
     }
    
    void PlayerControl()
    {
        // --- player controls vehicle ---

        if (Input.GetKeyDown(KeyCode.A)) {
            // go left
            GameObject prev = null;
            foreach (GameObject obj in m_Lanes) {
                if (m_CurrentLane == obj) {
                    if (prev != null) {
                        m_CurrentLane = prev;
                    }
                } else {
                    prev = obj;
                }
            }
        } else if (Input.GetKeyDown(KeyCode.D)) {
            // go right
            bool isPrevCurrent = false;
            foreach (GameObject obj in m_Lanes) {
                if (m_CurrentLane == obj) {
                    isPrevCurrent = true;
                } else {
                    if (isPrevCurrent) {
                        m_CurrentLane = obj;
                        break;
                    }
                }
            }
        }
    }


}
