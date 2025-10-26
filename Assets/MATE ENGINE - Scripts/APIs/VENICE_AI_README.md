# Venice AI Client for Unity

A C# HTTP client for integrating Venice AI's REST API into Unity projects. This client provides easy access to Venice AI's privacy-focused, uncensored AI capabilities.

## Features

- ✅ **Chat Completions** - Text generation using state-of-the-art language models
- ✅ **Streaming Support** - Real-time streaming responses for chat
- ✅ **Image Generation** - Create images from text prompts
- ✅ **Embeddings** - Generate text embeddings for semantic search
- ✅ **Model Listing** - Discover available AI models
- ✅ **OpenAI Compatible** - Works with OpenAI-compatible endpoints
- ✅ **Privacy First** - Built on Venice AI's privacy-focused architecture
- ✅ **Error Handling** - Comprehensive error handling and logging
- ✅ **Unity Native** - Built specifically for Unity using UnityWebRequest

## Installation

1. Copy the following files to your Unity project's `Assets/MATE ENGINE - Scripts/APIs/` directory:
   - `VeniceAIClient.cs`
   - `VeniceAIExample.cs`

2. The client will be automatically recognized by Unity.

## Getting Started

### 1. Obtain an API Key

Visit [Venice AI](https://venice.ai) to create an account and generate an API key:
1. Sign up at https://venice.ai
2. Navigate to your API settings
3. Generate a new API key
4. Copy the key (keep it secure!)

For detailed instructions, see: https://docs.venice.ai/overview/guides/generating-api-key

### 2. Setup in Unity

1. Create an empty GameObject in your scene (e.g., "VeniceAI")
2. Add the `VeniceAIClient` component to it
3. Paste your API key into the `Api Key` field in the Inspector
4. Optionally enable `Enable Debug Logs` for development

### 3. Basic Usage

Attach the `VeniceAIExample` script to the same GameObject to see example usage, or create your own script:

```csharp
using UnityEngine;
using MateEngine.APIs;

public class MyAIScript : MonoBehaviour
{
    public VeniceAIClient veniceClient;

    void Start()
    {
        StartCoroutine(SendMessage());
    }

    IEnumerator SendMessage()
    {
        var request = new VeniceChatRequest
        {
            model = "mistral-31-24b",
            messages = new VeniceChatMessage[]
            {
                new VeniceChatMessage("user", "Hello, Venice!")
            }
        };

        yield return veniceClient.SendChatCompletion(
            request,
            onSuccess: (response) =>
            {
                Debug.Log("AI Response: " + response.choices[0].message.content);
            },
            onError: (error) =>
            {
                Debug.LogError("Error: " + error);
            }
        );
    }
}
```

## API Reference

### VeniceAIClient

Main client class for interacting with Venice AI.

#### Configuration Properties

- `apiKey` (string) - Your Venice AI API key **(required)**
- `timeoutSeconds` (int) - Request timeout in seconds (default: 30)
- `enableDebugLogs` (bool) - Enable detailed debug logging (default: false)

#### Methods

##### SendChatCompletion
Send a chat completion request to Venice AI.

```csharp
IEnumerator SendChatCompletion(
    VeniceChatRequest request,
    Action<VeniceChatResponse> onSuccess,
    Action<string> onError
)
```

**Example:**
```csharp
var request = new VeniceChatRequest
{
    model = "mistral-31-24b",
    temperature = 0.7f,
    max_tokens = 500,
    messages = new VeniceChatMessage[]
    {
        new VeniceChatMessage("system", "You are a helpful assistant."),
        new VeniceChatMessage("user", "What is Unity?")
    }
};

yield return client.SendChatCompletion(request, 
    response => Debug.Log(response.choices[0].message.content),
    error => Debug.LogError(error)
);
```

##### SendChatCompletionStreaming
Send a streaming chat completion request.

```csharp
IEnumerator SendChatCompletionStreaming(
    VeniceChatRequest request,
    Action<string> onChunk,
    Action onComplete,
    Action<string> onError
)
```

**Example:**
```csharp
var request = new VeniceChatRequest
{
    model = "mistral-31-24b",
    messages = new VeniceChatMessage[]
    {
        new VeniceChatMessage("user", "Tell me a story.")
    }
};

yield return client.SendChatCompletionStreaming(request,
    chunk => Debug.Log("Chunk: " + chunk),
    () => Debug.Log("Stream complete"),
    error => Debug.LogError(error)
);
```

##### GetModels
Retrieve a list of available models.

```csharp
IEnumerator GetModels(
    Action<VeniceModelsResponse> onSuccess,
    Action<string> onError
)
```

**Example:**
```csharp
yield return client.GetModels(
    response => {
        foreach (var model in response.data)
            Debug.Log("Model: " + model.id);
    },
    error => Debug.LogError(error)
);
```

##### GenerateEmbeddings
Generate embeddings for text input.

```csharp
IEnumerator GenerateEmbeddings(
    VeniceEmbeddingRequest request,
    Action<VeniceEmbeddingResponse> onSuccess,
    Action<string> onError
)
```

**Example:**
```csharp
var request = new VeniceEmbeddingRequest
{
    model = "text-embedding-004",
    input = "Your text here"
};

yield return client.GenerateEmbeddings(request,
    response => Debug.Log("Embedding dimensions: " + response.data[0].embedding.Length),
    error => Debug.LogError(error)
);
```

##### GenerateImage
Generate an image from a text prompt.

```csharp
IEnumerator GenerateImage(
    VeniceImageRequest request,
    Action<VeniceImageResponse> onSuccess,
    Action<string> onError
)
```

**Example:**
```csharp
var request = new VeniceImageRequest
{
    prompt = "A beautiful sunset over mountains",
    model = "fluently-xl",
    width = 1024,
    height = 1024
};

yield return client.GenerateImage(request,
    response => Debug.Log("Image URL: " + response.data[0].url),
    error => Debug.LogError(error)
);
```

## Available Models

### Chat Models
- `mistral-31-24b` - Mistral 31 24B (recommended)
- `llama-3.1-405b` - Meta's Llama 3.1 405B
- `qwen-2.5-72b` - Qwen 2.5 72B
- And more...

### Image Models
- `fluently-xl` - High-quality image generation
- `flux-1.1-pro` - Professional image generation
- And more...

### Embedding Models
- `text-embedding-004` - Text embeddings

For the complete list of models, call `GetModels()` or visit https://docs.venice.ai/overview/models

## Request Models

### VeniceChatRequest
```csharp
public class VeniceChatRequest
{
    public string model;                  // Model ID (required)
    public VeniceChatMessage[] messages;  // Conversation messages (required)
    public float temperature;             // 0.0 to 2.0 (default: 0.7)
    public int max_tokens;                // Max response length (default: 2048)
    public bool stream;                   // Enable streaming (default: false)
    public float top_p;                   // Nucleus sampling (default: 1.0)
    public float frequency_penalty;       // -2.0 to 2.0 (default: 0.0)
    public float presence_penalty;        // -2.0 to 2.0 (default: 0.0)
}
```

### VeniceChatMessage
```csharp
public class VeniceChatMessage
{
    public string role;     // "system", "user", or "assistant"
    public string content;  // Message content
}
```

### VeniceImageRequest
```csharp
public class VeniceImageRequest
{
    public string prompt;     // Image description (required)
    public string model;      // Image model (default: "fluently-xl")
    public int width;         // Image width (default: 1024)
    public int height;        // Image height (default: 1024)
    public int num_images;    // Number of images (default: 1)
    public string style;      // Optional style preset
}
```

### VeniceEmbeddingRequest
```csharp
public class VeniceEmbeddingRequest
{
    public string model;           // Embedding model (default: "text-embedding-004")
    public string input;           // Input text (required)
    public string encoding_format; // "float" or "base64" (default: "float")
}
```

## Response Models

### VeniceChatResponse
```csharp
public class VeniceChatResponse
{
    public string id;
    public string @object;
    public long created;
    public string model;
    public VeniceChatChoice[] choices;
    public VeniceUsage usage;
}
```

### VeniceImageResponse
```csharp
public class VeniceImageResponse
{
    public long created;
    public VeniceImageData[] data;
}

public class VeniceImageData
{
    public string url;       // URL to the generated image
    public string b64_json;  // Base64-encoded image (if requested)
}
```

## Best Practices

### 1. API Key Security
- **Never commit API keys** to version control
- Store keys in Unity's PlayerPrefs or a secure configuration
- Use environment variables in production builds

### 2. Error Handling
Always implement error handlers:
```csharp
yield return client.SendChatCompletion(request,
    onSuccess: (response) => {
        // Handle success
    },
    onError: (error) => {
        Debug.LogError($"Venice AI Error: {error}");
        // Show user-friendly error message
    }
);
```

### 3. Rate Limiting
- Be mindful of API rate limits
- Implement request queuing for high-volume applications
- Cache responses when appropriate

### 4. Token Management
- Monitor `usage.total_tokens` in responses
- Set appropriate `max_tokens` limits
- Use shorter prompts when possible

### 5. Model Selection
- Use `mistral-31-24b` for general chat (balanced quality/speed)
- Use larger models like `llama-3.1-405b` for complex tasks
- Test different models to find the best fit for your use case

## Examples

### Multi-turn Conversation
```csharp
var request = new VeniceChatRequest
{
    model = "mistral-31-24b",
    messages = new VeniceChatMessage[]
    {
        new VeniceChatMessage("system", "You are a game character."),
        new VeniceChatMessage("user", "What's your backstory?"),
        new VeniceChatMessage("assistant", "I'm a brave knight..."),
        new VeniceChatMessage("user", "Tell me about your quest.")
    }
};
```

### Custom Temperature
```csharp
var request = new VeniceChatRequest
{
    model = "mistral-31-24b",
    temperature = 0.3f,  // More deterministic
    messages = new VeniceChatMessage[]
    {
        new VeniceChatMessage("user", "Write code to sort an array")
    }
};
```

### Image Generation with Style
```csharp
var request = new VeniceImageRequest
{
    prompt = "A futuristic city at night",
    model = "flux-1.1-pro",
    width = 1024,
    height = 768,
    style = "cyberpunk"
};
```

## Troubleshooting

### "API key is not set"
- Ensure you've set the `apiKey` field in the VeniceAIClient component
- Check that the key is not empty or whitespace

### Request timeout
- Increase `timeoutSeconds` in the client settings
- Check your internet connection
- Verify Venice AI service status at https://veniceai-status.com

### JSON parsing errors
- Enable `enableDebugLogs` to see raw responses
- Ensure you're using compatible model names
- Check that your Unity version supports the JSON structure

### Rate limit errors
- Implement exponential backoff
- Reduce request frequency
- Upgrade your Venice AI plan if needed

## Integration with MateEngine

This client is designed to integrate seamlessly with MateEngine:

1. **Desktop Companion AI**: Add conversational abilities to your virtual pet
2. **Dynamic Responses**: Generate context-aware responses based on user interactions
3. **Image Assets**: Generate custom images for your companion
4. **Personality System**: Use different system prompts for varied personalities

## Documentation & Support

- **Venice AI Docs**: https://docs.venice.ai
- **API Reference**: https://docs.venice.ai/api-reference/api-spec
- **Models List**: https://docs.venice.ai/overview/models
- **Pricing**: https://docs.venice.ai/overview/pricing
- **Status Page**: https://veniceai-status.com

## License

This client is part of the MateEngine project. See the main project LICENSE for details.

## Contributing

Contributions are welcome! Please ensure:
- Code follows existing style conventions
- All methods include XML documentation
- Examples are provided for new features
- Changes are tested in Unity

## Changelog

### v1.0.0 (Initial Release)
- Chat completions (standard and streaming)
- Image generation
- Embeddings generation
- Model listing
- Comprehensive error handling
- Example usage scripts
- Full documentation
