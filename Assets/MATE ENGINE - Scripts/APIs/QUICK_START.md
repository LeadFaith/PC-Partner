# Quick Start Guide - Venice AI Integration

This guide will help you quickly integrate Venice AI into your MateEngine project.

## Prerequisites

1. A Venice AI account (sign up at https://venice.ai)
2. An active API key (see https://docs.venice.ai/overview/guides/generating-api-key)
3. Unity 2020.3 or later

## 5-Minute Setup

### Step 1: Add the Client Component

1. In your Unity scene hierarchy, create a new empty GameObject
2. Name it "VeniceAI"
3. In the Inspector, click "Add Component"
4. Search for "VeniceAIClient" and add it

### Step 2: Configure the API Key

**Recommended: Use Environment Variable**

1. Set the `VENICE_API_KEY` environment variable:
   - **Windows**: Open Command Prompt and run: `setx VENICE_API_KEY "your-api-key-here"`
   - **macOS/Linux**: Add to your shell profile: `export VENICE_API_KEY="your-api-key-here"`
2. Restart Unity Editor to pick up the new environment variable
3. The client will automatically use this key

**Alternative: Use Inspector (Development Only)**

1. Select the VeniceAI GameObject
2. In the VeniceAIClient component, paste your API key into the "Api Key" field
3. ⚠️ **Warning**: Do not commit scenes with API keys to version control

(Optional) Check "Enable Debug Logs" to see detailed logging during development

### Step 3: Add Example Script (Optional)

To test the integration:

1. Add the "VeniceAIExample" component to the same GameObject
2. Drag the VeniceAIClient component into the "Venice Client" field
3. Check "Test Chat On Start" to test on scene load
4. Press Play to see it in action!

### Step 4: Use in Your Scripts

Create a new script or add to an existing one:

```csharp
using UnityEngine;
using MateEngine.APIs;
using System.Collections;

public class MyVeniceIntegration : MonoBehaviour
{
    // Reference to the VeniceAIClient
    public VeniceAIClient veniceClient;
    
    void Start()
    {
        // Send a simple message
        StartCoroutine(AskVenice("What is Unity?"));
    }
    
    IEnumerator AskVenice(string question)
    {
        // Create the request
        var request = new VeniceChatRequest
        {
            model = "mistral-31-24b",
            messages = new VeniceChatMessage[]
            {
                new VeniceChatMessage("user", question)
            }
        };
        
        // Send it
        yield return veniceClient.SendChatCompletion(
            request,
            onSuccess: (response) =>
            {
                string answer = response.choices[0].message.content;
                Debug.Log($"Venice says: {answer}");
            },
            onError: (error) =>
            {
                Debug.LogError($"Error: {error}");
            }
        );
    }
}
```

## Common Use Cases for MateEngine

### 1. Desktop Companion Conversations

```csharp
IEnumerator TalkToCompanion(string userMessage)
{
    var request = new VeniceChatRequest
    {
        model = "mistral-31-24b",
        temperature = 0.8f,
        messages = new VeniceChatMessage[]
        {
            new VeniceChatMessage("system", "You are a friendly desktop companion named Mate."),
            new VeniceChatMessage("user", userMessage)
        }
    };
    
    yield return veniceClient.SendChatCompletion(request, 
        response => DisplayCompanionMessage(response.choices[0].message.content),
        error => Debug.LogError(error)
    );
}
```

### 2. Dynamic Companion Personality

```csharp
// Store conversation history
List<VeniceChatMessage> conversationHistory = new List<VeniceChatMessage>
{
    new VeniceChatMessage("system", "You are a cheerful virtual pet who loves jokes.")
};

IEnumerator ContinueConversation(string userInput)
{
    // Add user message to history
    conversationHistory.Add(new VeniceChatMessage("user", userInput));
    
    var request = new VeniceChatRequest
    {
        model = "mistral-31-24b",
        messages = conversationHistory.ToArray()
    };
    
    yield return veniceClient.SendChatCompletion(request,
        response => {
            string reply = response.choices[0].message.content;
            // Add assistant reply to history
            conversationHistory.Add(new VeniceChatMessage("assistant", reply));
            ShowDialogBubble(reply);
        },
        error => Debug.LogError(error)
    );
}
```

### 3. Context-Aware Responses

```csharp
IEnumerator ReactToUserAction(string action)
{
    var request = new VeniceChatRequest
    {
        model = "mistral-31-24b",
        temperature = 0.9f,
        max_tokens = 100,
        messages = new VeniceChatMessage[]
        {
            new VeniceChatMessage("system", "You are a desktop pet. React to user actions with short, cute responses."),
            new VeniceChatMessage("user", $"The user just {action}. React to this.")
        }
    };
    
    yield return veniceClient.SendChatCompletion(request,
        response => PlayReactionAnimation(response.choices[0].message.content),
        error => Debug.LogError(error)
    );
}

// Usage:
// StartCoroutine(ReactToUserAction("petted you"));
// StartCoroutine(ReactToUserAction("gave you a treat"));
```

### 4. Generate Custom Avatar Accessories

```csharp
IEnumerator GenerateAccessory(string description)
{
    var request = new VeniceImageRequest
    {
        prompt = $"cute accessory for a virtual pet: {description}, transparent background, icon style",
        model = "fluently-xl",
        width = 512,
        height = 512
    };
    
    yield return veniceClient.GenerateImage(request,
        response => {
            string imageUrl = response.data[0].url;
            StartCoroutine(DownloadAndApplyAccessory(imageUrl));
        },
        error => Debug.LogError(error)
    );
}
```

## Testing Your Integration

### Using Unity Inspector

1. Select the GameObject with VeniceAIExample
2. Right-click on the component header
3. Choose any of the test methods:
   - "Test Chat Completion"
   - "Test Streaming Chat"
   - "Test List Models"
   - "Test Generate Embeddings"
   - "Test Generate Image"
4. Check the Console for results

### Using Code

```csharp
// In your MonoBehaviour
[ContextMenu("Quick Test")]
void QuickTest()
{
    StartCoroutine(TestVeniceConnection());
}

IEnumerator TestVeniceConnection()
{
    var request = new VeniceChatRequest
    {
        model = "mistral-31-24b",
        max_tokens = 50,
        messages = new VeniceChatMessage[]
        {
            new VeniceChatMessage("user", "Say 'Hello, MateEngine!'")
        }
    };
    
    yield return veniceClient.SendChatCompletion(request,
        response => Debug.Log("✅ Venice AI Connected! Response: " + response.choices[0].message.content),
        error => Debug.LogError("❌ Connection failed: " + error)
    );
}
```

## Troubleshooting

### API Key Issues
- **Error: "API key is not set"**
  - Solution 1: Set the `VENICE_API_KEY` environment variable (recommended)
  - Solution 2: Paste your API key in the Inspector field
  - Verify there are no extra spaces before/after the key
  - Restart Unity Editor after setting environment variables

### Connection Issues
- **Error: "Request failed"**
  - Check your internet connection
  - Verify Venice AI service status: https://veniceai-status.com
  - Try increasing the timeout in the client settings

### Unity Compilation Errors
- **Error: "The type or namespace name 'MateEngine' could not be found"**
  - Make sure the files are in the correct directory: `Assets/MATE ENGINE - Scripts/APIs/`
  - Try reimporting the scripts in Unity

## Performance Tips

1. **Cache the Client Reference**: Don't find the component every time
   ```csharp
   private VeniceAIClient veniceClient;
   
   void Awake()
   {
       veniceClient = FindObjectOfType<VeniceAIClient>();
   }
   ```

2. **Use Appropriate Timeouts**: For longer requests (e.g., image generation), increase the timeout
   ```csharp
   veniceClient.timeoutSeconds = 60;
   ```

3. **Limit Token Usage**: Set reasonable max_tokens to save costs
   ```csharp
   request.max_tokens = 200; // For short responses
   ```

4. **Implement Rate Limiting**: Don't spam requests
   ```csharp
   float lastRequestTime = 0f;
   float requestCooldown = 2f; // 2 seconds between requests
   
   void Update()
   {
       if (Input.GetKeyDown(KeyCode.Space) && Time.time - lastRequestTime > requestCooldown)
       {
           StartCoroutine(SendRequest());
           lastRequestTime = Time.time;
       }
   }
   ```

## Next Steps

1. Read the full documentation: `VENICE_AI_README.md`
2. Explore Venice AI's capabilities: https://docs.venice.ai
3. Join the Venice AI community: https://discord.gg/askvenice
4. Check out available models: https://docs.venice.ai/overview/models

## Support

- **Venice AI Docs**: https://docs.venice.ai
- **API Status**: https://veniceai-status.com
- **MateEngine GitHub**: https://github.com/LeadFaith/PC-Partner

---

Happy building with Venice AI! 🎉
