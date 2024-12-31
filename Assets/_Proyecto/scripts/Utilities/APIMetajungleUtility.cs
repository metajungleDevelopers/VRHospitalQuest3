using System;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace MetaJungle.Utilities.API
{
    public static class APIMetajungleUtility
    {
        /// <summary>
        /// Arma el Json que se envia a la API
        /// </summary>
        /// <param name="caller">El MonoBehaviour desde el que se quiere enviar la API. Escribe "this" para ese parametro</param>
        /// <param name="userMail">l email de la persona que esta haciendo los desafios. Deberia ser el que devuelva la base de datos cuando se implemente</param>
        /// <param name="collisionName">Nombre del gatillador de la llamada a la API. Se llama collision ya que originalmente esta API se activaba al hacer colisiones, pero ahora deberia tener el nombre de lo que lo activa</param>
        /// <param name="scene">El nombre de la escena desde la que se manda el </param>
        /// <param name="tenant">El tenant del proyecto.</param>
        /// <param name="company">La compañia del proyecto</param>
        /// <remarks>Si no se necesitan asignar todos los valores, se pueden elegir manualmente al momento de llamar a 'ArmarJsonDashboard' escribiendo por ejemplo: 
        /// ArmarJsonDashboard(this, userMail: "[el nomre del mail]", tenant: "[nombre del tenant]"). todos excepto this son parametros opcionales.</remarks>
        public static void ArmarJsonDashboard(MonoBehaviour caller, string userMail = "", string collisionName = "", string scene = "", string tenant = "",string company = "")
        {
            OnCollision data = new OnCollision
            {
                userMail = userMail,
                collisionName = collisionName,
                scene = scene,
                tenant = tenant,
                company = company,
            };

            string json = JsonUtility.ToJson(data);
            caller.StartCoroutine(SendPOSTRequest(json));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jsonData"></param>
        /// <returns></returns>
        private static IEnumerator SendPOSTRequest(string jsonData)
        {
            string url = "https://southamerica-east1-unity-397015.cloudfunctions.net/user/OnCollision";
            UnityWebRequest request = new UnityWebRequest(url, "POST");
            byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(request.error);
            }
            else
            {
                Debug.Log("Response: " + request.downloadHandler.text);
            }
        }

        public static void ArmarJsonAvatar(MonoBehaviour caller, string userMail = "", string tenant = "", int avatar = 0)
        {
            Avatar data = new Avatar
            {
                userMail = userMail,
                tenant = tenant,
                avatar = avatar
            };

            string json = JsonUtility.ToJson(data);
            caller.StartCoroutine(SendPUTRequest(json));
        }

        /// <summary>
        /// Sends a PUT request to the specified URL with JSON data.
        /// </summary>
        /// <param name="jsonData">The JSON string to be sent as the request body.</param>
        /// <returns>An IEnumerator for coroutine support in Unity.</returns>
        private static IEnumerator SendPUTRequest(string jsonData)
        {
            string url = "https://southamerica-east1-unity-397015.cloudfunctions.net/user/avatar";
            UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT);
            byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error in PUT request: {request.error}");
            }
            else
            {
                Debug.Log($"Response: {request.downloadHandler.text} Status Code: {request.responseCode}");
            }
        }

        public static IEnumerator SendGetRequest(string uri, Action<string> callback)
        {
            Debug.Log("Mandando request a " + uri);
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();

                string[] pages = uri.Split('/');
                int page = pages.Length - 1;

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError(pages[page] + ": Error: " + webRequest.error);
                        callback?.Invoke(null);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
                        callback?.Invoke(null);
                        break;
                    case UnityWebRequest.Result.Success:
                        Debug.Log(pages[page] + ":\nReceived: " + webRequest.downloadHandler.text);
                        callback?.Invoke(webRequest.downloadHandler.text);
                        break;
                }
            }
        }
    }

    [Serializable]
    public class OnCollision
    {
        public string userMail;
        public string collisionName;
        public string scene;
        public string tenant;
        public string company;
        //public string executionThread;
    }

    [System.Serializable]
    public class User
    {
        public int avatar;
        public string company;
        public string name;
        public string userMail;
    }

    [System.Serializable]
    public class Avatar
    {
        public string userMail;
        public string tenant;
        public int avatar;
    }
}
