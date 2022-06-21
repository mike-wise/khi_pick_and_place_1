using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace KhiDemo
{
    public enum RobStatus { busy,idle }
    public class MmController : MonoBehaviour
    {


        public MmMode mmMode = MmMode.None;
        public MmSubMode mmSubMode = MmSubMode.None;
        public MmBoxMode mmBoxMode = MmBoxMode.FakePooled;

        public MmRobot mmRobot = null;
        public MmTray mmtray = null;


        MagneMotion magmo;
        MmTable mmt;

        [Header("Simulation")]
        public bool processStep;
        [Space(10)]
        public bool reverseTrayRail;
        public bool doubleSpeed;
        public bool halfSpeed;

        float srobsec = 0.4f;
        float lrobsec = 1.0f;

        public RobStatus robstatus;

        public void Init(MagneMotion magmo)
        {
            this.magmo = magmo;
            mmt = magmo.mmt;
            mmtray = magmo.mmtray;
            mmRobot = magmo.mmRobot;
            robstatus = RobStatus.idle;
        }
        // Start is called before the first frame update
        void Start()
        {
        }

        public void Clear()
        {
            mmRobot.Clear();
            mmt.Clear();
            mmtray.Clear();
            MmBox.Clear();
        }

        public MmBoxMode InferBoxMode(MmMode mode)
        {
            switch (mode)
            {
                case MmMode.None:
                    return MmBoxMode.FakePooled;
                case MmMode.Echo:
                    return MmBoxMode.FakePooled;
                case MmMode.SimuRailToRail:
                    return MmBoxMode.RealPooled;
                case MmMode.StartRailToTray:
                    return MmBoxMode.RealPooled;
                default:
                case MmMode.StartTrayToRail:
                    return MmBoxMode.RealPooled;
            }
        }

        public void SetModeFast(MmMode newMode)
        {
            Debug.Log($"SetModeFast {newMode}");
            var newBoxMode = InferBoxMode(newMode);
            if (newBoxMode == MmBoxMode.RealPooled && newBoxMode == mmBoxMode)
            {
                mmMode = newMode;
                magmo.mmMode = newMode;
                switch (newMode)
                {
                    case MmMode.SimuRailToRail:
                        mmSubMode = MmSubMode.None;
                        break;
                    case MmMode.StartRailToTray:
                        mmSubMode = MmSubMode.RailToTray;
                        break;
                    case MmMode.StartTrayToRail:
                        mmSubMode = MmSubMode.TrayToRail;
                        break;
                }
            }
            else
            {
                SetMode(newMode, clear: true);
            }
            magmo.CheckNetworkActivation();
            CheckConsistency();
        }

        public void SetMode(MmMode newMode,bool clear=false)
        {
            Debug.Log($"SetMode {newMode} clear:{clear}");
            if (clear)
            {
                Clear();
            }
            mmMode = newMode;
            magmo.mmMode = newMode;
            mmBoxMode = InferBoxMode(newMode);
            switch (newMode)
            {
                default:
                case MmMode.Echo:
                    mmSubMode = MmSubMode.None;
                    magmo.boxForm = MmBox.BoxForm.Prefab;
                    magmo.echoMovementsRos = true;
                    magmo.publishMovementsRos = false;
                    magmo.publishMovementsZmq = false;
                    magmo.mmRobot.RealiseRobotPose(RobotPose.rest);
                    mmt.SetupSledSpeeds(SledSpeedDistrib.fixedValue, 0);

                    mmRobot.InitRobotBoxState(startLoadState:true);
                    mmt.SetupSledLoads(SledLoadDistrib.allLoaded);
                    mmtray.InitAllLoadstate(nbox: 12); 
                    break;
                case MmMode.SimuRailToRail:
                    mmSubMode = MmSubMode.None;
                    magmo.boxForm = MmBox.BoxForm.PrefabWithMarkerCube;
                    magmo.echoMovementsRos = false;
                    magmo.publishMovementsRos = false;// queue is always full
                    magmo.publishMovementsZmq = true;


                    magmo.mmRobot.RealiseRobotPose(RobotPose.rest);
                    mmt.SetupSledSpeeds(SledSpeedDistrib.alternateHiLo, 0.5f);

                    mmRobot.InitRobotBoxState(startLoadState: false);
                    mmtray.InitAllLoadstate(nbox: 5);
                    mmt.SetupSledLoads(SledLoadDistrib.alternateLoadedUnloaded);
                    break;
                case MmMode.StartRailToTray:
                    mmSubMode = MmSubMode.RailToTray;
                    magmo.echoMovementsRos = false;
                    magmo.publishMovementsRos = false;// queue is always full
                    magmo.publishMovementsZmq = true;

                    magmo.boxForm = MmBox.BoxForm.PrefabWithMarkerCube;
                    mmRobot.InitRobotBoxState(startLoadState: false);
                    mmt.SetupSledSpeeds( SledSpeedDistrib.alternateHiLo, 0.5f);
                    mmt.SetupSledLoads(SledLoadDistrib.allLoaded);
                    mmtray.InitAllLoadstate(nbox: 0);
                    magmo.mmRobot.RealiseRobotPose(RobotPose.rest);
                    break;
                case MmMode.StartTrayToRail:
                    mmSubMode = MmSubMode.TrayToRail;
                    magmo.boxForm = MmBox.BoxForm.PrefabWithMarkerCube;
                    magmo.echoMovementsRos = false;
                    magmo.publishMovementsRos = false;// queue is always full
                    magmo.publishMovementsZmq = true;

                    mmRobot.InitRobotBoxState(startLoadState: false);
                    mmt.SetupSledSpeeds( SledSpeedDistrib.alternateHiLo, 0.5f);
                    mmt.SetupSledLoads(SledLoadDistrib.allUnloaded);
                    mmtray.InitAllLoadstate(nbox: 10);
                    magmo.mmRobot.RealiseRobotPose(RobotPose.rest);
                    break;
            }
            magmo.CheckNetworkActivation();
            CheckConsistency();
        }


        public void AdjustRobotSpeedFactor(float fak)
        {
            srobsec /= fak;
            lrobsec /= fak;
        }


        public void DoReverseTrayRail()
        {
            if (mmSubMode == MmSubMode.RailToTray)
            {
                Debug.Log($"Reversing submode to {MmSubMode.TrayToRail}");
                mmSubMode = MmSubMode.TrayToRail;
            }
            else if (mmSubMode == MmSubMode.TrayToRail)
            {
                Debug.Log($"Reversing submode to {MmSubMode.RailToTray}");
                mmSubMode = MmSubMode.RailToTray;
            }
        }

        public void ReverseTrayRail()
        {
            if (!reverseTrayRail) return;
            reverseTrayRail = false;
            DoReverseTrayRail();
        }

        IEnumerator TransferBoxFromSledToRobot(MmSled sled,MmRobot rob)
        {
            yield return new WaitUntil(() => robstatus == RobStatus.idle);
            robstatus = RobStatus.busy;
            mmRobot.RealiseRobotPose(RobotPose.fcartup);
            yield return new WaitForSeconds(srobsec);
            mmRobot.RealiseRobotPose(RobotPose.fcartdn);
            yield return new WaitForSeconds(lrobsec);
            var box = sled.DetachhBoxFromSled();
            if (box != null)
            {
                rob.AttachBoxToRobot(box);
            }
            mmRobot.RealiseRobotPose(RobotPose.fcartup);
            yield return new WaitForSeconds(srobsec);
            if (mmMode == MmMode.SimuRailToRail)
            {
                mmRobot.RealiseRobotPose(RobotPose.restr2r);
            }
            else
            {
                mmRobot.RealiseRobotPose(RobotPose.rest);
            }
            yield return new WaitForSeconds(lrobsec);
            robstatus = RobStatus.idle;
        }

        IEnumerator TransferBoxFromRobotToSled(MmRobot rob, MmSled sled)
        {
            yield return new WaitUntil(() => robstatus == RobStatus.idle);
            robstatus = RobStatus.busy;
            mmRobot.RealiseRobotPose(RobotPose.ecartup);
            yield return new WaitForSeconds(srobsec);
            mmRobot.RealiseRobotPose(RobotPose.ecartdn);
            yield return new WaitForSeconds(lrobsec);
            var box = rob.DetachhBoxFromRobot();
            if (box != null)
            {
                sled.AttachBoxToSled(box);
            }
            mmRobot.RealiseRobotPose(RobotPose.ecartup);
            yield return new WaitForSeconds(srobsec);
            if (mmMode == MmMode.SimuRailToRail)
            {
                mmRobot.RealiseRobotPose(RobotPose.restr2r);
            }
            else
            {
                mmRobot.RealiseRobotPose(RobotPose.rest);
            }
            yield return new WaitForSeconds(lrobsec);
            robstatus = RobStatus.idle;
        }

        IEnumerator TransferBoxFromTrayToRobot((int,int) key, MmRobot rob)
        {
            yield return new WaitUntil(() => robstatus == RobStatus.idle);
            robstatus = RobStatus.busy;
            var (poseup, posedn) = mmRobot.GetPoses(key);
            mmRobot.RealiseRobotPose(poseup);
            yield return new WaitForSeconds(srobsec);
            mmRobot.RealiseRobotPose(posedn);
            yield return new WaitForSeconds(lrobsec);
            var box = mmtray.DetachhBoxFromTraySlot(key);
            if (box != null)
            {
                rob.AttachBoxToRobot(box);
            }
            mmRobot.RealiseRobotPose(poseup);
            yield return new WaitForSeconds(srobsec);
            mmRobot.RealiseRobotPose(RobotPose.rest);
            yield return new WaitForSeconds(lrobsec);
            robstatus = RobStatus.idle;
        }

        IEnumerator TransferBoxFromRobotToTray(MmRobot rob, (int, int) key )
        {
            yield return new WaitUntil(() => robstatus == RobStatus.idle);
            robstatus = RobStatus.busy;
            var (poseup, posedn) = mmRobot.GetPoses(key);
            mmRobot.RealiseRobotPose(poseup);
            yield return new WaitForSeconds(srobsec);
            mmRobot.RealiseRobotPose(posedn);
            yield return new WaitForSeconds(lrobsec);
            var box = rob.DetachhBoxFromRobot();
            if (box != null)
            {
                mmtray.AttachBoxToTraySlot(key, box);
            }
            mmRobot.RealiseRobotPose(poseup);
            yield return new WaitForSeconds(srobsec);
            mmRobot.RealiseRobotPose(RobotPose.rest);
            yield return new WaitForSeconds(lrobsec);
            robstatus = RobStatus.idle;
        }

        public enum TranferType { SledToRob, RobToSled, TrayToRob, RobToTray }

        public float coroutineStart;

        public void StartCoroutineSledTransferBox(TranferType tt, MmSled sled)
        {
            coroutineStart = Time.time;
            var rob = mmRobot;
            switch (tt)
            {
                case TranferType.SledToRob:
                    switch (mmBoxMode)
                    {
                        case MmBoxMode.FakePooled:
                            sled.SetLoadState(false);
                            rob.ActivateRobBox(true);
                            break;
                        case MmBoxMode.RealPooled:
                            StartCoroutine(TransferBoxFromSledToRobot(sled,rob));
                            break;
                    }
                    break;
                case TranferType.RobToSled:
                    switch (mmBoxMode)
                    {
                        case MmBoxMode.FakePooled:
                            sled.SetLoadState(true);
                            rob.ActivateRobBox(false);
                            break;
                        case MmBoxMode.RealPooled:
                            StartCoroutine(TransferBoxFromRobotToSled(rob,sled));
                            break;
                    }
                    break;
                default:
                    magmo.ErrMsg("SledTransferBox - Wrong Function");
                    break;
            }
        }

        public void StartCoroutineTrayTransferBox(TranferType tt, (int, int) key)
        {
            coroutineStart = Time.time;
            var rob = mmRobot;
            switch (tt)
            {
                case TranferType.TrayToRob:

                    switch (mmBoxMode)
                    {
                        case MmBoxMode.FakePooled:
                            mmtray.SetVal(key, false);
                            rob.ActivateRobBox(true);
                            break;
                        case MmBoxMode.RealPooled:
                            Debug.Log($"TransferBoxFromTrayToRobot {key}");
                            //yield return new WaitUntil(() => robstatus == RobStatus.idle);
                            StartCoroutine(TransferBoxFromTrayToRobot(key, rob));
                            break;
                    }
                    break;
                case TranferType.RobToTray:
                    switch (mmBoxMode)
                    {
                        case MmBoxMode.FakePooled:
                            mmtray.SetVal(key, true);
                            rob.ActivateRobBox(false);
                            break;
                        case MmBoxMode.RealPooled:
                            StartCoroutine(TransferBoxFromRobotToTray(rob,key));
                            break;
                    }
                    break;
                default:
                    magmo.ErrMsg("TrayTransferBox - Wrong Function");
                    break;
            }
        }

        public bool CheckConsistency()
        {
            if (mmMode==MmMode.None || mmMode == MmMode.Echo)
            {
                if (mmBoxMode == MmBoxMode.RealPooled)
                {
                    var emsg2 = $"ChkCon error: unsupported mode mode:{mmMode} boxMode:{mmBoxMode}";
                    magmo.ErrMsg(emsg2);
                    return false;
                }
                return true;
            }
            if (mmBoxMode == MmBoxMode.FakePooled)
            {
                var emsg1 = $"ChkCon error: unsupported mode mode:{mmMode} boxMode:{mmBoxMode}";
                magmo.ErrMsg(emsg1);
                return false;
            }
            bool rv = false;
            var nTray = mmtray.CountLoaded();
            var nSled = mmt.CountLoadedSleds();
            var nRob = mmRobot.IsLoaded() ? 1 : 0;
            var emsg = $"ChkCon error: boxMode:{mmBoxMode} ntray:{nTray} nsled:{nSled} nrob:{nRob}";
            rv = (nTray + nSled + nRob)==10;
            if (!rv)
            {
                magmo.ErrMsg($"{emsg} does not sum to 10");
                //magmo.stopSimulation = true;
                
            }
            var (nFreeFake,nFreeReal, nTrayCbs, nRobCbs, nSledCbs) = MmBox.CountBoxStatus();
            if (mmBoxMode == MmBoxMode.FakePooled && nFreeFake != 0)
            {
                magmo.ErrMsg($"{emsg}  Fake Boxes got away - nFreeFake:{nFreeFake}");
                (nFreeFake, nFreeReal, nTrayCbs, nRobCbs, nSledCbs) = MmBox.CountBoxStatus();
            }
            if (mmBoxMode == MmBoxMode.RealPooled && nFreeReal != 0)
            {
                magmo.ErrMsg($"{emsg}  Real Boxes got away - nFreeReal:{nFreeReal}");
                (nFreeFake, nFreeReal, nTrayCbs, nRobCbs, nSledCbs) = MmBox.CountBoxStatus();
            }
            if (nTrayCbs!=nTray)
            {
                magmo.ErrMsg($"{emsg}  nTrayCbs:{nTrayCbs} not equal to nTray");
                (nFreeFake, nFreeReal, nTrayCbs, nRobCbs, nSledCbs) = MmBox.CountBoxStatus();
                //magmo.stopSimulation = true;
            }
            if (nRobCbs != nRob)
            {
                magmo.ErrMsg($"{emsg}  nRobCbs:{nRobCbs} not equal to nRob");
                (nFreeFake, nFreeReal, nTrayCbs, nRobCbs, nSledCbs) = MmBox.CountBoxStatus();
                //magmo.stopSimulation = true;
            }
            if (nSledCbs != nSled)
            {
                magmo.ErrMsg($"{emsg}  nSledCbs:{nSledCbs} not equal to nSled");
                (nFreeFake, nFreeReal, nTrayCbs, nRobCbs, nSledCbs) = MmBox.CountBoxStatus();
                //magmo.stopSimulation = true;
            }
            mmt.CheckConsistency();
            mmtray.CheckConsistency();
            return rv;
        }

        public bool CheckIfOkayForNextProcessStep()
        {
            var rv = false;
            var (nloadedstopped, nunloadedstopped) = mmt.CountStoppedSleds();
            var (_,_, nTray, nRob, nSled) = MmBox.CountBoxStatus();
            try
            {
                if (robstatus != RobStatus.idle)
                {
                    return rv;
                }
                //Debug.Log($"CountBoxStatus nTray:{nTray} nRob:{nRob} nSled:{nSled}");
                if (mmSubMode == MmSubMode.RailToTray && nSled == 0 && nRob == 0)
                {
                    mmSubMode = MmSubMode.TrayToRail;
                    //Debug.Log($"   Switched to {mmSubMode}");
                }
                else if (mmSubMode == MmSubMode.TrayToRail && nTray == 0 && nRob == 0)
                {
                    mmSubMode = MmSubMode.RailToTray;
                    //Debug.Log($"   Switched to {mmSubMode}");
                }
                switch (mmMode)
                {
                    //case MmMode.SimNew:
                    case MmMode.SimuRailToRail:
                        {
                            rv = (nloadedstopped > 0 && nunloadedstopped > 0);
                            return rv;
                        }
                    case MmMode.StartRailToTray:
                    case MmMode.StartTrayToRail:
                        {

                            switch (mmSubMode)
                            {
                                case MmSubMode.RailToTray:
                                    rv = nloadedstopped > 0 || nRob > 0;
                                    return rv;
                                case MmSubMode.TrayToRail:
                                    rv = nunloadedstopped > 0 && (nTray + nRob) > 0;
                                    return rv;
                            }
                        }
                        break;
                }
                return rv;
            }
            finally
            {
                //var msg = $"CIOFNP {mmMode} {mmSubMode} - nls:{nloadedstopped} nus:{nunloadedstopped} nTray:{nTray} nRob:{nRob} nSled:{nSled} rv:{rv}";
                //Debug.Log(msg);
            }
        }

        public void ProcessStep()
        {
            CheckConsistency();
            //if (!processStep) return;
            //processStep = false;
            if (!CheckIfOkayForNextProcessStep()) return;
            //Debug.Log($"ProcessStep mode:{mmMode}");
            if (Time.time - coroutineStart < 0.1f) return;

            var errhead = $"MagneMotion.demoStep - {mmMode}";
            var rob = mmRobot;
            switch (mmMode)
            {
                //case MmMode.SimNew:
                case MmMode.SimuRailToRail:
                    {
                        if (!rob.loadState)
                        {
                            var s = mmt.FindStoppedSled(neededLoadState: true);
                            if (s == null)
                            {
                                magmo.WarnMsg($"{errhead} - cound not find stoppedsled with loadState {true} to load");
                                return;
                            }
                            StartCoroutineSledTransferBox(TranferType.SledToRob, s);
                        }
                        else
                        {
                            var s = mmt.FindStoppedSled(neededLoadState: false);
                            if (s == null)
                            {
                                magmo.WarnMsg($"{errhead} - cound not find stoppedsled with loadState {false} to unload");
                                return;
                            }
                            StartCoroutineSledTransferBox(TranferType.RobToSled, s);
                        }
                        break;
                    }
                case MmMode.StartRailToTray:
                case MmMode.StartTrayToRail:
                    {
                        switch (mmSubMode)
                        {
                            case MmSubMode.RailToTray:
                                {
                                    if (!rob.loadState)
                                    {
                                        var s = mmt.FindStoppedSled(neededLoadState: true);
                                        if (s == null)
                                        {
                                            magmo.WarnMsg($"{errhead}  - cound not find stoppedsled with loadState {true} to unload");
                                            return;
                                        }
                                        StartCoroutineSledTransferBox(TranferType.SledToRob, s);
                                    }
                                    else
                                    {
                                        var (found, key) = mmtray.FindFirst(seekLoadState: false);
                                        if (!found)
                                        {
                                            magmo.WarnMsg($"{errhead}  - cound not find empty tray slot");
                                            return;
                                        }
                                        if (found)
                                        {
                                            StartCoroutineTrayTransferBox(TranferType.RobToTray, key);
                                        }
                                    }
                                    break;
                                }
                            case MmSubMode.TrayToRail:
                                {
                                    if (!rob.loadState)
                                    {
                                        var (found, key) = mmtray.FindFirst(seekLoadState: true);
                                        if (!found)
                                        {
                                            magmo.WarnMsg($"{errhead}  - cound not find loaded tray slot to unload");
                                            return;
                                        }
                                        StartCoroutineTrayTransferBox(TranferType.TrayToRob, key);
                                    }
                                    else
                                    {
                                        var s = mmt.FindStoppedSled(neededLoadState: false);
                                        if (s == null)
                                        {
                                            magmo.WarnMsg($"{errhead}  - cound not find stoppedsled with loadState {false} to load");
                                            return;
                                        }
                                        StartCoroutineSledTransferBox(TranferType.RobToSled, s);
                                    }
                                    break;
                                }
                        }
                        break;
                    }
            }
        }

        void AdjustSpeed()
        {
            if (doubleSpeed)
            {
                doubleSpeed = false;
                mmt.AdjustSledSpeedFactor(2f);
            }
            if (halfSpeed)
            {
                halfSpeed = false;
                mmt.AdjustSledSpeedFactor(0.5f);
            }
        }


        public void PhysicsStep()
        {
            AdjustSpeed();
            ReverseTrayRail();

        }

        // Update is called once per frame
        void Update()
        {
            if (!magmo.stopSimulation)
            {
                ProcessStep();
            }
        }
    }
}