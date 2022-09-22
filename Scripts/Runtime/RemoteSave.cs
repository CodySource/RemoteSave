using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Newtonsoft.Json;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace CodySource
{
    namespace RemoteSave
    {
        public class RemoteSave : MonoBehaviour
        {
            #region PROPERTIES

            /// <summary>
            /// Configure export properties
            /// </summary>
            [Header("SQL CONFIG")]
            public string SQL_URL = "";
            public string SQL_KEY = "";
            public string SQL_HOST = "";
            public string SQL_DB = "";
            public string SQL_USER = "";
            public string SQL_PASS = "";

            [Header("EVENT CONFIG")]
            public UnityEvent<string> onSaveSuccess = new UnityEvent<string>();
            public UnityEvent<string> onSaveFailed = new UnityEvent<string>();
            public UnityEvent<string> onLoadSuccess = new UnityEvent<string>();
            public UnityEvent<string> onLoadFailed = new UnityEvent<string>();

            #endregion

            #region PUBLIC METHODS

            public void Save(string pKey, string pVal) => StartCoroutine(_SQL_Request(true, pKey, pVal));
            public void Load(string pKey) => StartCoroutine(_SQL_Request(false, pKey));
            public void Print(string pVal) => Debug.Log(pVal);

            #endregion

            #region INTERNAL METHODS

            /// <summary>
            /// Send a sql request (save or load)
            /// </summary>
            internal IEnumerator _SQL_Request(bool pIsSave, string pKey, string pStr = "")
            {
                WWWForm form = new WWWForm();
                form.AddField("key", $"{SQL_KEY}");
                string JSON = JsonConvert.SerializeObject(new { key = pKey, val = pStr });
                form.AddField("payload", JSON);
                using (UnityWebRequest www = UnityWebRequest.Post($"https://{SQL_URL}/RemoteSave.php{((pIsSave)? "" : "?load=true")}", form))
                {
                    yield return www.SendWebRequest();
                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Debug.Log(www.error);
                        onSaveFailed?.Invoke(www.error);
                    }
                    else
                    {
                        SQL_Repsponse response = JsonConvert.DeserializeObject<SQL_Repsponse>(www.downloadHandler.text);
                        if (response.success)
                        {
                            if (pIsSave) onSaveSuccess?.Invoke(response.submission_success);
                            else onLoadSuccess?.Invoke(response.loadedVal);
                        }
                        else
                        {
                            if (pIsSave) onSaveFailed?.Invoke(response.error);
                            else onLoadFailed?.Invoke(response.error);
                        }
                    }
                }
            }

            /// <summary>
            /// Generates the php file
            /// </summary>
            internal void _GeneratePHP()
            {
#if UNITY_EDITOR
                //  TODO:   Update php to remove try catch or actually implement try/catch & input validation
                string _output = "<?php\n" +
                    "header('Access-Control-Allow-Origin: *');\n" +
                    $"const projectKey = '{SQL_KEY}';\n" +
                    $"const tableName = '{Application.productName.Replace(" ", "_")}_{Application.version.Replace(".", "_")}_RemoteSave';\n" +
                    $"const db_HOST = '{SQL_HOST}';\n" +
                    $"const db_NAME = '{SQL_DB}';\n" +
                    $"const db_USER = '{SQL_USER}';\n" +
                    $"const db_PASS = '{SQL_PASS}';\n" +
                    "if (!isset($_POST['key'])) Error('Missing or invalid project key!');\n" +
                    "if (!isset($_POST['payload'])) Error('Missing data!');\n" +
                    "if (!isset($_GET['load'])) // SAVE\n" +
                    "{\n" +
                    "\ttry {\n" +
                    "\t\t$obj = json_decode($_POST['payload']);\n" +
                    "\t\tif ($obj == null) throw new Exception('Invalid json payload!');\n" +
                    "\t\t$key = preg_replace('/[^\\w]/','',$obj->key);\n" +
                    "\t\t$val = preg_replace('/[^\\w.! {}:,\\[\\]\"]/','',$obj->val); }\n" +
                    "\tcatch (Exception $e) {Error('Invalid json payload!');}\n" +
                    "\tif (ConnectToDB()) {\n" +
                    "\t\tif (VerifyTables()) {\n" +
                    "\t\t\tif (StoreSubmission($key, $val)) {\n" +
                    "\t\t\t\t$mysqli->close();\n" +
                    "\t\t\t\tSuccess('submission_success', gmdate('Y-m-d H:i:s'));}\n" +
                    "\t\t\telse Error('An unkown error occured while storing submission.  Check your database permissions.');}\n" +
                    "\t\telse Error('An unkown error occured while creating/verifying tables.  Check your database permissions.');}\n" +
                    "\telse Error('Unable to connect to database.');\n" +
                    "}\n" +
                    "else\n" +
                    "{\n" +
                    "\ttry {\n" +
                    "\t\t$obj = json_decode($_POST['payload']);\n" +
                    "\t\tif ($obj == null) throw new Exception('Invalid json payload!');\n" +
                    "\t\t$key = preg_replace('/[^\\w]/','',$obj->key);\n" +
                    "\t}\n" +
                    "\tcatch (Exception $e) {Error('Invalid json payload!');}\n" +
                    "\tif (ConnectToDB()) {\n" +
                    "\t\tif (PullValue($key)) $mysqli->close();\n" +
                    "\t\telse Error('The provided key was not found.');}\n" +
                    "\telse Error('Unable to connect to database.');\n" +
                    "}\n" +
                    "function Error($text)\n" +
                    "{\n" +
                    "\t$output = new stdClass;\n" +
                    "\t$output->success = false;\n" +
                    "\t$output->error = $text;\n" +
                    "\tdie(json_encode($output));\n" +
                    "}\n" +
                    "function Success()\n" +
                    "{\n" +
                    "\t$output = new stdClass;\n" +
                    "\t$output->success = true;\n" +
                    "\t$argCount = func_num_args();\n" +
                    "\tif ($argCount % 2 != 0) return;\n" +
                    "\t$args = func_get_args();\n" +
                    "\tfor ($i = 0; $i < $argCount; $i += 2)\n" +
                    "\t{\n" +
                    "\t\t$arg = func_get_arg($i);\n" +
                    "\t\t$val = func_get_arg($i + 1);\n" +
                    "\t\t$output->$arg = $val;\n" +
                    "\t}\n" +
                    "\tdie(json_encode($output));\n" +
                    "}\n" +
                    "$mysqli; $timestamp;\n" +
                    "function ConnectToDB()\n" +
                    "{\n" +
                    "\tglobal $mysqli, $timestamp;\n" +
                    "\t$mysqli = new mysqli(db_HOST, db_USER, db_PASS, db_NAME);\n" +
                    "\tif ($mysqli->connect_errno)\n" +
                    "\t{\n" +
                    "\t\terror_log('Connect Error: '.$mysqli->connect_error,0);\n" +
                    "\t\treturn false;\n" +
                    "\t}\n" +
                    "\t$timestamp = date(DATE_RFC3339);\n" +
                    "\treturn true;\n" +
                    "}\n" +
                    "function VerifyTables()\n" +
                    "{\n" +
                    "\tglobal $mysqli, $timestamp;\n" +
                    "\tif ($mysqli->query('CREATE TABLE IF NOT EXISTS '.tableName.' (saveKey VARCHAR(1023) PRIMARY KEY, saveVal TEXT); ')) return true;\n" +
                    "\terror_log('Verify Tables Error: '.$mysqli->error,0);\n" +
                    "\treturn false;\n" +
                    "}\n" +
                    "function StoreSubmission($pKey,$pVal)\n" +
                    "{\n" +
                    "\tglobal $mysqli, $timestamp;\n" +
                    "\t$result = $mysqli->query('SELECT * FROM '.tableName.' WHERE saveKey=\\\''.$pKey.'\\\'');\n" +
                    "\tif ($result->num_rows == 0) { if ($mysqli->query('INSERT INTO '.tableName.' (saveKey, saveVal) VALUES(\\\''.$pKey.'\\\', \\\''.$pVal.'\\\');')) return true; }\n" +
                    "\telse { if ($mysqli->query('UPDATE '.tableName.' SET saveVal=\\\''.$pVal.'\\\' WHERE saveKey=\\\''.$pKey.'\\\'')) return true; }\n" +
                    "\terror_log('Store Submission Error: '.$mysqli->error,0);\n" +
                    "\treturn false;\n" +
                    "}\n" +
                    "function PullValue($pKey)\n" +
                    "{\n" +
                    "\tglobal $mysqli;\n" +
                    "\t$result = $mysqli->query('SELECT * FROM '.tableName.' WHERE saveKey=\\\''.$pKey.'\\\'');\n" +
                    "\tif ($result->num_rows == 0) return false;\n" +
                    "\t$row = $result->fetch_assoc();\n" +
                    "\tSuccess('loadedVal',$row['saveVal']);\n" +
                    "}\n" +
                    "?>";

                //  Write file
                Directory.CreateDirectory("./Assets/RemoteSave/");
                File.WriteAllText($"./Assets/RemoteSave/RemoteSave.php", _output);
#endif
            }

            [System.Serializable]
            public struct SQL_Repsponse
            {
                public bool success;
                public string error;
                public string submission_success;
                public string loadedVal;
            }

            #endregion
        }
    }
}