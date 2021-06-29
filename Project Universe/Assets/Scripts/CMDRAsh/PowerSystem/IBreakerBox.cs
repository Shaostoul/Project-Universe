﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ProjectUniverse.Data.Libraries;

namespace ProjectUniverse.PowerSystem
{
    /*
     * The purpose of this class is to distribute power to large amounts of small IMachines (not unlike IRoutingSubtation, save simpler).
     */
    public sealed class IBreakerBox : MonoBehaviour
    {
        private Guid guid;
        //power group or machine this unit provides power to.
        public ISubMachine[] targetSubMachine;
        //private float[] requestedPower;
        [SerializeField] private float totalRequiredPower;
        private IBreakerBox thisBreaker;
        private LinkedList<ICable> iCableDLL = new LinkedList<ICable>();
        private float energyBufferMax;
        [SerializeField] private float bufferCurrent;
        private int maxConnections = 30;
        [SerializeField] private GameObject[] occupiedSwitches;
        public int switchCount;
        private float defecitVbreaker;
        private List<IRoutingSubstation> mySubstations = new List<IRoutingSubstation>();
        private List<Renderer> yellowSwitchRenderers = new List<Renderer>();
        [SerializeField] AudioSource soundsource;

        //power legs update
        private int legsRequired = 3;
        private int legsReceived;

        void Start()
        {
            occupiedSwitches = new GameObject[switchCount];
            thisBreaker = GetComponent<IBreakerBox>();
            energyBufferMax = 300.0f;
            bufferCurrent = 0f;
            totalRequiredPower = 0.0f;
            guid = Guid.NewGuid();
            Debug.Log("Breaker Proxy");
            ProxyStart();
        }

        void Update()
        {
            totalRequiredPower = 0f;
            //int numSuppliers = 0;
            //requestedPower = new float[targetSubMachine.Length];
            //Uses the number and type of connections to predict how much power to allocate to each SubMachine.
            for (int i = 0; i < targetSubMachine.Length; i++)
            {
                if (targetSubMachine[i] != null)
                {
                    totalRequiredPower += targetSubMachine[i].RequestedEnergyAmount();//ref numSuppliers);
                }
            }
            //Breaker Box power request to IRoutingSubstation
            if (bufferCurrent < energyBufferMax)
            {
                float requestPerSubstation = (totalRequiredPower / mySubstations.Count);// + ((totalRequiredPower / mySubstations.Count) * 0.05f);
                foreach (IRoutingSubstation subs in mySubstations)
                {
                    subs.RequestPowerFromSubstation(requestPerSubstation, thisBreaker);
                }
            }
            else if (bufferCurrent >= energyBufferMax)
            {
                //Debug.Log("Overcap: "+bufferCurrent);
                bufferCurrent = energyBufferMax;
            }
            //power will be divided equally among linked machines. (BUGGED? If values not the same, non-uniform distribution?)
            if (bufferCurrent < totalRequiredPower)
            {
                //Debug.Log("Breaker dividing:" + bufferCurrent);
                float defecit = totalRequiredPower - bufferCurrent;
                defecitVbreaker = defecit / totalRequiredPower;//dvb is broke?
                                                               //Debug.Log(this.gameObject.name + " dvb=("+ totalRequiredPower + "-"+ bufferCurrent + ")/"+totalRequiredPower);
            }
            else
            {
                defecitVbreaker = 0.0f;
            }
            //monitor the yellow power indicator lights
            for (int b = 0; b < yellowSwitchRenderers.Count; b++)
            {
                if (targetSubMachine[b] != null)
                {
                    if (targetSubMachine[b].PowerMachine)
                    {
                        yellowSwitchRenderers[b].material = MaterialLibrary.GetPowerSystemStateMaterials(2);//yellow on
                    }
                    else
                    {
                        yellowSwitchRenderers[b].material = MaterialLibrary.GetPowerSystemStateMaterials(5);
                    }
                }
                else
                {
                    //Debug.Log("AAAAAAAAAAAA");
                    yellowSwitchRenderers[b].material = MaterialLibrary.GetPowerSystemStateMaterials(5);
                    //mySwitchLEDs[0].GetComponent<Renderer>().material = MaterialLibrary.GetPowerSystemStateMaterials(3);//green to off
                    //mySwitchLEDs[1].GetComponent<Renderer>().material = MaterialLibrary.GetPowerSystemStateMaterials(4);//red off
                    //mySwitchLEDs[0].GetComponent<Renderer>().material = MaterialLibrary.GetPowerSystemStateMaterials(5);//yellow off
                }
            }

        }

