﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;



public class TilingController : MonoBehaviour
{
    private GameObject Cmra;
    private Camera Cam;
    private GameObject Tile;
    private GameObject PlayerID;
    private MeshFilter mesh;
    public bool AllowBuilding;
    private bool SnapToGrid;

    private Vector3 Coords;
    private Vector3 debugrayend;

    // private bool sw = false;

    public GameObject BuildingMenu;
    public GameObject Button;
    private GameObject temp;
    private string FilePath;

    private GameObject newButton;
    private SelectTileButton but;



    //material var
    public Shader Shad;
    private Material TileMat;
   

    //  public TileCollection tileContainer = TileCollection.Load(Path.Combine(Application.dataPath, "Tiles.xml"));

    // Start is called before the first frame update
    void Start()
    {
        Cmra = GameObject.Find("Main Camera");
        Cam = Cmra.GetComponent<Camera>();

        SnapToGrid = true;

        ReadXMLTiles();

        /* SAVING XML FILE FOR TESTING - THIS WILL GO FOR EXTERIOR EDITOR
          if (sw == false)
         {
             SaveXMLTiles();
             sw = true;
          }
        */

        Tile = new GameObject();
        Tile.name = "TileGhost";
        Tile.tag = "TilingGhost";

        PlayerID = new GameObject();
        PlayerID.name = "PlayerID";

    }


