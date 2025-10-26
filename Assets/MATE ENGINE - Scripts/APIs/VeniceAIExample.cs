using System.Collections;
using UnityEngine;
using MateEngine.APIs;

/// <summary>
/// Example script demonstrating how to use the VeniceAIClient
/// Attach this to a GameObject in your scene to test the Venice AI integration
/// </summary>
public class VeniceAIExample : MonoBehaviour
{
    [Header("Venice AI Client")]
    [Tooltip("Reference to the VeniceAIClient component")]
    public VeniceAIClient veniceClient;

    [Header("Test Configuration")]
    [Tooltip("Enable to run chat completion test on start")]
    public bool testChatOnStart = false;
    
    [Tooltip("Enable to run models list test on start")]
    public bool testModelsOnStart = false;

    void Start()
    {
        // Ensure we have a client reference
        if (veniceClient == null)
        {
            veniceClient = GetComponent<VeniceAIClient>();
            if (veniceClient == null)
            {
                Debug.LogError("[VeniceAIExample] VeniceAIClient component not found!");
                return;
            }
        }

        // Run tests if enabled
        if (testChatOnStart)
        {
            StartCoroutine(TestChatCompletion());
        }

        if (testModelsOnStart)
        {
            StartCoroutine(TestListModels());
        }
    }

    /// <summary>
    /// Example: Send a simple chat completion request
    /// </summary>
    public IEnumerator TestChatCompletion()
    {
        Debug.Log("[VeniceAIExample] Testing chat completion...");

        // Create a chat request
        VeniceChatRequest request = new VeniceChatRequest
        {
            model = "llama-3.3-70b",
            temperature = 0.7f,
            max_tokens = 150,
            messages = new VeniceChatMessage[]
            {
                new VeniceChatMessage("system", "You are a helpful assistant."),
                new VeniceChatMessage("user", "Hello! Can you tell me about Venice AI?")
            }
        };

        // Send the request
        yield return veniceClient.SendChatCompletion(
            request,
            onSuccess: (response) =>
            {
                if (response.choices != null && response.choices.Length > 0)
                {
                    string assistantReply = response.choices[0].message.content;
                    Debug.Log($"[VeniceAIExample] Assistant: {assistantReply}");
                    Debug.Log($"[VeniceAIExample] Tokens used: {response.usage.total_tokens}");
                }
            },
            onError: (error) =>
            {
                Debug.LogError($"[VeniceAIExample] Chat completion error: {error}");
            }
        );
    }

    /// <summary>
    /// Example: Send a streaming chat completion request
    /// </summary>
    public IEnumerator TestStreamingChat()
    {
        Debug.Log("[VeniceAIExample] Testing streaming chat...");

        VeniceChatRequest request = new VeniceChatRequest
        {
            model = "llama-3.3-70b",
            temperature = 0.7f,
            max_tokens = 200,
            messages = new VeniceChatMessage[]
            {
                new VeniceChatMessage("user", "Write a short poem about AI.")
            }
        };

        yield return veniceClient.SendChatCompletionStreaming(
            request,
            onChunk: (chunk) =>
            {
                Debug.Log($"[VeniceAIExample] Received chunk: {chunk}");
            },
            onComplete: () =>
            {
                Debug.Log("[VeniceAIExample] Streaming completed");
            },
            onError: (error) =>
            {
                Debug.LogError($"[VeniceAIExample] Streaming error: {error}");
            }
        );
    }

    /// <summary>
    /// Example: List available models
    /// </summary>
    public IEnumerator TestListModels()
    {
        Debug.Log("[VeniceAIExample] Fetching available models...");

        yield return veniceClient.GetModels(
            onSuccess: (response) =>
            {
                if (response.data != null && response.data.Length > 0)
                {
                    Debug.Log($"[VeniceAIExample] Found {response.data.Length} models:");
                    foreach (var model in response.data)
                    {
                        Debug.Log($"  - {model.id}");
                    }
                }
                else
                {
                    Debug.Log("[VeniceAIExample] No models found");
                }
            },
            onError: (error) =>
            {
                Debug.LogError($"[VeniceAIExample] List models error: {error}");
            }
        );
    }