        /// <summary>
        /// Method called by ISubMachines. Transfers power from the breaker buffer to the machine.
        /// 
        /// Allocate Legs as demanded
        /// </summary>
        public void RequestPowerFromBreaker(float requestedAmount, ISubMachine thisSubMachine)
        {
            //find the cable linking the breaker to the calling submachine
            foreach (ICable cable in iCableDLL)
            {
                //Debug.Log("SubMachine tester:"+cable.subMach);
                //Debug.Log("SubMachine testee:" + thisSubMachine);
                if (cable.subMach == thisSubMachine)
                {
                    //Debug.Log(thisSubMachine.gameObject.name + " requests "+requestedAmount);
                    //get machine's leg req
                    int machineLegReq = cable.subMach.GetLegRequirement();
                    //split power between legs
                    float[] powerAmount = new float[machineLegReq];
                    //Debug.Log("dVb this update: "+defecitVbreaker);
                    for (int l = 0; l < machineLegReq; l++)
                    {
                        powerAmount[l] = requestedAmount / machineLegReq;
                        //remove the resultant multiplicant from defecitVbreaker from the amount of power to be sent, then round off to three places
                        powerAmount[l] -= (powerAmount[l] * defecitVbreaker);
                        //Debug.Log("powerAmount after dVb: "+powerAmount[l]+" for "+powerAmount[l]* machineLegReq);
                        powerAmount[l] = (float)Math.Round(powerAmount[l], 3);
                    }
                    // Debug.Log("defecitVbreakerbox:" + defecitVbreaker);
                    //recalculate the requested about - all powerAmount[] indicies should be equvalent.
                    requestedAmount = powerAmount[0] * machineLegReq;
                    if (cable.CheckConnection(5))//type is breaker to SubMachine linkage
                    {
                        if (bufferCurrent - requestedAmount >= 0)
                        {
                            //transfer the uniquely requested amount to the machine
                            cable.TransferIn(machineLegReq, powerAmount, 5);
                            bufferCurrent -= requestedAmount;
                        }
                        else if (bufferCurrent - requestedAmount < 0)
                        {
                            float[] tempfloat = new float[] { bufferCurrent / 3.0f, bufferCurrent / 3.0f, bufferCurrent / 3.0f };
                            //or transfer all that remains in the buffer
                            cable.TransferIn(machineLegReq, tempfloat, 5);
                            bufferCurrent = 0f;
                        }
                    }
                    break;
                }
            }
        }

        //called at the start of the substation update block
        public bool CheckMachineState(ref IRoutingSubstation thisSubstation)
        {
            if (!mySubstations.Contains(thisSubstation))
            {
                Debug.Log("breaker added");
                mySubstations.Add(thisSubstation);
            }
            return true;
        }

        public void SwitchToggle(int numID, ref GameObject[] mySwitchLEDs)
        {
            if (targetSubMachine[numID] != null)
            {
                //play switch sound (It should not be set to loop).
                soundsource.Play();
                //if the machines are not running, turn red emissive on. If running, green.
                if (targetSubMachine[numID].RunMachine)
                {
                    targetSubMachine[numID].RunMachine = false;
                    //update switch emissive to red
                    mySwitchLEDs[0].GetComponent<Renderer>().material = MaterialLibrary.GetPowerSystemStateMaterials(3);//green to off
                    mySwitchLEDs[1].GetComponent<Renderer>().material = MaterialLibrary.GetPowerSystemStateMaterials(1);//red to on
                }
                else
                {
                    targetSubMachine[numID].RunMachine = true;
                    //update emissive to green
                    mySwitchLEDs[0].GetComponent<Renderer>().material = MaterialLibrary.GetPowerSystemStateMaterials(0);//green on
                    mySwitchLEDs[1].GetComponent<Renderer>().material = MaterialLibrary.GetPowerSystemStateMaterials(4);//red off
                }
            }
            else
            {
                Debug.Log("AAAAAAAAAAAA");
                mySwitchLEDs[0].GetComponent<Renderer>().material = MaterialLibrary.GetPowerSystemStateMaterials(3);//green to off
                mySwitchLEDs[1].GetComponent<Renderer>().material = MaterialLibrary.GetPowerSystemStateMaterials(4);//red off
                mySwitchLEDs[0].GetComponent<Renderer>().material = MaterialLibrary.GetPowerSystemStateMaterials(5);//yellow off
            }
            /*
            //if the machines are not powered, turn yellow emissive off. If running, on.
            if (targetSubMachine[numID].PowerMachine == true)
            {
                //update switch emissive to on
                mySwitchLEDs[0].GetComponent<Renderer>().material = MaterialLibrary.GetPowerSystemStateMaterials(2);//yellow on?
            }
            else
            {
                //update emissive to off
                mySwitchLEDs[0].GetComponent<Renderer>().material = MaterialLibrary.GetPowerSystemStateMaterials(5);//yellow off
            }
            */
        }

