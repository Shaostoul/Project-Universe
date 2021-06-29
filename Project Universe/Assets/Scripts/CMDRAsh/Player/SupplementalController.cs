﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectUniverse.Player;
using ProjectUniverse.Base;
using ProjectUniverse.Serialization.Handler;
using ProjectUniverse.Serialization;
using UnityEditor;
using ProjectUniverse.Environment.Volumes;
using System;

namespace ProjectUniverse.Player.PlayerController
{
    [Serializable]
    public class SupplementalController : MonoBehaviour
    {
        private GUID guid;
        public string crouchKey;
        public string proneKey;
        public bool crouchToggle;
        public bool crouching;
        public bool prone;
        [SerializeField] GameObject playerRoot;
        [SerializeField] GameObject cameraRoot;
        [SerializeField] private float crouchHeight;
        [SerializeField] private float proneHeight;
        [SerializeField] private float shrinkerSize;
        [SerializeField] private float defaultHeight;
        //Player stats2
        [SerializeField] private float playerHealth = 100f;//Non-standard. Radiation, suffocation, etc.
        [SerializeField] private float headHealth = 45f;
        [SerializeField] private float chestHealth = 225f;
        [SerializeField] private float lArmHealth = 110f;
        [SerializeField] private float rArmHealth = 110f;
        [SerializeField] private float lHandHealth = 25f;
        [SerializeField] private float rHandHealth = 25f;
        [SerializeField] private float lLegHealth = 125f;
        [SerializeField] private float rLegHealth = 125f;
        [SerializeField] private float lFootHealth = 25f;
        [SerializeField] private float rFootHealth = 25f;
        [SerializeField] private float playerHydration = 100f;
        [SerializeField] private float playerHappyStomach = 100f;

        public float HeadHealth
        {
            get { return headHealth; }
            set { headHealth = value; }
        }
        public float ChestHealth
        {
            get { return chestHealth; }
            set { chestHealth = value; }
        }
        public float LArmHealth
        {
            get { return lArmHealth; }
            set { lArmHealth = value; }
        }
        public float RArmHealth
        {
            get { return rArmHealth; }
            set { rArmHealth = value; }
        }
        public float LHandHealth
        {
            get { return lHandHealth; }
            set { lHandHealth = value; }
        }
        public float RHandHealth
        {
            get { return rHandHealth; }
            set { rHandHealth = value; }
        }
        public float LLegHealth
        {
            get { return lLegHealth; }
            set { lLegHealth = value; }
        }
        public float RLegHealth
        {
            get { return rLegHealth; }
            set { rLegHealth = value; }
        }
        public float LFootHealth
        {
            get { return lFootHealth; }
            set { lFootHealth = value; }
        }
        public float RFootHealth
        {
            get { return rFootHealth; }
            set { rFootHealth = value; }
        }
        public float PlayerHydration
        {
            get { return playerHydration; }
            set { playerHydration = value; }
        }
        public float PlayerHappyStomach
        {
            get { return playerHappyStomach; }
            set { playerHappyStomach = value; }
        }

        private void Awake()
        {
            guid = GUID.Generate();
        }