    /// <summary>
    /// Example: Generate embeddings
    /// </summary>
    public IEnumerator TestGenerateEmbeddings()
    {
        Debug.Log("[VeniceAIExample] Generating embeddings...");

        VeniceEmbeddingRequest request = new VeniceEmbeddingRequest
        {
            model = "text-embedding-004",
            input = "This is a test sentence for embedding generation."
        };

        yield return veniceClient.GenerateEmbeddings(
            request,
            onSuccess: (response) =>
            {
                if (response.data != null && response.data.Length > 0)
                {
                    Debug.Log($"[VeniceAIExample] Generated embedding with {response.data[0].embedding.Length} dimensions");
                }
            },
            onError: (error) =>
            {
                Debug.LogError($"[VeniceAIExample] Embeddings error: {error}");
            }
        );
    }

    /// <summary>
    /// Example: Generate an image
    /// </summary>
    public IEnumerator TestGenerateImage()
    {
        Debug.Log("[VeniceAIExample] Generating image...");

        VeniceImageRequest request = new VeniceImageRequest
        {
            prompt = "A serene landscape with mountains and a lake at sunset",
            model = "fluently-xl",
            width = 1024,
            height = 1024,
            num_images = 1
        };

        yield return veniceClient.GenerateImage(
            request,
            onSuccess: (response) =>
            {
                if (response.data != null && response.data.Length > 0)
                {
                    Debug.Log($"[VeniceAIExample] Image generated successfully!");
                    Debug.Log($"[VeniceAIExample] Image URL: {response.data[0].url}");
                    // You can now download and display the image using the URL
                }
            },
            onError: (error) =>
            {
                Debug.LogError($"[VeniceAIExample] Image generation error: {error}");
            }
        );
    }

    /// <summary>
    /// Example: Multi-turn conversation
    /// </summary>
    public IEnumerator TestConversation()
    {
        Debug.Log("[VeniceAIExample] Testing multi-turn conversation...");

        VeniceChatRequest request = new VeniceChatRequest
        {
            model = "llama-3.3-70b",
            temperature = 0.8f,
            max_tokens = 300,
            messages = new VeniceChatMessage[]
            {
                new VeniceChatMessage("system", "You are a friendly desktop companion."),
                new VeniceChatMessage("user", "What's your name?"),
                new VeniceChatMessage("assistant", "I'm your friendly AI companion!"),
                new VeniceChatMessage("user", "Can you help me with coding?")
            }
        };

        yield return veniceClient.SendChatCompletion(
            request,
            onSuccess: (response) =>
            {
                if (response.choices != null && response.choices.Length > 0)
                {
                    Debug.Log($"[VeniceAIExample] Response: {response.choices[0].message.content}");
                }
            },
            onError: (error) =>
            {
                Debug.LogError($"[VeniceAIExample] Conversation error: {error}");
            }
        );
    }

    // You can call these test methods from Unity Inspector buttons or other scripts
    [ContextMenu("Test Chat Completion")]
    public void RunChatTest()
    {
        StartCoroutine(TestChatCompletion());
    }

    [ContextMenu("Test Streaming Chat")]
    public void RunStreamingTest()
    {
        StartCoroutine(TestStreamingChat());
    }

    [ContextMenu("Test List Models")]
    public void RunModelsTest()
    {
        StartCoroutine(TestListModels());
    }

    [ContextMenu("Test Generate Embeddings")]
    public void RunEmbeddingsTest()
    {
        StartCoroutine(TestGenerateEmbeddings());
    }

    [ContextMenu("Test Generate Image")]
    public void RunImageTest()
    {
        StartCoroutine(TestGenerateImage());
    }

    [ContextMenu("Test Conversation")]
    public void RunConversationTest()
    {
        StartCoroutine(TestConversation());
    }
}
