# Azure AI Invoice Scanner — Arquitectura y Servicios de Azure

> Ver también: [README.md](./README.md) para la documentación completa del proyecto, configuración y problemas conocidos.

---

## Arquitectura General

La aplicación está construida sobre **.NET MAUI** (Android API 21+) utilizando el patrón **MVVM**.
Todas las integraciones con Azure están encapsuladas en clases de **Servicio** dedicadas, inyectadas mediante el contenedor de DI nativo de .NET configurado en `MauiProgram.cs`.

### Diagrama de Arquitectura de Alto Nivel

```
+----------------------------------------------------------+
|                  Aplicación .NET MAUI                    |
|                   (Android, API 21+)                     |
|                                                          |
|  +-------------+    +--------------+    +--------------+ |
|  |    Vistas   |--->|  ViewModels  |--->|   Servicios  | |
|  | (XAML + CS) |    | (Lógica MVVM)|    | (SDKs Azure) | |
|  +-------------+    +--------------+    +------+-------+ |
|                                                |         |
|  Navegación: AppShell TabBar                  |         |
|  DI: MauiProgram.cs (Singleton/Transient)     |         |
+------------------------------------------------+---------+
                                                 |
                    +----------------------------v---------------------------+
                    |                 Servicios de Azure Cloud               |
                    |                                                       |
                    |  +--------------------------------------------------+|
                    |  | Azure Document Intelligence (Form Recognizer)     ||
                    |  |   Modelo: prebuilt-invoice                        ||
                    |  |   SDK: Azure.AI.FormRecognizer 4.1.0              ||
                    |  +--------------------------------------------------+|
                    |  +--------------------------------------------------+|
                    |  | Azure AI Vision (Image Analysis)                  ||
                    |  |   Funciones: Objects, Tags, Caption               ||
                    |  |   SDK: Azure.AI.Vision.ImageAnalysis 1.0.0        ||
                    |  +--------------------------------------------------+|
                    |  +--------------------------------------------------+|
                    |  | Azure Speech Service (Speech-to-Text)             ||
                    |  |   Idioma: es-ES (configurable)                    ||
                    |  |   SDK: Microsoft.CognitiveServices.Speech 1.48.2  ||
                    |  +--------------------------------------------------+|
                    |  +--------------------------------------------------+|
                    |  | Azure Translator (REST API)                       ||
                    |  |   Endpoint: api.cognitive.microsofttranslator.com ||
                    |  |   Versión API: 3.0                                ||
                    |  +--------------------------------------------------+|
                    |  +--------------------------------------------------+|
                    |  | Azure OpenAI (Chat Completions)                   ||
                    |  |   API: /openai/deployments/{name}/chat/completions||
                    |  |   Versión API: 2024-02-01                         ||
                    |  +--------------------------------------------------+|
                    |  +--------------------------------------------------+|
                    |  | Azure Service Bus (Cola)                          ||
                    |  |   SDK: Azure.Messaging.ServiceBus 7.20.1          ||
                    |  +--------------------------------------------------+|
                    +-------------------------------------------------------+
```

---

## Capas de la Aplicación

### Vistas (Capa de Interfaz)

Páginas basadas en XAML, cada una con su archivo de código subyacente `.cs`. La navegación es gestionada por `AppShell.xaml` mediante un `TabBar`.

| Vista | Descripción |
|---|---|
| `MainPage` | Captura de fotos de facturas y visualización de resultados OCR |
| `HistorialPage` | Historial de la sesión de todas las facturas analizadas |
| `VisionPage` | Detección de objetos en imágenes y subtitulado |
| `TraductorPage` | Entrada de voz + traducción de voz en tiempo real |
| `VoiceSummaryPage` | Grabación de audio + resumen generado por IA |

### ViewModels (Capa de Lógica de Negocio)

Implementan `ObservableObject` de `CommunityToolkit.Mvvm`. Utilizan los generadores de código para `[RelayCommand]` y `[ObservableProperty]`.

| ViewModel | Vista Vinculada | Servicios Utilizados |
|---|---|---|
| `MainViewModel` | `MainPage` | `DocumentIntelligenceService`, `IHistorialService` |
| `HistorialViewModel` | `HistorialPage` | `IHistorialService` |
| `VisionViewModel` | `VisionPage` | `ComputerVisionService` |
| `TraductorViewModel` | `TraductorPage` | `SpeechTranslatorService` |

### Servicios (Capa de Integración con Azure)

Todos los servicios se registran como **Singletons** en `MauiProgram.cs`.

| Clase de Servicio | Servicio de Azure | SDK / API |
|---|---|---|
| `DocumentIntelligenceService` | Azure Document Intelligence | `Azure.AI.FormRecognizer` 4.1.0 |
| `ComputerVisionService` | Azure AI Vision | `Azure.AI.Vision.ImageAnalysis` 1.0.0 |
| `SpeechTranslatorService` | Azure Speech + Azure Translator | `Microsoft.CognitiveServices.Speech` + REST |
| `SpeachService` | Azure Speech Service | `Microsoft.CognitiveServices.Speech` 1.48.2 |
| `OpenAIService` | Azure OpenAI | API REST (HttpClient) |
| `ServiceBusService` | Azure Service Bus | `Azure.Messaging.ServiceBus` 7.20.1 |
| `HistorialService` | N/A (en memoria) | Colecciones nativas de C# |

