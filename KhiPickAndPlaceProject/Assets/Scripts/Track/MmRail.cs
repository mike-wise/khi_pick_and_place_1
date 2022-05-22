using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MmRail : MonoBehaviour
{
    MmTable mmt;
    public enum RailForm { Cigar,Box }
    static float sphrad = 0.2f;
    float speed;
    int pathnum;
    float pathdist;
    RailForm railform;
    GameObject geomgo;
    GameObject formgo;
    public string sledid;
    // Start is called before the first frame update
    void Start()
    {
    }


    public static MmRail ConstructRail(MmTable mmt, string rname, int pathnum, float pathdist)
    {
        var railgo = new GameObject(rname);
        var (pt, ang) = mmt.GetPositionAndOrientation(pathnum, pathdist);
        railgo.transform.position = pt;
        railgo.transform.rotation = Quaternion.Euler(0, 0, -ang);
        var rail = railgo.AddComponent<MmRail>();
        var railform = MmRail.RailForm.Cigar;
        var sledid = "";
        rail.ConstructForm(mmt, railgo, railform, sledid, pathnum, pathdist, speed: 0);
        railgo.transform.parent = mmt.mmtgo.transform;
        return rail;
    }

    public void ConstructForm(MmTable mmt,GameObject geomgo, RailForm railform, string sledid,int pathnum,float pathdist,  float speed=0)
    {
        this.geomgo = geomgo;
        this.mmt = mmt;
        this.railform = railform;
        this.speed = speed;
        formgo = new GameObject("railform");
        this.pathnum = pathnum;
        this.pathdist = pathdist;
        this.sledid = sledid;
        var pt = Vector3.zero;
        var ang = 0f;
        (pt,ang) = mmt.GetPositionAndOrientation(pathnum,pathdist);
        switch (this.railform)
        {
            case RailForm.Cigar:
                {
                    var go = UnityUt.CreateSphere(formgo,"gray", size: sphrad / 3);
                    go.name = $"cigar";
                    go.transform.localScale = new Vector3(0.1f, 0.3f, 0.1f);

                    var go1 = UnityUt.CreateSphere(formgo, "red", size: sphrad / 3);
                    go1.name = $"nose";
                    go1.transform.position = new Vector3(0.0f, 0.1f, 0);
                    go1.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    break;
                }
            case RailForm.Box:
                {
                    var go = UnityUt.CreateCube(formgo, "gray", size: sphrad / 3);
                    go.name = $"tray";
                    // 6.5x11.0x2cm
                    go.transform.localScale = new Vector3(0.88f, 0.52f, 0.16f);


                    var clr = UnityUt.GetRandomColorString();
                    var go2 = UnityUt.CreateSphere(formgo,clr, size: sphrad / 3);
                    go2.name = $"nose";
                    go2.transform.position = new Vector3(0.0f, 0.2f, -0.16f);
                    go2.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    break;
                }

        }
        formgo.SetActive(pathnum >= 0);
        formgo.transform.position = pt;
        formgo.transform.rotation = Quaternion.Euler(0, 0, -ang);
        if (mmt.useMeters)
        {
            var u2m = mmt.UnitsToMeters;
            formgo.transform.localScale = new Vector3( u2m,u2m,u2m );
        }
        formgo.transform.parent = geomgo.transform;
        //AdjustSledPositionAndOrientation(pt, ang);
    }




    int updatecount = 0;
    // Update is called once per frame
    void Update()
    {
        updatecount++;
    }
}
