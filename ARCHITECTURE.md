# MauiOCRFacturas — Architecture & Azure Services

> See also: [README.md](./README.md) for full project documentation, features, configuration and known issues.

---

## General Architecture

The application is built on **.NET MAUI** (targeting Android, API 21+) using the **MVVM** pattern.
All Azure integrations are encapsulated in dedicated **Service** classes, injected via the built-in .NET DI container configured in `MauiProgram.cs`.

### High-Level Architecture Diagram

```
+----------------------------------------------------------+
|                  .NET MAUI Application                   |
|                   (Android, API 21+)                     |
|                                                          |
|  +-------------+    +--------------+    +--------------+ |
|  |    Views    |--->|  ViewModels  |--->|   Services   | |
|  | (XAML + CS) |    | (MVVM Logic) |    | (Azure SDKs) | |
|  +-------------+    +--------------+    +------+-------+ |
|                                                |         |
|  Navigation: AppShell TabBar                  |         |
|  DI: MauiProgram.cs (Singleton/Transient)     |         |
+------------------------------------------------+---------+
                                                 |
                    +----------------------------v---------------------------+
                    |                 Azure Cloud Services                  |
                    |                                                       |
                    |  +--------------------------------------------------+|
                    |  | Azure Document Intelligence (Form Recognizer)     ||
                    |  |   Model: prebuilt-invoice                         ||
                    |  |   SDK: Azure.AI.FormRecognizer 4.1.0              ||
                    |  +--------------------------------------------------+|
                    |  +--------------------------------------------------+|
                    |  | Azure AI Vision (Image Analysis)                  ||
                    |  |   Features: Objects, Tags, Caption                ||
                    |  |   SDK: Azure.AI.Vision.ImageAnalysis 1.0.0        ||
                    |  +--------------------------------------------------+|
                    |  +--------------------------------------------------+|
                    |  | Azure Speech Service (Speech-to-Text)             ||
                    |  |   Language: es-ES (configurable)                  ||
                    |  |   SDK: Microsoft.CognitiveServices.Speech 1.48.2  ||
                    |  +--------------------------------------------------+|
                    |  +--------------------------------------------------+|
                    |  | Azure Translator (REST API)                       ||
                    |  |   Endpoint: api.cognitive.microsofttranslator.com ||
                    |  |   API version: 3.0                                ||
                    |  +--------------------------------------------------+|
                    |  +--------------------------------------------------+|
                    |  | Azure OpenAI (Chat Completions)                   ||
                    |  |   API: /openai/deployments/{name}/chat/completions||
                    |  |   API version: 2024-02-01                         ||
                    |  +--------------------------------------------------+|
                    |  +--------------------------------------------------+|
                    |  | Azure Service Bus (Queue)                         ||
                    |  |   SDK: Azure.Messaging.ServiceBus 7.20.1          ||
                    |  +--------------------------------------------------+|
                    +-------------------------------------------------------+
```

---

## Application Layers

### Views (UI Layer)

XAML-based pages, each paired with a code-behind `.cs` file. Navigation is handled by `AppShell.xaml` using a `TabBar`.

| View | Description |
|---|---|
| `MainPage` | Invoice photo capture and OCR result display |
| `HistorialPage` | Session history of all analyzed invoices |
| `VisionPage` | Image object detection and captioning |
| `TraductorPage` | Voice input + real-time speech translation |
| `VoiceSummaryPage` | Audio recording + AI-generated summary |

### ViewModels (Business Logic Layer)

Implement `ObservableObject` from `CommunityToolkit.Mvvm`. Use `[RelayCommand]` and `[ObservableProperty]` source generators.

| ViewModel | Bound View | Services Used |
|---|---|---|
| `MainViewModel` | `MainPage` | `DocumentIntelligenceService`, `IHistorialService` |
| `HistorialViewModel` | `HistorialPage` | `IHistorialService` |
| `VisionViewModel` | `VisionPage` | `ComputerVisionService` |
| `TraductorViewModel` | `TraductorPage` | `SpeechTranslatorService` |

### Services (Azure Integration Layer)

All services are registered as **Singletons** in `MauiProgram.cs`.

| Service Class | Azure Service | SDK / API |
|---|---|---|
| `DocumentIntelligenceService` | Azure Document Intelligence | `Azure.AI.FormRecognizer` 4.1.0 |
| `ComputerVisionService` | Azure AI Vision | `Azure.AI.Vision.ImageAnalysis` 1.0.0 |
| `SpeechTranslatorService` | Azure Speech + Azure Translator | `Microsoft.CognitiveServices.Speech` + REST |
| `SpeachService` | Azure Speech Service | `Microsoft.CognitiveServices.Speech` 1.48.2 |
| `OpenAIService` | Azure OpenAI | REST API (HttpClient) |
| `ServiceBusService` | Azure Service Bus | `Azure.Messaging.ServiceBus` 7.20.1 |
| `HistorialService` | N/A (in-memory) | Native C# collections |

### Models (Data Layer)

| Model | Purpose |
|---|---|
| `ResultadoOCR` | Stores all fields extracted from an invoice by Document Intelligence |
| `ResultadoVision` | Stores description, detected objects, and tags from Azure AI Vision |

---

## Azure Services — Detailed Reference

### 1. Azure Document Intelligence

- **SDK:** `Azure.AI.FormRecognizer` v4.1.0
- **Client:** `DocumentAnalysisClient`
- **Model:** `prebuilt-invoice`
- **Input:** Image stream (camera photo or gallery pick)
- **Output fields extracted:**

