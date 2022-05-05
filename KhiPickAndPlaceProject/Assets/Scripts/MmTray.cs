using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Rs007Control;
using MmTray1Msg = RosMessageTypes.Rs007Control.MagneMotionTray1Msg;
using Unity.Robotics.ROSTCPConnector;



public class MmTray : MonoBehaviour
{
    MmTable mmt;
    public GameObject mmtraygo;

    const int nrow = 4;
    const int ncol = 3;
    Dictionary<(int, int), bool> loaded = new Dictionary<(int, int), bool>();
    Dictionary<(int, int), GameObject> box = new Dictionary<(int, int), GameObject>();

    bool positionOnFloor = true;

    public MmTray()
    {
        initVals();
    }

    (bool ok,string errmsg) CheckIndexes(int i,int j,string rooname)
    {
        if (i<0 || nrow<=i)
        {
            var msg = $"row out of rannge:{i} max:{nrow - 1} in {rooname}";
            return (false, msg);
        }
        if (j < 0 || ncol <= j)
        {
            var msg = $"col out of rannge:{j} max:{ncol - 1} in {rooname}";
            return (false, msg);
        }
        return (true, "");
    }


    void Start()
    {
        ROSConnection.GetOrCreateInstance().Subscribe<MmTray1Msg>("Rs007Tray1", Tray1Change);
    }

    public void Init(MmTable mmt)
    {
        this.mmt = mmt;
        // size in cm - 31.4, 28.9, 1.7 
        var floorgo = GameObject.Find("Floor");
        if (positionOnFloor && floorgo != null)
        {

            // attach to floor if there is one
            MmUtil.mmcolor = Color.gray;
            mmtraygo = MmUtil.CreateCube(floorgo, size: 1);
            mmtraygo.transform.parent = null;
            mmtraygo.transform.position = new Vector3(0.63f, 0.314f, 0.017f);
            mmtraygo.transform.SetParent(floorgo.transform, worldPositionStays: false);
            mmtraygo.name = "mmtraygo";
            //// move it behind the robot and up to the first robot joint 
            mmtraygo.transform.localScale = new Vector3(0.289f, 0.017f, 0.314f);
        }
        else
        {
            MmUtil.mmcolor = Color.gray;
            mmtraygo.transform.localScale = new Vector3(0.289f, 0.314f, 0.017f);

            mmtraygo.transform.position = new Vector3(0, 0, 0);
        }
        CreateBoxes();
    }
    void CreateBoxes()
    {
        var rowdelt = 0.25f;
        var coldelt = 0.3f;
        var rowstar = -rowdelt * (((float)nrow-1) / 2f);
        var colstar = -coldelt * (((float)ncol-1) / 2f);
        var rowpos = rowstar;
        for (var i = 0; i< nrow; i++)
        {
            //var msg = $"i:{i} rowpos:{rowpos:f4}";
            //Debug.Log(msg);
            var colpos = colstar;
            for( var j=0; j<ncol; j++)
            {
                //var msgj = $"     j:{j} colpos:{colpos:f4}";
                //Debug.Log(msgj);
                var pt = new Vector3(rowpos, 1, colpos);
                MmUtil.mmcolor = Color.yellow;
                var boxgo = MmUtil.CreateCube(null, size: 1);
                boxgo.name = $"box {i}-{j}";
                boxgo.transform.parent = null;
                boxgo.transform.position = pt;
                boxgo.transform.localScale = new Vector3(0.12f, 1, 0.16f );
                boxgo.transform.SetParent(mmtraygo.transform,worldPositionStays:false);
                box[(i, j)] = boxgo;
                loaded[(i, j)] = true;
                colpos += coldelt;
            }
            rowpos += rowdelt;
        }
        RealizeLoadStatus();
    }

    void RealizeLoadStatus()
    {
        for (var i = 0; i < nrow; i++)
        {
            for (var j = 0; j < ncol; j++)
            {
                var bkey = (i, j);
                box[bkey].SetActive(loaded[bkey]);
            }
        }
    }

    void Tray1Change(MmTray1Msg traymsg)
    {
        Debug.Log($"Received ROS message on topic Rs007Tray1:{traymsg.ToString()}");
        var (ok, msg) = CheckIndexes(traymsg.row, traymsg.col, "Tray1Change");
        if (ok)
        {
            var oldstat = GetVal(traymsg.row, traymsg.col);
            var newstat = traymsg.loaded != 0;
            Debug.Log($"   oldstat:{oldstat} newstat:{newstat}");
            if (oldstat != newstat)
            {
                SetVal(traymsg.row, traymsg.col, traymsg.loaded != 0);
                RealizeLoadStatus();
            }
        }
        else
        {
            Debug.LogError(msg);
        }
    }

    bool GetVal(int i,int j)
    {
        var key = (i, j);
        if (!loaded.ContainsKey(key)) return false;
        var rv = loaded[key];
        return rv;
    }

    void SetVal(int i, int j, bool newstat)
    {
        var key = (i, j);
        loaded[key] = newstat;
    }
    void initVals()
    {
        for( int i = 1; i< nrow; i++)
        {
            for (int j = 1; j < ncol; j++)
            {
                loaded[(i,j)] = false;
            }
        }
    }


    // Update is called once per frame
    void Update()
    {
    }
}