### Modelos (Capa de Datos)

| Modelo | Propósito |
|---|---|
| `ResultadoOCR` | Almacena los campos extraídos de una factura por Document Intelligence |
| `ResultadoVision` | Almacena descripción, objetos detectados y etiquetas de Azure AI Vision |

---

## Servicios de Azure — Referencia Detallada

### 1. Azure Document Intelligence

- **SDK:** `Azure.AI.FormRecognizer` v4.1.0
- **Cliente:** `DocumentAnalysisClient`
- **Modelo:** `prebuilt-invoice` (factura preconfigurada)
- **Entrada:** Flujo de imagen (foto de cámara o galería)
- **Campos extraídos:**

| Campo | Descripción |
|---|---|
| `VendorName` | Nombre del proveedor |
| `CustomerName` | Nombre del cliente |
| `InvoiceId` | Número de factura |
| `InvoiceDate` | Fecha de emisión |
| `InvoiceTotal` | Importe total |
| `SubTotal` | Base imponible |
| `TotalTax` | Importe de impuestos |
| `Items` | Líneas de detalle: descripción + importe |

---

### 2. Azure AI Vision (Visión Artificial)

- **SDK:** `Azure.AI.Vision.ImageAnalysis` v1.0.0
- **Cliente:** `ImageAnalysisClient`
- **Funciones:** `VisualFeatures.Objects | VisualFeatures.Tags | VisualFeatures.Caption`
- **Salida:**
  - Subtítulo generado automáticamente (descripción)
  - Objetos detectados con nombre + nivel de confianza
  - Etiquetas de imagen con porcentajes de confianza
- **Estado:** Funcional — se han notado inconsistencias menores en la detección

---

### 3. Azure Speech Service

Utilizado por dos clases de servicio diferentes:

**`SpeachService`** (grabación de voz y STT):
- **SDK:** `Microsoft.CognitiveServices.Speech` v1.48.2
- **Cliente:** `SpeechRecognizer` con entrada de micrófono predeterminada
- **Idioma:** `es-ES`
- **Método:** `RecognizeOnceAsync()`

**`SpeechTranslatorService`** (paso de STT en el pipeline de traducción):
- Mismo SDK y tipo de cliente
- Ampliado con propiedades de tiempo de espera de silencio inicial/final
- Envía la transcripción a Azure Translator

---

### 4. Azure Translator

- **Tipo API:** REST (vía `HttpClient`)
- **Endpoint:** `https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&to={targetLanguage}`
- **Uso:** Recibe texto transcrito de Azure Speech y devuelve el texto traducido

---

### 5. Azure OpenAI

- **Tipo API:** REST (vía `HttpClient`)
- **Versión API:** 2024-02-01
- **Prompt de sistema:** Instruye al modelo para generar un resumen estructurado en puntos a partir de una transcripción de voz
- **Estado:** Código implementado correctamente — no funciona en ejecución por un problema trivial de configuración

---

### 6. Azure Service Bus

- **SDK:** `Azure.Messaging.ServiceBus` v7.20.1
- **Cliente:** `ServiceBusClient` + `ServiceBusSender`
- **Propósito:** Envía mensajes de eventos cuando se procesan facturas (integración dirigida por eventos)

---

## Registro de Inyección de Dependencias

Configurado en `MauiProgram.cs`:

```csharp
// Servicios — Singleton (compartidos, con estado)
builder.Services.AddSingleton<DocumentIntelligenceService>();
builder.Services.AddSingleton<IHistorialService, HistorialService>();
builder.Services.AddSingleton<ComputerVisionService>();
builder.Services.AddSingleton<SpeechTranslatorService>();
builder.Services.AddSingleton<ServiceBusService>();
builder.Services.AddSingleton<SpeechService>();
builder.Services.AddSingleton<OpenAIService>();

// ViewModels — Transient (nueva instancia por página)
builder.Services.AddTransient<MainViewModel>();
builder.Services.AddTransient<HistorialViewModel>();
builder.Services.AddTransient<VisionViewModel>();
builder.Services.AddTransient<TraductorViewModel>();

// Páginas — Transient
builder.Services.AddTransient<MainPage>();
builder.Services.AddTransient<HistorialPage>();
builder.Services.AddTransient<VisionPage>();
builder.Services.AddTransient<TraductorPage>();
builder.Services.AddTransient<VoiceSummaryPage>();
```

---

## Configuración

Las credenciales de Azure se cargan desde un archivo `appsettings.json` embebido al iniciar:

```csharp
var assembly = Assembly.GetExecutingAssembly();
var resourceName = assembly.GetManifestResourceNames()
    .FirstOrDefault(r => r.EndsWith("appsettings.json"));
```

Consulte el [README.md](./README.md#configuration) para el esquema completo de `appsettings.json`.