| Field | Description |
|---|---|
| `VendorName` | Supplier / vendor name |
| `CustomerName` | Customer name |
| `InvoiceId` | Invoice number |
| `InvoiceDate` | Invoice issue date |
| `InvoiceTotal` | Total amount due |
| `SubTotal` | Subtotal before tax |
| `TotalTax` | Tax amount |
| `Items` | Line items: description + amount |

- **Config keys:** `AzureDocumentIntelligence:Endpoint`, `AzureDocumentIntelligence:ApiKey`

---

### 2. Azure AI Vision (Computer Vision)

- **SDK:** `Azure.AI.Vision.ImageAnalysis` v1.0.0
- **Client:** `ImageAnalysisClient`
- **Features requested:** `VisualFeatures.Objects | VisualFeatures.Tags | VisualFeatures.Caption`
- **Output:**
  - Auto-generated image caption (description)
  - Detected objects with name + confidence score
  - Image tags with confidence percentages
- **Config keys:** `AzureComputerVision:Endpoint`, `AzureComputerVision:ApiKey`
- **Status:** Functional — minor detection inconsistencies noted

---

### 3. Azure Speech Service

Used by two separate service classes:

**`SpeachService`** (voice recording & STT):
- **SDK:** `Microsoft.CognitiveServices.Speech` v1.48.2
- **Client:** `SpeechRecognizer` with default microphone input
- **Language:** `es-ES`
- **Method:** `RecognizeOnceAsync()`
- **Config keys:** `AzureSpeech:Key`, `AzureSpeech:Region`

**`SpeechTranslatorService`** (STT step in translation pipeline):
- Same SDK and client type
- Extended with initial/end silence timeout properties for better detection
- Feeds transcription into Azure Translator

---

### 4. Azure Translator

- **API type:** REST (via `HttpClient`)
- **Endpoint:** `https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&to={targetLanguage}`
- **Authentication:** Subscription key via HTTP header
- **Usage:** Receives transcribed text from Azure Speech, returns translated text
- **Config keys:** `AzureTranslator:ApiKey`, `AzureTranslator:Region`

---

### 5. Azure OpenAI

- **API type:** REST (via `HttpClient`)
- **Endpoint:** `{AzureOpenAI:Endpoint}/openai/deployments/{DeploymentName}/chat/completions?api-version=2024-02-01`
- **Authentication:** `api-key` HTTP header
- **System prompt:** Instructs the model to generate a clear, concise bullet-point summary from a voice transcription
- **Config keys:** `AzureOpenAI:Endpoint`, `AzureOpenAI:Key`, `AzureOpenAI:DeploymentName`
- **Status:** Code correctly implemented — not working at runtime due to a trivial configuration issue

---

### 6. Azure Service Bus

- **SDK:** `Azure.Messaging.ServiceBus` v7.20.1
- **Client:** `ServiceBusClient` + `ServiceBusSender`
- **Method:** `SendMessageAsync(new ServiceBusMessage(content))`
- **Purpose:** Sends event messages when invoices are processed (event-driven integration)
- **Config keys:** `AzureServiceBus:ConnectionString`, `AzureServiceBus:Cola`

---

## Dependency Injection Registration

All services and pages are registered in `MauiProgram.cs`:

```csharp
// Services — Singleton (shared, stateful)
builder.Services.AddSingleton<DocumentIntelligenceService>();
builder.Services.AddSingleton<IHistorialService, HistorialService>();
builder.Services.AddSingleton<ComputerVisionService>();
builder.Services.AddSingleton<SpeechTranslatorService>();
builder.Services.AddSingleton<ServiceBusService>();
builder.Services.AddSingleton<SpeechService>();
builder.Services.AddSingleton<OpenAIService>();

// ViewModels — Transient (new instance per page)
builder.Services.AddTransient<MainViewModel>();
builder.Services.AddTransient<HistorialViewModel>();
builder.Services.AddTransient<VisionViewModel>();
builder.Services.AddTransient<TraductorViewModel>();

// Pages — Transient
builder.Services.AddTransient<MainPage>();
builder.Services.AddTransient<HistorialPage>();
builder.Services.AddTransient<VisionPage>();
builder.Services.AddTransient<TraductorPage>();
builder.Services.AddTransient<VoiceSummaryPage>();
```

---

## Configuration

Azure credentials are loaded from an embedded `appsettings.json` file at startup:

```csharp
var assembly = Assembly.GetExecutingAssembly();
var resourceName = assembly.GetManifestResourceNames()
    .FirstOrDefault(r => r.EndsWith("appsettings.json"));
// ... loaded via ConfigurationBuilder().AddJsonStream(stream)
```

See [README.md](./README.md#configuration) for the full `appsettings.json` schema.

---

## Technology Stack Summary

| Technology | Version | Role |
|---|---|---|
| .NET MAUI | .NET 9 | Cross-platform UI framework |
| C# | 13 | Primary language |
| CommunityToolkit.Mvvm | 8.2.2 | MVVM Source Generators |
| Camera.MAUI | 1.5.1 | Camera preview & capture |
| Azure.AI.FormRecognizer | 4.1.0 | Invoice OCR |
| Azure.AI.Vision.ImageAnalysis | 1.0.0 | Computer vision |
| Microsoft.CognitiveServices.Speech | 1.48.2 | Speech-to-text |
| Azure.Messaging.ServiceBus | 7.20.1 | Message queuing |
| Target Platform | Android API 21+ | Android 5.0+ |