    // Update is called once per frame
    void Update()
    {
        if (AllowBuilding)
        {
            Tile.SetActive(true);

            RaycastHit hit;
            Ray ray = Cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                if (Physics.Raycast(ray))
                {
                    //this is for debug on Scene, draw line for raycasting
                    Debug.DrawLine(hit.point, debugrayend, Color.red);
                    //   Debug.Log(hit.point);

                    if (hit.transform.gameObject.tag != "TilingGhost")  // prevents from raycasting tile

                        //tile snapping into 1x1x1 grid
                        if (SnapToGrid)
                        {
                            Coords.x = Mathf.Round(hit.point.x);
                            Coords.y = Mathf.Round(hit.point.y);
                            Coords.z = Mathf.Round(hit.point.z);
                        }

                    Tile.transform.position = Vector3.Lerp(Tile.transform.position, Coords, Time.deltaTime * 15f); //moving tile

                    if (Input.GetButtonDown("Rotate"))  //rotating tile 90 degree
                    {
                        Tile.transform.rotation = Tile.transform.rotation * Quaternion.Euler(0, 90 * Input.GetAxisRaw("Rotate"), 0);
                        //Tile.transform.rotation = Quaternion.Lerp(Tile.transform.rotation, Tile.transform.rotation * Quaternion.Euler(0, 90 * Input.GetAxisRaw("Rotate"), 0), Time.deltaTime * 10f); WIP
                    }
                }
            }

            //simple building without restrictions
            if (Input.GetMouseButtonDown(0))
            {
                PlaceTile();
            }


        }
        else
            Tile.SetActive(false);



    }


    private LODGroup group;
    private Texture colorMask;    //public for debug - should be private
    private Texture Albedo;
    private Texture Metal;
    private Texture Emissive;
    private Texture Normal;
    private Texture Detail;
    private Texture Dirt;
    private Color mainColor;
    private Color secColor;
    private Color detColor;
    private Color trimColor;
    private Color emColor;

    private MaterialContainer matContainer;
    private bool matXml;

    //READING / SAVING XML FILE

    public void ReadXMLTiles()
    {
        var tileContainer = TileCollection.Load(Path.Combine(Application.dataPath, "Tiles.xml"));  //Loading from XML

      //  Debug.Log(matContainer.material[0].ColorMask);


        var parentDatabase = new GameObject();       //Creating tile database gameobjects
        parentDatabase.name = "Tile_Database";
        parentDatabase.layer = 10;
        for (int c = 0; c < tileContainer.tiles.Length; c++)
        {

            //##########################PROCEDURE FOR LOADED TILE#############################################


            FilePath = tileContainer.tiles[c].model_path;   //reading model info 
          //  Debug.Log(tileContainer.tiles[c].model_path);
            //  FilePath = "Models/Tiles/Floors/nukeguard/mesh1";
            GameObject model = Resources.Load<GameObject>(FilePath);
            GameObject obj = (GameObject)Instantiate(model);

          //  Debug.Log("searching for: " + tileContainer.tiles[c].model_path + ".xml");
           // Debug.Log(Application.dataPath +"/Resources/" + tileContainer.tiles[c].model_path);
            if (System.IO.File.Exists(Application.dataPath + "/Resources/" + tileContainer.tiles[c].model_path + ".xml"))
            {
                Debug.Log("found: " + tileContainer.tiles[c].model_path + ".xml");
                matContainer = MaterialContainer.Load(Path.Combine(Application.dataPath + "/Resources/" + tileContainer.tiles[c].model_path + ".xml"));  //Loading from XML
                matXml = true;

            }
            else
                matXml = false;

            //LOAD AND INSTANTIATE MODEL





            // --------------------------Creating and assigning tile metadata------------------------------------
            TileMetadata mt = obj.AddComponent(typeof(TileMetadata)) as TileMetadata;
            mt.type = tileContainer.tiles[c].type;
            mt.name = tileContainer.tiles[c].Name;
            mt.description = "Description to implement";
            //-------------------------------------------------------------------------------------


            //-------------------put in database as not renderable with all children--------------------------
            foreach (Transform child in obj.transform)
            {
                child.gameObject.layer = 10;
                foreach (Transform child2 in child.transform)
                    child2.gameObject.layer = 10;
            }

            obj.transform.SetParent(parentDatabase.transform);
            obj.name = c.ToString();    //Adding and assigning tiles into database, change name by tile ID position


            //-------------Create GUI with buttons-----------
            newButton = Instantiate(Button);
            but = newButton.GetComponent<SelectTileButton>();
            but.TileID = c;
            newButton.GetComponentInChildren<Text>().text = tileContainer.tiles[c].Name;
            newButton.transform.SetParent(BuildingMenu.transform);
            //-----------------------------------------------


            // TO REMOVE WHEN ALL TILES IS UNIFIED - Blender garbage hotfix ;)
            if (obj.transform.Find("Camera") != null)
            {
                obj.transform.Find("Camera").GetComponent<Camera>().enabled = false;
            }
            if (obj.transform.Find("hitbox") != null)
            {
                if (obj.transform.Find("hitbox").GetComponent<MeshRenderer>() != null)
                    obj.transform.Find("hitbox").GetComponent<MeshRenderer>().enabled = false;   //disable render of hitbox
            }
            // END REMOVE


            //------------------------IF TILE HAS SCALE ANIMATION BEHAVIOR--------------------------------
            // Debug.Log(obj.transform.childCount);
            if (tileContainer.tiles[c].Scalable)
            {
                //Assign in runtime animator
                Animator ContainerAnimator = obj.GetComponent<Animator>();
                ContainerAnimator.runtimeAnimatorController = Resources.Load(tileContainer.tiles[c].model_path) as RuntimeAnimatorController;
                ContainerAnimator.StopPlayback();
                ContainerAnimator.enabled = false;
                //and stop playing and disable by default
            }
            //---------------------------------------------------------------------------------------


            //----------------MATERIAL INSTANTIATE---------


            // if (TileMat != null)
            //   TileMat.shader = Shad;
            if (matXml)
            {
                colorMask = Resources.Load<Texture2D>(matContainer.material[0].ColorMask);   //Loading textures from XML
                Albedo = Resources.Load<Texture2D>(matContainer.material[0].Albedo);
                Metal = Resources.Load<Texture2D>(matContainer.material[0].Metal);
                Emissive = Resources.Load<Texture2D>(matContainer.material[0].Emmisive);
                Normal = Resources.Load<Texture2D>(matContainer.material[0].Normal);
                Detail = Resources.Load<Texture2D>(matContainer.material[0].Detail);
                Dirt = Resources.Load<Texture2D>(matContainer.material[0].Dirt);

                if (colorMask != null)                                      //Setting textures into new material 
                    TileMat = new Material(Shad);

                ColorUtility.TryParseHtmlString(matContainer.material[0].MainColor, out mainColor);    //convert hex into color
                ColorUtility.TryParseHtmlString(matContainer.material[0].SecColor, out secColor);
                ColorUtility.TryParseHtmlString(matContainer.material[0].DetailColor, out detColor);
                ColorUtility.TryParseHtmlString(matContainer.material[0].TrimColor, out trimColor);
                ColorUtility.TryParseHtmlString(matContainer.material[0].EmissionColor, out emColor);

                TileMat.SetTexture("_CM", colorMask);     //assigning textures and colors into new material
                TileMat.SetTexture("_AL", Albedo);
                TileMat.SetTexture("_MT", Metal);
                TileMat.SetTexture("_EM", Emissive);
                TileMat.SetTexture("_NM", Normal);
                TileMat.SetTexture("_DM", Detail);
                TileMat.SetTexture("_DW", Dirt);
                TileMat.SetColor("_MC", mainColor);
                TileMat.SetColor("_SC", secColor);
                TileMat.SetColor("_DC", detColor);
                TileMat.SetColor("_TC", trimColor);
                TileMat.SetColor("_EC", emColor);
                TileMat.SetFloat("_EI", 7);
                TileMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;    // realtime emissive flag

                if (obj.transform.Find("model") != null)                                //Assigning new instance of material to model
                {
                    for (int m = 0; m < obj.transform.Find("model").GetComponentInChildren<MeshRenderer>().materials.Length; m++)      //For every material in model
                        obj.transform.Find("model").GetComponentInChildren<MeshRenderer>().materials[m].CopyPropertiesFromMaterial(TileMat);
                }

            }
                //---------------------------------------------


                //--------------------------------Building LOD groups------------------------------ 
                if (obj.transform.Find("model") != null)
                {
                    group = obj.AddComponent<LODGroup>();

                    // Add 4 LOD levels
                    LOD[] lods = new LOD[4];

                    for (int i = 0; i < 4; i++)
                    {
                        GameObject primType = obj.transform.Find("model").gameObject;
                        switch (i)
                        {
                            case 1:
                                if (obj.transform.Find("lod1") != null)
                                    primType = obj.transform.Find("lod1").gameObject;
                                break;
                            case 2:
                                if (obj.transform.Find("lod2") != null)
                                    primType = obj.transform.Find("lod2").gameObject;
                                break;
                            case 3:
                                if (obj.transform.Find("lod3") != null)
                                    primType = obj.transform.Find("lod3").gameObject;
                                break;
                        }
                        Renderer[] renderers = new Renderer[1];
                        renderers[0] = primType.GetComponentInChildren<Renderer>();
                        lods[i] = new LOD(1.0F / (i + 12f), renderers); // i+1.2f
                    }
                    group.SetLODs(lods);
                    group.RecalculateBounds();
                }
                //--------------------------------------------------------------------------------
            }
        }

        public void SaveXMLTiles()
        {
            var tileContainer = TileCollection.Load(Path.Combine(Application.dataPath, "Tiles.xml"));
            tileContainer.Save(Path.Combine(Application.persistentDataPath, "Tiles.xml"));
        }

        public void PlaceTile()
        {

            if (Tile.transform.childCount > 0)
            {
                Debug.Log(Tile.gameObject.transform.GetChild(0).gameObject);
                GameObject newTileChild = Tile.gameObject.transform.GetChild(0).gameObject;
                GameObject newTile = (GameObject)Instantiate(newTileChild);
                newTile.transform.localPosition = Coords;
                newTile.transform.localRotation = Tile.transform.rotation;
                newTile.transform.localScale = Tile.transform.localScale;
                newTile.transform.SetParent(PlayerID.transform);
                newTile.GetComponent<TileMetadata>().buildBy = "PeterHammerman test";
            }
        }

        public void RemoveTile()
        {

        }
    }

