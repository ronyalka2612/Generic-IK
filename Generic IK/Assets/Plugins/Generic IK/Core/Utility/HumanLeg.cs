﻿using System;
using UnityEngine;


namespace Generics.Dynamics
{
    /// <summary>
    /// A leg object
    /// </summary>
    [Serializable]
    public class HumanLeg
    {
        public enum HumanLegs
        {
            RightLeg = 4,
            LeftLeg = 5
        }


        [Header("General")]
        [Tooltip("Used only if Auto-building the chain is wished through the HumanLeg.AutoBuild() call")]
        public HumanLegs LegType;

        public Core.Chain LegChain;


        [Header("Terrain Adjustment")]
        public float FootOffset = 0f;
        public float MaxStepHeight = 0.6f;

        private Quaternion _EEAnimRot;
        private Quaternion _EETargetRot;
        private Transform EE;

        /// <summary>
        /// Automatically build the chain
        /// </summary>
        /// <param name="anim"></param>
        public void AutoBuild(Animator anim)
        {
            if (anim == null)
            {
                Debug.LogError("The Animator component passed is NULL");
                return;
            }

            RigReader rigReader = new RigReader(anim);

            switch (LegType)
            {
                case HumanLegs.RightLeg:
                    var tempR = rigReader.BuildChain(LegType);
                    LegChain.Joints = tempR.Joints;
                    break;
                case HumanLegs.LeftLeg:
                    var tempL = rigReader.BuildChain(LegType);
                    LegChain.Joints = tempL.Joints;
                    break;
            }

            LegChain.InitiateJoints();
            LegChain.Weight = 1;

            _EEAnimRot = LegChain.GetEndEffector().rotation;
            EE = LegChain.GetEndEffector();
        }

        /// <summary>
        /// Cast rays to find pumps in the terrain and sets the IK target to the appropriate hit point.
        /// (does not solve the IK, you need to Call a Solver separately)
        /// (The AnalyticalSolver is suggested)
        /// </summary>
        public void TerrainAdjustment(LayerMask mask, Transform root)
        {
            RaycastHit hit;
            Ray ray = new Ray(EE.position, Vector3.down);
            bool intersect = Physics.Raycast(ray, out hit, MaxStepHeight, mask, QueryTriggerInteraction.Ignore);

#if UNITY_EDITOR
            if (intersect)
            {
                Debug.DrawLine(ray.origin, hit.point, Color.green);   //enable for debug purposes
            }
#endif
            if (intersect)
            {
                Vector3 rootUp = root.up;

                float footHeight = root.position.y - EE.position.y;
                float footFromGround = hit.point.y - root.position.y;

                float offsetTarget = Mathf.Clamp(footFromGround, -MaxStepHeight, MaxStepHeight) + FootOffset;
                float currentMaxOffset = Mathf.Clamp(MaxStepHeight - footHeight, 0f, MaxStepHeight);
                float IK = Mathf.Clamp(offsetTarget, -currentMaxOffset, offsetTarget);

                Vector3 IKPoint = EE.position + rootUp * IK;
                LegChain.SetIKTarget(IKPoint);

                //calculate the ankle rot, before applying the IK
                _EETargetRot = Quaternion.FromToRotation(hit.normal, rootUp);
                _EEAnimRot = EE.rotation;
            }
            else
            {
                LegChain.SetIKTarget(EE.position);
            }
        }


        /// <summary>
        /// Rotate the ankle
        /// </summary>
        public void RotateFoot()
        {
            EE.rotation = Quaternion.Inverse(_EETargetRot) * _EEAnimRot;
        }
    }
}