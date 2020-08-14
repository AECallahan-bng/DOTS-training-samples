//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//using UnityEditor.Profiling;
//using UnityEditorInternal;

//public class profileFarmer : MonoBehaviour
//{
//    // Start is called before the first frame update
//    System.IO.StreamWriter file;
//    void Start()
//    {
//        file = new System.IO.StreamWriter(@"C:\temp\ported.txt");

//    }


//    int lastFarmerCount=0;
//    int lastSampleCount;
//    double total;

//    // Update is called once per frame
//    void Update()
//    {

//        if (!UnityEditorInternal.ProfilerDriver.enabled)
//        {
//            UnityEditorInternal.ProfilerDriver.enabled = true;
//        }
//        else
//        {
//            int totalAi = 0;
//            foreach(var w in Unity.Entities.World.All)
//            {
//                var sfs = w.GetExistingSystem<SpawnFarmersSystem>();
//                if(sfs != null)
//                {
//                    totalAi += sfs.CurrentCount;
//                }
//                var sds = w.GetExistingSystem<SpawnDronesSystem>();
//                if (sds != null)
//                {
//                    totalAi += sds.CurrentCount;
//                }
//            }
//            int frameIndex = UnityEditorInternal.ProfilerDriver.lastFrameIndex;
//            using (RawFrameDataView frameData = ProfilerDriver.GetRawFrameDataView(frameIndex, 0))
//            {

//                var plId = frameData.GetMarkerId("PlayerLoop");

//                float total = 0;
//                int sampleCount = frameData.sampleCount;
//                for (int i = 0; i < sampleCount; ++i)
//                {
//                    if (plId != frameData.GetSampleMarkerId(i))
//                        continue;
//                    var v = frameData.GetSampleTimeMs(i);
//                    total += v;
//                }



//                //UnityEngine.Debug.Log($"Ai = {totalAi}, Time = {total}");
//                if(totalAi != lastFarmerCount)
//                {
//                    var resultForCount = total / lastSampleCount;
//                    file.WriteLine($"{lastFarmerCount}, {resultForCount}, {total}, {lastSampleCount}");
//                    lastFarmerCount = totalAi;
//                    lastSampleCount = 0;
//                    total = 0;
//                }
                

//            }
//        }
//    }
//    private void OnDestroy()
//    {
//        file.Close();
//    }
//}




using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor.Profiling;
using UnityEditorInternal;
using System.IO;

public class profileFarmer : MonoBehaviour
{
    //public GameObject FarmerManagerGo;
    //public GameObject DroneManagerGo;
    //FarmerManager FM;
    //DroneManager DM;
    // Start is called before the first frame update
    System.IO.StreamWriter file;
    int lastFarmerCount = 0;
    int lastSampleCount;
    double totalForFarmer = 0;
    void Start()
    {
        //FM = FarmerManagerGo.GetComponent<FarmerManager>();
        //DM = DroneManagerGo.GetComponent<DroneManager>();
        file = new System.IO.StreamWriter(@"C:\temp\ported.txt");
    }
    // Update is called once per frame
    void Update()
    {

        if (!UnityEditorInternal.ProfilerDriver.enabled)
        {
            UnityEditorInternal.ProfilerDriver.enabled = true;
        }
        else
        {

            int totalAi = 0;
            foreach (var w in Unity.Entities.World.All)
            {
                var sfs = w.GetExistingSystem<SpawnFarmersSystem>();
                if (sfs != null)
                {
                    totalAi += sfs.CurrentCount;
                }
                var sds = w.GetExistingSystem<SpawnDronesSystem>();
                if (sds != null)
                {
                    totalAi += sds.CurrentCount;
                }
            }
            int frameIndex = UnityEditorInternal.ProfilerDriver.lastFrameIndex;
            using (RawFrameDataView frameData = ProfilerDriver.GetRawFrameDataView(frameIndex, 0))
            {

                var plId = frameData.GetMarkerId("PlayerLoop");

                float total = 0;
                int sampleCount = frameData.sampleCount;
                for (int i = 0; i < sampleCount; ++i)
                {
                    if (plId != frameData.GetSampleMarkerId(i))
                        continue;
                    var v = frameData.GetSampleTimeMs(i);
                    total += v;
                }
                totalForFarmer += total;
                ++lastSampleCount;
                //file.WriteLine($"{totalAi}, {total}");

                //UnityEngine.Debug.Log($"Ai = {totalAi}, Time = {total}");


                //UnityEngine.Debug.Log($"Ai = {totalAi}, Time = {total}");
                if (totalAi != lastFarmerCount)
                {
                    var resultForCount = totalForFarmer / lastSampleCount;
                    file.WriteLine($"{lastFarmerCount}, {resultForCount}, {totalForFarmer}, {lastSampleCount}");
                    lastFarmerCount = totalAi;
                    lastSampleCount = 0;
                    totalForFarmer = 0;
                }


            }
        }
    }
    private void OnDestroy()
    {
        file.Close();
    }
}
