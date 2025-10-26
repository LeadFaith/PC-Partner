using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace MateEngine.APIs
{
    /// <summary>
    /// HTTP client for interacting with the Venice AI REST API
    /// Documentation: https://docs.venice.ai
    /// </summary>
    public class VeniceAIClient : MonoBehaviour
    {
        private const string BASE_URL = "https://venice.ai/api/v1";
        
        [Header("API Configuration")]
        [Tooltip("Your Venice AI API key (required)")]
        public string apiKey = "";
        
        [Tooltip("Request timeout in seconds")]
        public int timeoutSeconds = 30;
        
        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool enableDebugLogs = false;

        /// <summary>
        /// Send a chat completion request
        /// </summary>
        public IEnumerator SendChatCompletion(VeniceChatRequest request, Action<VeniceChatResponse> onSuccess, Action<string> onError)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                onError?.Invoke("API key is not set");
                yield break;
            }

            string url = $"{BASE_URL}/chat/completions";
            string jsonData = JsonUtility.ToJson(request);
            
            if (enableDebugLogs)
                Debug.Log($"[VeniceAI] Sending chat completion request to {url}");

            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                webRequest.timeout = timeoutSeconds;

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        VeniceChatResponse response = JsonUtility.FromJson<VeniceChatResponse>(webRequest.downloadHandler.text);
                        if (enableDebugLogs)
                            Debug.Log($"[VeniceAI] Chat completion successful");
                        onSuccess?.Invoke(response);
                    }
                    catch (Exception ex)
                    {
                        string errorMsg = $"Failed to parse response: {ex.Message}";
                        if (enableDebugLogs)
                            Debug.LogError($"[VeniceAI] {errorMsg}");
                        onError?.Invoke(errorMsg);
                    }
                }
                else
                {
                    string errorMsg = $"Request failed: {webRequest.error} - {webRequest.downloadHandler.text}";
                    if (enableDebugLogs)
                        Debug.LogError($"[VeniceAI] {errorMsg}");
                    onError?.Invoke(errorMsg);
                }
            }
        }

        /// <summary>
        /// Send a streaming chat completion request
        /// </summary>
        public IEnumerator SendChatCompletionStreaming(VeniceChatRequest request, Action<string> onChunk, Action onComplete, Action<string> onError)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                onError?.Invoke("API key is not set");
                yield break;
            }

            request.stream = true;
            string url = $"{BASE_URL}/chat/completions";
            string jsonData = JsonUtility.ToJson(request);

            if (enableDebugLogs)
                Debug.Log($"[VeniceAI] Sending streaming chat completion request to {url}");

            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                webRequest.timeout = timeoutSeconds;

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string[] lines = webRequest.downloadHandler.text.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("data: ") && !line.Contains("[DONE]"))
                        {
                            string jsonChunk = line.Substring(6);
                            onChunk?.Invoke(jsonChunk);
                        }
                    }
                    onComplete?.Invoke();
                }
                else
                {
                    string errorMsg = $"Streaming request failed: {webRequest.error}";
                    if (enableDebugLogs)
                        Debug.LogError($"[VeniceAI] {errorMsg}");
                    onError?.Invoke(errorMsg);
                }
            }
        }

        /// <summary>
        /// List available models
        /// </summary>
        public IEnumerator GetModels(Action<VeniceModelsResponse> onSuccess, Action<string> onError)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                onError?.Invoke("API key is not set");
                yield break;
            }

            string url = $"{BASE_URL}/models";
            
            if (enableDebugLogs)
                Debug.Log($"[VeniceAI] Fetching models from {url}");

            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                webRequest.timeout = timeoutSeconds;

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        VeniceModelsResponse response = JsonUtility.FromJson<VeniceModelsResponse>(webRequest.downloadHandler.text);
                        if (enableDebugLogs)
                            Debug.Log($"[VeniceAI] Models retrieved successfully");
                        onSuccess?.Invoke(response);
                    }
                    catch (Exception ex)
                    {
                        string errorMsg = $"Failed to parse models response: {ex.Message}";
                        if (enableDebugLogs)
                            Debug.LogError($"[VeniceAI] {errorMsg}");
                        onError?.Invoke(errorMsg);
                    }
                }
                else
                {
                    string errorMsg = $"Get models failed: {webRequest.error}";
                    if (enableDebugLogs)
                        Debug.LogError($"[VeniceAI] {errorMsg}");
                    onError?.Invoke(errorMsg);
                }
            }
        }

        /// <summary>
        /// Generate embeddings
        /// </summary>
        public IEnumerator GenerateEmbeddings(VeniceEmbeddingRequest request, Action<VeniceEmbeddingResponse> onSuccess, Action<string> onError)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                onError?.Invoke("API key is not set");
                yield break;
            }

            string url = $"{BASE_URL}/embeddings";
            string jsonData = JsonUtility.ToJson(request);

            if (enableDebugLogs)
                Debug.Log($"[VeniceAI] Generating embeddings");

            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                webRequest.timeout = timeoutSeconds;

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        VeniceEmbeddingResponse response = JsonUtility.FromJson<VeniceEmbeddingResponse>(webRequest.downloadHandler.text);
                        if (enableDebugLogs)
                            Debug.Log($"[VeniceAI] Embeddings generated successfully");
                        onSuccess?.Invoke(response);
                    }
                    catch (Exception ex)
                    {
                        string errorMsg = $"Failed to parse embeddings response: {ex.Message}";
                        if (enableDebugLogs)
                            Debug.LogError($"[VeniceAI] {errorMsg}");
                        onError?.Invoke(errorMsg);
                    }
                }
                else
                {
                    string errorMsg = $"Generate embeddings failed: {webRequest.error}";
                    if (enableDebugLogs)
                        Debug.LogError($"[VeniceAI] {errorMsg}");
                    onError?.Invoke(errorMsg);
                }
            }
        }

        /// <summary>
        /// Generate image
        /// </summary>
        public IEnumerator GenerateImage(VeniceImageRequest request, Action<VeniceImageResponse> onSuccess, Action<string> onError)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                onError?.Invoke("API key is not set");
                yield break;
            }

            string url = $"{BASE_URL}/image/generate";
            string jsonData = JsonUtility.ToJson(request);

            if (enableDebugLogs)
                Debug.Log($"[VeniceAI] Generating image");

            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                webRequest.timeout = timeoutSeconds;

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        VeniceImageResponse response = JsonUtility.FromJson<VeniceImageResponse>(webRequest.downloadHandler.text);
                        if (enableDebugLogs)
                            Debug.Log($"[VeniceAI] Image generated successfully");
                        onSuccess?.Invoke(response);
                    }
                    catch (Exception ex)
                    {
                        string errorMsg = $"Failed to parse image response: {ex.Message}";
                        if (enableDebugLogs)
                            Debug.LogError($"[VeniceAI] {errorMsg}");
                        onError?.Invoke(errorMsg);
                    }
                }
                else
                {
                    string errorMsg = $"Generate image failed: {webRequest.error}";
                    if (enableDebugLogs)
                        Debug.LogError($"[VeniceAI] {errorMsg}");
                    onError?.Invoke(errorMsg);
                }
            }
        }
    }

    #region Request/Response Models

    [Serializable]
    public class VeniceChatRequest
    {
        public string model = "mistral-31-24b";
        public VeniceChatMessage[] messages;
        public float temperature = 0.7f;
        public int max_tokens = 2048;
        public bool stream = false;
        public float top_p = 1.0f;
        public float frequency_penalty = 0.0f;
        public float presence_penalty = 0.0f;
    }

    [Serializable]
    public class VeniceChatMessage
    {
        public string role; // "system", "user", or "assistant"
        public string content;

        public VeniceChatMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }

    [Serializable]
    public class VeniceChatResponse
    {
        public string id;
        public string @object;
        public long created;
        public string model;
        public VeniceChatChoice[] choices;
        public VeniceUsage usage;
    }

    [Serializable]
    public class VeniceChatChoice
    {
        public int index;
        public VeniceChatMessage message;
        public string finish_reason;
    }

    [Serializable]
    public class VeniceUsage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
    }

    [Serializable]
    public class VeniceModelsResponse
    {
        public string @object;
        public VeniceModel[] data;
    }

    [Serializable]
    public class VeniceModel
    {
        public string id;
        public string @object;
        public long created;
        public string owned_by;
    }

    [Serializable]
    public class VeniceEmbeddingRequest
    {
        public string model = "text-embedding-004";
        public string input;
        public string encoding_format = "float";
    }

    [Serializable]
    public class VeniceEmbeddingResponse
    {
        public string @object;
        public VeniceEmbeddingData[] data;
        public string model;
        public VeniceUsage usage;
    }

    [Serializable]
    public class VeniceEmbeddingData
    {
        public string @object;
        public float[] embedding;
        public int index;
    }

    [Serializable]
    public class VeniceImageRequest
    {
        public string prompt;
        public string model = "fluently-xl";
        public int width = 1024;
        public int height = 1024;
        public int num_images = 1;
        public string style = "";
    }

    [Serializable]
    public class VeniceImageResponse
    {
        public long created;
        public VeniceImageData[] data;
    }

    [Serializable]
    public class VeniceImageData
    {
        public string url;
        public string b64_json;
    }

    #endregion
}
