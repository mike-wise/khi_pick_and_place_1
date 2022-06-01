using System.Collections.Generic;
using UnityEngine;
using MmTray1Msg = RosMessageTypes.Rs007Control.MagneMotionTray1Msg;
using Unity.Robotics.ROSTCPConnector;

namespace KhiDemo
{

    public class MmTray : MonoBehaviour
    {
        MagneMotion magmo;
        MnTable mmt;

        public enum TrayBoxForm { Box, Prefab, BoxComp }

        public GameObject mmtrayrep;
        public GameObject mmtraygo;

        const int nrow = 3;
        const int ncol = 4;
        Dictionary<(int, int), bool> loadState = new Dictionary<(int, int), bool>();
        Dictionary<(int, int), GameObject> trayboxes = new Dictionary<(int, int), GameObject>();
        Dictionary<(int, int), GameObject> trayslots = new Dictionary<(int, int), GameObject>();

        public MmTray()
        {
            InitVals();
        }

        (bool ok, string errmsg) CheckIndexes(int i, int j, string rooname)
        {
            if (i < 0 || nrow <= i)
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

        public void Init(MagneMotion magmo)
        {
            this.magmo = magmo;
            this.mmt = magmo.mmt;
            // size in cm - 31.4, 28.9, 1.7
            CreateTray(gameObject.transform);
            CreateTraySlots();
        }
        void CreateTray(Transform parent)
        {
            mmtrayrep = new GameObject("mmtrayrep");

            mmtrayrep.transform.SetParent(parent, worldPositionStays: false);

            mmtraygo = UnityUt.CreateCube(mmtrayrep, "gray", size: 1, wps: false);
            mmtraygo.name = "mmtraygo";
            mmtraygo.transform.localScale = new Vector3(0.289f, 0.017f, 0.314f);
        }
        float slotw;
        float sloth;
        void CreateTraySlots()
        {
            var rowdelt = 0.03f * 3;
            var coldelt = 0.022f * 3;
            var rowstar = rowdelt * (((float)nrow - 1) / 2f);
            var colmid = 0.003f;
            var colstar = coldelt * (((float)ncol - 1) / 2f);
            var rowpos = rowstar;
            slotw = coldelt * 0.95f;
            sloth = rowdelt * 0.95f;
            for (var i = 0; i < nrow; i++)
            {
                var colpos = colstar;
                for (var j = 0; j < ncol; j++)
                {
                    var key = (i, j);
                    // so the columns are not evenly distributed (sigh)
                    var colpos1 = colpos - colmid;
                    if (j < (ncol / 2))
                    {
                        colpos1 = colpos + colmid;
                    }
                    var pt = new Vector3(colpos1, 0.02f, rowpos);

                    CreateTraySlot(key, pt);

                    colpos -= coldelt;
                }
                rowpos -= rowdelt;
            }
        }

        void CreateTraySlot((int,int) key, Vector3 pt)
        {
            var slotgo = new GameObject($"slot:{key}");
            trayslots[key] = slotgo;
            slotgo.transform.position = pt;
            slotgo.transform.SetParent(mmtrayrep.transform, worldPositionStays: false);

            var slotformgo = UnityUt.CreateCube(null, "gray", size: 1);
            slotformgo.transform.localScale = new Vector3(slotw, 0.005f, sloth);
            slotformgo.transform.SetParent(slotgo.transform, worldPositionStays: false);

        }

        void DestroyBoxes()
        {
            for (var i = 0; i < nrow; i++)
            {
                for (var j = 0; j < ncol; j++)
                {
                    var key = (i, j);
                    if (trayboxes.ContainsKey(key))
                    {
                        Destroy(trayboxes[key]);
                        trayboxes[key] = null;
                    }
                }
            }
        }

        void CreateBoxes()
        {
            for (var i = 0; i < nrow; i++)
            {
                for (var j = 0; j < ncol; j++)
                {
                    var key = (i, j);

                    var boxid = $"{key}";
                    var box = MmBox.ConstructBox(mmt.magmo, boxid, BoxStatus.onTray);
                    AttachBoxToSlot(key, box);
                    trayboxes[key] = box.gameObject;
                    loadState[key] = true;
                }
            }
            RealizeLoadStatus();
        }

        public void AttachBoxToSlot((int,int) key, MmBox box)
        {
            var slot = trayslots[key];
            box.transform.localRotation = Quaternion.Euler(0, 0, 0);
            box.transform.parent = null;
            box.transform.SetParent(slot.transform, worldPositionStays: false);
        }


        public void InitAllLoadstate(bool loaded)
        {
            DestroyBoxes();
            CreateBoxes();
            for (var i = 0; i < nrow; i++)
            {
                for (var j = 0; j < ncol; j++)
                {
                    var bkey = (i, j);
                    loadState[bkey] = loaded;
                    trayboxes[bkey].SetActive(loaded);
                }
            }
        }


        void RealizeLoadStatus()
        {
            for (var i = 0; i < nrow; i++)
            {
                for (var j = 0; j < ncol; j++)
                {
                    var bkey = (i, j);
                    var oldstat = trayboxes[bkey].activeSelf;
                    var newstat = loadState[bkey];
                    if (oldstat != newstat)
                    {
                        magmo.mmRobot.ActivateRobBox(!newstat);
                        trayboxes[bkey].SetActive(loadState[bkey]);
                    }
                }
            }
        }

        void Tray1Change(MmTray1Msg traymsg)
        {
            //Debug.Log($"Received ROS message on topic Rs007Tray1:{traymsg.ToString()}");
            var (ok, msg) = CheckIndexes(traymsg.row, traymsg.col, "Tray1Change");
            if (ok)
            {
                var oldstat = GetVal(traymsg.row, traymsg.col);
                var newstat = traymsg.loaded != 0;
                //Debug.Log($"   oldstat:{oldstat} newstat:{newstat}");
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

        bool GetVal(int i, int j)
        {
            var key = (i, j);
            if (!loadState.ContainsKey(key)) return false;
            var rv = loadState[key];
            return rv;
        }

        public void SetVal(int i, int j, bool newstat)
        {
            SetVal((i, j), newstat);
        }
        public void SetVal((int i, int j) key, bool newstat)
        {
            loadState[key] = newstat;
            if (trayboxes.ContainsKey(key))
            {
                trayboxes[key].SetActive(newstat);
            }
        }
        void InitVals()
        {
            for (int i = 0; i < nrow; i++)
            {
                for (int j = 0; j < ncol; j++)
                {
                    loadState[(i, j)] = false;
                }
            }
        }

        public (bool found,(int i,int j)) FindFirst(bool seekLoadState)
        {
            for (int i = 0; i < nrow; i++)
            {
                for (int j = 0; j < ncol; j++)
                {
                    if (loadState[(i, j)] == seekLoadState)
                    {
                        return (true,(i, j));
                    }
                }
            }
            return (false, (-1,-1));
        }
    }
}