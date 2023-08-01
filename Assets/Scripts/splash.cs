using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using SimpleJSON;

public class splash : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //Screen.orientation = ScreenOrientation.Landscape;
#if UNITY_IPHONE
		Global.imgPath = Application.persistentDataPath + "/border_img/";
#elif UNITY_ANDROID
        Global.imgPath = Application.persistentDataPath + "/border_img/";
#else
if( Application.isEditor == true ){ 
    	Global.imgPath = "/img/";
} 
#endif

#if UNITY_IPHONE
		Global.prePath = @"file://";
#elif UNITY_ANDROID
        Global.prePath = @"file:///";
#else
		Global.prePath = @"file://" + Application.dataPath.Replace("/Assets","/");
#endif
        //delete all downloaded images
        try
        {
            if (Directory.Exists(Global.imgPath))
            {
                Directory.Delete(Global.imgPath, true);
            }
        }
        catch (Exception)
        {

        }
        if (PlayerPrefs.GetString("ip") != "")
        {
            Debug.Log("saved info");
            bool is_client_call = false;
            Global.server_address = PlayerPrefs.GetString("ip");
            Global.socket_server = "ws://" + Global.server_address + ":" + Global.api_server_port;
            Global.api_url = "http://" + Global.server_address + ":" + Global.api_server_port + "/";
            if (PlayerPrefs.GetInt("is_client_call") == 1)
            {
                is_client_call = true;
            }
            Global.setInfo.soft_key = "";
            Global.setInfo.tablenolist = new List<Table_Info>();
            Global.setInfo.table_no = new Table_Info();
            Global.setInfo.slide_option = 0;
            Global.setInfo.is_client_call = false;
            //테이블정보 갱신 여부 확인
            WWWForm form = new WWWForm();
            WWW www = new WWW(Global.api_url + Global.check_tableinfo_api, form);
            StartCoroutine(ProcessCheckTableInfo(www, is_client_call));
        }
        else
        {
            StartCoroutine(LoadScene());
        }
    }

    IEnumerator ProcessCheckTableInfo(WWW www, bool is_client_call)
    {
        yield return www;
        if (www.error == null)
        {
            JSONNode jsonNode = SimpleJSON.JSON.Parse(www.text);
            if (jsonNode["suc"].AsInt == 1)
            {
                Debug.Log("check table info success");
                Global.setInfo.bus_id = PlayerPrefs.GetString("bus_id");
                Global.setInfo.soft_key = PlayerPrefs.GetString("sft_key");
                Debug.Log(Global.setInfo.soft_key);
                Table_Info table = new Table_Info();
                //get table no list
                JSONNode tlist = JSON.Parse(jsonNode["tablelist"].ToString());
                Global.setInfo.tablenolist = new List<Table_Info>();
                for (int i = 0; i < tlist.Count; i++)
                {
                    Table_Info tinfo = new Table_Info();
                    tinfo.id = tlist[i]["id"];
                    tinfo.name = tlist[i]["name"];
                    Global.setInfo.tablenolist.Add(tinfo);
                    if (PlayerPrefs.GetString("tableid") != "")
                    {
                        if (tlist[i]["id"] == PlayerPrefs.GetString("tableid"))
                        {
                            table.name = tlist[i]["name"];
                            table.id = PlayerPrefs.GetString("tableid");
                        }
                    }
                }
                int slide_option = PlayerPrefs.GetInt("slide_option");
                string sImgs = PlayerPrefs.GetString("slideImgs");
                string[] slideImgs = sImgs.Split(',');
                if (PlayerPrefs.GetString("tableid") != "")
                {
                    Global.setInfo.table_no = table;
                    Global.setInfo.is_client_call = is_client_call;
                    Global.setInfo.slide_option = slide_option;
                    Global.setInfo.paths = slideImgs;
                }
            }
        }
        yield return new WaitForSeconds(0.1f);
        SceneManager.LoadScene("home");
    }

    IEnumerator LoadScene()
    {
        yield return new WaitForSeconds(0.1f);
        SceneManager.LoadScene("home");
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
