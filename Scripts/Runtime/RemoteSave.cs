using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Newtonsoft.Json;
using CodySource.Singleton;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CodySource
{
    namespace RemoteSave
    {
        public class RemoteSave : SingletonPersistent<RemoteSave>
        {
            #region PROPERTIES

            /// <summary>
            /// Configure export properties
            /// </summary>
            [Header("CONFIG")]
            public string url = "";
            public string appKey = "";
            public string auth = "";
            public string table = "";
            public bool overwrite = true;
            public string saveKey = "";

            [Header("EVENT CONFIG")]
            public UnityEvent<string> onSaveSuccess = new UnityEvent<string>();
            public UnityEvent<string> onSaveFailed = new UnityEvent<string>();
            public UnityEvent<string> onLoadSuccess = new UnityEvent<string>();
            public UnityEvent<string> onLoadFailed = new UnityEvent<string>();

            #endregion

            #region PUBLIC METHODS

            public void SetUrl(string pURL) => url = pURL;
            public void SetAuth(string pAuth) => auth = pAuth;
            public void SetTable(string pTable) => table = pTable;
            public void SetOverwrite(bool pOverwrite) => overwrite = pOverwrite;
            public void SetSaveKey(string pSaveKey) => saveKey = pSaveKey;
            public void SetAuthAndSaveKey(string pAuth)
            {
                auth = pAuth;
                saveKey = pAuth;
            }
            public void Save(string pVal) => StartCoroutine(_SQL_Request(pVal));
            public void Load() => StartCoroutine(_SQL_Request());
            public void LoadAll()
            {
                string cache = saveKey;
                saveKey = "*";
                StartCoroutine(_SQL_Request());
                saveKey = cache;
            }
            public void Print(string pVal) => Debug.Log(pVal);
            public void ClearListeners()
            {
                onSaveSuccess.RemoveAllListeners();
                onSaveFailed.RemoveAllListeners();
                onLoadSuccess.RemoveAllListeners();
                onLoadFailed.RemoveAllListeners();
            }

            #endregion

            #region INTERNAL METHODS

            /// <summary>
            /// Send a sql request (save or load)
            /// </summary>
            internal IEnumerator _SQL_Request(string pVal = "")
            {
                bool isSave = pVal != "";
                if (url == "") Fail("A url has not been set for the save/load request.");
                if (appKey == "") Fail("An appKey has not been set for the save/load request.");
                if (auth == "") Fail("An auth has not been set for the save/load request.");
                if (table == "") Fail("A table has not been set for the save/load request.");
                if (saveKey == "") Fail("A saveKey has not been set for the save/load request.");
                if (url == "" || appKey == "" || auth == "" || table == "" || saveKey == "") yield break;
                WWWForm form = new WWWForm();
                form.AddField("saveKey", saveKey);
                form.AddField("appKey", sha256(appKey + Regex.Replace(auth, @"/[^\w]/", "").ToUpper()));
                form.AddField("table", table);
                if (overwrite) form.AddField("overwrite", overwrite.ToString());
#if UNITY_EDITOR
                form.AddField("editor", Regex.Replace(auth, @"/[^\w]/", "").ToUpper());
#endif
                if (isSave) form.AddField("payload", pVal);
                using (UnityWebRequest www = UnityWebRequest.Post(url, form))
                {
                    yield return www.SendWebRequest();
                    if (www.result != UnityWebRequest.Result.Success) Fail(www.error);
                    else
                    {
                        try
                        {
                            ServerResponse response = JsonConvert.DeserializeObject<ServerResponse>(www.downloadHandler.text);
                            if (response.isEmpty) Fail(www.downloadHandler.text);
                            if (response.error != null && response.error != "") Fail(response.error);
                            else Success(response.value);
                        }
                        catch (System.Exception e) { Fail(e.Message); }
                    }
                }
                void Fail(string pMessage) => ((isSave) ? onSaveFailed : onLoadFailed)?.Invoke(pMessage);
                void Success(string pMessage) => ((isSave) ? onSaveSuccess : onLoadSuccess)?.Invoke(pMessage);
                string sha256(string randomString)
                {
                    var crypt = new SHA256Managed();
                    var hash = new System.Text.StringBuilder();
                    byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(randomString));
                    foreach (byte theByte in crypto) hash.Append(theByte.ToString("x2"));
                    return hash.ToString();
                }
            }

            [System.Serializable]
            internal struct ServerResponse
            {
                public bool isEmpty => (error == null || error == "") && (value == null || value == "");
                public string error;
                public string value;
            }

            #endregion
        }
    }
}