        public void ProxyStart()
        {
            if (targetSubMachine.Length > maxConnections)
            {
                ISubMachine[] temp = new ISubMachine[maxConnections];
                for (int i = 0; i < maxConnections; i++)
                {
                    temp[i] = targetSubMachine[i];
                }
                targetSubMachine = temp;
            }
            Debug.Log("SubMachine Array Length: " + targetSubMachine.Length);
            if (targetSubMachine.Length > 0)
            {
                //create a cable between breaker and machine/s
                for (int i = 0; i < targetSubMachine.Length; i++)
                {
                    if (targetSubMachine[i] != null)
                    {
                        ICable cable = new ICable(this, targetSubMachine[i]);
                        iCableDLL.AddLast(cable);
                        Debug.Log("Checking Submachine State " + i);
                        targetSubMachine[i].CheckMachineState(ref thisBreaker);
                        //add one cell to occupiedSwitches
                        occupiedSwitches = new GameObject[i + 1];
                        //Debug.Log("New occSch: " + occupiedSwitches.Length);
                    }
                }
                //button emiss logic
                int count = 0;
                GameObject[] allswitches = new GameObject[switchCount]; ;
                foreach (Transform child in transform)
                {
                    if (child.gameObject.name.Contains("BreakerBox_Switch"))
                    {
                        //Debug.Log("Adding index to all switches. Index: "+count);
                        allswitches[count] = child.gameObject;
                        //Debug.Log(child.gameObject.name + " "+ count);
                        if (count < occupiedSwitches.Length && count < 30)
                        {
                            occupiedSwitches[count] = child.gameObject;
                        }
                        count++;
                    }
                }

                //disable all button after targetmachine.length
                for (int i = targetSubMachine.Length; i < allswitches.Length; i++)//occupied switches OR switchcount
                {
                    //Debug.Log(i);
                    foreach (Transform child in allswitches[i].transform)//nullRef
                    {
                        //get yellow mesh
                        if (child.gameObject.name == "BreakerBox_Yellow")
                        {
                            child.GetComponent<Renderer>().material = MaterialLibrary.GetPowerSystemStateMaterials(5);//yellow off
                        }
                        //turn off r and g
                        else if (child.gameObject.name == "BreakerBox_Red")
                        {
                            child.GetComponent<Renderer>().material = MaterialLibrary.GetPowerSystemStateMaterials(4);//red off
                        }
                        else if (child.gameObject.name == "BreakerBox_Green")
                        {
                            //Debug.Log(child.GetComponent<Renderer>().material.ToString());
                            child.GetComponent<Renderer>().material = MaterialLibrary.GetPowerSystemStateMaterials(3);//green off
                        }
                        //disable wsbutton3
                        else if (child.gameObject.name == "WSButton3")
                        {
                            child.gameObject.SetActive(false);
                        }
                    }
                }

                //for every machine, enable the yellow emissive and enable WSButton3?
                for (int j = 0; j < targetSubMachine.Length; j++)
                {
                    if (targetSubMachine[j] != null)
                    {
                        foreach (Transform child in occupiedSwitches[j].transform)
                        {
                            //get yellow mesh
                            if (child.gameObject.name == "BreakerBox_Yellow")
                            {
                                child.GetComponent<Renderer>().material = MaterialLibrary.GetPowerSystemStateMaterials(2);
                                yellowSwitchRenderers.Add(child.GetComponent<Renderer>());
                            }
                            else if (child.gameObject.name == "BreakerBox_Red")
                            {
                                child.GetComponent<Renderer>().material = MaterialLibrary.GetPowerSystemStateMaterials(4);//red off
                            }
                            //enable wsbutton3
                            else if (child.gameObject.name == "WSButton3")
                            {
                                child.gameObject.SetActive(true);
                            }
                        }
                    }
                }
            }
        }

        public void SetMachines(ISubMachine[] newSubMachines)
        {
            targetSubMachine = newSubMachines;
        }

        public int GetLegRequirement()
        {
            return legsRequired;
        }

        public void ReceivePowerFromSubstation(int legCount, float[] amounts)
        {
            //Debug.Log("Breaker Received Power");
            //receive X legs with X amounts
            for (int i = 0; i < legCount; i++)
            {
                //Debug.Log(legCount);
                bufferCurrent += amounts[i];
            }
            legsReceived = legCount;
            bufferCurrent = (float)Math.Round(bufferCurrent, 3);
        }

        public float GetTotalRequiredPower()
        {
            return totalRequiredPower;
        }

        public Guid GetGUID()
        {
            return guid;
        }
    }
}