        // Update is called once per frame
        void Update()
        {
            //crouch (hold c)
            if (!crouchToggle)
            {
                if (Input.GetKeyDown(crouchKey))
                {
                    crouching = true;
                    prone = false;
                    playerRoot.transform.localScale = new Vector3(playerRoot.transform.localScale.x,
                        crouchHeight, playerRoot.transform.localScale.z);//.GetComponent<CapsuleCollider>()
                }
                if (Input.GetKeyUp(crouchKey))
                {
                    crouching = false;
                    prone = false;
                    playerRoot.transform.localScale = new Vector3(playerRoot.transform.localScale.x,
                        defaultHeight, playerRoot.transform.localScale.z);
                }
            }
            //crouch (press c)
            else
            {
                if (Input.GetKeyDown(crouchKey))
                {
                    if (crouching)
                    {
                        crouching = false;
                        prone = false;
                        playerRoot.transform.localScale = new Vector3(1.0f, defaultHeight, 1.0f);
                    }
                    else
                    {
                        crouching = true;
                        prone = false;
                        playerRoot.transform.localScale = new Vector3(1.0f, crouchHeight, 1.0f);
                    }
                }
            }
            //prone (press z)
            if (Input.GetKeyDown(proneKey))
            {
                if (prone)
                {
                    crouching = false;
                    prone = false;
                    playerRoot.transform.localScale = new Vector3(1.0f,
                    defaultHeight, 1.0f);
                    playerRoot.GetComponent<CharacterController>().height = defaultHeight;
                    playerRoot.GetComponent<CharacterController>().radius = 0.31f;
                    //playerRoot.GetComponent<CapsuleCollider>().height = defaultHeight;
                    //playerRoot.GetComponent<CapsuleCollider>().radius = 0.31f;
                }
                else
                {
                    crouching = false;
                    prone = true;
                    playerRoot.transform.localScale = new Vector3(1.0f, proneHeight, 1.0f);
                    playerRoot.GetComponent<CharacterController>().height = proneHeight;
                    playerRoot.GetComponent<CharacterController>().radius = shrinkerSize;
                    //playerRoot.GetComponent<CapsuleCollider>().height = proneHeight;
                    //playerRoot.GetComponent<CapsuleCollider>().radius = shrinkerSize;
                }
            }
            if (Input.GetKeyDown(KeyCode.I))
            {
                IPlayer_Inventory inventory = playerRoot.GetComponent<IPlayer_Inventory>();
                inventory.GetPlayerInventoryUI().ToggleDisplay();
                //inventory.GetPlayerInventoryUI().UpdateDisplay();
            }
            if (Input.GetKeyDown(KeyCode.Q))
            {
                IPlayer_Inventory inventory = playerRoot.GetComponent<IPlayer_Inventory>();
                foreach (ItemStack stack in inventory.GetPlayerInventory())
                {
                    Debug.Log(stack);
                }
            }
        }

        public void InflictPlayerDamage(float amount)
        {

            // \/ Naw. Let health be negative. Competative dying.
            //if (playerHealth <= 0)
            //{
            //    playerHealth = 0;
            //}
            playerHealth -= amount;
        }
        public float PlayerHealth
        {
            get { return playerHealth; }
            set { playerHealth = value; }
        }


        public void SavePlayer()
        {
            PlayerData data = new PlayerData(guid, playerRoot.transform,cameraRoot.transform.rotation.eulerAngles,
                GetComponent<IPlayer_Inventory>(), GetComponent<PlayerVolumeController>(),this);
            SerializationHandler.SavePlayer("Player_current",data);
            Debug.Log("Saved");
        }

        public void LoadPlayer()
        {
            PlayerData data = SerializationHandler.Load("Player_current");
            if(data != null)
            {
                //Debug.Log("...Injecting");
                guid = data.GetGUID();
                //Debug.Log(data.Position);
                Vector3 dataRot = data.Rotation.eulerAngles;
                playerRoot.transform.position = data.Position;
                playerRoot.transform.rotation = Quaternion.Euler(0, dataRot.y, 0);
                cameraRoot.transform.rotation = Quaternion.Euler(dataRot.x, 0, dataRot.z);
                playerRoot.transform.localScale = data.Scale;
                object[] prams = 
                    { data.PlayerInventory, data.InventoryWeight };
                GetComponent<IPlayer_Inventory>().OnLoad(prams);
                prams = new object[]{
                data.GetPlayerVolumeController(),
                    };
                GetComponent<PlayerVolumeController>().OnLoad(prams);

                crouchToggle = data.LoadStatsSupplement.crouchToggle;
                crouching = data.LoadStatsSupplement.crouching;
                prone = data.LoadStatsSupplement.prone;
                PlayerHealth = data.LoadStatsSupplement.PlayerHealth;
                HeadHealth = data.LoadStatsSupplement.HeadHealth;
                ChestHealth = data.LoadStatsSupplement.ChestHealth;
                LArmHealth = data.LoadStatsSupplement.LArmHealth;
                RArmHealth = data.LoadStatsSupplement.RArmHealth;
                LHandHealth = data.LoadStatsSupplement.LHandHealth;
                RHandHealth = data.LoadStatsSupplement.RHandHealth;
                LLegHealth = data.LoadStatsSupplement.LLegHealth;
                RLegHealth = data.LoadStatsSupplement.RLegHealth;
                LFootHealth = data.LoadStatsSupplement.LFootHealth;
                RFootHealth = data.LoadStatsSupplement.RFootHealth;
                PlayerHydration = data.LoadStatsSupplement.PlayerHydration;
                PlayerHappyStomach = data.LoadStatsSupplement.PlayerHappyStomach;
            }
            else
            {
                Debug.LogError("Failed to Load");
            }
           
        }

    }
}