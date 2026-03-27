using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class VillagerMoving : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public bool MoveBack;
    public NavMeshAgent agent;
    

   
    void Update()
    {
        if (MoveBack)  {
            agent.SetDestination(pointA.position);
            if (!agent.pathPending) {
                if (agent.remainingDistance <= agent.stoppingDistance)
                {
                    agent.SetDestination(FindObjectOfType<BuildingTouchable>().transform.position);
                    agent.isStopped = true;
                    Destroy(gameObject, 2f);
                } }  }
        else
        {
            agent.SetDestination(pointB.position);
            if (!agent.pathPending) {
                if (agent.remainingDistance <= agent.stoppingDistance){
                    agent.SetDestination(pointA.position);
                    MoveBack = true;}     } }  }
}
